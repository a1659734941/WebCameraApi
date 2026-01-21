using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Dto;
using WebCameraApi.Services;

namespace WebCameraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CameraRtspController : ControllerBase
    {
        private readonly CameraRtspService _cameraRtspService;
        private readonly ILogger<CameraRtspController> _logger;

        public CameraRtspController(CameraRtspService cameraRtspService, ILogger<CameraRtspController> logger)
        {
            _cameraRtspService = cameraRtspService;
            _logger = logger;
            _ = _cameraRtspService.InitPostgreAsync();
        }

        [HttpGet("GetRtsp")]
        public async Task<IActionResult> GetRtsp([FromQuery] string cameraName)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(cameraName))
            {
                var errorMsg = "摄像头名称不能为空！";
                return BadRequest(ApiResponseDto.Fail(errorMsg, 400));
            }

            // 调用服务获取结果
            var (response, status) = await _cameraRtspService.GetSingleRtspUri(cameraName);
            if (status.IsSuccess)
            {
                return Ok(ApiResponseDto<CameraRtspResponse>.Success(response, status.Message));
            }
            else
            {
                return BadRequest(ApiResponseDto<CameraRtspResponse>.Fail(status.Message, 400));
            }
        }

        [HttpPost("GetRtsp")]
        public async Task<IActionResult> PostRtsp([FromBody] string cameraName)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(cameraName))
            {
                var errorMsg = "摄像头名称不能为空！";
                return BadRequest(ApiResponseDto.Fail(errorMsg, 400));
            }

            // 调用服务获取结果
            var (response, status) = await _cameraRtspService.GetSingleRtspUri(cameraName);
            if (status.IsSuccess)
            {
                return Ok(ApiResponseDto<CameraRtspResponse>.Success(response, status.Message));
            }
            else
            {
                return BadRequest(ApiResponseDto<CameraRtspResponse>.Fail(status.Message, 400));
            }
        }
    }
}