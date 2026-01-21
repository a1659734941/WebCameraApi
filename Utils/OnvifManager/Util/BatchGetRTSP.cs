using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using PostgreConfig;

namespace Onvif_GetPhoto.Util
{
    /// <summary>
    /// 批量获取多个摄像头RTSP地址工具类
    /// </summary>
    public class BatchGetRTSP
    {
        /// <summary>
        /// 批量获取摄像头RTSP地址（视频流地址）
        /// </summary>
        /// <param name="cameraConfigDict">摄像头配置字典
        /// 结构：Key=摄像头标识（如camera_1），Value=配置字典（ip/user/password/port/retryCount/waitMilliseconds）
        /// </param>
        /// <returns>结果字典：Key=摄像头标识，Value=RTSP地址（失败时为null）</returns>
        public static async Task<Dictionary<string, string>> BatchGetRTSPAddresses(
            Dictionary<string, Dictionary<string, object>> cameraConfigDict)
        {
            // 最终返回结果字典（Key=摄像头标识，Value=RTSP地址）
            var resultDict = new Dictionary<string, string>();

            if (cameraConfigDict == null || !cameraConfigDict.Any())
            {
                Console.WriteLine("摄像头配置字典为空，无需处理");
                return resultDict;
            }

            Console.WriteLine($"开始批量处理 {cameraConfigDict.Count} 个摄像头的RTSP地址获取请求");

            // 遍历每个摄像头配置，调用单个获取方法处理
            foreach (var cameraItem in cameraConfigDict)
            {
                string cameraKey = cameraItem.Key;
                var cameraConfig = cameraItem.Value;

                // 解析配置项（带默认值）
                string ip = cameraConfig.ContainsKey("ip") ? cameraConfig["ip"].ToString() : string.Empty;
                string user = cameraConfig.ContainsKey("user") ? cameraConfig["user"].ToString() : string.Empty;
                string password = cameraConfig.ContainsKey("password") ? cameraConfig["password"].ToString() : string.Empty;
                int port = cameraConfig.ContainsKey("port") ? Convert.ToInt32(cameraConfig["port"]) : 80;
                int retryCount = cameraConfig.ContainsKey("retryCount") ? Convert.ToInt32(cameraConfig["retryCount"]) : 3;
                int waitMilliseconds = cameraConfig.ContainsKey("waitMilliseconds") ? Convert.ToInt32(cameraConfig["waitMilliseconds"]) : 1000;

                // 调用单个获取方法
                string rtspAddress = await GetSingleCameraRTSPAsync(cameraKey, ip, user, password, port, retryCount, waitMilliseconds);
                resultDict.Add(cameraKey, rtspAddress);
            }

            // 输出汇总信息
            Console.WriteLine("\n===== 批量处理汇总 =====");
            var successCount = resultDict.Values.Count(r => !string.IsNullOrEmpty(r));
            var failedCount = resultDict.Count - successCount;
            Console.WriteLine($"总处理数：{resultDict.Count} | 成功数：{successCount} | 失败数：{failedCount}");

            return resultDict;
        }

        /// <summary>
        /// 单独获取单个摄像头的RTSP地址（对外公开接口）
        /// </summary>
        /// <param name="cameraKey">摄像头唯一标识（用于日志输出）</param>
        /// <param name="ip">摄像头IP地址</param>
        /// <param name="user">摄像头用户名</param>
        /// <param name="password">摄像头密码</param>
        /// <param name="port">摄像头Onvif端口（默认80）</param>
        /// <param name="retryCount">重试次数（默认3）</param>
        /// <param name="waitMilliseconds">重试间隔（毫秒，默认1000）</param>
        /// <returns>RTSP地址（失败返回null）</returns>
        public static async Task<string> GetSingleRTSPAddressAsync(
            string cameraName,
            string ip,
            string user = "",
            string password = "",
            int port = 80,
            int retryCount = 3,
            int waitMilliseconds = 1000)
        {
            return await GetSingleCameraRTSPAsync(cameraName, ip, user, password, port, retryCount, waitMilliseconds);
        }

        /// <summary>
        /// 单个摄像头RTSP地址获取的核心逻辑（私有复用方法）
        /// </summary>
        /// <param name="cameraName">摄像头标识</param>
        /// <param name="ip">IP地址</param>
        /// <param name="user">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="port">端口</param>
        /// <param name="retryCount">重试次数</param>
        /// <param name="waitMilliseconds">重试间隔</param>
        /// <returns>RTSP地址（失败返回null）</returns>
        private static async Task<string> GetSingleCameraRTSPAsync(
            string cameraName,
            string ip,
            string user,
            string password,
            int port,
            int retryCount,
            int waitMilliseconds)
        {
            string rtspAddress = null;

            try
            {
                // 基础参数校验
                if (string.IsNullOrEmpty(ip))
                {
                    Console.WriteLine($"[{cameraName}] 配置错误：IP地址为空");
                    return rtspAddress;
                }

                Console.WriteLine($"\n===== 开始处理摄像头 [{cameraName}] =====");
                Console.WriteLine($"配置信息：IP={ip}, Port={port}, 重试次数={retryCount}, 重试间隔={waitMilliseconds}ms");

                // 重试逻辑获取RTSP地址
                Dictionary<string, Dictionary<string, string>> rtspResult = null;
                int currentRetry = 0;
                bool isSuccess = false;

                while (currentRetry < retryCount && !isSuccess)
                {
                    try
                    {
                        currentRetry++;
                        Console.WriteLine($"[{cameraName}] 第 {currentRetry}/{retryCount} 次尝试获取RTSP地址");

                        // 调用现有MediaService获取RTSP地址
                        rtspResult = await OnvifMediaService.getMediaService(ip, user, password, port);

                        if (rtspResult != null && rtspResult.Any())
                        {
                            isSuccess = true;
                            Console.WriteLine($"[{cameraName}] 成功获取RTSP地址");
                            // 提取第一个Token对应的视频流地址
                            var firstTokenItem = rtspResult.First();
                            rtspAddress = firstTokenItem.Value["视频流地址"];
                        }
                        else
                        {
                            Console.WriteLine($"[{cameraName}] 第 {currentRetry} 次尝试未获取到有效RTSP地址");
                            if (currentRetry < retryCount)
                            {
                                Console.WriteLine($"[{cameraName}] 等待 {waitMilliseconds}ms 后重试...");
                                await Task.Delay(waitMilliseconds);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{cameraName}] 第 {currentRetry} 次尝试异常：{ex.Message}");
                        if (currentRetry < retryCount)
                        {
                            Console.WriteLine($"[{cameraName}] 等待 {waitMilliseconds}ms 后重试...");
                            await Task.Delay(waitMilliseconds);
                        }
                    }
                }

                // 处理最终结果
                if (isSuccess && !string.IsNullOrEmpty(rtspAddress))
                {
                    Console.WriteLine($"[{cameraName}] 最终获取到RTSP地址：{rtspAddress}");
                }
                else
                {
                    Console.WriteLine($"[{cameraName}] 重试 {retryCount} 次后仍未获取到RTSP地址");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{cameraName}] 处理异常：{ex.Message}");
            }
            finally
            {
                Console.WriteLine($"===== 结束处理摄像头 [{cameraName}] =====\n");
            }

            return rtspAddress;
        }

        /// <summary>
        /// 将摄像头配置字典转换为批量获取RTSP地址的请求字典
        /// </summary>
        /// <param name="cameraConfigs">原摄像头配置字典</param>
        /// <returns>BatchGetRTSPAddresses所需的字典</returns>
        public static Dictionary<string, Dictionary<string, object>> ConvertToRtspRequestDict(
            Dictionary<string, OnvifCameraInfomation> cameraConfigs)
        {
            if (cameraConfigs == null) return new Dictionary<string, Dictionary<string, object>>();

            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var kvp in cameraConfigs)
            {
                var innerDict = new Dictionary<string, object>
                {
                    ["ip"] = kvp.Value.CameraIP,
                    ["user"] = kvp.Value.CameraUser,
                    ["password"] = kvp.Value.CameraPassword,
                    ["port"] = kvp.Value.CameraPort,
                    ["retryCount"] = kvp.Value.CameraRetryCount,
                    ["waitMilliseconds"] = kvp.Value.CameraWaitmillisecounds
                };
                result.Add(kvp.Key, innerDict);
            }
            return result;
        }
    }
}