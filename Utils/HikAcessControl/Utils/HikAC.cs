using HikHCNetSDK;
using Microsoft.Extensions.Logging; // 补充正确的ILogger命名空间（原代码可能遗漏）
using Serilog;
using System;
using System.Text;
using System.Windows;
using WebCameraApi.Controllers;
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
    }
}