using Microsoft.AspNetCore.Mvc;
using WebCameraApi.Services;
using WebCameraApi.Dto;
using System.Linq;

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
        public async Task<IActionResult> OpenHikAC([FromBody] HikAcConfigDto config)
        {
            // 1. 参数校验
            if (config == null)
            {
                var errorMsg = "调用openHikAC接口失败：门禁配置不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
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

        /// <summary>
        /// 门禁人脸录制（保存到wwwroot/RecordFaceImage并返回Base64）
        /// </summary>
        /// <param name="config">门禁连接参数（设备IP、账号、密码、端口）</param>
        /// <returns>人脸图片Base64与保存信息</returns>
        [HttpPost("recordFace")]
        [ProducesResponseType(typeof(ApiResponseDto<HikAcRecordFaceResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RecordFace([FromBody] HikAcRecordFaceRequestDto config)
        {
            if (config == null)
            {
                var errorMsg = "调用recordFace接口失败：门禁参数不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                var (response, status) = await _hikAcService.RecordHikAcFace(config);
                if (status.isSuccess)
                {
                    _logger.LogInformation("recordFace接口调用成功：{AcValue}", config.AcName);
                }
                else
                {
                    _logger.LogWarning("recordFace接口调用失败：{AcValue}，原因：{Message}", config.AcName, status.message);
                }
                return Ok(ApiResponseDto<HikAcRecordFaceResponseDto>.Success(response, status.message));
            }
            catch (Exception ex)
            {
                var errorMsg = $"recordFace接口调用异常：{config?.AcName}";
                _logger.LogError(ex, errorMsg);
                return Ok(ApiResponseDto<HikAcRecordFaceResponseDto>.Success(new HikAcRecordFaceResponseDto { AcName = config?.AcName ?? string.Empty }, $"{errorMsg}，错误详情：{ex.Message}"));
            }
        }

        /// <summary>
        /// 下发人员信息（新增）
        /// </summary>
        /// <param name="request">门禁连接与人员信息</param>
        /// <remarks>
        /// 请求示例：
        /// {
        ///   "devices": [
        ///     {
        ///       "hikAcIP": "192.168.1.10",
        ///       "hikAcPort": 8000,
        ///       "hikAcUserName": "admin",
        ///       "hikAcPassword": "12345",
        ///       "acName": "A门禁"
        ///     },
        ///     {
        ///       "hikAcIP": "192.168.1.11",
        ///       "hikAcPort": 8000,
        ///       "hikAcUserName": "admin",
        ///       "hikAcPassword": "12345",
        ///       "acName": "B门禁"
        ///     }
        ///   ],
        ///   "userID": "1001",
        ///   "userName": "张三",
        ///   "startTime": "2026-01-01 00:00:00",
        ///   "endTime": "2026-12-31 23:59:59"
        /// }
        /// </remarks>
        [HttpPost("addUser")]
        [ProducesResponseType(typeof(ApiResponseDto<HikAcUserAddResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddUser([FromBody] HikAcUserAddRequestDto request)
        {
            if (request == null)
            {
                var errorMsg = "调用addUser接口失败：请求参数不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (request.Devices == null || request.Devices.Count == 0)
            {
                var errorMsg = "调用addUser接口失败：门禁设备列表不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (string.IsNullOrWhiteSpace(request.UserID) || string.IsNullOrWhiteSpace(request.UserName))
            {
                var errorMsg = "调用addUser接口失败：UserID与UserName不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                var (response, status) = await _hikAcService.AddHikAcUser(request);
                if (status.isSuccess)
                {
                    _logger.LogInformation("addUser接口调用成功：{UserId}-{UserName}", request.UserID, request.UserName);
                }
                else
                {
                    _logger.LogWarning("addUser接口调用失败：{UserId}-{UserName}，原因：{Message}", request.UserID, request.UserName, status.message);
                }

                return Ok(ApiResponseDto<HikAcUserAddResponseDto>.Success(response, status.message));
            }
            catch (Exception ex)
            {
                var errorMsg = $"addUser接口调用异常：{request?.UserID}";
                _logger.LogError(ex, errorMsg);
                return Ok(ApiResponseDto<HikAcUserAddResponseDto>.Success(new HikAcUserAddResponseDto(), $"{errorMsg}，错误详情：{ex.Message}"));
            }
        }

        /// <summary>
        /// 查询人员信息。若不携带 userID 和 userName，默认返回设备上所有人员信息。
        /// </summary>
        /// <param name="request">门禁连接与查询条件（userID、userName 可选，不传则返回全部人员）</param>
        /// <remarks>
        /// 请求示例（按条件查询）：
        /// {
        ///   "devices": [
        ///     {
        ///       "hikAcIP": "192.168.1.10",
        ///       "hikAcPort": 8000,
        ///       "hikAcUserName": "admin",
        ///       "hikAcPassword": "12345",
        ///       "acName": "A门禁"
        ///     }
        ///   ],
        ///   "userID": "1001",
        ///   "userName": "张三"
        /// }
        /// 不传 userID、userName 时返回设备所有人员；每条人员信息包含 numOfCard（录卡数）、numOfFace（人脸数）。
        /// </remarks>
        [HttpPost("queryUsers")]
        [ProducesResponseType(typeof(ApiResponseDto<HikAcUserSearchResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> QueryUsers([FromBody] HikAcUserSearchRequestDto request)
        {
            if (request == null)
            {
                var errorMsg = "调用queryUsers接口失败：请求参数不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (request.Devices == null || request.Devices.Count == 0)
            {
                var errorMsg = "调用queryUsers接口失败：门禁设备列表不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                var (response, status) = await _hikAcService.SearchHikAcUsers(request);
                if (status.isSuccess)
                {
                    int totalUsers = response.Results.Sum(r => r.Users.Count);
                    _logger.LogInformation("queryUsers接口调用成功：返回{Count}条", totalUsers);
                }
                else
                {
                    _logger.LogWarning("queryUsers接口调用失败：{Message}", status.message);
                }

                return Ok(ApiResponseDto<HikAcUserSearchResponseDto>.Success(response, status.message));
            }
            catch (Exception ex)
            {
                var errorMsg = "queryUsers接口调用异常";
                _logger.LogError(ex, errorMsg);
                return Ok(ApiResponseDto<HikAcUserSearchResponseDto>.Success(new HikAcUserSearchResponseDto(), $"{errorMsg}，错误详情：{ex.Message}"));
            }
        }

        /// <summary>
        /// 下发人脸到门禁设备（新增人脸并绑定到已有人员）
        /// </summary>
        /// <param name="request">人脸下发参数</param>
        /// <remarks>
        /// 请求示例：
        /// {
        ///   "devices": [
        ///     {
        ///       "hikAcIP": "192.168.1.10",
        ///       "hikAcPort": 8000,
        ///       "hikAcUserName": "admin",
        ///       "hikAcPassword": "12345",
        ///       "acName": "A门禁"
        ///     }
        ///   ],
        ///   "employeeNo": "1001",
        ///   "name": "张三",
        ///   "faceImageBase64": "base64编码的人脸图片...",
        ///   "fdid": "1"
        /// }
        /// 
        /// 说明：
        /// - employeeNo: 人员工号，用于关联已有人员
        /// - name: 人员姓名
        /// - faceImageBase64: 人脸图片Base64编码（与faceImagePath二选一）
        /// - faceImagePath: 人脸图片服务器相对路径，如 "RecordFaceImage/face_xxx.jpg"（与faceImageBase64二选一）
        /// - fdid: 人脸库ID，默认为"1"
        /// </remarks>
        [HttpPost("addFace")]
        [ProducesResponseType(typeof(ApiResponseDto<HikAcFaceAddResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddFace([FromBody] HikAcFaceAddRequestDto request)
        {
            if (request == null)
            {
                var errorMsg = "调用addFace接口失败：请求参数不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (request.Devices == null || request.Devices.Count == 0)
            {
                var errorMsg = "调用addFace接口失败：门禁设备列表不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (string.IsNullOrWhiteSpace(request.EmployeeNo))
            {
                var errorMsg = "调用addFace接口失败：EmployeeNo（人员工号）不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (string.IsNullOrWhiteSpace(request.FaceImageBase64) && string.IsNullOrWhiteSpace(request.FaceImagePath))
            {
                var errorMsg = "调用addFace接口失败：请提供人脸图片（FaceImageBase64或FaceImagePath）";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                var (response, status) = await _hikAcService.AddFaceToHikAc(request);
                if (status.isSuccess)
                {
                    _logger.LogInformation("addFace接口调用成功：{EmployeeNo}-{Name}", request.EmployeeNo, request.Name);
                }
                else
                {
                    _logger.LogWarning("addFace接口调用失败：{EmployeeNo}-{Name}，原因：{Message}", request.EmployeeNo, request.Name, status.message);
                }

                return Ok(ApiResponseDto<HikAcFaceAddResponseDto>.Success(response, status.message));
            }
            catch (Exception ex)
            {
                var errorMsg = $"addFace接口调用异常：{request?.EmployeeNo}";
                _logger.LogError(ex, errorMsg);
                return Ok(ApiResponseDto<HikAcFaceAddResponseDto>.Success(new HikAcFaceAddResponseDto(), $"{errorMsg}，错误详情：{ex.Message}"));
            }
        }

        /// <summary>
        /// 删除门禁用户信息
        /// </summary>
        /// <param name="request">门禁设备列表与要删除的人员工号</param>
        /// <remarks>
        /// 按海康文档：先下发删除命令（PUT /ISAPI/AccessControl/UserInfoDetail/Delete），再建立长连接查询删除进度（GET DeleteProcess），进度为 success 时表示删除完成。删除人员会同时删除其关联的卡、指纹、人脸信息。
        /// 请求示例：
        /// {
        ///   "devices": [
        ///     {
        ///       "hikAcIP": "192.168.1.10",
        ///       "hikAcPort": 8000,
        ///       "hikAcUserName": "admin",
        ///       "hikAcPassword": "12345",
        ///       "acName": "A门禁"
        ///     }
        ///   ],
        ///   "userID": "1001"
        /// }
        /// </remarks>
        [HttpPost("deleteUser")]
        [ProducesResponseType(typeof(ApiResponseDto<HikAcUserDeleteResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteUser([FromBody] HikAcUserDeleteRequestDto request)
        {
            if (request == null)
            {
                var errorMsg = "调用deleteUser接口失败：请求参数不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (request.Devices == null || request.Devices.Count == 0)
            {
                var errorMsg = "调用deleteUser接口失败：门禁设备列表不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            if (string.IsNullOrWhiteSpace(request.UserID))
            {
                var errorMsg = "调用deleteUser接口失败：要删除的人员工号(UserID)不能为空";
                _logger.LogWarning(errorMsg);
                return Ok(ApiResponseDto.Fail(errorMsg, 400));
            }

            try
            {
                var (response, status) = await _hikAcService.DeleteHikAcUser(request);
                if (status.isSuccess)
                {
                    _logger.LogInformation("deleteUser接口调用成功：{UserId}", request.UserID);
                }
                else
                {
                    _logger.LogWarning("deleteUser接口调用失败：{UserId}，原因：{Message}", request.UserID, status.message);
                }
                return Ok(ApiResponseDto<HikAcUserDeleteResponseDto>.Success(response, status.message));
            }
            catch (Exception ex)
            {
                var errorMsg = $"deleteUser接口调用异常：{request.UserID}";
                _logger.LogError(ex, errorMsg);
                return Ok(ApiResponseDto<HikAcUserDeleteResponseDto>.Success(new HikAcUserDeleteResponseDto { UserID = request.UserID }, $"{errorMsg}，错误详情：{ex.Message}"));
            }
        }

    }
}