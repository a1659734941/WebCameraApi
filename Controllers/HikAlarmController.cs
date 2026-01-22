using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebCameraApi.Dto;
using WebCameraApi.Services;
using System.Collections.Generic;
using HikAlarmEndPoints;

namespace WebCameraApi.Controllers
{
    /// <summary>
    /// 海康摄像头报警/计数接口控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")] // 路由：/api/HikAlarm/BehaviorAnalysisJson
    [Produces("application/json")]
    public class HikAlarmController : ControllerBase
    {
        private readonly ILogger<HikAlarmController> _logger;
        private readonly HikAlarmService _hikAlarmService;

        /// <summary>
        /// 人数计数接口：http://服务器IP:端口/api/HikAlarm/CountingCamera
        /// 行为分析接口：http://服务器IP:端口/api/HikAlarm/BehaviorAnalysisJson
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="hikAlarmService"></param>
        public HikAlarmController(ILogger<HikAlarmController> logger, HikAlarmService hikAlarmService)
        {
            _logger = logger;
            _hikAlarmService = hikAlarmService;
        }

        /// <summary>
        /// 接收摄像头人数计数数据（供摄像头每秒调用）
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CountingCamera()
        {
            try
            {
                await _hikAlarmService.GetCountingCameraAsync(HttpContext);
                return Ok(ApiResponseDto.Success("处理成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HikAlarmController.CountingCamera 接口处理异常");
                return Ok(ApiResponseDto.Fail("处理异常（内部日志已记录）", 500));
            }
        }

        /// <summary>
        /// 接收摄像头行为分析JSON数据（POST API）
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BehaviorAnalysisJson()
        {
            try
            {
                // 高并发场景：控制器仅转发请求到服务层
                await _hikAlarmService.GetBehaviorAnalysisJson(HttpContext);

                // 统一返回200，避免摄像头重试
                return Ok(ApiResponseDto.Success("行为分析数据处理成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HikAlarmController.BehaviorAnalysisJson 接口处理异常");

                // 返回200而非500，防止摄像头重复发送
                return Ok(ApiResponseDto.Fail("行为分析数据处理异常（内部日志已记录）", 500));
            }
        }

        /// <summary>
        /// 查询报警记录信息（支持时间、类型、设备筛选 + 分页）
        /// </summary>
        /// <param name="startTime">开始时间（格式：yyyy-MM-dd HH:mm:ss，为空则不限制）</param>
        /// <param name="endTime">结束时间（格式：yyyy-MM-dd HH:mm:ss，为空则不限制）</param>
        /// <param name="eventType">报警类型（为空则不限制）</param>
        /// <param name="deviceName">设备名称（为空则不限制）</param>
        /// <param name="pageNumber">页码（默认1）</param>
        /// <param name="pageSize">页大小（默认15）</param>
        /// <returns>分页后的报警记录列表（EventType为中文）</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDto<HikAlarmRecordPageDto>), StatusCodes.Status200OK)]
        public IActionResult SelectAlarmInfomation(
            [FromQuery] string? startTime = null,
            [FromQuery] string? endTime = null,
            [FromQuery] string? eventType = null,
            [FromQuery] string? deviceName = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            try
            {
                // 调用服务层查询方法（已包含翻译逻辑）
                var result = _hikAlarmService.SelectAlarmInfomation(
                    startTime, endTime, eventType, deviceName, pageNumber, pageSize);

                // 返回成功响应（带翻译后的中文EventType）
                return Ok(ApiResponseDto<HikAlarmRecordPageDto>.Success(result, "查询成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HikAlarmController.SelectAlarmInfomation 接口查询异常");
                return Ok(ApiResponseDto<HikAlarmRecordPageDto>.Fail("查询异常（内部日志已记录）", 500));
            }
        }
        /// <summary>
        /// 获取所有报警记录，查看不同事件的出现次数
        /// </summary>
        /// <returns>每一种事件出现了多少次</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmCountDto>>), StatusCodes.Status200OK)]
        public IActionResult GetAllAlarmRecordCount()
        {
            try
            {
                var result = _hikAlarmService.GetAllAlarmRecordCount();
                return Ok(ApiResponseDto<List<AlarmCountDto>>.Success(result, "获取成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HikAlarmController.GetAllAlarmRecordCount 接口获取异常");
                return Ok(ApiResponseDto<List<AlarmCountDto>>.Fail("获取异常（内部日志已记录）", 500));
            }
        }
    }
}