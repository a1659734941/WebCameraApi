using ConfigGet;
using Onvif_GetPhoto.DTO;
using Onvif_GetPhoto.Util;
using PostgreConfig;
using Serilog;
using System.Text;
using WebCameraApi.Dto;

namespace WebCameraApi.Services
{
    public class CameraRtspService
    {
        private Dictionary<string, OnvifCameraInfomation> cameraConfigs = new Dictionary<string, OnvifCameraInfomation>();
        private readonly ILogger<CameraRtspService> _logger;

        public CameraRtspService(ILogger<CameraRtspService> logger)
        {
            _logger = logger;
            InitPostgreAsync().Wait();
        }

        public async Task InitPostgreAsync()
        {
            try
            {
                // 1. 获取appsetting里的配置
                dynamic dbConfigDynamic = Appsettings_Get.GetConfigByKey("PostresSQLConfig");
                PgConnectionOptions pgConnectionOptions = new PgConnectionOptions();
                pgConnectionOptions.Host = dbConfigDynamic.host;
                pgConnectionOptions.Port = dbConfigDynamic.port;
                pgConnectionOptions.Username = dbConfigDynamic.username;
                pgConnectionOptions.Password = dbConfigDynamic.password;
                pgConnectionOptions.Database = dbConfigDynamic.database;
                string _connectionString = pgConnectionOptions.BuildConnectionString();
                // 实例化仓储类（自动拼接连接字符串）
                var repository = new PgOnvifCameraInfomationRepository();

                await repository.CreateTableIfNotExistsAsync(_connectionString);

                cameraConfigs = await repository.GetAllOnvifCameraInfomationsAsync(_connectionString); // 获取所有摄像头配置
                //_logger.LogInformation("CameraConfigSql 初始化成功，已加载 {Count} 个摄像头配置", cameraConfigs.Count);
                if (cameraConfigs.Count == 0)
                {
                    _logger.LogError("CameraConfigSql 初始化完成，但未加载到任何摄像头配置");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CameraConfigSql 初始化失败");
            }
        }

        public async Task<(CameraRtspResponse response, CameraRtspResponse.StatusInfo status)> GetSingleRtspUri(string cameraName)
        {
            var response = new CameraRtspResponse { CameraName = cameraName };
            var status = new CameraRtspResponse.StatusInfo { IsSuccess = false }; // 默认设为失败，避免误判

            try
            {
                var rtspCameraConfigs = BatchGetRTSP.ConvertToRtspRequestDict(cameraConfigs);

                // 先校验摄像头名称是否存在于配置中（补充可选校验，避免KeyNotFoundException）
                if (!rtspCameraConfigs.ContainsKey(cameraName))
                {
                    status.Message = $"获取RTSP地址失败：未找到名称为【{cameraName}】的摄像头配置";
                    return (response, status);
                }

                string cameraIp = rtspCameraConfigs[cameraName]["ip"].ToString();
                int cameraPort = Convert.ToInt32(rtspCameraConfigs[cameraName]["port"]);
                string cameraUser = rtspCameraConfigs[cameraName]["user"].ToString();
                string cameraPassword = rtspCameraConfigs[cameraName]["password"].ToString();
                int cameraretryCount = Convert.ToInt32(rtspCameraConfigs[cameraName]["retryCount"]);
                int waitMilliseconds = Convert.ToInt32(rtspCameraConfigs[cameraName]["waitMilliseconds"]);

                var rtspUrl = await BatchGetRTSP.GetSingleRTSPAddressAsync(
                                                                        cameraName,
                                                                        cameraIp,
                                                                        cameraUser,
                                                                        cameraPassword,
                                                                        cameraPort,
                                                                        cameraretryCount,
                                                                        waitMilliseconds);

                // 核心修复：校验RTSP地址有效性
                if (string.IsNullOrEmpty(rtspUrl))
                {
                    status.IsSuccess = false;
                    status.Message = $"获取RTSP地址失败：摄像头【{cameraName}】重试{cameraretryCount}次未获取到有效地址";
                    _logger.LogError($"获取RTSP地址失败：摄像头【{cameraName}】重试{cameraretryCount}次未获取到有效地址");
                }
                else
                {
                    _logger.LogInformation($"获取摄像头【{cameraName}】初始RTSP地址成功,现在开始拼接成完整RTSP");
                }
                Uri originalUri = new Uri(rtspUrl);
                if (originalUri.Scheme != "rtsp")
                {
                    throw new ArgumentException("原始地址不是RTSP协议！");
                }
                string encodedUser = string.IsNullOrWhiteSpace(cameraUser) ? "" : Uri.EscapeDataString(cameraUser);
                string encodedPassword = string.IsNullOrWhiteSpace(cameraPassword) ? "" : Uri.EscapeDataString(cameraPassword);
                string authPart = $"{encodedUser}:{encodedPassword}@";
                StringBuilder newRtspUrl = new StringBuilder();
                newRtspUrl.Append($"{originalUri.Scheme}://"); // 协议（rtsp://）
                newRtspUrl.Append(authPart); // 认证信息（user:pass@）
                newRtspUrl.Append(originalUri.Host); // 主机地址（如192.168.8.2）
                                                     // 端口（非默认554时添加）
                if (originalUri.Port != -1 && originalUri.Port != 554)
                {
                    newRtspUrl.Append($":{originalUri.Port}");
                }
                else if (originalUri.Port == 554)
                {
                    newRtspUrl.Append($":554"); // 显式添加默认端口（可选）
                }
                newRtspUrl.Append(originalUri.PathAndQuery);
                if (newRtspUrl != null)
                {
                    status.IsSuccess = true;
                    status.Message = "获取RTSP地址成功";
                    response.RtspUrl = (new Uri(newRtspUrl.ToString())).ToString();
                }
            }
            catch (Exception ex)
            {
                status.IsSuccess = false;
                status.Message = $"获取摄像头【{cameraName}】RTSP地址时发生异常：{ex.Message}";
                _logger.LogError(ex, status.Message);
            }

            return (response, status);
        }
    }
}