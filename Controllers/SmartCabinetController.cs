using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Services;
using WebCameraApi.Dto;

namespace WebCameraApi.Controllers
{
    /// <summary>
    /// 智能柜接口（志信锁控板）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SmartCabinetController : ControllerBase
    {
        private readonly SmartCabinetService _smartCabinetService;
        private readonly ILogger<SmartCabinetController> _logger;

        public SmartCabinetController(SmartCabinetService smartCabinetService, ILogger<SmartCabinetController> logger)
        {
            _smartCabinetService = smartCabinetService;
            _logger = logger;
        }

        /// <summary>
        /// 志信锁控板：单开、全开、单查
        /// </summary>
        /// <param name="request">IP+端口(IP:端口)、动作、地址、门号</param>
        /// <returns>执行结果</returns>
        /// <remarks>
        /// 动作: 单开, 全开, 单查；
        /// 地址: 默认从 0 开始；
        /// 门: 默认从 1 开始。
        /// </remarks>
        [HttpPost("ZhiXin")]
        [ProducesResponseType(typeof(ApiResponseDto<SmartCabinetZhiXinResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ZhiXin([FromBody] SmartCabinetZhiXinRequestDto request)
        {
            if (request == null)
            {
                _logger.LogWarning("志信锁控板接口请求为空");
                return Ok(ApiResponseDto.Fail("请求参数不能为空", 400));
            }

            var (code, msg, data) = await _smartCabinetService.ZhiXinAsync(request);

            if (code == 200)
                _logger.LogInformation("志信锁控板接口成功: {IP}, {Action}", request.IP, request.Action);
            else
                _logger.LogWarning("志信锁控板接口失败: {IP}, {Action}, {Msg}", request.IP, request.Action, msg);

            return Ok(new ApiResponseDto<SmartCabinetZhiXinResponseDto>
            {
                Code = code,
                Msg = msg,
                Data = data
            });
        }
    }
}
