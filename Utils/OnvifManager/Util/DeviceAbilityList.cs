using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Onvif_GetPhoto.DTO;
using XiaoFeng.Onvif;

namespace Onvif_GetPhoto.Util
{
    public class DeviceAbilityList
    {
        public static async Task getDeviceAbilityList(string ip,int port)
        {
            var _ip = ip;
            var _port = port;
            var iPEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
            var abilityUrls = await DeviceService.GetCapabilities(iPEndPoint); // 获取服务URL列表

            if (abilityUrls != null)
            {
                foreach (var url in abilityUrls)
                {
                    // 用URL创建实体，直接打印（利用重写的ToString）
                    var deviceAbility = new DeviceAbilityListDto(url);
                    Console.WriteLine(deviceAbility);
                }
            }

        }
    }
}