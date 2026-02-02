// WebCameraApi/Dto/HikAcDtos.cs
using System.Collections.Generic;
namespace WebCameraApi.Dto
{
    // 门禁设备配置DTO（存储IP、端口、账号密码、名称）
    public class HikAcConfigDto
    {
        public string HikAcIP { get; set; } = string.Empty;
        public ushort HikAcPort { get; set; } = 8000; // 海康默认端口
        public string HikAcUserName { get; set; } = string.Empty;
        public string HikAcPassword { get; set; } = string.Empty;
        public string AcName { get; set; } = string.Empty; // 门禁名称
    }

    // 响应体的response子对象
    public class AcResponseDto
    {
        /// <summary>
        /// 门禁名称
        /// </summary>
        public string AcName { get; set; } = string.Empty;
    }

    // 人脸录制请求DTO（前端传门禁IP、端口、账号密码）
    public class HikAcRecordFaceRequestDto
    {
        public string HikAcIP { get; set; } = string.Empty;
        public ushort HikAcPort { get; set; } = 8000; // 海康默认端口
        public string HikAcUserName { get; set; } = string.Empty;
        public string HikAcPassword { get; set; } = string.Empty;
        public string AcName { get; set; } = string.Empty;
    }

    // 人脸录制响应DTO
    public class HikAcRecordFaceResponseDto
    {
        public string AcName { get; set; } = string.Empty;
        public string FaceImageBase64 { get; set; } = string.Empty;
        public string FaceImageFileName { get; set; } = string.Empty;
        public string FaceImageRelativePath { get; set; } = string.Empty;
    }

    // 响应体的status子对象
    public class StatusDto
    {
        public bool isSuccess { get; set; }
        public string message { get; set; } = string.Empty;
    }

    // 最终返回的完整响应DTO
    public class HikAcResponseDto
    {
        public AcResponseDto response { get; set; } = new AcResponseDto();
        public StatusDto status { get; set; } = new StatusDto();
    }

    public class HikAcDeviceDto
    {
        public string HikAcIP { get; set; } = string.Empty;
        public ushort HikAcPort { get; set; } = 8000;
        public string HikAcUserName { get; set; } = string.Empty;
        public string HikAcPassword { get; set; } = string.Empty;
        public string AcName { get; set; } = string.Empty;
    }

    public class HikAcUserAddRequestDto
    {
        /// <summary>
        /// 门禁设备列表
        /// </summary>
        public List<HikAcDeviceDto> Devices { get; set; } = new();
        /// <summary>
        /// 人员工号
        /// </summary>
        public string UserID { get; set; } = string.Empty;
        /// <summary>
        /// 人员姓名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>
        /// 有效期开始时间
        /// </summary>
        public string? StartTime { get; set; }
        /// <summary>
        /// 有效期结束时间
        /// </summary>
        public string? EndTime { get; set; }
    }

    public class HikAcUserAddDeviceResultDto
    {
        /// <summary>
        /// 门禁IP
        /// </summary>
        public string HikAcIP { get; set; } = string.Empty;
        /// <summary>
        /// 门禁端口
        /// </summary>
        public ushort HikAcPort { get; set; } = 8000;
        /// <summary>
        /// 门禁名称
        /// </summary>
        public string AcName { get; set; } = string.Empty;
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 处理结果说明
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// 设备原始返回内容
        /// </summary>
        public string DeviceResponse { get; set; } = string.Empty;
    }

    public class HikAcUserAddResponseDto
    {
        /// <summary>
        /// 人员工号
        /// </summary>
        public string UserID { get; set; } = string.Empty;
        /// <summary>
        /// 人员姓名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>
        /// 各门禁处理结果
        /// </summary>
        public List<HikAcUserAddDeviceResultDto> Results { get; set; } = new();
    }

    public class HikAcUserSearchRequestDto
    {
        /// <summary>
        /// 门禁设备列表
        /// </summary>
        public List<HikAcDeviceDto> Devices { get; set; } = new();
        /// <summary>
        /// 人员工号（可选）
        /// </summary>
        public string? UserID { get; set; }
        /// <summary>
        /// 人员姓名（可选）
        /// </summary>
        public string? UserName { get; set; }
    }

    public class HikAcUserInfoDto
    {
        public string UserID { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool ValidEnabled { get; set; }
        public string BeginTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class HikAcUserSearchDeviceResultDto
    {
        /// <summary>
        /// 门禁IP
        /// </summary>
        public string HikAcIP { get; set; } = string.Empty;
        /// <summary>
        /// 门禁端口
        /// </summary>
        public ushort HikAcPort { get; set; } = 8000;
        /// <summary>
        /// 门禁名称
        /// </summary>
        public string AcName { get; set; } = string.Empty;
        /// <summary>
        /// 匹配总数
        /// </summary>
        public int TotalMatches { get; set; }
        /// <summary>
        /// 人员列表
        /// </summary>
        public List<HikAcUserInfoDto> Users { get; set; } = new();
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 处理结果说明
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    public class HikAcUserSearchResponseDto
    {
        /// <summary>
        /// 各门禁查询结果
        /// </summary>
        public List<HikAcUserSearchDeviceResultDto> Results { get; set; } = new();
    }

    /// <summary>
    /// 人脸下发请求DTO
    /// </summary>
    public class HikAcFaceAddRequestDto
    {
        /// <summary>
        /// 门禁设备列表
        /// </summary>
        public List<HikAcDeviceDto> Devices { get; set; } = new();
        /// <summary>
        /// 人员工号（用于关联已有人员）
        /// </summary>
        public string EmployeeNo { get; set; } = string.Empty;
        /// <summary>
        /// 人员姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 人脸图片Base64编码（与FaceImagePath二选一）
        /// </summary>
        public string? FaceImageBase64 { get; set; }
        /// <summary>
        /// 人脸图片服务器路径（与FaceImageBase64二选一）
        /// </summary>
        public string? FaceImagePath { get; set; }
        /// <summary>
        /// 人脸库ID，默认为1
        /// </summary>
        public string FDID { get; set; } = "1";
    }

    /// <summary>
    /// 单个设备人脸下发结果
    /// </summary>
    public class HikAcFaceAddDeviceResultDto
    {
        public string HikAcIP { get; set; } = string.Empty;
        public ushort HikAcPort { get; set; } = 8000;
        public string AcName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeviceResponse { get; set; } = string.Empty;
    }

    /// <summary>
    /// 人脸下发响应DTO
    /// </summary>
    public class HikAcFaceAddResponseDto
    {
        public string EmployeeNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<HikAcFaceAddDeviceResultDto> Results { get; set; } = new();
    }

    /// <summary>
    /// 删除门禁用户请求DTO
    /// </summary>
    public class HikAcUserDeleteRequestDto
    {
        /// <summary>
        /// 门禁设备列表
        /// </summary>
        public List<HikAcDeviceDto> Devices { get; set; } = new();
        /// <summary>
        /// 要删除的人员工号（employeeNo）
        /// </summary>
        public string UserID { get; set; } = string.Empty;
    }

    /// <summary>
    /// 单设备删除用户结果
    /// </summary>
    public class HikAcUserDeleteDeviceResultDto
    {
        public string HikAcIP { get; set; } = string.Empty;
        public ushort HikAcPort { get; set; } = 8000;
        public string AcName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DeviceResponse { get; set; } = string.Empty;
    }

    /// <summary>
    /// 删除门禁用户响应DTO
    /// </summary>
    public class HikAcUserDeleteResponseDto
    {
        public string UserID { get; set; } = string.Empty;
        public List<HikAcUserDeleteDeviceResultDto> Results { get; set; } = new();
    }
}