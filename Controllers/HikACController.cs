using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Services;
using WebCameraApi.Dto;

namespace WebCameraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HikACController : ControllerBase
    {
        private readonly HikAcService _hikAcService;
        private readonly ILogger<HikACController> _logger;

        public HikACController(HikAcService hikAcService, ILogger<HikACController> logger)
        {
            _hikAcService = hikAcService;
            _logger = logger;
        }

        [HttpPost("openHikAC")]
        public async Task<IActionResult> OpenHikAC([FromBody] HikAcConfigDto config)
        {
            // 1. 参数校验
            if (config == null)
            {
                var errorMsg = "调用openHikAC接口失败：门禁配置不能为空";
                _logger.LogWarning(errorMsg);
                return BadRequest(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                // 2. 调用服务层方法
                var (response, status) = await _hikAcService.OpenHikAccessGetway(config);

                // 3. 封装响应
                if (status.isSuccess)
                {
                    _logger.LogInformation("openHikAC接口调用成功：{AcValue}", config.AcName);
                    return Ok(ApiResponseDto<AcResponseDto>.Success(response, status.message));
                }
                else
                {
                    _logger.LogWarning("openHikAC接口调用失败：{AcValue}", config.AcName);
                    return BadRequest(ApiResponseDto<AcResponseDto>.Fail(status.message ?? "获取门禁网关信息失败", 400));
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"openHikAC接口调用异常：{config?.AcName}";
                _logger.LogError(ex, errorMsg);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto.Fail($"{errorMsg}，错误详情：{ex.Message}", 500));
            }
        }
    }
}