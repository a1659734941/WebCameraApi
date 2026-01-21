using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Services;
using WebCameraApi.Dto;
using Serilog;

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

        [HttpGet("openHikAC")]
        public async Task<IActionResult> OpenHikAC(
            [FromQuery] string? acName = null,
            [FromQuery] string? acIp = null)
        {
            // 1. 参数校验
            if (string.IsNullOrWhiteSpace(acName) && string.IsNullOrWhiteSpace(acIp))
            {
                var errorMsg = "调用openHikAC接口失败：必须传入acName（门禁名称）或acIp（门禁IP）参数";
                _logger.LogWarning(errorMsg);
                return BadRequest(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                // 2. 调用服务层方法
                var (response, status) = await _hikAcService.OpenHikAccessGetway(acName, acIp);

                // 3. 封装响应
                if (status.isSuccess)
                {
                    _logger.LogInformation("openHikAC接口调用成功：{AcValue}", string.IsNullOrEmpty(acName) ? acIp : acName);
                    return Ok(ApiResponseDto<AcResponseDto>.Success(response, status.message));
                }
                else
                {
                    _logger.LogWarning("openHikAC接口调用失败：{AcValue}", string.IsNullOrEmpty(acName) ? acIp : acName);
                    return BadRequest(ApiResponseDto<AcResponseDto>.Fail(status.message ?? "获取门禁网关信息失败", 400));
                }
            }
            catch (Exception ex)
            {
                var targetValue = string.IsNullOrEmpty(acName) ? acIp : acName;
                var errorMsg = $"openHikAC接口调用异常：{targetValue}";
                _logger.LogError(ex, errorMsg);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto.Fail($"{errorMsg}，错误详情：{ex.Message}", 500));
            }
        }
    }
}