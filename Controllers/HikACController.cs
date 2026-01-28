using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Services;
using WebCameraApi.Dto;

namespace WebCameraApi.Controllers
{
    /// <summary>
    /// 海康门禁控制接口
    /// </summary>
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

        /// <summary>
        /// 远程开门（海康门禁网关）
        /// </summary>
        /// <param name="config">门禁连接与开门参数（设备IP、账号、密码、通道等）</param>
        /// <returns>开门执行结果与设备名称</returns>
        [HttpPost("openHikAC")]
        [ProducesResponseType(typeof(ApiResponseDto<AcResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status400BadRequest)]
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
                }
                else
                {
                    _logger.LogWarning("openHikAC接口调用失败：{AcValue}", config.AcName);
                }
                return Ok(ApiResponseDto<AcResponseDto>.Success(response, status.message));
            }
            catch (Exception ex)
            {
                var errorMsg = $"openHikAC接口调用异常：{config?.AcName}";
                _logger.LogError(ex, errorMsg);
                return Ok(ApiResponseDto<AcResponseDto>.Success(new AcResponseDto { AcName = config?.AcName ?? string.Empty }, $"{errorMsg}，错误详情：{ex.Message}"));
            }
        }
    }
}