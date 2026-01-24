using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Services;
using WebCameraApi.Dto;
using Serilog;
using PostgreConfig;
using System.Collections.Generic;
using System;

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

        [HttpPost("BatchAddConfig")]
        public async Task<IActionResult> BatchAddConfig([FromBody] List<HikAcBatchConfigDto> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                return BadRequest(ApiResponseDto.Fail("批量门禁配置不能为空", 400));
            }

            var result = new BatchImportResultDto { Total = configs.Count };
            var validList = new List<HikAcInfomationDto>();

            for (int i = 0; i < configs.Count; i++)
            {
                var item = configs[i];
                var errors = ValidateHikAcConfig(item);
                if (errors.Count > 0)
                {
                    foreach (var err in errors)
                    {
                        result.Errors.Add($"第{i + 1}条：{err}");
                    }
                    continue;
                }

                validList.Add(new HikAcInfomationDto
                {
                    HikAcName = item.HikAcName.Trim(),
                    HikAcIP = item.HikAcIP.Trim(),
                    HikAcUser = item.HikAcUser.Trim(),
                    HikAcPassword = item.HikAcPassword,
                    HikAcPort = item.HikAcPort,
                    HikAcRetryCount = item.HikAcRetryCount,
                    HikAcWaitmillisecounds = item.HikAcWaitmillisecounds
                });
            }

            try
            {
                await _hikAcService.BatchUpsertHikAcConfigsAsync(validList);
                result.Success = validList.Count;
                result.Failed = result.Total - result.Success;
                return Ok(ApiResponseDto<BatchImportResultDto>.Success(result, "批量门禁配置处理完成"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量门禁配置写入数据库失败");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<BatchImportResultDto>.Fail("批量门禁配置写入数据库失败", 500));
            }
        }

        private static List<string> ValidateHikAcConfig(HikAcBatchConfigDto item)
        {
            var errors = new List<string>();
            if (item == null)
            {
                errors.Add("对象为空");
                return errors;
            }
            if (string.IsNullOrWhiteSpace(item.HikAcName)) errors.Add("HikAcName不能为空");
            if (string.IsNullOrWhiteSpace(item.HikAcIP)) errors.Add("HikAcIP不能为空");
            if (string.IsNullOrWhiteSpace(item.HikAcUser)) errors.Add("HikAcUser不能为空");
            if (string.IsNullOrWhiteSpace(item.HikAcPassword)) errors.Add("HikAcPassword不能为空");
            if (item.HikAcPort <= 0) errors.Add("HikAcPort必须大于0");
            if (item.HikAcRetryCount < 0) errors.Add("HikAcRetryCount不能为负数");
            if (item.HikAcWaitmillisecounds < 0) errors.Add("HikAcWaitmillisecounds不能为负数");
            return errors;
        }
    }
}