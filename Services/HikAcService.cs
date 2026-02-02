using HikAcessControl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HikHCNetSDK;
using WebCameraApi.Dto;

namespace WebCameraApi.Services
{
    public class HikAcService
    {
        private readonly ILogger<HikAcService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public HikAcService(ILogger<HikAcService> logger, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _env = env;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(AcResponseDto response, StatusDto status)> OpenHikAccessGetway(HikAcConfigDto config)
        {
            var response = new AcResponseDto { AcName = config?.AcName ?? string.Empty };
            var status = new StatusDto { isSuccess = false }; // 默认设为失败，避免误判
            if (config == null)
            {
                _logger.LogError("门禁配置为空");
                return (response, status);
            }
            try
            {
                HikAC hikAc = new HikAC(_logger);
                bool loginResult = hikAc.LoginAC(config.HikAcIP, config.HikAcPort, config.HikAcUserName, config.HikAcPassword);
                if (loginResult)
                {
                    // 登录成功后开门
                    bool openResult = hikAc.OpenGetway();
                    if (openResult)
                    {
                        status.isSuccess = true;
                        status.message = $"门禁 : {config.AcName}开门成功!";
                        return (response, status);
                    }
                    else
                    {
                        status.message = $"门禁 : {config.AcName}开门失败!";
                        return (response, status);
                    }
                }
                else
                {
                    status.message = $"门禁 : {config.AcName}登录失败!";
                    return (response, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取门禁配置失败");
                return (response, status);
            }
        }

        public Task<(HikAcRecordFaceResponseDto response, StatusDto status)> RecordHikAcFace(HikAcRecordFaceRequestDto config)
        {
            var response = new HikAcRecordFaceResponseDto { AcName = config?.AcName ?? string.Empty };
            var status = new StatusDto { isSuccess = false };
            if (config == null)
            {
                _logger.LogError("人脸录制参数为空");
                return Task.FromResult((response, status));
            }

            try
            {
                string webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
                    : _env.WebRootPath;
                string recordDir = Path.Combine(webRoot, "RecordFaceImage");
                Directory.CreateDirectory(recordDir);

                HikAC hikAc = new HikAC(_logger);
                try
                {
                    if (!hikAc.LoginAC(config.HikAcIP, config.HikAcPort, config.HikAcUserName, config.HikAcPassword))
                    {
                        status.message = $"门禁 : {config.AcName}登录失败!";
                        return Task.FromResult((response, status));
                    }
            
                    var capture = hikAc.CaptureFaceImage(recordDir, 10000);
                    if (capture?.FaceBytes == null || capture.FaceBytes.Length == 0)
                    {
                        string detail = string.IsNullOrWhiteSpace(hikAc.LastFaceCaptureErrorMessage)
                            ? "设备未返回人脸图片"
                            : hikAc.LastFaceCaptureErrorMessage!;
                        status.message = $"门禁 : {config.AcName}人脸录制失败! {detail}";
                        return Task.FromResult((response, status));
                    }

                    string fileName = string.IsNullOrWhiteSpace(capture.SavedFileName)
                        ? $"FaceData_{DateTime.Now:yyyyMMddHHmmss}.jpg"
                        : capture.SavedFileName;

                    if (string.IsNullOrWhiteSpace(capture.SavedFilePath) || !File.Exists(capture.SavedFilePath))
                    {
                        string filePath = Path.Combine(recordDir, fileName);
                        File.WriteAllBytes(filePath, capture.FaceBytes);
                    }

                    response.FaceImageBase64 = Convert.ToBase64String(capture.FaceBytes);
                    response.FaceImageFileName = fileName;
                    response.FaceImageRelativePath = Path.Combine("RecordFaceImage", fileName).Replace("\\", "/");

                    status.isSuccess = true;
                    status.message = $"门禁 : {config.AcName}人脸录制成功!";
                    return Task.FromResult((response, status));
                }
                finally
                {
                    hikAc.Logout();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "人脸录制失败");
                return Task.FromResult((response, status));
            }
        }

        public async Task<(HikAcUserAddResponseDto response, StatusDto status)> AddHikAcUser(HikAcUserAddRequestDto request)
        {
            var response = new HikAcUserAddResponseDto
            {
                UserID = request?.UserID ?? string.Empty,
                UserName = request?.UserName ?? string.Empty
            };
            var status = new StatusDto { isSuccess = false };
            if (request == null)
            {
                status.message = "下发人员参数为空";
                return (response, status);
            }

            try
            {
                foreach (var device in request.Devices ?? new List<HikAcDeviceDto>())
                {
                    var deviceResult = await AddUserToDevice(device, request);
                    response.Results.Add(deviceResult);
                }

                if (response.Results.Count == 0)
                {
                    status.message = "下发人员失败：未提供门禁设备";
                    return (response, status);
                }

                status.isSuccess = response.Results.All(item => item.IsSuccess);
                status.message = status.isSuccess ? "下发人员成功" : "下发人员完成（部分失败）";
                return (response, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下发人员信息失败");
                status.message = $"下发人员异常：{ex.Message}";
                return (response, status);
            }
        }

        public async Task<(HikAcUserSearchResponseDto response, StatusDto status)> SearchHikAcUsers(HikAcUserSearchRequestDto request)
        {
            var response = new HikAcUserSearchResponseDto();
            var status = new StatusDto { isSuccess = false };
            if (request == null)
            {
                status.message = "查询人员参数为空";
                return (response, status);
            }

            try
            {
                foreach (var device in request.Devices ?? new List<HikAcDeviceDto>())
                {
                    var deviceResult = await SearchUsersOnDevice(device, request);
                    response.Results.Add(deviceResult);
                }

                if (response.Results.Count == 0)
                {
                    status.message = "查询人员失败：未提供门禁设备";
                    return (response, status);
                }

                status.isSuccess = response.Results.All(item => item.IsSuccess);
                status.message = status.isSuccess ? "查询人员成功" : "查询人员完成（部分失败）";
                return (response, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询人员信息失败");
                status.message = $"查询人员异常：{ex.Message}";
                return (response, status);
            }
        }

        /// <summary>
        /// 下发人脸到门禁设备（新增人脸并绑定到已有人员）
        /// </summary>
        public async Task<(HikAcFaceAddResponseDto response, StatusDto status)> AddFaceToHikAc(HikAcFaceAddRequestDto request)
        {
            var response = new HikAcFaceAddResponseDto
            {
                EmployeeNo = request?.EmployeeNo ?? string.Empty,
                Name = request?.Name ?? string.Empty
            };
            var status = new StatusDto { isSuccess = false };

            if (request == null)
            {
                status.message = "人脸下发参数为空";
                return (response, status);
            }

            if (string.IsNullOrWhiteSpace(request.EmployeeNo))
            {
                status.message = "人员工号不能为空";
                return (response, status);
            }

            // 获取人脸图片数据
            byte[]? faceImageBytes = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(request.FaceImageBase64))
                {
                    faceImageBytes = Convert.FromBase64String(request.FaceImageBase64);
                }
                else if (!string.IsNullOrWhiteSpace(request.FaceImagePath))
                {
                    string fullPath = request.FaceImagePath;
                    if (!Path.IsPathRooted(fullPath))
                    {
                        fullPath = Path.Combine(_env.WebRootPath, request.FaceImagePath);
                    }
                    if (File.Exists(fullPath))
                    {
                        faceImageBytes = await File.ReadAllBytesAsync(fullPath);
                    }
                    else
                    {
                        status.message = $"人脸图片文件不存在：{fullPath}";
                        return (response, status);
                    }
                }
                else
                {
                    status.message = "请提供人脸图片（Base64或文件路径）";
                    return (response, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析人脸图片失败");
                status.message = $"解析人脸图片失败：{ex.Message}";
                return (response, status);
            }

            if (faceImageBytes == null || faceImageBytes.Length == 0)
            {
                status.message = "人脸图片数据为空";
                return (response, status);
            }

            _logger.LogInformation($"开始下发人脸，工号：{request.EmployeeNo}，姓名：{request.Name}，图片大小：{faceImageBytes.Length}字节");

            // FDID 规范化：空或非数字（如 Swagger 示例 "string"）时使用默认 "1"
            string fdid = !string.IsNullOrWhiteSpace(request.FDID) && Regex.IsMatch(request.FDID, @"^\d+$")
                ? request.FDID
                : "1";

            try
            {
                foreach (var device in request.Devices ?? new List<HikAcDeviceDto>())
                {
                    var deviceResult = await AddFaceToDevice(device, request.EmployeeNo, request.Name, faceImageBytes, fdid);
                    response.Results.Add(deviceResult);
                }

                if (response.Results.Count == 0)
                {
                    status.message = "下发人脸失败：未提供门禁设备";
                    return (response, status);
                }

                status.isSuccess = response.Results.All(item => item.IsSuccess);
                status.message = status.isSuccess ? "下发人脸成功" : "下发人脸完成（部分失败）";
                return (response, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下发人脸失败");
                status.message = $"下发人脸异常：{ex.Message}";
                return (response, status);
            }
        }

        /// <summary>
        /// 向单个设备下发人脸
        /// </summary>
        private async Task<HikAcFaceAddDeviceResultDto> AddFaceToDevice(HikAcDeviceDto device, string employeeNo, string name, byte[] faceImageBytes, string fdid)
        {
            var result = new HikAcFaceAddDeviceResultDto
            {
                HikAcIP = device.HikAcIP,
                HikAcPort = device.HikAcPort,
                AcName = device.AcName
            };

            if (string.IsNullOrWhiteSpace(device.HikAcIP) ||
                string.IsNullOrWhiteSpace(device.HikAcUserName) ||
                string.IsNullOrWhiteSpace(device.HikAcPassword))
            {
                result.IsSuccess = false;
                result.Message = "门禁IP/账号/密码不能为空";
                return result;
            }

            HikAC hikAc = new HikAC(_logger);
            try
            {
                if (!hikAc.LoginAC(device.HikAcIP, device.HikAcPort, device.HikAcUserName, device.HikAcPassword))
                {
                    result.IsSuccess = false;
                    result.Message = "设备登录失败";
                    return result;
                }

                var (success, deviceResponse) = hikAc.AddFaceToDevice(employeeNo, name, faceImageBytes, fdid);
                result.IsSuccess = success;
                result.DeviceResponse = deviceResponse;
                result.Message = success ? "下发人脸成功" : $"下发人脸失败：{deviceResponse}";

                _logger.LogInformation($"设备 [{device.HikAcIP}:{device.HikAcPort}] 人脸下发结果：{(success ? "成功" : "失败")}，响应：{deviceResponse}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"向设备 [{device.HikAcIP}:{device.HikAcPort}] 下发人脸异常");
                result.IsSuccess = false;
                result.Message = $"下发人脸异常：{ex.Message}";
                return result;
            }
            finally
            {
                hikAc.Logout();
            }
        }

        /// <summary>
        /// 删除门禁用户信息（按文档：下发删除命令后建立长连接查询删除进度）
        /// </summary>
        public async Task<(HikAcUserDeleteResponseDto response, StatusDto status)> DeleteHikAcUser(HikAcUserDeleteRequestDto request)
        {
            var response = new HikAcUserDeleteResponseDto
            {
                UserID = request?.UserID ?? string.Empty
            };
            var status = new StatusDto { isSuccess = false };

            if (request == null)
            {
                status.message = "删除用户请求参数为空";
                return (response, status);
            }

            if (request.Devices == null || request.Devices.Count == 0)
            {
                status.message = "门禁设备列表不能为空";
                return (response, status);
            }

            if (string.IsNullOrWhiteSpace(request.UserID))
            {
                status.message = "要删除的人员工号(UserID)不能为空";
                return (response, status);
            }

            try
            {
                foreach (var device in request.Devices)
                {
                    var deviceResult = await DeleteUserOnDevice(device, request.UserID);
                    response.Results.Add(deviceResult);
                }

                status.isSuccess = response.Results.All(r => r.IsSuccess);
                status.message = status.isSuccess ? "删除用户成功" : "删除用户完成（部分失败）";
                return (response, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除门禁用户信息失败");
                status.message = $"删除用户异常：{ex.Message}";
                return (response, status);
            }
        }

        private async Task<HikAcUserDeleteDeviceResultDto> DeleteUserOnDevice(HikAcDeviceDto device, string employeeNo)
        {
            var result = new HikAcUserDeleteDeviceResultDto
            {
                HikAcIP = device.HikAcIP,
                HikAcPort = device.HikAcPort,
                AcName = device.AcName
            };

            if (string.IsNullOrWhiteSpace(device.HikAcIP) ||
                string.IsNullOrWhiteSpace(device.HikAcUserName) ||
                string.IsNullOrWhiteSpace(device.HikAcPassword))
            {
                result.IsSuccess = false;
                result.Message = "门禁IP/账号/密码不能为空";
                return result;
            }

            // 1) 下发删除命令：改用 HTTP PUT ISAPI，避免 NET_DVR_STDXMLConfig 错误码 17
            (bool deleteCmdOk, string cmdErr) = await SendDeleteUserViaHttp(device, employeeNo);
            if (!deleteCmdOk)
            {
                result.IsSuccess = false;
                result.Message = $"下发删除命令失败：{cmdErr}";
                result.DeviceResponse = cmdErr;
                _logger.LogWarning($"设备 [{device.HikAcIP}:{device.HikAcPort}] 下发删除命令失败：{cmdErr}");
                return result;
            }

            // 2) 建立长连接查询删除进度（SDK）
            HikAC hikAc = new HikAC(_logger);
            try
            {
                if (!hikAc.LoginAC(device.HikAcIP, device.HikAcPort, device.HikAcUserName, device.HikAcPassword))
                {
                    result.IsSuccess = false;
                    result.Message = "设备登录失败";
                    return result;
                }

                const int progressTimeoutMs = 30000;
                bool success = hikAc.PollDeleteProgress(progressTimeoutMs, out string progressErr);
                result.IsSuccess = success;
                result.Message = success ? "删除用户成功" : $"删除用户失败：{progressErr}";
                result.DeviceResponse = success ? string.Empty : progressErr;

                _logger.LogInformation($"设备 [{device.HikAcIP}:{device.HikAcPort}] 删除用户结果：{(success ? "成功" : "失败")}，{progressErr}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"向设备 [{device.HikAcIP}:{device.HikAcPort}] 删除用户异常");
                result.IsSuccess = false;
                result.Message = $"删除用户异常：{ex.Message}";
                return result;
            }
            finally
            {
                hikAc.Logout();
            }
        }

        /// <summary>
        /// 通过 HTTP ISAPI 下发删除人员命令（PUT /ISAPI/AccessControl/UserInfoDetail/Delete?format=json），避免 SDK STDXMLConfig 错误码 17。
        /// </summary>
        private async Task<(bool success, string errorMessage)> SendDeleteUserViaHttp(HikAcDeviceDto device, string employeeNo)
        {
            string path = "/ISAPI/AccessControl/UserInfoDetail/Delete?format=json";
            // ISAPI 走 HTTP 端口 80；8000 为 SDK 私有协议，发 HTTP 会报错，故删除命令固定用 80
            ushort httpPort = (device.HikAcPort == 8000 || device.HikAcPort == 0) ? (ushort)80 : device.HikAcPort;
            string url = BuildIsapiUrl(device.HikAcIP, httpPort, path);
            // 设备要求带 mode，按工号删除使用 byEmployeeNo
            string jsonBody = "{\"UserInfoDetail\":{\"mode\":\"byEmployeeNo\",\"employeeNo\":\"" + EscapeJsonString(employeeNo) + "\"}}";
            _logger.LogInformation($"HTTP 删除人员：URL={url}，Body={jsonBody}");

            try
            {
                using var response = await SendIsapiJsonRequest(url, HttpMethod.Put, jsonBody, device.HikAcUserName, device.HikAcPassword);
                string responseText = await SafeReadAsString(response);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} {responseText}".Trim());
                }
                if (!IsIsapiResponseOk(responseText, out string msg))
                {
                    return (false, msg);
                }
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<HikAcUserAddDeviceResultDto> AddUserToDevice(HikAcDeviceDto device, HikAcUserAddRequestDto request)
        {
            var result = new HikAcUserAddDeviceResultDto
            {
                HikAcIP = device.HikAcIP,
                HikAcPort = device.HikAcPort,
                AcName = device.AcName
            };

            if (string.IsNullOrWhiteSpace(device.HikAcIP) ||
                string.IsNullOrWhiteSpace(device.HikAcUserName) ||
                string.IsNullOrWhiteSpace(device.HikAcPassword))
            {
                result.IsSuccess = false;
                result.Message = "门禁IP/账号/密码不能为空";
                return result;
            }

            HikAC hikAc = new HikAC(_logger);
            int handle = -1;
            try
            {
                if (!hikAc.LoginAC(device.HikAcIP, device.HikAcPort, device.HikAcUserName, device.HikAcPassword))
                {
                    result.IsSuccess = false;
                    result.Message = "设备登录失败";
                    return result;
                }

                // 使用官方示例的 URL 格式
                string url = "PUT /ISAPI/AccessControl/UserInfo/SetUp?format=json";
                handle = hikAc.StartJsonRemoteConfig(url, 1, out string startError);
                if (handle == -1)
                {
                    result.IsSuccess = false;
                    result.Message = startError;
                    _logger.LogWarning($"建立下发人员参数长连接失败 [{device.HikAcIP}:{device.HikAcPort}] : {startError}");
                    return result;
                }
                _logger.LogInformation($"建立下发人员参数长连接成功，handle={handle}");
                
                // 构建 JSON 请求体（参考官方示例格式）
                string? beginTime = NormalizeHikTime(request.StartTime);
                string? endTime = NormalizeHikTime(request.EndTime);
                
                var sb = new StringBuilder();
                sb.Append("{\"UserInfo\":{");
                sb.Append($"\"employeeNo\":\"{EscapeJsonString(request.UserID)}\",");
                sb.Append($"\"name\":\"{EscapeJsonString(request.UserName)}\",");
                sb.Append("\"userType\":\"normal\"");
                
                if (!string.IsNullOrWhiteSpace(beginTime) || !string.IsNullOrWhiteSpace(endTime))
                {
                    sb.Append(",\"Valid\":{");
                    sb.Append("\"enable\":true,");
                    sb.Append($"\"beginTime\":\"{beginTime ?? string.Empty}\",");
                    sb.Append($"\"endTime\":\"{endTime ?? string.Empty}\",");
                    sb.Append("\"timeType\":\"local\"");
                    sb.Append("}");
                }
                
                // 添加门禁权限相关字段
                sb.Append(",\"doorRight\":\"1\"");
                sb.Append(",\"RightPlan\":[{\"doorNo\":1,\"planTemplateNo\":\"1\"}]");
                
                sb.Append("}}");
                string body = sb.ToString();
                _logger.LogInformation($"下发人员请求体：{body}");

                string responseText = string.Empty;
                int status = -1;
                string sendError = string.Empty;
                
                // 使用直接缓冲区方式发送（参考官方示例）
                while (true)
                {
                    status = hikAc.SendWithRecvRemoteConfigDirect(handle, body, out responseText, out sendError);
                    _logger.LogInformation($"下发人员响应：status={status}，响应={responseText}");
                    
                    if (status == -1)
                    {
                        _logger.LogError($"NET_DVR_SendWithRecvRemoteConfig接口调用失败：{sendError}");
                        result.IsSuccess = false;
                        result.Message = sendError;
                        return result;
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT)
                    {
                        _logger.LogInformation("配置等待...");
                        await Task.Delay(10);
                        continue;
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FAILED)
                    {
                        _logger.LogWarning($"下发人员失败：{responseText}");
                        result.IsSuccess = false;
                        result.Message = $"下发人员失败：{responseText}";
                        result.DeviceResponse = responseText;
                        return result;
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_EXCEPTION)
                    {
                        _logger.LogError($"下发人员异常：{responseText}");
                        result.IsSuccess = false;
                        result.Message = $"下发人员异常：{responseText}";
                        result.DeviceResponse = responseText;
                        return result;
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS)
                    {
                        _logger.LogInformation($"下发人员成功：{responseText}");
                        result.IsSuccess = true;
                        result.Message = "下发人员成功";
                        result.DeviceResponse = responseText;
                        return result;
                    }
                    else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                    {
                        _logger.LogInformation("下发人员完成");
                        result.IsSuccess = true;
                        result.Message = "下发人员完成";
                        result.DeviceResponse = responseText;
                        return result;
                    }
                    else
                    {
                        _logger.LogWarning($"未知状态码：{status}");
                        break;
                    }
                }
                
                result.IsSuccess = false;
                result.Message = $"下发人员未知状态：{status}";
                return result;
            }
            finally
            {
                if (handle >= 0)
                {
                    hikAc.StopRemoteConfig(handle);
                }
                hikAc.Logout();
            }
        }

        private async Task<HikAcUserSearchDeviceResultDto> SearchUsersOnDevice(HikAcDeviceDto device, HikAcUserSearchRequestDto request)
        {
            var result = new HikAcUserSearchDeviceResultDto
            {
                HikAcIP = device.HikAcIP,
                HikAcPort = device.HikAcPort,
                AcName = device.AcName
            };

            if (string.IsNullOrWhiteSpace(device.HikAcIP) ||
                string.IsNullOrWhiteSpace(device.HikAcUserName) ||
                string.IsNullOrWhiteSpace(device.HikAcPassword))
            {
                result.IsSuccess = false;
                result.Message = "门禁IP/账号/密码不能为空";
                return result;
            }

            HikAC hikAc = new HikAC(_logger);
            int handle = -1;
            try
            {
                if (!hikAc.LoginAC(device.HikAcIP, device.HikAcPort, device.HikAcUserName, device.HikAcPassword))
                {
                    result.IsSuccess = false;
                    result.Message = "设备登录失败";
                    return result;
                }

                bool hasFilter = !string.IsNullOrWhiteSpace(request.UserID) || !string.IsNullOrWhiteSpace(request.UserName);
                int totalCount = 0;
                if (!hasFilter)
                {
                    totalCount = await GetUserCountBySdk(hikAc);
                }

                // 使用官方示例的 URL 格式
                string url = "POST /ISAPI/AccessControl/UserInfo/Search?format=json";
                _logger.LogInformation($"查询人员开始，URL={url}");
                handle = hikAc.StartJsonRemoteConfig(url, 1, out string startError);
                if (handle < 0)
                {
                    result.IsSuccess = false;
                    result.Message = startError;
                    _logger.LogWarning($"建立查询人员参数长连接失败：{startError}");
                    return result;
                }
                _logger.LogInformation($"建立查询人员参数长连接成功，handle={handle}");

                const int pageSize = 30;
                int position = 0;
                int totalMatches = 0;
                // searchID 使用官方示例格式：yyyyMMdd+9位数字
                string searchId = DateTime.Now.ToString("yyyyMMdd") + "000000000";

                while (true)
                {
                    string body = BuildUserSearchBody(request.UserID, request.UserName, position, pageSize, searchId);
                    _logger.LogInformation($"查询人员请求体：{body}");
                    string responseText = string.Empty;
                    int status = -1;
                    string sendError = string.Empty;

                    while (true)
                    {
                        status = hikAc.SendWithRecvRemoteConfigDirect(handle, body, out responseText, out sendError);
                        _logger.LogInformation($"查询人员返回：status={status}，响应={responseText}");
                        
                        if (status == -1)
                        {
                            _logger.LogError($"NET_DVR_SendWithRecvRemoteConfig查询人员参数调用失败：{sendError}");
                            result.IsSuccess = false;
                            result.Message = sendError;
                            return result;
                        }
                        else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT)
                        {
                            _logger.LogInformation("配置等待...");
                            await Task.Delay(10);
                            continue;
                        }
                        else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FAILED)
                        {
                            _logger.LogWarning("获取人员参数失败");
                            result.IsSuccess = false;
                            result.Message = "获取人员参数失败";
                            return result;
                        }
                        else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_EXCEPTION)
                        {
                            _logger.LogError("获取人员参数异常");
                            result.IsSuccess = false;
                            result.Message = "获取人员参数异常";
                            return result;
                        }
                        else if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS ||
                                 status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                        {
                            break;
                        }
                        else
                        {
                            _logger.LogWarning($"未知状态码：{status}");
                            break;
                        }
                    }

                    if (!TryParseUserSearchResponse(responseText, result.Users, out int batchTotalMatches, out int batchCount, out string parseMessage))
                    {
                        // NO MATCH 表示没有匹配的记录，视为正常结束
                        if (batchCount == 0 && string.Equals(parseMessage, "NO MATCH", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        // 当响应为空时，根据status判断是否为正常情况
                        if (string.Equals(parseMessage, "设备返回为空", StringComparison.Ordinal))
                        {
                            // FINISH状态且响应为空，表示设备上没有用户，视为正常结束
                            if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                            {
                                _logger.LogInformation("设备返回FINISH状态且无数据，视为无用户数据");
                                break;
                            }
                            // SUCCESS状态但响应为空，可能是设备无数据或需要重试
                            if (status == (int)CHCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS)
                            {
                                _logger.LogWarning($"设备返回SUCCESS状态但响应为空，position={position}，可能设备无用户数据");
                                // 如果是第一次查询就返回空，视为设备无用户
                                if (position == 0)
                                {
                                    break;
                                }
                            }
                        }

                        result.IsSuccess = false;
                        result.Message = parseMessage;
                        return result;
                    }

                    if (batchTotalMatches > 0)
                    {
                        totalMatches = batchTotalMatches;
                    }

                    if (batchCount <= 0)
                    {
                        break;
                    }

                    position += batchCount;
                    if (totalMatches > 0 && position >= totalMatches)
                    {
                        break;
                    }
                    if (!hasFilter && totalCount > 0 && position >= totalCount)
                    {
                        break;
                    }
                }

                result.TotalMatches = totalMatches > 0 ? totalMatches : result.Users.Count;
                result.IsSuccess = true;
                result.Message = result.Users.Count == 0 ? "未查询到人员信息" : "查询成功";
                return result;
            }
            finally
            {
                if (handle >= 0)
                {
                    hikAc.StopRemoteConfig(handle);
                }
                hikAc.Logout();
            }
        }

        private async Task<(byte[]? faceBytes, string fileName, string errorMessage)> CaptureFaceByIsapi(
            string ip,
            ushort port,
            string userName,
            string password,
            string saveDirectory)
        {
            const string defaultFileName = "FaceData.jpg";
            try
            {
                // ISAPI通常走HTTP端口(默认80)，门禁SDK端口(如8000)不一定支持ISAPI
                string url = (port == 80 || port == 0 || port == 8000)
                    ? $"http://{ip}/ISAPI/AccessControl/CaptureFaceData"
                    : $"http://{ip}:{port}/ISAPI/AccessControl/CaptureFaceData";

                string xmlBody = """
                    <CaptureFaceDataCond version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
                        <dataType>binary</dataType>
                    </CaptureFaceDataCond>
                    """;

                using var response = await SendIsapiRequest(url, xmlBody, userName, password);
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await SafeReadAsString(response);
                    string wwwAuth = response.Headers.WwwAuthenticate?.ToString() ?? string.Empty;
                    return (null, defaultFileName, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} {errorBody} {wwwAuth}".Trim());
                }

                byte[] rawBytes = await response.Content.ReadAsByteArrayAsync();
                string? boundary = GetMultipartBoundary(response.Content.Headers.ContentType?.Parameters);
                if (string.IsNullOrWhiteSpace(boundary))
                {
                    boundary = DetectBoundaryFromBody(rawBytes) ?? "MIME_boundary";
                }

                // 先尝试标准Multipart解析
                using (var stream = new MemoryStream(rawBytes))
                {
                    var reader = new MultipartReader(boundary, stream);
                    MultipartSection? section;
                    while ((section = await reader.ReadNextSectionAsync()) != null)
                    {
                        var headers = section.Headers;
                        if (headers == null)
                        {
                            continue;
                        }

                        string? contentType = headers.TryGetValue("Content-Type", out var ct)
                            ? ct.ToString()
                            : null;
                        string? contentDisposition = headers.TryGetValue("Content-Disposition", out var cd)
                            ? cd.ToString()
                            : null;

                        bool isImageBlock = string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(contentType, "image/jpg", StringComparison.OrdinalIgnoreCase);
                        string? fileName = null;
                        if (!string.IsNullOrWhiteSpace(contentDisposition) &&
                            ContentDispositionHeaderValue.TryParse(contentDisposition, out var cdHeader))
                        {
                            fileName = cdHeader.FileName?.Trim('"');
                            var name = cdHeader.Name?.Trim('"');
                            if (string.Equals(name, "FaceData", StringComparison.OrdinalIgnoreCase))
                            {
                                isImageBlock = true;
                            }
                        }

                        if (!isImageBlock)
                        {
                            continue;
                        }

                        using var ms = new MemoryStream();
                        await section.Body.CopyToAsync(ms);
                        var imageBytes = ms.ToArray();
                        if (imageBytes.Length == 0)
                        {
                            continue;
                        }

                        Directory.CreateDirectory(saveDirectory);
                        string finalFileName = string.IsNullOrWhiteSpace(fileName)
                            ? $"FaceData_{DateTime.Now:yyyyMMddHHmmss}.jpg"
                            : fileName;
                        string filePath = Path.Combine(saveDirectory, finalFileName);
                        await File.WriteAllBytesAsync(filePath, imageBytes);
                        return (imageBytes, finalFileName, string.Empty);
                    }
                }

                // 兼容老设备：手动按boundary分块解析
                if (!string.IsNullOrWhiteSpace(boundary))
                {
                    var manualResult = TryParseMultipartImage(rawBytes, boundary);
                    if (manualResult != null)
                    {
                        Directory.CreateDirectory(saveDirectory);
                    string finalFileName = manualResult.FileName
                        ?? $"FaceData_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                        string filePath = Path.Combine(saveDirectory, finalFileName);
                        await File.WriteAllBytesAsync(filePath, manualResult.Bytes);
                        return (manualResult.Bytes, finalFileName, string.Empty);
                    }
                }

                // 兜底：直接查找JPEG段
                var jpegBytes = TryFindJpegBytes(rawBytes);
                if (jpegBytes != null)
                {
                    Directory.CreateDirectory(saveDirectory);
                    string finalFileName = $"FaceData_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                    string filePath = Path.Combine(saveDirectory, finalFileName);
                    await File.WriteAllBytesAsync(filePath, jpegBytes);
                    return (jpegBytes, finalFileName, string.Empty);
                }

                return (null, defaultFileName, $"未找到图片分块，boundary={boundary}");
            }
            catch (HttpRequestException ex)
            {
                return (null, defaultFileName, $"请求失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, defaultFileName, $"解析失败：{ex.Message}");
            }
        }

        private static string? GetMultipartBoundary(ICollection<NameValueHeaderValue>? parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            return parameters
                .FirstOrDefault(p => p.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.Trim('"')
                ?.TrimStart('-');
        }

        private static async Task<string> SafeReadAsString(HttpResponseMessage response)
        {
            try
            {
                string body = await response.Content.ReadAsStringAsync();
                return TrimResponseText(body);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TrimResponseText(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            string trimmed = body.Replace("\r", " ").Replace("\n", " ").Trim();
            return trimmed.Length > 200 ? trimmed.Substring(0, 200) + "..." : trimmed;
        }

        private async Task<HttpResponseMessage> SendIsapiRequest(string url, string xmlBody, string userName, string password)
        {
            var client = _httpClientFactory.CreateClient("hik");
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml")
            };

            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            response.Dispose();
            using var digestHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                PreAuthenticate = true,
                Credentials = new NetworkCredential(userName, password)
            };
            using var digestClient = new HttpClient(digestHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            using var digestRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml")
            };
            return await digestClient.SendAsync(digestRequest);
        }

        private async Task<HttpResponseMessage> SendIsapiJsonRequest(string url, HttpMethod method, string? jsonBody, string userName, string password)
        {
            var client = _httpClientFactory.CreateClient("hik");
            using var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrWhiteSpace(jsonBody))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            response.Dispose();
            using var digestHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                PreAuthenticate = true,
                Credentials = new NetworkCredential(userName, password)
            };
            using var digestClient = new HttpClient(digestHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            using var digestRequest = new HttpRequestMessage(method, url);
            if (!string.IsNullOrWhiteSpace(jsonBody))
            {
                digestRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }
            return await digestClient.SendAsync(digestRequest);
        }

        private static string BuildIsapiUrl(string ip, ushort port, string pathAndQuery)
        {
            if (port == 0 || port == 80)
            {
                return $"http://{ip}{pathAndQuery}";
            }
            // 门禁 ISAPI 常在 8000 端口，需显式带端口
            return $"http://{ip}:{port}{pathAndQuery}";
        }

        private static string? NormalizeHikTime(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (DateTime.TryParse(input, out var time))
            {
                return time.ToString("yyyy-MM-ddTHH:mm:ss");
            }

            return input;
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

        private static bool IsIsapiResponseOk(string responseText, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return true;
            }

            try
            {
                var root = JsonNode.Parse(responseText);
                var statusNode = root?["ResponseStatus"];
                if (statusNode != null)
                {
                    var statusString = statusNode["statusString"]?.ToString();
                    var statusCode = statusNode["statusCode"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(statusString) && !string.Equals(statusString, "OK", StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"设备返回：{statusString} (code={statusCode})";
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        private Task<int> GetUserCountBySdk(HikAC hikAc)
        {
            try
            {
                string url = "GET /ISAPI/AccessControl/UserInfo/Count?format=json";
                if (!hikAc.StdXmlConfig(url, null, out string output, out _, out _))
                {
                    return Task.FromResult(0);
                }

                var root = JsonNode.Parse(output);
                var countNode = root?["UserInfoCount"]?["total"];
                if (countNode != null && int.TryParse(countNode.ToString(), out int total))
                {
                    return Task.FromResult(total);
                }
            }
            catch
            {
                // 忽略统计失败，后续仍可按搜索分页获取
            }

            return Task.FromResult(0);
        }

        private static string BuildUserSearchBody(string? userId, string? userName, int position, int maxResults, string searchId)
        {
            // 严格按照官方示例格式：{"UserInfoSearchCond":{"searchID":"20210301000000000","searchResultPosition":0,"maxResults":30}}
            var sb = new StringBuilder();
            sb.Append("{\"UserInfoSearchCond\":{");
            sb.Append($"\"searchID\":\"{searchId}\",");
            sb.Append($"\"searchResultPosition\":{position},");
            sb.Append($"\"maxResults\":{maxResults}");
            
            if (!string.IsNullOrWhiteSpace(userId))
            {
                sb.Append($",\"EmployeeNoList\":[{{\"employeeNo\":\"{EscapeJsonString(userId)}\"}}]");
            }
            if (!string.IsNullOrWhiteSpace(userName))
            {
                sb.Append($",\"name\":\"{EscapeJsonString(userName)}\"");
            }
            
            sb.Append("}}");
            return sb.ToString();
        }

        private static bool TryParseUserSearchResponse(
            string responseText,
            List<HikAcUserInfoDto> users,
            out int totalMatches,
            out int batchCount,
            out string message)
        {
            totalMatches = 0;
            batchCount = 0;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                message = "设备返回为空";
                return false;
            }

            try
            {
                var root = JsonNode.Parse(responseText);
                var searchNode = root?["UserInfoSearch"];
                if (searchNode == null)
                {
                    message = "设备返回结构不包含UserInfoSearch";
                    return false;
                }

                var statusStr = searchNode["responseStatusStrg"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(statusStr) &&
                    !string.Equals(statusStr, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(statusStr, "NO MATCH", StringComparison.OrdinalIgnoreCase))
                    {
                        message = "NO MATCH";
                        return false;
                    }
                    message = statusStr;
                    return false;
                }

                var totalNode = searchNode["totalMatches"];
                if (totalNode != null && int.TryParse(totalNode.ToString(), out int total))
                {
                    totalMatches = total;
                }

                if (searchNode["UserInfo"] is JsonArray userArray)
                {
                    foreach (var item in userArray)
                    {
                        AppendUserInfo(item, users, ref batchCount);
                    }
                }
                else if (searchNode["UserInfo"] is JsonObject userObject)
                {
                    AppendUserInfo(userObject, users, ref batchCount);
                }

                return true;
            }
            catch (Exception ex)
            {
                message = $"解析失败：{ex.Message}";
                return false;
            }
        }

        private static void AppendUserInfo(JsonNode? item, List<HikAcUserInfoDto> users, ref int batchCount)
        {
            if (item == null)
            {
                return;
            }

            var info = new HikAcUserInfoDto
            {
                UserID = item["employeeNo"]?.ToString() ?? string.Empty,
                UserName = item["name"]?.ToString() ?? string.Empty,
                UserType = item["userType"]?.ToString() ?? string.Empty
            };

            var validNode = item["Valid"];
            if (validNode != null)
            {
                if (bool.TryParse(validNode["enable"]?.ToString(), out bool enabled))
                {
                    info.ValidEnabled = enabled;
                }
                info.BeginTime = validNode["beginTime"]?.ToString() ?? string.Empty;
                info.EndTime = validNode["endTime"]?.ToString() ?? string.Empty;
            }

            users.Add(info);
            batchCount++;
        }

        private static string? DetectBoundaryFromBody(byte[] rawBytes)
        {
            int lineEnd = IndexOf(rawBytes, Encoding.ASCII.GetBytes("\r\n"), 0);
            if (lineEnd <= 2)
            {
                return null;
            }

            string firstLine = Encoding.ASCII.GetString(rawBytes, 0, lineEnd);
            if (!firstLine.StartsWith("--", StringComparison.Ordinal))
            {
                return null;
            }

            return firstLine.Substring(2).Trim();
        }

        private sealed class MultipartImageResult
        {
            public byte[] Bytes { get; set; } = Array.Empty<byte>();
            public string? FileName { get; set; }
        }

        private static MultipartImageResult? TryParseMultipartImage(byte[] rawBytes, string boundary)
        {
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary);
            byte[] boundaryEndBytes = Encoding.ASCII.GetBytes("--" + boundary + "--");
            int index = IndexOf(rawBytes, boundaryBytes, 0);
            while (index >= 0)
            {
                int partStart = index + boundaryBytes.Length;
                if (StartsWithAt(rawBytes, boundaryEndBytes, index))
                {
                    break;
                }

                if (StartsWithAt(rawBytes, Encoding.ASCII.GetBytes("\r\n"), partStart))
                {
                    partStart += 2;
                }

                int headerEnd = IndexOf(rawBytes, Encoding.ASCII.GetBytes("\r\n\r\n"), partStart);
                if (headerEnd < 0)
                {
                    break;
                }

                string headers = Encoding.ASCII.GetString(rawBytes, partStart, headerEnd - partStart);
                bool isImage = headers.IndexOf("Content-Type: image/jpeg", StringComparison.OrdinalIgnoreCase) >= 0
                               || headers.IndexOf("Content-Type: image/jpg", StringComparison.OrdinalIgnoreCase) >= 0
                               || headers.IndexOf("name=\"FaceData\"", StringComparison.OrdinalIgnoreCase) >= 0;

                int bodyStart = headerEnd + 4;
                int nextBoundary = IndexOf(rawBytes, boundaryBytes, bodyStart);
                if (nextBoundary < 0)
                {
                    nextBoundary = IndexOf(rawBytes, boundaryEndBytes, bodyStart);
                }

                if (nextBoundary < 0)
                {
                    break;
                }

                int bodyEnd = nextBoundary - 2; // 去掉前置的\r\n
                if (bodyEnd > bodyStart && isImage)
                {
                    byte[] imageBytes = new byte[bodyEnd - bodyStart];
                    Buffer.BlockCopy(rawBytes, bodyStart, imageBytes, 0, imageBytes.Length);

                    string? fileName = null;
                    var match = System.Text.RegularExpressions.Regex.Match(headers, "filename=\"(?<name>[^\"]+)\"");
                    if (match.Success)
                    {
                        fileName = match.Groups["name"].Value;
                    }

                    if (imageBytes.Length > 0)
                    {
                        return new MultipartImageResult { Bytes = imageBytes, FileName = fileName };
                    }
                }

                index = IndexOf(rawBytes, boundaryBytes, nextBoundary);
            }

            return null;
        }

        private static byte[]? TryFindJpegBytes(byte[] rawBytes)
        {
            byte[] soi = { 0xFF, 0xD8 };
            byte[] eoi = { 0xFF, 0xD9 };
            int start = IndexOf(rawBytes, soi, 0);
            if (start < 0)
            {
                return null;
            }

            int end = IndexOf(rawBytes, eoi, start + 2);
            if (end < 0)
            {
                return null;
            }

            int length = (end + 2) - start;
            byte[] jpeg = new byte[length];
            Buffer.BlockCopy(rawBytes, start, jpeg, 0, length);
            return jpeg;
        }

        private static int IndexOf(byte[] haystack, byte[] needle, int startIndex)
        {
            for (int i = startIndex; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool StartsWithAt(byte[] haystack, byte[] needle, int index)
        {
            if (index < 0 || index + needle.Length > haystack.Length)
            {
                return false;
            }
            for (int i = 0; i < needle.Length; i++)
            {
                if (haystack[index + i] != needle[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}