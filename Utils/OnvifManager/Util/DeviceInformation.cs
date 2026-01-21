using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using XiaoFeng.Onvif;
using Onvif_GetPhoto.DTO;  // 引用DTO命名空间

namespace Onvif_GetPhoto.Util
{
    public class GetDeviceInfo
    {
        /// <summary>
        /// 获取Onvif设备信息（带重连重试+等待功能）
        /// </summary>
        /// <param name="ip">设备IP</param>
        /// <param name="user">设备用户名</param>
        /// <param name="password">设备密码</param>
        /// <param name="port">设备端口</param>
        /// <param name="retryCount">重试次数（传入0则不重试，仅尝试1次）</param>
        /// <param name="waitMilliseconds">每次重试前的等待时间（毫秒）</param>
        /// <returns>异步任务</returns>
        // 修正方法名规范：PascalCase（首字母大写）
        public static async Task GetDeviceInfoAsync(string ip, string user, string password, int port, 
                                                   int retryCount = 3, int waitMilliseconds = 1000)
        {
            // 记录当前重试次数（初始为0，代表第一次尝试）
            int currentRetry = 0;
            bool isSuccess = false;

            // 循环重试：直到成功 或 重试次数用尽
            while (currentRetry <= retryCount && !isSuccess)
            {
                try
                {
                    // 标记当前是第几次尝试（方便日志输出）
                    int attemptNum = currentRetry + 1;
                    Console.WriteLine($"【第{attemptNum}次尝试】正在连接设备：{ip}");

                    var iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                    
                    // 获取设备时间（用于Onvif认证时间同步）
                    var onvifUTCDateTime = await DeviceUtcTime.getDeviceUtcTimeAsync(ip, port, "中国");
                    Console.WriteLine($"【第{attemptNum}次尝试】设备时间：{onvifUTCDateTime.ToString()}");

                    // 获取设备信息JSON
                    var resultJson = await DeviceService.GetDeviceInformation(iPEndPoint, user, password, onvifUTCDateTime);
                    
                    // 判断是否为有效结果
                    if (IsErrorResult(resultJson))
                    {
                        throw new Exception("返回无效数据（非JSON格式/空数据/错误信息）");
                    }

                    // 反序列化为DTO对象（使用DeviceInformationDto）
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var deviceInfo = JsonSerializer.Deserialize<DeviceInformationDto>(resultJson, options);

                    if (deviceInfo == null)
                    {
                        throw new Exception("设备信息JSON解析失败");
                    }

                    // 执行到这里代表成功，输出设备信息
                    Console.WriteLine("==================== 设备信息详情 ====================");
                    Console.WriteLine($"厂商：{deviceInfo.Manufacturer}");
                    Console.WriteLine($"型号：{deviceInfo.Model}");
                    Console.WriteLine($"固件版本：{deviceInfo.FirmwareVersion}");
                    Console.WriteLine($"序列号：{deviceInfo.SerialNumber}");
                    Console.WriteLine($"硬件ID：{deviceInfo.HardwareId}");
                    Console.WriteLine("======================================================");

                    // 标记成功，退出循环
                    isSuccess = true;
                }
                catch (Exception ex)
                {
                    // 计算剩余重试次数
                    int remainingRetry = retryCount - currentRetry;
                    Console.WriteLine($"【第{currentRetry + 1}次尝试失败】原因：{ex.Message}");

                    // 如果还有重试次数，等待后继续重试
                    if (remainingRetry > 0)
                    {
                        Console.WriteLine($"将等待{waitMilliseconds}毫秒后进行第{currentRetry + 2}次尝试，剩余重试次数：{remainingRetry}");
                        await Task.Delay(waitMilliseconds); // 异步等待，不阻塞线程
                        currentRetry++;
                    }
                    else
                    {
                        // 重试次数用尽，最终失败
                        Console.WriteLine($"【最终失败】已用尽所有重试次数（共{retryCount + 1}次尝试），无法获取{ip}设备信息");
                        isSuccess = false;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 判断返回结果是否为错误结果
        /// </summary>
        /// <param name="result">接口返回的字符串</param>
        /// <returns>true=错误结果，false=有效结果</returns>
        private static bool IsErrorResult(string result)
        {
            return string.IsNullOrWhiteSpace(result) ||
                   result.StartsWith("<") ||  // XML格式（通常是错误响应）
                   result.Contains("Exception") || 
                   result.Contains("Fault") || 
                   result.Length < 50;
        }
    }
}