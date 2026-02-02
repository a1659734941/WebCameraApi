using HikHCNetSDK;
using Microsoft.Extensions.Logging; // 补充正确的ILogger命名空间（原代码可能遗漏）
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WebCameraApi.Services;

namespace HikAcessControl
{
    public class HikAC
    {
        private readonly ILogger<HikAcService> _logger;
        private CHCNetSDK.NET_DVR_USER_LOGIN_INFO pLoginInfo;

        // 设备信息结构体
        private CHCNetSDK.NET_DVR_DEVICEINFO_V40 lpDeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();

        private int lUserID = -1;
        public Int32 m_lGetUserCfgHandle = -1;
        public Int32 m_lSetUserCfgHandle = -1;
        public Int32 m_lDelUserCfgHandle = -1;
        private static readonly CHCNetSDK.MSGCallBack_V31 FaceAlarmCallback = OnFaceAlarmCallback;
        private static readonly CHCNetSDK.RemoteConfigCallback CardRemoteConfigCallback = OnCardRemoteConfigCallback;

        public class FaceCaptureResult
        {
            public byte[]? FaceBytes { get; set; }
            public string? SavedFilePath { get; set; }
            public string? SavedFileName { get; set; }
        }

        public string? LastFaceCaptureErrorMessage { get; private set; }

        // 构造函数仅保留日志依赖，移除hikAcDto相关逻辑
        public HikAC(ILogger<HikAcService> logger)
        {
            _logger = logger;
            // 仅初始化结构体缓冲区，不赋值业务参数（参数改为外部传入）
            pLoginInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO
            {
                sDeviceAddress = new byte[129],  // 设备地址缓冲区（SDK定义129字节）
                sUserName = new byte[64],       // 用户名缓冲区（64字节）
                sPassword = new byte[64],       // 密码缓冲区（64字节）
                byLoginMode = 0,                // 私有协议登录（默认）
                byProxyType = 0                 // 不使用代理
            };
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (CHCNetSDK.NET_DVR_Init())
            {
                _logger.LogInformation("海康SDK初始化成功");
            }
            else
            {
                _logger.LogError("海康SDK初始化失败");
            }
        }

        /// <summary>
        /// 登录海康门禁设备（参数改为外部传入）
        /// </summary>
        /// <param name="deviceIp">设备IP地址</param>
        /// <param name="devicePort">设备端口号</param>
        /// <param name="userName">登录用户名</param>
        /// <param name="password">登录密码</param>
        /// <returns>登录是否成功</returns>
        public bool LoginAC(string deviceIp, ushort devicePort, string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(deviceIp))
            {
                _logger.LogError("设备IP不能为空");
                return false;
            }
            if (string.IsNullOrWhiteSpace(userName))
            {
                _logger.LogError("登录用户名不能为空");
                return false;
            }

            // 填充端口（改为使用外部传入的参数）
            pLoginInfo.wPort = devicePort;

            // 填充IP（GBK编码，海康SDK默认）
            byte[] ipBytes = Encoding.GetEncoding("GBK").GetBytes(deviceIp);
            Array.Copy(ipBytes, 0, pLoginInfo.sDeviceAddress, 0, Math.Min(ipBytes.Length, pLoginInfo.sDeviceAddress.Length));

            // 填充用户名（改为外部传入）
            byte[] userNameBytes = Encoding.GetEncoding("GBK").GetBytes(userName);
            Array.Copy(userNameBytes, 0, pLoginInfo.sUserName, 0, Math.Min(userNameBytes.Length, pLoginInfo.sUserName.Length));

            // 填充密码（改为外部传入）
            byte[] passwordBytes = Encoding.GetEncoding("GBK").GetBytes(password ?? string.Empty);
            Array.Copy(passwordBytes, 0, pLoginInfo.sPassword, 0, Math.Min(passwordBytes.Length, pLoginInfo.sPassword.Length));

            // 执行登录
            _logger.LogInformation($"开始登录设备 : {deviceIp}" +
                $", 用户名 : {userName}, 密码 : {password}" +
                $", 端口 : {devicePort}");
            lUserID = CHCNetSDK.NET_DVR_Login_V40(ref pLoginInfo, ref lpDeviceInfo);
            _logger.LogInformation($"设备登录结果 : {lUserID}");
            if (lUserID < 0)
            {
                _logger.LogError($"设备 : {deviceIp} 登录失败，错误码：{CHCNetSDK.NET_DVR_GetLastError()}");
                return false;
            }
            _logger.LogInformation($"设备 : {deviceIp} 登录成功");
            return true;
        }

        /// <summary>
        /// 开门操作（修复原代码未返回值的漏洞）
        /// </summary>
        /// <returns>开门是否成功</returns>
        public bool OpenGetway()
        {
            if (lUserID < 0)
            {
                _logger.LogWarning("请先登录设备！");
                return false; // 原代码遗漏return，补充返回false
            }
            bool isGateOpen = CHCNetSDK.NET_DVR_ControlGateway(lUserID, 1, 1);
            if (isGateOpen)
            {
                _logger.LogInformation("开门操作执行成功");
            }
            else
            {
                _logger.LogError($"开门失败，错误码：{CHCNetSDK.NET_DVR_GetLastError()}");
            }
            return isGateOpen;
        }

        public bool Logout()
        {
            if (lUserID < 0)
            {
                return true;
            }

            bool result = CHCNetSDK.NET_DVR_Logout_V30(lUserID);
            lUserID = -1;
            return result;
        }

        public int StartJsonRemoteConfig(string requestUrl, byte requestType, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (lUserID < 0)
            {
                errorMessage = "未登录设备";
                return -1;
            }

            IntPtr urlPtr = IntPtr.Zero;
            try
            {
                // 根据海康官方文档，NET_DVR_JSON_CONFIG命令的lpInBuffer应该直接是URL字符串指针
                // cbStateCallback应该设置为NULL
                byte[] urlBytes = Encoding.UTF8.GetBytes(requestUrl);
                urlPtr = Marshal.AllocHGlobal(urlBytes.Length + 1);
                Marshal.Copy(urlBytes, 0, urlPtr, urlBytes.Length);
                Marshal.WriteByte(urlPtr, urlBytes.Length, 0); // null结尾

                int handle = CHCNetSDK.NET_DVR_StartRemoteConfig(
                    lUserID,
                    CHCNetSDK.NET_DVR_JSON_CONFIG,
                    urlPtr,                    // 直接传URL字符串指针
                    urlBytes.Length,           // URL字符串长度
                    null!,                     // 回调设置为NULL
                    IntPtr.Zero);

                if (handle < 0)
                {
                    uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                    errorMessage = $"StartRemoteConfig失败，错误码：{errCode}，lUserID={lUserID}，URL长度={urlBytes.Length}";
                    return -1;
                }
                return handle;
            }
            catch (Exception ex)
            {
                errorMessage = $"StartRemoteConfig异常：{ex.Message}";
                return -1;
            }
            finally
            {
                if (urlPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(urlPtr);
                }
            }
        }

        public int SendWithRecvJsonRemoteConfig(int handle, string inputJson, out string outputJson, out string errorMessage)
        {
            outputJson = string.Empty;
            errorMessage = string.Empty;
            if (handle < 0)
            {
                errorMessage = "远程配置句柄无效";
                return -1;
            }

            IntPtr inStructPtr = IntPtr.Zero;
            IntPtr outBufferPtr = IntPtr.Zero;
            GCHandle inJsonHandle = default;
            try
            {
                int structSize = Marshal.SizeOf<CHCNetSDK.NET_DVR_JSON_DATA_CFG>();
                _logger.LogInformation($"NET_DVR_JSON_DATA_CFG结构体大小：{structSize}字节，IntPtr大小：{IntPtr.Size}字节，Is64Bit：{Environment.Is64BitProcess}");
                _logger.LogInformation($"输入JSON：{inputJson}");

                byte[] inJsonBytes = Encoding.UTF8.GetBytes(inputJson);
                inJsonHandle = GCHandle.Alloc(inJsonBytes, GCHandleType.Pinned);
                var inCfg = new CHCNetSDK.NET_DVR_JSON_DATA_CFG
                {
                    dwSize = (uint)structSize,
                    lpJsonData = inJsonHandle.AddrOfPinnedObject(),
                    dwJsonDataSize = (uint)inJsonBytes.Length,
                    lpPicData = IntPtr.Zero,
                    dwPicDataSize = 0,
                    dwInfraredFacePicSize = 0,
                    lpInfraredFacePicBuffer = IntPtr.Zero,
                    byRes = new byte[248]
                };

                _logger.LogInformation($"输入结构体：dwSize={inCfg.dwSize}，lpJsonData={inCfg.lpJsonData}，dwJsonDataSize={inCfg.dwJsonDataSize}");

                inStructPtr = Marshal.AllocHGlobal(structSize);
                Marshal.StructureToPtr(inCfg, inStructPtr, false);

                // 关键修复：输出参数直接使用字节缓冲区，而不是结构体！
                // 海康SDK的NET_DVR_SendWithRecvRemoteConfig对于NET_DVR_JSON_CONFIG命令，
                // 输出参数是直接的数据缓冲区，不是NET_DVR_JSON_DATA_CFG结构体
                const int outBufferSize = 64 * 1024;
                outBufferPtr = Marshal.AllocHGlobal(outBufferSize);
                // 清零输出缓冲区
                for (int i = 0; i < Math.Min(outBufferSize, 1024); i++)
                {
                    Marshal.WriteByte(outBufferPtr, i, 0);
                }

                uint outDataLen = 0;
                int status = CHCNetSDK.NET_DVR_SendWithRecvRemoteConfig(
                    handle,
                    inStructPtr,
                    (uint)structSize,
                    outBufferPtr,
                    (uint)outBufferSize,
                    ref outDataLen);

                uint lastError = CHCNetSDK.NET_DVR_GetLastError();
                _logger.LogInformation($"SendWithRecvRemoteConfig调用完成，status={status}，outDataLen={outDataLen}，错误码={lastError}");
                
                if (status < 0)
                {
                    errorMessage = $"SendWithRecvRemoteConfig失败，错误码：{lastError}";
                    return status;
                }

                // 从输出缓冲区直接读取数据
                if (outDataLen > 0 && outDataLen <= outBufferSize)
                {
                    byte[] outBytes = new byte[outDataLen];
                    Marshal.Copy(outBufferPtr, outBytes, 0, (int)outDataLen);
                    outputJson = Encoding.UTF8.GetString(outBytes).Trim('\0', '\r', '\n', ' ');
                    _logger.LogInformation($"从输出缓冲区读取到{outDataLen}字节数据：[{outputJson}]");
                }
                else
                {
                    // 尝试扫描缓冲区找到数据
                    byte[] scanBuffer = new byte[Math.Min(outBufferSize, 4096)];
                    Marshal.Copy(outBufferPtr, scanBuffer, 0, scanBuffer.Length);
                    int dataEnd = Array.IndexOf(scanBuffer, (byte)0);
                    if (dataEnd > 0)
                    {
                        outputJson = Encoding.UTF8.GetString(scanBuffer, 0, dataEnd).Trim('\0', '\r', '\n', ' ');
                        _logger.LogInformation($"扫描缓冲区找到{dataEnd}字节数据：[{outputJson}]");
                    }
                    else
                    {
                        _logger.LogWarning($"输出缓冲区为空或无法解析，outDataLen={outDataLen}");
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                errorMessage = $"SendWithRecvRemoteConfig异常：{ex.Message}";
                _logger.LogError(ex, "SendWithRecvRemoteConfig异常");
                return -1;
            }
            finally
            {
                if (inStructPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inStructPtr);
                }
                if (outBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outBufferPtr);
                }
                if (inJsonHandle.IsAllocated)
                {
                    inJsonHandle.Free();
                }
            }
        }

        public void StopRemoteConfig(int handle)
        {
            if (handle >= 0)
            {
                CHCNetSDK.NET_DVR_StopRemoteConfig(handle);
            }
        }

        /// <summary>
        /// 仅查询删除进度（假定删除命令已通过 HTTP 或其他方式下发）。建立长连接循环 GetNextRemoteConfig 直到进度 success 或结束。
        /// </summary>
        /// <param name="progressTimeoutMs">超时时间（毫秒）</param>
        /// <param name="errorMessage">失败时的错误信息</param>
        /// <returns>是否删除完成（进度为 success）</returns>
        public bool PollDeleteProgress(int progressTimeoutMs, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (lUserID < 0)
            {
                errorMessage = "未登录设备";
                return false;
            }

            string processUrl = "GET /ISAPI/AccessControl/UserInfoDetail/DeleteProcess?format=json";
            int handle = StartJsonRemoteConfig(processUrl, 1, out string startErr);
            if (handle < 0)
            {
                errorMessage = $"建立查询删除进度长连接失败：{startErr}";
                _logger.LogWarning(errorMessage);
                return false;
            }

            IntPtr outBufferPtr = IntPtr.Zero;
            try
            {
                const int outBufferSize = 4096;
                outBufferPtr = Marshal.AllocHGlobal(outBufferSize);
                var sw = Stopwatch.StartNew();
                bool deleteSuccess = false;

                // 3. 循环调用 NET_DVR_GetNextRemoteConfig 逐条获取删除进度（UserInfoDetailDeleteProcess JSON）
                while (sw.ElapsedMilliseconds < progressTimeoutMs)
                {
                    for (int i = 0; i < outBufferSize; i++)
                    {
                        Marshal.WriteByte(outBufferPtr, i, 0);
                    }
                    int status = CHCNetSDK.NET_DVR_GetNextRemoteConfig(handle, outBufferPtr, outBufferSize);

                    if (status == -1)
                    {
                        uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                        errorMessage = $"获取删除进度失败，错误码：{errCode}";
                        _logger.LogWarning(errorMessage);
                        return false;
                    }

                    // 从缓冲区读取 JSON
                    byte[] outBytes = new byte[outBufferSize];
                    Marshal.Copy(outBufferPtr, outBytes, 0, outBufferSize);
                    int dataEnd = Array.IndexOf(outBytes, (byte)0);
                    if (dataEnd <= 0)
                    {
                        dataEnd = outBufferSize;
                    }
                    string responseJson = Encoding.UTF8.GetString(outBytes, 0, dataEnd).Trim('\0', '\r', '\n', ' ');
                    if (!string.IsNullOrWhiteSpace(responseJson))
                    {
                        _logger.LogInformation($"删除进度返回：status={status}，response={responseJson}");
                        deleteSuccess = IsDeleteProgressSuccess(responseJson);
                    }

                    if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_SUCCESS || status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_NEED_WAIT)
                    {
                        if (deleteSuccess)
                        {
                            _logger.LogInformation("删除人员进度为 success，删除完成");
                            return true;
                        }
                        Thread.Sleep(100);
                        continue;
                    }
                    if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_FINISH)
                    {
                        return deleteSuccess;
                    }
                    if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_FAILED)
                    {
                        errorMessage = string.IsNullOrWhiteSpace(responseJson) ? "设备返回删除失败" : responseJson;
                        return false;
                    }
                    Thread.Sleep(100);
                }

                errorMessage = "查询删除进度超时";
                return false;
            }
            finally
            {
                if (outBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outBufferPtr);
                }
                if (handle >= 0)
                {
                    StopRemoteConfig(handle);
                }
            }
        }

        /// <summary>
        /// 解析 UserInfoDetailDeleteProcess 报文，判断 progress 是否为 success
        /// </summary>
        private static bool IsDeleteProgressSuccess(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return false;
            }
            try
            {
                var node = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responseJson);
                var process = node?["UserInfoDetailDeleteProcess"];
                if (process == null)
                {
                    var status = node?["ResponseStatus"];
                    var statusStr = status?["statusString"]?.ToString();
                    return string.Equals(statusStr, "OK", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(statusStr, "success", StringComparison.OrdinalIgnoreCase);
                }
                // 设备可能返回 progress 或 status 表示完成
                var progress = process["progress"]?.ToString();
                var statusVal = process["status"]?.ToString();
                return string.Equals(progress, "success", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(statusVal, "success", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return responseJson.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private static string EscapeJsonString(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// 直接缓冲区方式发送和接收远程配置（参考官方C++示例）
        /// 输入输出都是直接的字节缓冲区，而不是结构体
        /// </summary>
        public int SendWithRecvRemoteConfigDirect(int handle, string inputJson, out string outputJson, out string errorMessage)
        {
            outputJson = string.Empty;
            errorMessage = string.Empty;
            if (handle < 0)
            {
                errorMessage = "远程配置句柄无效";
                return -1;
            }

            IntPtr inBufferPtr = IntPtr.Zero;
            IntPtr outBufferPtr = IntPtr.Zero;
            try
            {
                // 输入缓冲区：直接使用 JSON 字符串（参考官方示例）
                byte[] inJsonBytes = Encoding.UTF8.GetBytes(inputJson);
                // 分配足够大的缓冲区（官方示例用 1024）
                int inBufferSize = Math.Max(inJsonBytes.Length + 1, 1024);
                inBufferPtr = Marshal.AllocHGlobal(inBufferSize);
                // 清零缓冲区
                for (int i = 0; i < inBufferSize; i++)
                {
                    Marshal.WriteByte(inBufferPtr, i, 0);
                }
                // 复制 JSON 数据
                Marshal.Copy(inJsonBytes, 0, inBufferPtr, inJsonBytes.Length);
                
                _logger.LogInformation($"输入缓冲区大小：{inBufferSize}，JSON长度：{inJsonBytes.Length}");

                // 输出缓冲区（官方示例用 1024 * 4）
                const int outBufferSize = 1024 * 4;
                outBufferPtr = Marshal.AllocHGlobal(outBufferSize);
                // 清零输出缓冲区
                for (int i = 0; i < outBufferSize; i++)
                {
                    Marshal.WriteByte(outBufferPtr, i, 0);
                }

                uint outDataLen = 0;
                int status = CHCNetSDK.NET_DVR_SendWithRecvRemoteConfig(
                    handle,
                    inBufferPtr,
                    (uint)inBufferSize,
                    outBufferPtr,
                    (uint)outBufferSize,
                    ref outDataLen);

                uint lastError = CHCNetSDK.NET_DVR_GetLastError();
                _logger.LogInformation($"SendWithRecvRemoteConfig返回：status={status}，outDataLen={outDataLen}，错误码={lastError}");
                
                if (status == -1)
                {
                    errorMessage = $"接口调用失败，错误码：{lastError}";
                    return -1;
                }

                // 从输出缓冲区读取数据
                if (outDataLen > 0 && outDataLen <= outBufferSize)
                {
                    byte[] outBytes = new byte[outDataLen];
                    Marshal.Copy(outBufferPtr, outBytes, 0, (int)outDataLen);
                    outputJson = Encoding.UTF8.GetString(outBytes).Trim('\0', '\r', '\n', ' ');
                }
                else
                {
                    // 尝试扫描缓冲区找到数据
                    byte[] scanBuffer = new byte[outBufferSize];
                    Marshal.Copy(outBufferPtr, scanBuffer, 0, outBufferSize);
                    int dataEnd = Array.IndexOf(scanBuffer, (byte)0);
                    if (dataEnd > 0)
                    {
                        outputJson = Encoding.UTF8.GetString(scanBuffer, 0, dataEnd).Trim('\0', '\r', '\n', ' ');
                    }
                }

                _logger.LogInformation($"输出内容：{outputJson}");
                return status;
            }
            catch (Exception ex)
            {
                errorMessage = $"SendWithRecvRemoteConfigDirect异常：{ex.Message}";
                _logger.LogError(ex, "SendWithRecvRemoteConfigDirect异常");
                return -1;
            }
            finally
            {
                if (inBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inBufferPtr);
                }
                if (outBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outBufferPtr);
                }
            }
        }

        /// <summary>
        /// 下发人脸到设备（新增或修改）- 按官方文档实现
        /// </summary>
        /// <param name="employeeNo">人员工号FPID（用于关联人员）</param>
        /// <param name="name">人员姓名（可选）</param>
        /// <param name="faceImageBytes">人脸图片二进制数据</param>
        /// <param name="fdid">人脸库ID，默认为1</param>
        /// <returns>元组：(是否成功, 设备响应内容)</returns>
        public (bool success, string deviceResponse) AddFaceToDevice(string employeeNo, string name, byte[] faceImageBytes, string fdid = "1")
        {
            // 1. 基础校验（新增FDID数字校验，修复核心参数错误）
            if (lUserID < 0)
            {
                return (false, "未登录设备");
            }
            if (string.IsNullOrWhiteSpace(employeeNo))
            {
                return (false, "人员工号不能为空");
            }
            if (faceImageBytes == null || faceImageBytes.Length == 0)
            {
                return (false, "人脸图片数据不能为空");
            }
            if (!Regex.IsMatch(fdid ?? "", @"^\d+$"))
            {
                return (false, $"人脸库ID(FDID)必须为数字字符串，当前值：{fdid}");
            }

            int handle = -1;
            IntPtr urlPtr = IntPtr.Zero;
            IntPtr jsonPtr = IntPtr.Zero;
            IntPtr picPtr = IntPtr.Zero;
            IntPtr inStructPtr = IntPtr.Zero;

            try
            {
                // 2. 建立长连接 - 按官方：char sFaceURL[1024]，传 sizeof(sFaceURL)=1024
                const int urlBufferSize = 1024;
                string url = "PUT /ISAPI/Intelligent/FDLib/FDSetUp?format=json";
                byte[] urlBytes = Encoding.UTF8.GetBytes(url);
                urlPtr = Marshal.AllocHGlobal(urlBufferSize);
                for (int i = 0; i < urlBufferSize; i++) Marshal.WriteByte(urlPtr, i, 0);
                Marshal.Copy(urlBytes, 0, urlPtr, Math.Min(urlBytes.Length, urlBufferSize - 1));

                handle = CHCNetSDK.NET_DVR_StartRemoteConfig(
                    lUserID,
                    CHCNetSDK.NET_DVR_FACE_DATA_RECORD,
                    urlPtr,
                    urlBufferSize,
                    null!,
                    IntPtr.Zero);

                if (handle < 0)
                {
                    uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                    return (false, $"建立下发人脸参数长连接失败，错误码：{errCode}");
                }
                _logger.LogInformation("建立下发人脸参数长连接成功");

                // 3. 构建人脸JSON - 与官方一致：faceLibType、FDID、FPID、featurePointType
                var faceConfig = new
                {
                    faceLibType = "blackFD",
                    FDID = fdid,
                    FPID = employeeNo,
                    featurePointType = "face"
                };
                string jsonData = JsonConvert.SerializeObject(faceConfig);
                _logger.LogInformation($"下发人脸JSON：{jsonData}，图片大小：{faceImageBytes.Length}字节");

                // 4. 按官方：一个 struUserRecord，lpJsonData 指向 C 风格字符串（带 \0），dwJsonDataSize=strlen（不含 \0）
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
                int jsonLen = jsonBytes.Length;
                jsonPtr = Marshal.AllocHGlobal(jsonLen + 1);
                Marshal.Copy(jsonBytes, 0, jsonPtr, jsonLen);
                Marshal.WriteByte(jsonPtr, jsonLen, 0);

                picPtr = Marshal.AllocHGlobal(faceImageBytes.Length);
                Marshal.Copy(faceImageBytes, 0, picPtr, faceImageBytes.Length);

                CHCNetSDK.NET_DVR_JSON_DATA_CFG faceDataStruct = new CHCNetSDK.NET_DVR_JSON_DATA_CFG
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(CHCNetSDK.NET_DVR_JSON_DATA_CFG)),
                    lpJsonData = jsonPtr,
                    dwJsonDataSize = (uint)jsonLen,
                    lpPicData = picPtr,
                    dwPicDataSize = (uint)faceImageBytes.Length,
                    dwInfraredFacePicSize = 0,
                    lpInfraredFacePicBuffer = IntPtr.Zero,
                    byRes = new byte[248]
                };

                int structSize = Marshal.SizeOf(typeof(CHCNetSDK.NET_DVR_JSON_DATA_CFG));
                inStructPtr = Marshal.AllocHGlobal(structSize);
                Marshal.StructureToPtr(faceDataStruct, inStructPtr, false);

                // 5. 循环调用 - 与官方一致：每次传同一 &struUserRecord
                string responseJson = string.Empty;
                int status;
                while (true)
                {
                    status = SendFaceDataRemoteConfigWithStruct(handle, inStructPtr, structSize, out responseJson);
                    _logger.LogInformation($"人脸下发返回：status={status}，响应={responseJson}");

                    if (status == -1)
                    {
                        uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                        return (false, $"NET_DVR_SendWithRecvRemoteConfig接口调用失败，错误码：{errCode}");
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT)
                    {
                        _logger.LogInformation("配置等待...");
                        Thread.Sleep(10);
                        continue;
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FAILED)
                    {
                        return (false, $"下发人脸失败：{responseJson}");
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_EXCEPTION)
                    {
                        return (false, $"下发人脸异常：{responseJson}");
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS)
                    {
                        if (!IsResponseStatusOk(responseJson))
                        {
                            return (false, string.IsNullOrWhiteSpace(responseJson) ? "下发人脸失败" : responseJson);
                        }
                        return (true, string.IsNullOrWhiteSpace(responseJson) ? "下发人脸成功" : responseJson);
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                    {
                        if (!IsResponseStatusOk(responseJson))
                        {
                            return (false, string.IsNullOrWhiteSpace(responseJson) ? "下发人脸失败" : responseJson);
                        }
                        return (true, string.IsNullOrWhiteSpace(responseJson) ? "下发人脸完成" : responseJson);
                    }
                    else
                    {
                        return (false, $"下发人脸未知状态：{status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下发人脸业务逻辑异常");
                return (false, $"下发人脸异常：{ex.Message}");
            }
            finally
            {
                // 6. 释放所有资源（新增JSON/图片非托管内存，避免内存泄漏）
                if (handle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRemoteConfig(handle);
                    _logger.LogInformation("已关闭人脸下发长连接");
                }
                if (urlPtr != IntPtr.Zero) Marshal.FreeHGlobal(urlPtr);
                if (inStructPtr != IntPtr.Zero) Marshal.FreeHGlobal(inStructPtr);
                if (jsonPtr != IntPtr.Zero) Marshal.FreeHGlobal(jsonPtr);
                if (picPtr != IntPtr.Zero) Marshal.FreeHGlobal(picPtr);
                _logger.LogInformation("已释放所有非托管内存，避免内存泄漏");
            }
        }

        /// <summary>
        /// 按官方：传入已组好的 NET_DVR_JSON_DATA_CFG 指针，只做 SendWithRecv 与读响应
        /// </summary>
        private int SendFaceDataRemoteConfigWithStruct(int handle, IntPtr inStructPtr, int structSize, out string responseJson)
        {
            responseJson = string.Empty;
            IntPtr outBufferPtr = IntPtr.Zero;
            try
            {
                const int outBufferSize = 1024 * 8;
                outBufferPtr = Marshal.AllocHGlobal(outBufferSize);
                for (int i = 0; i < outBufferSize; i++)
                    Marshal.WriteByte(outBufferPtr, i, 0);

                uint outDataLen = 0;
                int status = CHCNetSDK.NET_DVR_SendWithRecvRemoteConfig(
                    handle,
                    inStructPtr,
                    (uint)structSize,
                    outBufferPtr,
                    (uint)outBufferSize,
                    ref outDataLen);

                if (outDataLen > 0 && outDataLen <= outBufferSize)
                {
                    byte[] outBytes = new byte[outDataLen];
                    Marshal.Copy(outBufferPtr, outBytes, 0, (int)outDataLen);
                    responseJson = Encoding.UTF8.GetString(outBytes).Trim('\0', '\r', '\n', ' ');
                }
                else
                {
                    byte[] scanBuffer = new byte[outBufferSize];
                    Marshal.Copy(outBufferPtr, scanBuffer, 0, outBufferSize);
                    int dataEnd = Array.IndexOf(scanBuffer, (byte)0);
                    if (dataEnd > 0)
                        responseJson = Encoding.UTF8.GetString(scanBuffer, 0, dataEnd).Trim('\0', '\r', '\n', ' ');
                }
                return status;
            }
            finally
            {
                if (outBufferPtr != IntPtr.Zero) Marshal.FreeHGlobal(outBufferPtr);
            }
        }

        /// <summary>
        /// 检查ISAPI响应是否成功
        /// </summary>
        private bool IsResponseStatusOk(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return false;
            }
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("statusCode", out var statusCode))
                {
                    int code = statusCode.GetInt32();
                    return code == 1 || code == 0; // 1=成功，0=OK
                }
                if (doc.RootElement.TryGetProperty("statusString", out var statusString))
                {
                    string str = statusString.GetString() ?? "";
                    return str.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                           str.Equals("success", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                // 解析失败，尝试简单匹配
            }
            return responseJson.Contains("\"statusCode\":1") || 
                   responseJson.Contains("\"statusString\":\"OK\"", StringComparison.OrdinalIgnoreCase);
        }

        public bool StdXmlConfig(string requestUrl, string? inputXml, out string output, out string status, out string errorMessage)
        {
            output = string.Empty;
            status = string.Empty;
            errorMessage = string.Empty;
            if (lUserID < 0)
            {
                errorMessage = "未登录设备";
                return false;
            }

            IntPtr urlPtr = IntPtr.Zero;
            IntPtr inputPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            IntPtr statusPtr = IntPtr.Zero;
            try
            {
                // 海康 SDK 要求请求 URL 为 C 风格字符串（以 \0 结尾），否则可能返回错误码 17（参数错误）
                byte[] urlBytes = Encoding.ASCII.GetBytes(requestUrl);
                int urlBufLen = urlBytes.Length + 1;
                urlPtr = Marshal.AllocHGlobal(urlBufLen);
                Marshal.Copy(urlBytes, 0, urlPtr, urlBytes.Length);
                Marshal.WriteByte(urlPtr, urlBytes.Length, 0);

                byte[] inputBytes = Array.Empty<byte>();
                if (!string.IsNullOrWhiteSpace(inputXml))
                {
                    inputBytes = Encoding.UTF8.GetBytes(inputXml);
                    // 输入缓冲区也以 \0 结尾，避免参数错误
                    int inBufLen = inputBytes.Length + 1;
                    inputPtr = Marshal.AllocHGlobal(inBufLen);
                    Marshal.Copy(inputBytes, 0, inputPtr, inputBytes.Length);
                    Marshal.WriteByte(inputPtr, inputBytes.Length, 0);
                }

                byte[] outputBytes = new byte[64 * 1024];
                outputPtr = Marshal.AllocHGlobal(outputBytes.Length);

                byte[] statusBytes = new byte[8 * 1024];
                statusPtr = Marshal.AllocHGlobal(statusBytes.Length);

                // 文档：dwRequestUrlLen/dwInBufferSize 通常包含字符串结束符 \0，否则易报错误码 17
                uint urlLen = (uint)urlBufLen;
                uint inLen = inputPtr != IntPtr.Zero ? (uint)(inputBytes.Length + 1) : 0u;

                var input = new CHCNetSDK.NET_DVR_XML_CONFIG_INPUT
                {
                    dwSize = (uint)Marshal.SizeOf<CHCNetSDK.NET_DVR_XML_CONFIG_INPUT>(),
                    lpRequestUrl = urlPtr,
                    dwRequestUrlLen = urlLen,
                    lpInBuffer = inputPtr,
                    dwInBufferSize = inLen,
                    dwRecvTimeOut = 5000,
                    byForceEncrpt = 0,   // 0-否，不强制加密
                    byRes = new byte[31] // 保留，置为0
                };

                var outputCfg = new CHCNetSDK.NET_DVR_XML_CONFIG_OUTPUT
                {
                    dwSize = (uint)Marshal.SizeOf<CHCNetSDK.NET_DVR_XML_CONFIG_OUTPUT>(),
                    lpOutBuffer = outputPtr,
                    dwOutBufferSize = (uint)outputBytes.Length,
                    dwReturnedXMLSize = 0,
                    lpStatusBuffer = statusPtr,
                    dwStatusSize = (uint)statusBytes.Length,
                    lpDataBuffer = IntPtr.Zero,
                    byNumOfMultiPart = 0,
                    byRes = new byte[23] // 保留，置为0
                };

                // 使用 IntPtr 重载，避免 ref 结构体封送导致参数错误 17
                IntPtr inputStructPtr = IntPtr.Zero;
                IntPtr outputStructPtr = IntPtr.Zero;
                try
                {
                    inputStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CHCNetSDK.NET_DVR_XML_CONFIG_INPUT>());
                    outputStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CHCNetSDK.NET_DVR_XML_CONFIG_OUTPUT>());
                    Marshal.StructureToPtr(input, inputStructPtr, false);
                    Marshal.StructureToPtr(outputCfg, outputStructPtr, false);
                    bool result = CHCNetSDK.NET_DVR_STDXMLConfig(lUserID, inputStructPtr, outputStructPtr);
                    if (!result)
                    {
                        errorMessage = $"STDXMLConfig失败，错误码：{CHCNetSDK.NET_DVR_GetLastError()}";
                        return false;
                    }
                    outputCfg = Marshal.PtrToStructure<CHCNetSDK.NET_DVR_XML_CONFIG_OUTPUT>(outputStructPtr);
                }
                finally
                {
                    if (inputStructPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(inputStructPtr);
                    }
                    if (outputStructPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(outputStructPtr);
                    }
                }

                if (outputCfg.dwReturnedXMLSize > 0)
                {
                    byte[] outBytes = new byte[outputCfg.dwReturnedXMLSize];
                    Marshal.Copy(outputPtr, outBytes, 0, outBytes.Length);
                    output = Encoding.UTF8.GetString(outBytes).Trim('\0', '\r', '\n', ' ');
                }
                else
                {
                    Marshal.Copy(outputPtr, outputBytes, 0, outputBytes.Length);
                    output = DecodeBufferToString(outputBytes, (uint)outputBytes.Length);
                }

                if (outputCfg.dwStatusSize > 0)
                {
                    Marshal.Copy(statusPtr, statusBytes, 0, statusBytes.Length);
                    status = DecodeBufferToString(statusBytes, (uint)statusBytes.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"STDXMLConfig异常：{ex.Message}";
                return false;
            }
            finally
            {
                if (urlPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(urlPtr);
                }
                if (inputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inputPtr);
                }
                if (outputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outputPtr);
                }
                if (statusPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(statusPtr);
                }
            }
        }

        private static string DecodeBufferToString(byte[] buffer, uint length)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return string.Empty;
            }

            int actualLength = (int)Math.Min(buffer.Length, length);
            string text = Encoding.UTF8.GetString(buffer, 0, actualLength);
            return text.Trim('\0', '\r', '\n', ' ');
        }

        /// <summary>
        /// 与 NET_DVR_XML_CONFIG_INPUT 布局一致，供 NET_DVR_JSON_CONFIG 使用。
        /// 海康文档：建立长连接时回调置 NULL；URL 与 XML 透传一致（无 null 结尾，长度为字节数）。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NET_DVR_JSON_CONFIG_INPUT
        {
            public uint dwSize;
            public IntPtr lpRequestUrl;
            public uint dwRequestUrlLen;
            public IntPtr lpInBuffer;
            public uint dwInBufferSize;
            public uint dwRecvTimeOut;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] byRes;
        }

        /// <summary>
        /// 采集人脸图片（使用报警回调，识别到后直接保存本地）
        /// </summary>
        /// <param name="saveDirectory">保存目录（为空则仅返回字节）</param>
        /// <param name="maxWaitMs">最长等待时间</param>
        /// <returns>人脸图片结果，失败返回null</returns>
        public FaceCaptureResult? CaptureFaceImage(string? saveDirectory = null, int maxWaitMs = 10000)
        {
            LastFaceCaptureErrorMessage = string.Empty;
            if (lUserID < 0)
            {
                _logger.LogWarning("请先登录设备！");
                LastFaceCaptureErrorMessage = "未登录设备";
                return null;
            }

            _logger.LogInformation($"运行环境：Is64BitProcess={Environment.Is64BitProcess}, IntPtrSize={IntPtr.Size}");

            var (result, _) = TryCaptureFaceByRemoteConfig(maxWaitMs, saveDirectory);
            if (result == null && string.IsNullOrWhiteSpace(LastFaceCaptureErrorMessage))
            {
                LastFaceCaptureErrorMessage = "设备未返回人脸图片";
            }
            return result;
        }

        private sealed class FaceCaptureContext
        {
            public FaceCaptureContext(ILogger logger, string? saveDirectory)
            {
                Logger = logger;
                SaveDirectory = saveDirectory;
                Completed = new ManualResetEventSlim(false);
            }

            public ILogger Logger { get; }
            public string? SaveDirectory { get; }
            public ManualResetEventSlim Completed { get; }
            public FaceCaptureResult Result { get; } = new FaceCaptureResult();
            public int LastError { get; set; }
            public bool IsCompleted { get; set; }
        }

        private static bool OnFaceAlarmCallback(int lCommand, ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUserData)
        {
            if (pUserData == IntPtr.Zero)
            {
                return true;
            }

            var handle = GCHandle.FromIntPtr(pUserData);
            if (!handle.IsAllocated || handle.Target is not FaceCaptureContext context)
            {
                return true;
            }

            if (context.IsCompleted)
            {
                return true;
            }

            if (lCommand == CHCNetSDK.COMM_UPLOAD_FACESNAP_RESULT)
            {
                int size = Marshal.SizeOf<CHCNetSDK.NET_VCA_FACESNAP_RESULT>();
                if (dwBufLen >= (uint)size)
                {
                    var snap = Marshal.PtrToStructure<CHCNetSDK.NET_VCA_FACESNAP_RESULT>(pAlarmInfo);
                    if (snap.dwFacePicLen > 0 && snap.pBuffer1 != IntPtr.Zero)
                    {
                        byte[] faceBytes = new byte[snap.dwFacePicLen];
                        Marshal.Copy(snap.pBuffer1, faceBytes, 0, faceBytes.Length);
                        SaveCaptureResult(context, faceBytes);
                        return true;
                    }
                }
            }

            if (lCommand == CHCNetSDK.COMM_SNAP_MATCH_ALARM)
            {
                int size = Marshal.SizeOf<CHCNetSDK.NET_VCA_FACESNAP_MATCH_ALARM>();
                if (dwBufLen >= (uint)size)
                {
                    var match = Marshal.PtrToStructure<CHCNetSDK.NET_VCA_FACESNAP_MATCH_ALARM>(pAlarmInfo);
                    if (match.byContrastStatus == 1 && match.dwSnapPicLen > 0 && match.pSnapPicBuffer != IntPtr.Zero)
                    {
                        byte[] faceBytes = new byte[match.dwSnapPicLen];
                        Marshal.Copy(match.pSnapPicBuffer, faceBytes, 0, faceBytes.Length);
                        SaveCaptureResult(context, faceBytes);
                        return true;
                    }
                }
            }

            return true;
        }

        private static void SaveCaptureResult(FaceCaptureContext context, byte[] faceBytes)
        {
            context.Result.FaceBytes = faceBytes;
            if (!string.IsNullOrWhiteSpace(context.SaveDirectory))
            {
                Directory.CreateDirectory(context.SaveDirectory);
                string fileName = $"face_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.jpg";
                string filePath = Path.Combine(context.SaveDirectory, fileName);
                File.WriteAllBytes(filePath, faceBytes);
                context.Result.SavedFileName = fileName;
                context.Result.SavedFilePath = filePath;
            }

            context.IsCompleted = true;
            context.Completed.Set();
        }

        private (FaceCaptureResult? result, int lastError) TryCaptureFaceByRemoteConfig(int maxWaitMs, string? saveDirectory)
        {
            int lastError = 0;
            int handle = -1;
            IntPtr condPtr = IntPtr.Zero;
            IntPtr facePicPtr = IntPtr.Zero;
            IntPtr template1Ptr = IntPtr.Zero;
            IntPtr template2Ptr = IntPtr.Zero;
            IntPtr infraredPicPtr = IntPtr.Zero;
            IntPtr outPtr = IntPtr.Zero;

            try
            {
                var cond = new CHCNetSDK.NET_DVR_CAPTURE_FACE_COND();
                cond.init(); 
                cond.dwSize = Marshal.SizeOf<CHCNetSDK.NET_DVR_CAPTURE_FACE_COND>();
                condPtr = Marshal.AllocHGlobal(cond.dwSize);
                Marshal.StructureToPtr(cond, condPtr, false);

                handle = CHCNetSDK.NET_DVR_StartRemoteConfig(
                    lUserID,
                    CHCNetSDK.NET_DVR_CAPTURE_FACE_INFO,
                    condPtr,
                    cond.dwSize,
                    null!,
                    IntPtr.Zero);
                if (handle < 0)
                {
                    lastError = (int)CHCNetSDK.NET_DVR_GetLastError();
                    _logger.LogError($"人脸采集建立远程配置失败，错误码：{lastError}");
                    LastFaceCaptureErrorMessage = $"建立远程配置失败，错误码：{lastError}";
                    return (null, lastError);
                }
                CHCNetSDK.NET_DVR_CAPTURE_FACE_CFG RemoteGet = new CHCNetSDK.NET_DVR_CAPTURE_FACE_CFG();
                RemoteGet.init();
                const int facePicMaxSize = 200 * 1024;       // 人脸图片缓冲区大小（200K）
                facePicPtr = Marshal.AllocHGlobal(facePicMaxSize);
                RemoteGet.pFacePicBuffer = facePicPtr;               // 绑定人脸图片缓冲区
                RemoteGet.dwFacePicSize = facePicMaxSize;            // 告诉SDK：人脸图片缓冲区的最大大小
                RemoteGet.pFaceTemplate1Buffer = IntPtr.Zero;
                RemoteGet.dwFaceTemplate1Size = 0;
                RemoteGet.pFaceTemplate2Buffer = IntPtr.Zero;
                RemoteGet.dwFaceTemplate2Size = 0;
                RemoteGet.pInfraredFacePicBuffer = IntPtr.Zero;
                RemoteGet.dwInfraredFacePicSize = 0;
                int outSize = Marshal.SizeOf<CHCNetSDK.NET_DVR_CAPTURE_FACE_CFG>();
                RemoteGet.dwSize = outSize;
                _logger.LogInformation($"RemoteGet.dwSize: {RemoteGet.dwSize}，outSize: {outSize}");
                outPtr = Marshal.AllocHGlobal(outSize);
                Marshal.StructureToPtr(RemoteGet, outPtr, false);
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < maxWaitMs)
                {
                    int status = CHCNetSDK.NET_DVR_GetNextRemoteConfig(handle, outPtr, outSize);
                    if (status == -1)
                    {
                        lastError = (int)CHCNetSDK.NET_DVR_GetLastError();
                        _logger.LogError($"获取人脸采集结果失败，错误码：{lastError}");
                        LastFaceCaptureErrorMessage = $"获取人脸采集结果失败，错误码：{lastError}";
                        return (null, lastError);
                    }
                    RemoteGet = Marshal.PtrToStructure<CHCNetSDK.NET_DVR_CAPTURE_FACE_CFG>(outPtr);
                    if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_NEED_WAIT)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_FAILED)
                    {
                        lastError = (int)CHCNetSDK.NET_DVR_GetLastError();
                        _logger.LogError($"获取人脸采集结果失败，错误码：{lastError}");
                        LastFaceCaptureErrorMessage = $"获取人脸采集结果失败，错误码：{lastError}";
                        return (null, lastError);
                    }
                    if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_SUCCESS ||
                        status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_FINISH)
                    {
                        _logger.LogInformation($"获取人脸采集结果成功，进度：{RemoteGet.byCaptureProgress}");
                        if (RemoteGet.byCaptureProgress == 100 &&
                            RemoteGet.dwFacePicSize > 0 &&
                            RemoteGet.pFacePicBuffer != IntPtr.Zero)
                        {
                            byte[] faceBytes = new byte[RemoteGet.dwFacePicSize];
                            Marshal.Copy(RemoteGet.pFacePicBuffer, faceBytes, 0, faceBytes.Length);
                            var result = new FaceCaptureResult
                            {
                                FaceBytes = faceBytes
                            };
                            if (!string.IsNullOrWhiteSpace(saveDirectory))
                            {
                                Directory.CreateDirectory(saveDirectory);
                                string fileName = $"face_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.jpg";
                                string filePath = Path.Combine(saveDirectory, fileName);
                                File.WriteAllBytes(filePath, faceBytes);
                                _logger.LogInformation($"保存人脸图片成功，文件路径：{filePath}\n文件名：{fileName}");
                                result.SavedFileName = fileName;
                                result.SavedFilePath = filePath;
                            }
                            return (result, 0);
                        }
                        if (status == CHCNetSDK.NET_SDK_GET_NEXT_STATUS_FINISH)
                        {
                            LastFaceCaptureErrorMessage = "人脸采集已结束但未获取到图片";
                            return (null, lastError);
                        }
                    }

                    Thread.Sleep(100);
                }
                lastError = (int)CHCNetSDK.NET_DVR_GetLastError();
                LastFaceCaptureErrorMessage = "人脸采集超时或未获取到人脸图片";
                _logger.LogError($"人脸采集超时或未获取到人脸图片，错误码：{lastError}");
                return (null, lastError);
            }
            finally
            {
                if (handle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRemoteConfig(handle);
                }
                if (condPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(condPtr);
                }
                if (facePicPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(facePicPtr);
                }
                if (template1Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(template1Ptr);
                }
                if (template2Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(template2Ptr);
                }
                if (infraredPicPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(infraredPicPtr);
                }
                if (outPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outPtr);
                }
            }
        }

        private bool HasFaceCaptureAbility(out string? abilityPreview)
        {
            abilityPreview = null;
            if (lUserID < 0)
            {
                LastFaceCaptureErrorMessage = "未登录设备";
                return false;
            }

            const int abilityBufferSize = 128 * 1024;
            IntPtr outPtr = IntPtr.Zero;
            try
            {
                outPtr = Marshal.AllocHGlobal(abilityBufferSize);
                bool ok = CHCNetSDK.NET_DVR_GetDeviceAbility(
                    lUserID,
                    CHCNetSDK.DEVICE_ABILITY_INFO,
                    IntPtr.Zero,
                    0,
                    outPtr,
                    abilityBufferSize);
                if (!ok)
                {
                    int err = (int)CHCNetSDK.NET_DVR_GetLastError();
                    _logger.LogError($"获取设备能力失败，错误码：{err}");
                    LastFaceCaptureErrorMessage = $"获取设备能力失败，错误码：{err}";
                    return false;
                }

                var outBytes = new byte[abilityBufferSize];
                Marshal.Copy(outPtr, outBytes, 0, outBytes.Length);
                string textUtf8 = DecodeBufferToString(outBytes, (uint)outBytes.Length);
                string textGbk = DecodeBufferToString(outBytes, (uint)outBytes.Length, Encoding.GetEncoding("GBK"));
                string text = textUtf8.Contains('\uFFFD') && !string.IsNullOrWhiteSpace(textGbk)
                    ? textGbk
                    : textUtf8;

                if (text.Length > 512)
                {
                    abilityPreview = text.Substring(0, 512);
                }
                else
                {
                    abilityPreview = text;
                }

                return text.Contains("CaptureFaceData", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("captureFace", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("CAPTURE_FACE", StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (outPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outPtr);
                }
            }
        }

        private static string DecodeBufferToString(byte[] buffer, uint length, Encoding encoding)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return string.Empty;
            }

            int actualLength = (int)Math.Min(buffer.Length, length);
            string text = encoding.GetString(buffer, 0, actualLength);
            return text.Trim('\0', '\r', '\n', ' ');
        }

        /// <summary>
        /// 远程配置状态回调。查看最近一次回调内容：调试时读 <see cref="LastRemoteConfigCallbackInfo"/>，
        /// 或查看 Debug 输出（Visual Studio 输出窗口）。
        /// </summary>
        public static (uint DwType, uint DwBufLen, string? BufferPreview) LastRemoteConfigCallbackInfo { get; private set; }

        private static void OnCardRemoteConfigCallback(uint dwType, IntPtr lpBuffer, uint dwBufLen, IntPtr pUserData)
        {
            string? preview = null;
            if (lpBuffer != IntPtr.Zero && dwBufLen > 0)
            {
                int len = (int)Math.Min(dwBufLen, 256);
                var buf = new byte[len];
                Marshal.Copy(lpBuffer, buf, 0, len);
                try { preview = Encoding.UTF8.GetString(buf).TrimEnd('\0'); } catch { preview = Convert.ToHexString(buf); }
            }
            LastRemoteConfigCallbackInfo = (dwType, dwBufLen, preview);
            Debug.WriteLine($"[RemoteConfig] dwType={dwType}, dwBufLen={dwBufLen}, preview={preview ?? "(null)"}");
        }

        private (FaceCaptureResult? result, int lastError) TryCaptureFaceByAlarm(int maxWaitMs, string? saveDirectory)
        {
            int lastError = 0;
            FaceCaptureContext? context = null;
            GCHandle ctxHandle = default;
            int alarmHandle = -1;

            try
            {
                context = new FaceCaptureContext(_logger, saveDirectory);
                ctxHandle = GCHandle.Alloc(context);

                if (!CHCNetSDK.NET_DVR_SetDVRMessageCallBack_V31(FaceAlarmCallback, GCHandle.ToIntPtr(ctxHandle)))
                {
                    lastError = (int)CHCNetSDK.NET_DVR_GetLastError();
                    _logger.LogError($"设置报警回调失败，错误码：{lastError}");
                    return (null, lastError);
                }

                CHCNetSDK.NET_DVR_SETUPALARM_PARAM alarmParam = new CHCNetSDK.NET_DVR_SETUPALARM_PARAM
                {
                    dwSize = (uint)Marshal.SizeOf<CHCNetSDK.NET_DVR_SETUPALARM_PARAM>(),
                    byLevel = 1,
                    byRetAlarmTypeV40 = 1,
                    byRetDevInfoVersion = 1,
                    byFaceAlarmDetection = 1,
                    byDeployType = 1,
                    byAlarmTypeURL = 0
                };

                alarmHandle = CHCNetSDK.NET_DVR_SetupAlarmChan_V41(lUserID, ref alarmParam);
                if (alarmHandle < 0)
                {
                    lastError = (int)CHCNetSDK.NET_DVR_GetLastError();
                    _logger.LogError($"报警布防失败，错误码：{lastError}");
                    return (null, lastError);
                }
                _logger.LogInformation($"报警布防成功，handle={alarmHandle}");

                if (context.Completed.Wait(maxWaitMs))
                {
                    if (context.Result.FaceBytes != null && context.Result.FaceBytes.Length > 0)
                    {
                        return (context.Result, 0);
                    }
                    lastError = context.LastError;
                    return (null, lastError);
                }

                _logger.LogWarning("人脸采集超时或未获取到人脸图片");
                return (null, lastError);
            }
            finally
            {
                if (alarmHandle >= 0)
                {
                    CHCNetSDK.NET_DVR_CloseAlarmChan_V30(alarmHandle);
                }
                if (ctxHandle.IsAllocated)
                {
                    ctxHandle.Free();
                }
            }
        }
    }
}