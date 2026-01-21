using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using XiaoFeng.Onvif;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Diagnostics;

namespace Onvif_GetPhoto.Util
{
    public class OnvifMediaService
    {
        // 常见的Onvif默认端口列表（按优先级排序）
        private static readonly int[] DefaultOnvifPorts = { 80, 8080, 7001, 5000, 9000, 6666 };

        // 默认重试次数
        private const int DefaultRetryCount = 2;

        // 重试间隔（毫秒）
        private const int RetryDelayMs = 1000;

        /// <summary>
        /// 检测IP+端口是否可达
        /// </summary>
        /// <param name="ip">设备IP</param>
        /// <param name="port">端口号</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>是否可达</returns>
        private static async Task<bool> CheckIpPortReachable(string ip, int port, int timeoutMs = 3000)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(timeoutMs);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    stopwatch.Stop();
                    if (completedTask == timeoutTask)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{port}] 端口连接超时（耗时：{stopwatch.ElapsedMilliseconds}ms，超时阈值：{timeoutMs}ms）");
                        return false;
                    }

                    // 验证连接是否成功
                    if (client.Connected)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{port}] 端口连接成功（耗时：{stopwatch.ElapsedMilliseconds}ms）");
                        client.Close();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{port}] 端口连接失败：未建立有效连接");
                        return false;
                    }
                }
            }
            catch (SocketException ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{port}] 端口连接失败（Socket异常，耗时：{stopwatch.ElapsedMilliseconds}ms）：{ex.SocketErrorCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{port}] 连通性检测异常（耗时：{stopwatch.ElapsedMilliseconds}ms）：{ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 自动检测Onvif可用端口
        /// </summary>
        /// <param name="ip">设备IP</param>
        /// <param name="preferredPort">首选端口（用户传入的端口）</param>
        /// <returns>可用端口，无则返回-1</returns>
        private static async Task<int> AutoDetectOnvifPort(string ip, int preferredPort)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 开始端口自动检测，首选端口：{preferredPort}");

            // 1. 先尝试首选端口
            if (preferredPort > 0 && await CheckIpPortReachable(ip, preferredPort))
            {
                return preferredPort;
            }

            // 2. 首选端口不可用，检测默认端口列表
            foreach (var port in DefaultOnvifPorts)
            {
                // 跳过已经尝试过的首选端口
                if (port == preferredPort) continue;

                if (await CheckIpPortReachable(ip, port))
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 自动检测到可用端口：{port}");
                    return port;
                }
            }

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 所有端口检测完毕，未找到可用的Onvif端口");
            return -1;
        }

        /// <summary>
        /// 带重试的通用执行方法
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="retryCount">重试次数</param>
        /// <returns>操作结果，失败则返回默认值</returns>
        private static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, string operationName, int retryCount = DefaultRetryCount)
        {
            int attempt = 0;
            while (attempt <= retryCount)
            {
                attempt++;
                try
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 执行操作：{operationName}（第{attempt}次尝试）");
                    var result = await action();
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 操作[{operationName}]执行成功（第{attempt}次尝试）");
                    return result;
                }
                catch (Exception ex)
                {
                    if (attempt > retryCount)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 操作[{operationName}]执行失败（已重试{retryCount}次）：{ex.GetType().Name} - {ex.Message}");
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 错误堆栈：{ex.StackTrace}");
                        return default;
                    }

                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 操作[{operationName}]第{attempt}次执行失败，{RetryDelayMs}ms后重试：{ex.Message}");
                    await Task.Delay(RetryDelayMs);
                }
            }
            return default;
        }

        /// <summary>
        /// 获取Onvif媒体服务信息（视频流、快照地址）
        /// </summary>
        /// <param name="ip">设备IP</param>
        /// <param name="user">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="port">端口（会自动检测）</param>
        /// <returns>媒体服务信息字典</returns>
        public static async Task<Dictionary<string, Dictionary<string, string>>> getMediaService(string ip, string user, string password, int port)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ====== 开始获取[{ip}]的Onvif媒体服务信息 ======");

                // 步骤1：自动检测可用端口
                int availablePort = await AutoDetectOnvifPort(ip, port);
                if (availablePort == -1)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 无可用端口，终止操作");
                    return null;
                }

                // 步骤2：构建端点（使用检测到的可用端口）
                var endPoint = new IPEndPoint(IPAddress.Parse(ip), availablePort);
                // 移除UTC时间强依赖，使用本地时间
                DateTime localTime = DateTime.Now;
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{availablePort}] 使用本地时间替代UTC时间：{localTime:yyyy-MM-dd HH:mm:ss}");

                // 步骤3：获取Profiles（带重试）
                var profileTokens = await ExecuteWithRetry(() =>
                    MediaService.GetProfiles(endPoint, user, password, localTime),
                    "获取Profiles列表");

                if (profileTokens == null || !profileTokens.Any())
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{availablePort}] 未获取到Profiles信息（可能是用户名/密码错误、Onvif服务未开启或设备不兼容）");
                    return null;
                }

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{availablePort}] 共获取到{profileTokens.Count}个Profile Token:\n{string.Join("\n", profileTokens)}");

                // 步骤4：遍历获取每个Token的流地址和快照地址
                var mediaServiceDict = new Dictionary<string, Dictionary<string, string>>();
                foreach (var token in profileTokens)
                {
                    mediaServiceDict[token] = new Dictionary<string, string>();
                    Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ----- 处理Token [{token}] -----");

                    // 获取视频流地址（带重试）
                    var streamUri = await ExecuteWithRetry(() =>
                        MediaService.GetStreamUri(endPoint, user, password, localTime, token),
                        $"获取Token[{token}]的视频流地址");
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{availablePort}] Token[{token}] 视频流地址: {streamUri ?? "获取失败"}");

                    // 获取快照地址（带重试）
                    var snapshotUri = await ExecuteWithRetry(() =>
                        MediaService.GetSnapshotUri(endPoint, user, password, localTime, token),
                        $"获取Token[{token}]的快照地址");
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}:{availablePort}] Token[{token}] 快照地址: {snapshotUri ?? "获取失败"}");

                    // 仅存储成功获取的地址
                    if (!string.IsNullOrEmpty(streamUri))
                        mediaServiceDict[token].Add("视频流地址", streamUri);
                    if (!string.IsNullOrEmpty(snapshotUri))
                        mediaServiceDict[token].Add("快照地址", snapshotUri);
                }

                stopwatch.Stop();
                Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ====== [{ip}:{availablePort}] 媒体服务信息获取完成（总耗时：{stopwatch.ElapsedMilliseconds}ms）======");
                return mediaServiceDict;
            }
            catch (UnauthorizedAccessException ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 认证错误（总耗时：{stopwatch.ElapsedMilliseconds}ms）：用户名/密码错误 - {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
                return null;
            }
            catch (SocketException ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 网络错误（总耗时：{stopwatch.ElapsedMilliseconds}ms）：{ex.SocketErrorCode} - {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{ip}] 未知错误（总耗时：{stopwatch.ElapsedMilliseconds}ms）：{ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
                return null;
            }
        }
    }
}