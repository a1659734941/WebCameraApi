// WebCameraApi/Dto/HikAcDtos.cs
using System.Collections.Generic;
namespace WebCameraApi.Dto
{
    /// <summary>
    /// 门控/梯控命令值（NET_DVR_ControlGateway 的 dwStaic）
    /// </summary>
    public static class HikAcGatewayCommand
    {
        /// <summary>关闭（梯控：受控）</summary>
        public const int Close = 0;
        /// <summary>打开（梯控：开门）</summary>
        public const int Open = 1;
        /// <summary>常开（梯控：自由、通道状态）</summary>
        public const int NormallyOpen = 2;
        /// <summary>常关（梯控：禁用）</summary>
        public const int NormallyClosed = 3;
        /// <summary>恢复（梯控：普通状态）</summary>
        public const int Restore = 4;
    }

    // 门禁设备配置DTO（存储IP、端口、账号密码、名称；门控/梯控命令与序号）
    public class HikAcConfigDto
    {
        public string HikAcIP { get; set; } = string.Empty;
        public ushort HikAcPort { get; set; } = 8000; // 海康默认端口
        public string HikAcUserName { get; set; } = string.Empty;
        public string HikAcPassword { get; set; } = string.Empty;
        public string AcName { get; set; } = string.Empty; // 门禁名称
        /// <summary>
        /// 门禁序号（楼层/锁ID），从1开始；-1 表示对所有门或梯控所有楼层操作。默认 1
        /// </summary>
        public int GatewayIndex { get; set; } = 1;
        /// <summary>
        /// 命令值：0-关闭 1-打开 2-常开 3-常关 4-恢复（门控/梯控）。默认 1（打开）
        /// </summary>
        public int Command { get; set; } = 1;
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
        /// <summary>
        /// 卡数量（设备返回的录卡信息数量）
        /// </summary>
        public int NumOfCard { get; set; }
        /// <summary>
        /// 人脸数量（设备返回的人脸信息数量）
        /// </summary>
        public int NumOfFace { get; set; }
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

    /// <summary>
    /// 屏幕状态请求DTO
    /// </summary>
    public class HikAcScreenStatusRequestDto
    {
        /// <summary>
        /// 门禁IP（可选，默认为配置文件中的IP）
        /// </summary>
        public string? IP { get; set; }
        /// <summary>
        /// 图片名称（必填，一般4为空闲，5为使用中，123是官方自带图片）
        /// </summary>
        public string BGP { get; set; } = string.Empty;
        /// <summary>
        /// 门禁名称（可选，用于日志记录）
        /// </summary>
        public string? AcName { get; set; }
        /// <summary>
        /// 用户名（可选，用于认证）
        /// </summary>
        public string? UserName { get; set; }
        /// <summary>
        /// 密码（可选，用于认证）
        /// </summary>
        public string? Password { get; set; }
    }

    /// <summary>
    /// 屏幕状态设备操作结果DTO
    /// </summary>
    public class HikAcScreenStatusDeviceResultDto
    {
        /// <summary>
        /// 门禁IP
        /// </summary>
        public string IP { get; set; } = string.Empty;
        /// <summary>
        /// 门禁名称
        /// </summary>
        public string? AcName { get; set; }
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

    /// <summary>
    /// 屏幕状态响应DTO
    /// </summary>
    public class HikAcScreenStatusResponseDto
    {
        /// <summary>
        /// 各门禁处理结果
        /// </summary>
        public List<HikAcScreenStatusDeviceResultDto> Results { get; set; } = new();
    }
}