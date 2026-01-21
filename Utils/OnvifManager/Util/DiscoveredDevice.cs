using System;
using System.Threading.Tasks;
using XiaoFeng.Onvif;
using Newtonsoft.Json.Linq;
using System.IO.Pipes;
using System.Formats.Asn1;
using Onvif_GetPhoto.DTO;
using Newtonsoft.Json;
namespace Onvif_GetPhoto.Util
{
    public class DiscoveredDevice
    {
        public static async Task getDiscoveredDevice()
        {
            DisCoveredDeviceDto discoveredDevice = new DisCoveredDeviceDto();
            try
            {
                string deviceJson = await DeviceService.DiscoveryOnvif(timeoutSecond: 5); // udp多播，设备发现方法等待30s
                if (!string.IsNullOrEmpty(deviceJson))
                {
                    JArray devices = JArray.Parse(deviceJson);
                    Console.WriteLine("发现设备数量：" + devices.Count);
                    // Console.WriteLine("device原始数据" + devices);
                    foreach (JObject device in devices)
                    {
                        discoveredDevice.DiscDev_Name = device["Name"].ToString();
                        discoveredDevice.DiscDev_Hardware = device["Hardware"].ToString();
                        discoveredDevice.DiscDev_Ipv4Address = device["Ipv4Address"].ToString();
                        discoveredDevice.DiscDev_Ipv6Address = device["Ipv6Address"].ToString();
                        discoveredDevice.DiscDev_Remote = device["Remote"].ToString();
                        discoveredDevice.DiscDev_Port = device["Port"].ToString();
                        discoveredDevice.DiscDev_Types = device["Types"].ToString();
                        // Console.WriteLine("设备名称：" + discoveredDevice.DiscDev_Name);
                        Console.WriteLine($"========以下是设备{discoveredDevice.DiscDev_Name}的信息==========");
                        Console.WriteLine("设备硬件型号：" + discoveredDevice.DiscDev_Hardware);
                        Console.WriteLine("设备IPv4地址：" + discoveredDevice.DiscDev_Ipv4Address);
                        Console.WriteLine("设备IPv6地址：" + discoveredDevice.DiscDev_Ipv6Address);
                        Console.WriteLine("设备远程地址：" + discoveredDevice.DiscDev_Remote);
                        Console.WriteLine("设备端口号：" + discoveredDevice.DiscDev_Port);
                        Console.WriteLine("设备类型：" + discoveredDevice.DiscDev_Types);
                    }
                }
                else
                {
                    Console.WriteLine("未发现设备");
                }
            
            }catch (Exception ex)
            {
                Console.WriteLine($"发现设备失败：{ex.Message}");

            }
        }
    }
}