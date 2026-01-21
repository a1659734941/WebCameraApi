using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using XiaoFeng.Onvif;

namespace Onvif_GetPhoto.Util
{
    public class DeviceUtcTime
    {
        // 地址与UTC偏移量的映射（正数为GMT+，负数为GMT-，单位：小时）
        // 注：适用于无夏令时调整的地区（如中国、日本等）
        private static readonly Dictionary<string, int> _locationUtcOffsetMap = new Dictionary<string, int>
        {
            {"中国", 8},         // 中国：GMT+8
            {"日本", 9},         // 日本：GMT+9
            {"韩国", 9},         // 韩国：GMT+9
            {"英国", 0},         // 英国（非夏令时）：GMT±0
            {"美国纽约", -5},    // 美国纽约（非夏令时）：GMT-5
        };

        /// <summary>
        /// 离线环境下，根据地址获取设备对应时区的本地时间（基于固定UTC偏移量）
        /// </summary>
        /// <param name="ip">设备IP</param>
        /// <param name="port">设备端口</param>
        /// <param name="location">地址（如"中国"）</param>
        /// <returns>目标时区的本地时间（格式化字符串）</returns>
        /// <exception cref="ArgumentException">不支持的地址</exception>
        public static async Task<DateTime> getDeviceUtcTimeAsync(string ip, int port, string location)
        {
            // 1. 获取设备UTC时间
            var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var utcTime = await DeviceService.GetSystemDateAndTime(endPoint);

            // 确保时间被标记为UTC（避免计算歧义）
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }

            // 2. 根据地址获取UTC偏移量
            if (!_locationUtcOffsetMap.TryGetValue(location, out int utcOffsetHours))
            {
                throw new ArgumentException($"不支持的地址：{location}，请添加到偏移量映射表");
            }

            // 3. 计算目标时区时间（UTC时间 + 偏移量）
            var localTime = utcTime.AddHours(utcOffsetHours);
            
            // 4. 格式化输出
            return localTime;
        }
    }
}