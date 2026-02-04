using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Dto;
using WebCameraApi.Services;
using PostgreConfig;
using System.Collections.Generic;
using System.Linq;
using System;

namespace WebCameraApi.Controllers
{
    /// <summary>
    /// 摄像头RTSP配置与查询接口
    /// </summary>
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

        /// <summary>
        /// 查询单个摄像头RTSP地址（GET）
        /// </summary>
        /// <param name="cameraName">摄像头名称（必填）</param>
        /// <returns>RTSP地址与摄像头基础信息</returns>
        [HttpGet("GetRtsp")]
        [ProducesResponseType(typeof(ApiResponseDto<CameraRtspResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRtsp([FromQuery] string cameraName)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(cameraName))
            {
                var errorMsg = "摄像头名称不能为空！";
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            // 调用服务获取结果
            var (response, status) = await _cameraRtspService.GetSingleRtspUri(cameraName);
            if (status.IsSuccess)
            {
                return Ok(ApiResponseDto<CameraRtspResponse>.Success(response, status.Message));
            }
            else
            {
                return Ok(ApiResponseDto<CameraRtspResponse>.Fail(status.Message, 400));
            }
        }

        /// <summary>
        /// 查询单个摄像头RTSP地址（POST）
        /// </summary>
        /// <param name="cameraName">摄像头名称（必填，JSON字符串）</param>
        /// <returns>RTSP地址与摄像头基础信息</returns>
        [HttpPost("GetRtsp")]
        [ProducesResponseType(typeof(ApiResponseDto<CameraRtspResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostRtsp([FromBody] string cameraName)
        {
            // 参数校验
            if (string.IsNullOrWhiteSpace(cameraName))
            {
                var errorMsg = "摄像头名称不能为空！";
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            // 调用服务获取结果
            var (response, status) = await _cameraRtspService.GetSingleRtspUri(cameraName);
            if (status.IsSuccess)
            {
                return Ok(ApiResponseDto<CameraRtspResponse>.Success(response, status.Message));
            }
            else
            {
                return Ok(ApiResponseDto<CameraRtspResponse>.Fail(status.Message, 400));
            }
        }

        /// <summary>
        /// 批量新增/更新摄像头配置
        /// </summary>
        /// <param name="configs">摄像头配置列表（必填）</param>
        /// <returns>批量处理结果，包含成功/失败数量与错误明细</returns>
        [HttpPost("BatchAddConfig")]
        [ProducesResponseType(typeof(ApiResponseDto<BatchImportResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchAddConfig([FromBody] List<CameraConfigDto> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                return Ok(ApiResponseDto.Fail("批量摄像头配置不能为空", 400));
            }

            var result = new BatchImportResultDto { Total = configs.Count };
            var validList = new List<OnvifCameraInfomation>();

            for (int i = 0; i < configs.Count; i++)
            {
                var item = configs[i];
                var errors = ValidateCameraConfig(item);
                if (errors.Count > 0)
                {
                    foreach (var err in errors)
                    {
                        result.Errors.Add($"第{i + 1}条：{err}");
                    }
                    continue;
                }

                validList.Add(new OnvifCameraInfomation
                {
                    CameraName = item.CameraName.Trim(),
                    CameraIP = item.CameraIP.Trim(),
                    CameraUser = item.CameraUser.Trim(),
                    CameraPassword = item.CameraPassword,
                    CameraPort = item.CameraPort,
                    CameraRetryCount = item.CameraRetryCount,
                    CameraWaitmillisecounds = item.CameraWaitmillisecounds
                });
            }

            try
            {
                await _cameraRtspService.BatchUpsertCameraConfigsAsync(validList);
                result.Success = validList.Count;
                result.Failed = result.Total - result.Success;
                return Ok(ApiResponseDto<BatchImportResultDto>.Success(result, "批量摄像头配置处理完成"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量摄像头配置写入数据库失败");
                return Ok(ApiResponseDto<BatchImportResultDto>.Fail("批量摄像头配置写入数据库失败", 500));
            }
        }

        private static List<string> ValidateCameraConfig(CameraConfigDto item)
        {
            var errors = new List<string>();
            if (item == null)
            {
                errors.Add("对象为空");
                return errors;
            }
            if (string.IsNullOrWhiteSpace(item.CameraName)) errors.Add("CameraName不能为空");
            if (string.IsNullOrWhiteSpace(item.CameraIP)) errors.Add("CameraIP不能为空");
            if (string.IsNullOrWhiteSpace(item.CameraUser)) errors.Add("CameraUser不能为空");
            if (string.IsNullOrWhiteSpace(item.CameraPassword)) errors.Add("CameraPassword不能为空");
            if (item.CameraPort <= 0) errors.Add("CameraPort必须大于0");
            if (item.CameraRetryCount < 0) errors.Add("CameraRetryCount不能为负数");
            if (item.CameraWaitmillisecounds < 0) errors.Add("CameraWaitmillisecounds不能为负数");
            return errors;
        }
    }
}