using ConfigGet;
using HikAlarmEndPoints;
using Microsoft.AspNetCore.Hosting; // 核心：用于获取项目根目录
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PostgreConfig;
using Serilog;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using WebCameraApi.Utils.PostgreConfig.Dto;

namespace WebCameraApi.Services
{
    public class HikAlarmService
    {
        // 1. 并发安全字典：替代普通Dictionary，适配多线程高并发读写
        private readonly ConcurrentDictionary<string, HikAlarmBindDto> _alarmBindDic = new();
        private readonly ILogger<HikAlarmService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        // 2. 防抖缓存：避免同一IP短时间重复发送相同IsBlock请求（默认1000ms防抖）
        private readonly ConcurrentDictionary<string, (bool IsBlock, DateTime LastSendTime)> _requestDebounceCache = new();
        private const int _debounceMilliseconds = 1000;
        private const int _httpRequestTimeoutMs = 3000; // HTTP请求超时时间（3秒）
        private readonly PgHikAlarmRecordConfigRepository _PgHikAlarmRecord = new PgHikAlarmRecordConfigRepository();
        private string _connectionString;
        // 新增：项目根目录 + 图片保存最终路径
        private readonly string _projectRootPath; // 项目地址（根目录）
        private readonly string _snapshotSaveBasePath; // 最终路径：项目根目录\wwwroot\HikAlarmSnapshotBase64
        private readonly Dictionary<string, string> _eventTypeTransDict = new Dictionary<string, string>
        {
            { "uniformDetection", "制服检测" },
            { "peopleNumCounting", "人数统计" },
            { "overtimeTarry", "超时逗留" },
            { "standUp", "起立检测" },
            { "advReachHeight", "攀高检测" },
        };
        // 3. 构造函数注入核心依赖
        public HikAlarmService(ILogger<HikAlarmService> logger,
                               IHttpClientFactory httpClientFactory,
                               IWebHostEnvironment webHostEnvironment) // 用于获取项目根目录
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            // 第一步：获取项目根目录（项目地址）
            _projectRootPath = webHostEnvironment.ContentRootPath;
            _logger.LogInformation("获取到项目根目录（项目地址）：{ProjectRootPath}", _projectRootPath);

            // 第二步：手动拼接 wwwroot + 图片存储目录
            var wwwrootPath = Path.Combine(_projectRootPath, "wwwroot"); // 项目地址拼接wwwroot
            _snapshotSaveBasePath = Path.Combine(wwwrootPath, "HikAlarmSnapshotBase64"); // 最终保存路径

            // 确保目录存在（不存在则创建）
            Directory.CreateDirectory(_snapshotSaveBasePath);
            _logger.LogInformation("图片保存目录初始化完成：{SnapshotPath}", _snapshotSaveBasePath);

            InitializeServiceAsync().Wait();
        }

        public async Task InitializeServiceAsync()
        {
            try
            {
                dynamic dbConfigDynamic = Appsettings_Get.GetConfigByKey("PostresSQLConfig");
                if (dbConfigDynamic == null)
                {
                    _logger.LogError("未读取到PostresSQLConfig配置，HikAlarmService初始化失败");
                    return;
                }

                PgConnectionOptions pgConnectionOptions = new PgConnectionOptions();
                pgConnectionOptions.Host = dbConfigDynamic.host;
                pgConnectionOptions.Port = dbConfigDynamic.port;
                pgConnectionOptions.Username = dbConfigDynamic.username;
                pgConnectionOptions.Password = dbConfigDynamic.password;
                pgConnectionOptions.Database = dbConfigDynamic.database;
                _connectionString = pgConnectionOptions.BuildConnectionString();

                var alarmBincRepository = new PgHikAlarmBindConfigRepository();
                var alarmBindList = await alarmBincRepository.GetAllHikAlarmBindsAsync(_connectionString);

                // 清空并批量写入并发字典
                _alarmBindDic.Clear();
                foreach (var item in alarmBindList)
                {
                    _alarmBindDic.TryAdd(item.Key, item.Value);
                }

                _logger.LogInformation("HikAlarmService 初始化完成，加载绑定配置数：{ConfigCount}", _alarmBindDic.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HikAlarmService初始化失败");
                throw; // 向上抛出，让启动流程感知初始化失败
            }
        }

        // 异步方法命名规范：加Async后缀
        public async Task GetCountingCameraAsync(HttpContext context)
        {
            // 空值校验：前置拦截无效请求
            if (context == null)
            {
                _logger.LogWarning("HttpContext为空，跳过处理");
                return;
            }

            try
            {
                var remoteIp = context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
                // 无效IP直接返回（仅保留关键Warning日志）
                if (string.IsNullOrWhiteSpace(remoteIp))
                {
                    _logger.LogWarning("无法解析客户端IP，跳过处理");
                    return;
                }

                // 并发安全的存在性检查
                if (!_alarmBindDic.TryGetValue(remoteIp, out var bindDto))
                {
                    // 高频率场景下，非关键日志降级为Debug（生产环境可关闭）
                    _logger.LogDebug("IP[{RemoteIp}]无绑定配置，跳过处理", remoteIp);
                    return;
                }

                // 表单读取：增加超时控制（避免阻塞）
                var form = await context.Request.ReadFormAsync(new CancellationTokenSource(1000).Token); // 1秒超时
                if (!form.TryGetValue("personQueueCounting", out var personCountingJson) || string.IsNullOrEmpty(personCountingJson))
                {
                    _logger.LogWarning("IP[{RemoteIp}]未找到personQueueCounting数据或数据为空", remoteIp);
                    return;
                }

                // JSON解析：使用池化JsonDocument，减少内存分配
                using var doc = JsonDocument.Parse(personCountingJson.ToString(), new JsonDocumentOptions { AllowTrailingCommas = true });
                int personCount;
                try
                {
                    var regionCapture = doc.RootElement.GetProperty("RegionCapture");
                    var humanCounting = regionCapture.GetProperty("humanCounting");
                    var countElement = humanCounting.GetProperty("count");
                    personCount = countElement.GetInt32();
                }
                catch (KeyNotFoundException ex)
                {
                    _logger.LogWarning(ex, "IP[{RemoteIp}]JSON解析缺失字段，完整JSON：{JsonData}", remoteIp, personCountingJson);
                    return;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "IP[{RemoteIp}]JSON字段类型错误，完整JSON：{JsonData}", remoteIp, personCountingJson);
                    return;
                }

                bool isBlock = personCount < 3;
                // 防抖校验：避免短时间重复发送相同请求
                if (IsDebounceValid(remoteIp, isBlock))
                {
                    // 仅保留关键Info日志，非关键日志降级为Debug
                    _logger.LogInformation($"IP[{remoteIp}]人数[{personCount}]，准备发送{(isBlock ? "锁定" : "解锁")}请求");

                    await SendBlockRequestAsync(bindDto.BlockComputerIP, isBlock, remoteIp, personCount);
                }
                else
                {
                    _logger.LogDebug("IP[{RemoteIp}]防抖校验未通过，跳过重复请求（IsBlock：{IsBlock}）", remoteIp, isBlock);
                }
            }
            catch (OperationCanceledException ex)
            {
                // 超时异常：仅记录Warning，避免Error日志泛滥
                _logger.LogWarning(ex, "处理请求超时");
            }
            catch (HttpRequestException ex)
            {
                // HTTP请求异常：关联IP记录，便于定位问题
                var remoteIp = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "未知IP";
                _logger.LogError(ex, "IP[{RemoteIp}]发送锁定/解锁请求时出现网络异常", remoteIp);
            }
            catch (Exception ex)
            {
                var remoteIp = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "未知IP";
                _logger.LogError(ex, "IP[{RemoteIp}]处理计数摄像头请求失败", remoteIp);
            }
        }

        /// <summary>
        /// 防抖校验：同一IP短时间内相同IsBlock请求不重复发送
        /// </summary>
        private bool IsDebounceValid(string remoteIp, bool isBlock)
        {
            if (_requestDebounceCache.TryGetValue(remoteIp, out var cacheItem))
            {
                // 相同IsBlock且间隔小于防抖时间 → 跳过
                if (cacheItem.IsBlock == isBlock && DateTime.Now - cacheItem.LastSendTime < TimeSpan.FromMilliseconds(_debounceMilliseconds))
                {
                    return false;
                }
            }
            // 更新缓存（并发安全）
            _requestDebounceCache.AddOrUpdate(remoteIp,
                (isBlock, DateTime.Now),
                (key, old) => (isBlock, DateTime.Now));
            return true;
        }

        /// <summary>
        /// 通用发送锁定/解锁请求（复用HttpClient，带超时控制）
        /// </summary>
        private async Task SendBlockRequestAsync(string computerIP, bool isBlock, string remoteIp, int personCount)
        {
            if (string.IsNullOrWhiteSpace(computerIP))
            {
                _logger.LogWarning("IP[{RemoteIp}]绑定的BlockComputerIP为空，无法发送请求", remoteIp);
                return;
            }

            // 复用HttpClientFactory创建的客户端（避免每次从Context获取）
            var httpClient = _httpClientFactory.CreateClient("hik");
            httpClient.Timeout = TimeSpan.FromMilliseconds(_httpRequestTimeoutMs); // 设置超时

            var blockUrl = new UriBuilder($"http://{computerIP}:7410/blockinput").Uri;
            var requestBody = new { IsBlock = isBlock };

            // 预编译JSON序列化选项，减少内存分配
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(blockUrl, content);
                // 响应日志降级为Debug（仅保留失败的Error日志）
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug($"IP[{remoteIp}]向[{blockUrl}]发送{(isBlock ? "锁定" : "解锁")}请求成功，响应码：{response.StatusCode}");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"IP[{remoteIp}]向[{blockUrl}]发送{(isBlock ? "锁定" : "解锁")}请求失败，响应码：{response.StatusCode}，响应内容：{responseContent}");
                }
            }
            catch (TaskCanceledException ex)
            {
                // HTTP请求超时：单独捕获，避免混入通用异常
                _logger.LogWarning(ex, "IP[{RemoteIp}]向[{BlockUrl}]发送请求超时（超时时间：{TimeoutMs}ms）",
                    remoteIp, blockUrl, _httpRequestTimeoutMs);
            }
        }

        public async Task GetBehaviorAnalysisJson(HttpContext context)
        {
            string jsonString = string.Empty;
            try
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                jsonString = await reader.ReadToEndAsync();

                // 修正语法错误：空值/格式校验
                if (string.IsNullOrWhiteSpace(jsonString) || (!jsonString.TrimStart().StartsWith('{') && !jsonString.TrimStart().StartsWith('[')))
                {
                    _logger.LogError($"无效的 JSON 格式 - 原始数据: {jsonString}");
                    return;
                }

                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // 修正语法错误：闭合括号
                if (!root.TryGetProperty("eventType", out var eventTypeProp) || !root.TryGetProperty("dateTime", out var dateTimeProp))
                {
                    _logger.LogError($"缺少必要字段: eventType/dateTime");
                    return;
                }

                var eventType = eventTypeProp.GetString();
                if (string.IsNullOrEmpty(eventType))
                {
                    _logger.LogError("eventType 为空");
                    return;
                }

                if (!DateTime.TryParse(dateTimeProp.GetString(), out var eventTime))
                {
                    _logger.LogError($"无效的时间日期格式 : {dateTimeProp.GetString()}");
                    return;
                }

                var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("hik");
                var record = new HikAlarmRecordDto();
                record.Id = Guid.NewGuid();
                record.EventType = eventType;
                record.EventTime = eventTime;

                // 获取设备名称
                var deviceName = root.TryGetProperty("targetAttrs", out var attrs) && attrs.TryGetProperty("deviceName", out var deviceNameProp)
                          ? deviceNameProp.GetString() ?? "未知设备"
                          : "未知设备";

                record.DeviceName = deviceName;
                record.ChannelName = attrs.TryGetProperty("channelName", out var channelName)
                          ? channelName.GetString()
                          : null;
                record.TaskName = attrs.TryGetProperty("taskname", out var taskName)
                          ? taskName.GetString()
                          : null;

                // 核心修改：下载图片到本地，获取文件路径（替代Base64）
                var imageUrl = ExtractImageUrl(root, eventType);
                record.SnapshotBase64Path = await DownloadImageToLocalAsync(httpClient, imageUrl, deviceName, eventType, eventTime);

                record.RawData = root.ToString()!;

                bool InsertSuccess = await _PgHikAlarmRecord.InsertHikAlarmRecordAsync(_connectionString, record);
                if (InsertSuccess)
                {
                    _logger.LogInformation("新增一条行为分析记录到数据库中，图片路径：{ImagePath}", record.SnapshotBase64Path);
                    return;
                }
                else
                {
                    _logger.LogError("新增行为分析记录到数据库中失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理行为分析JSON失败，原始数据：{JsonData}", jsonString);
            }
        }

        /// <summary>
        /// 提取图片URL
        /// </summary>
        private static string ExtractImageUrl(JsonElement root, string eventType)
        {
            try
            {
                // 统一使用PascalCase匹配属性名
                var pascalEventType = char.ToUpper(eventType[0]) + eventType[1..];

                if (root.TryGetProperty(pascalEventType, out var eventObj) &&
                    eventObj.TryGetProperty("BackgroundImage", out var bgImage) &&
                    bgImage.TryGetProperty("resourcesContent", out var url))
                {
                    return url.GetString()!;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "提取图片URL时发生异常");
            }
            return string.Empty;
        }

        /// <summary>
        /// 核心修改：下载图片到本地指定目录，返回文件相对路径
        /// </summary>
        private async Task<string> DownloadImageToLocalAsync(HttpClient client, string url, string deviceName, string eventType, DateTime eventTime)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning("图片URL为空，跳过下载");
                    return string.Empty;
                }

                // 1. 生成安全的文件名（替换非法字符）
                var safeDeviceName = ReplaceInvalidFileNameChars(deviceName);
                var safeEventType = ReplaceInvalidFileNameChars(eventType);
                var timeStr = eventTime.ToString("yyyyMMddHHmmssfff"); // 精确到毫秒避免重复
                var fileName = $"{safeDeviceName}_{safeEventType}_{timeStr}.jpg";

                // 基于「项目根目录拼接的路径」保存文件
                var saveAbsolutePath = Path.Combine(_snapshotSaveBasePath, fileName);
                // 数据库存储相对路径（wwwroot下的路径，便于前端访问）
                var relativePath = Path.Combine("HikAlarmSnapshotBase64", fileName);

                // 2. 下载图片并保存到本地
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(saveAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await (await response.Content.ReadAsStreamAsync()).CopyToAsync(fileStream);

                _logger.LogInformation("图片下载成功，绝对路径：{SavePath}", saveAbsolutePath);
                return relativePath; // 返回相对路径存入数据库
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图片下载失败: {Url}", url);
                return string.Empty;
            }
        }

        /// <summary>
        /// 辅助方法：替换文件名中的非法字符
        /// </summary>
        private string ReplaceInvalidFileNameChars(string input)
        {
            if (string.IsNullOrEmpty(input)) return "未知";

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                input = input.Replace(c.ToString(), "_");
            }
            // 限制文件名长度（避免超长）
            return input.Length > 50 ? input[..50] : input;
        }
        /// <summary>
        /// 获取报警信息（EventType翻译为中文）
        /// </summary>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="_eventType"></param>
        /// <param name="_deviceName"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        public HikAlarmRecordPageDto SelectAlarmInfomation(string? _startTime, string? _endTime, string? _eventType, string? _deviceName, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);

            // 调用仓储层查询原始数据
            var resultDict = _PgHikAlarmRecord.SelectHikAlarmRecordAsync(
                _connectionString, _startTime, _endTime, _eventType, _deviceName, pageNumber, pageSize).Result;

            var total = _PgHikAlarmRecord.CountHikAlarmRecordAsync(
                _connectionString, _startTime, _endTime, _eventType, _deviceName).Result;

            // 遍历结果，翻译EventType为中文
            foreach (var key in resultDict.Keys.ToList()) // ToList避免遍历中修改集合
            {
                var dto = resultDict[key];
                // 存在翻译映射则替换，否则保留原字符串
                if (_eventTypeTransDict.TryGetValue(dto.EventType, out var chineseName))
                {
                    dto.EventType = chineseName;
                }
                // 替换字典中的DTO（值类型为引用类型，直接修改即可，此处为显式赋值）
                resultDict[key] = dto;
            }

            var orderedList = resultDict
                .OrderByDescending(item => item.Value.EventTime)
                .Select(item => new Dictionary<string, HikAlarmRecordDto>
                {
                    { item.Key, item.Value }
                })
                .ToList();

            return new HikAlarmRecordPageDto
            {
                List = orderedList,
                PageNum = pageNumber,
                PageSize = pageSize,
                Total = total
            };
        }
        /// <summary>
        /// 获取所有报警记录，查看不同事件的出现次数
        /// </summary>
        /// <returns>每一种事件出现了多少次</returns>
        /// 修改返回为

        public List<AlarmCountDto> GetAllAlarmRecordCount()
        {
            var rawList = _PgHikAlarmRecord.GetAllAlarmRecordCountAsync(_connectionString).Result;

            // 将原始结果转为字典，便于翻译与补全
            var rawDict = rawList.ToDictionary(
                item => item.Name,
                item => item.Value,
                StringComparer.OrdinalIgnoreCase);

            var result = new List<AlarmCountDto>();

            // 先输出已知事件类型（无记录时补0，保证前端稳定展示）
            foreach (var item in _eventTypeTransDict)
            {
                rawDict.TryGetValue(item.Key, out var count);
                result.Add(new AlarmCountDto { Name = item.Value, Value = count });
                rawDict.Remove(item.Key);
            }

            // 再输出未在映射表中的事件类型（保留原名称）
            foreach (var item in rawDict)
            {
                result.Add(new AlarmCountDto { Name = item.Key, Value = item.Value });
            }

            return result;
        }
    }
}