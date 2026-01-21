// WebCameraApi/Dto/HikAcDtos.cs
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
}