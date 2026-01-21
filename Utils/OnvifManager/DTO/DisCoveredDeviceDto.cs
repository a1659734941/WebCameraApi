using System;
using System.Collections.Generic;
/// <summary>
/// 发现设备DTO基础信息
/// </summary>
namespace Onvif_GetPhoto.DTO
{
    public class DisCoveredDeviceDto
    {
        public string DiscDev_Name { get; set; } // 设备名称    
        public string DiscDev_Hardware { get; set; } // 硬件型号
        public string DiscDev_Ipv4Address { get; set; } // IPv4地址
        public string DiscDev_Ipv6Address { get; set; } // IPv6地址
        public string DiscDev_Remote { get; set; } // 远程地址
        public string DiscDev_Port { get; set; } // 端口号
        public string DiscDev_Types { get; set; } // 设备类型
    }
}