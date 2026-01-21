namespace Onvif_GetPhoto.DTO 
{
    /// <summary>
    /// 设备信息数据传输对象（DTO）
    /// 用于封装设备信息的JSON数据
    /// </summary>
    public class DeviceInformationDto
    {
        public string Manufacturer { get; set; }      // 厂商
        public string Model { get; set; }             // 型号
        public string FirmwareVersion { get; set; }   // 固件版本
        public string SerialNumber { get; set; }      // 序列号
        public string HardwareId { get; set; }        // 硬件ID
    }
}