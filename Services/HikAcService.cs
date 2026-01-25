using HikAcessControl;
using WebCameraApi.Dto;

namespace WebCameraApi.Services
{
    public class HikAcService
    {
        private readonly ILogger<HikAcService> _logger;

        public HikAcService(ILogger<HikAcService> logger)
        {
            _logger = logger;
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
    }
}