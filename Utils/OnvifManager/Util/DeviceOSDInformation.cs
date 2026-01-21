using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net.Http;

namespace Onvif_GetPhoto.Util
{
    public class OnvifDeviceOSDInformation
    {
        private string DeviceIp { get; set; }
        private int DevicePort { get; set; }
        private string Username { get; set; }
        private string Password { get; set; }
        private DateTime DeviceUtcTime { get; set; }
        private List<string> _allProfileTokens; // 缓存所有通道Token

        // 常见默认ProfileToken列表（适配不同厂商设备）
        private readonly List<string> _defaultProfileTokens = new List<string>
        {
            "Profile_1", "Profile_0", "MainStream", "SubStream", "StreamingChannel_1", "ch1_main"
        };

        public OnvifDeviceOSDInformation(string ip, int port, string username, string password)
        {
            DeviceIp = ip;
            DevicePort = port;
            Username = username;
            Password = password;
        }

        /// <summary>
        /// 获取设备通道数量（ProfileToken数量即通道数）
        /// </summary>
        public async Task<int> GetChannelCount()
        {
            if (_allProfileTokens != null && _allProfileTokens.Any())
                return _allProfileTokens.Count;

            // 获取Media服务地址
            var mediaServiceUrl = await GetMediaServiceUrl();
            if (string.IsNullOrEmpty(mediaServiceUrl))
            {
                Console.WriteLine("获取Media服务地址失败，无法统计通道数");
                return 0;
            }

            // 获取所有通道Token
            _allProfileTokens = await GetAllProfileTokens(mediaServiceUrl);
            if (!_allProfileTokens.Any())
            {
                _allProfileTokens = GetAllDefaultProfileTokens(mediaServiceUrl);
            }

            Console.WriteLine($"设备总通道数：{_allProfileTokens.Count}，通道列表：{string.Join(", ", _allProfileTokens)}");
            return _allProfileTokens.Count;
        }

        /// <summary>
        /// 主入口：获取指定通道的OSD信息（返回字典格式）
        /// </summary>
        /// <param name="targetChannelIndex">通道索引（从1开始）</param>
        public async Task<Dictionary<string, List<Dictionary<string, string>>>> GetOSDInformationByChannel(int targetChannelIndex)
        {
            var resultDict = new Dictionary<string, List<Dictionary<string, string>>>();
            try
            {
                Console.WriteLine($"正在连接设备：{DeviceIp}:{DevicePort}");

                // 1. 先获取通道数量和通道列表
                int channelCount = await GetChannelCount();
                if (channelCount == 0)
                {
                    Console.WriteLine("无可用通道");
                    return null;
                }

                // 验证指定通道索引有效性
                if (targetChannelIndex < 1 || targetChannelIndex > channelCount)
                {
                    Console.WriteLine($"指定通道索引无效！当前通道数：{channelCount}，请输入1-{channelCount}之间的索引");
                    return null;
                }

                // 2. 获取目标通道Token（索引从0开始）
                string targetChannelToken = _allProfileTokens[targetChannelIndex - 1];
                Console.WriteLine($"\n已选择通道：{targetChannelToken}（索引：{targetChannelIndex}）");

                // 3. 获取Media服务地址
                var mediaServiceUrl = await GetMediaServiceUrl();
                if (string.IsNullOrEmpty(mediaServiceUrl))
                {
                    Console.WriteLine("获取Media服务地址失败");
                    return null;
                }

                // 4. 获取设备UTC时间
                DeviceUtcTime = await GetDeviceUtcTime();
                Console.WriteLine($"设备UTC时间：{DeviceUtcTime:yyyy-MM-dd HH:mm:ss}");

                // 5. 查询指定通道的OSD信息
                Console.WriteLine($"正在查询通道 [{targetChannelToken}] 的OSD信息...");
                var osdResponseContent = await SendGetOSDsRequestWithRetry(mediaServiceUrl, targetChannelToken);
                if (string.IsNullOrEmpty(osdResponseContent))
                {
                    Console.WriteLine($"通道 [{targetChannelToken}] 未获取到OSD信息");
                    return null;
                }

                // 6. 解析OSD信息为字典格式
                var osdDictList = ParseOSDResponseToDict(osdResponseContent, targetChannelToken);
                if (osdDictList.Any())
                {
                    resultDict.Add(targetChannelToken, osdDictList);
                    PrintOSDSummary(targetChannelToken, osdDictList); // 打印精简摘要
                }

                return resultDict.Any() ? resultDict : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取OSD信息异常：{ex.Message}");
                return null;
            }
        }

        #region 核心辅助方法（优化后）
        /// <summary>
        /// 获取所有动态ProfileToken（通道Token）
        /// </summary>
        private async Task<List<string>> GetAllProfileTokens(string mediaServiceUrl)
        {
            var requestBody = @"<trt:GetProfiles xmlns:trt=""http://www.onvif.org/ver10/media/wsdl"">
                <trt:IncludeInactive>false</trt:IncludeInactive>
            </trt:GetProfiles>";

            var response = await SendOnvifRequest(mediaServiceUrl, requestBody, "text/xml");
            if (response == null || !response.IsSuccessStatusCode)
            {
                Console.WriteLine("获取所有通道Token失败");
                return new List<string>();
            }

            try
            {
                var responseContent = Encoding.UTF8.GetString(response.Data);
                var xml = XElement.Parse(responseContent);
                XNamespace tt = "http://www.onvif.org/ver10/schema";
                
                var profileTokens = xml.Descendants(tt + "Profile")
                    .Select(p => p.Element(tt + "Token")?.Value ?? p.Attribute("token")?.Value)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .ToList();

                // 补充海康设备特殊结构解析
                var trtProfiles = xml.Descendants(XNamespace.Get("http://www.onvif.org/ver10/media/wsdl") + "Profiles");
                profileTokens.AddRange(trtProfiles.Select(p => p.Attribute("token")?.Value).Where(t => !string.IsNullOrEmpty(t)));

                return profileTokens.Distinct().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析通道Token失败：{ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有可用的默认通道Token
        /// </summary>
        private List<string> GetAllDefaultProfileTokens(string mediaServiceUrl)
        {
            Console.WriteLine("开始尝试默认通道Token...");
            var validTokens = new List<string>();
            foreach (var token in _defaultProfileTokens)
            {
                var testRequestBody = $@"<trt:GetOSDs xmlns:trt=""http://www.onvif.org/ver10/media/wsdl"">
                    <trt:ProfileToken>{token}</trt:ProfileToken>
                </trt:GetOSDs>";

                var testResponse = SendOnvifRequestSync(mediaServiceUrl, testRequestBody, "text/xml");
                if (testResponse?.IsSuccessStatusCode == true)
                {
                    Console.WriteLine($"默认通道Token [{token}] 验证通过");
                    validTokens.Add(token);
                }
            }
            return validTokens;
        }

        /// <summary>
        /// 发送GetOSDs请求（带重试，精简打印）
        /// </summary>
        private async Task<string> SendGetOSDsRequestWithRetry(string mediaServiceUrl, string profileToken)
        {
            var requestBody = $@"<trt:GetOSDs xmlns:trt=""http://www.onvif.org/ver10/media/wsdl"">
                <trt:ProfileToken>{profileToken}</trt:ProfileToken>
            </trt:GetOSDs>";

            // 第一次请求
            var response = await SendOnvifRequest(mediaServiceUrl, requestBody, "text/xml");
            if (response != null && response.IsSuccessStatusCode)
            {
                return Encoding.UTF8.GetString(response.Data);
            }

            // 重试逻辑
            Console.WriteLine("OSD请求失败，尝试重试...");
            await Task.Delay(1000);
            response = await SendOnvifRequest(mediaServiceUrl, requestBody, "text/xml");
            if (response == null || !response.IsSuccessStatusCode)
            {
                Console.WriteLine($"重试失败，状态码：{response?.StatusCode}");
                return null;
            }

            return Encoding.UTF8.GetString(response.Data);
        }

        /// <summary>
        /// 解析OSD响应为字典格式
        /// </summary>
        private List<Dictionary<string, string>> ParseOSDResponseToDict(string responseContent, string profileToken)
        {
            var osdDictList = new List<Dictionary<string, string>>();
            try
            {
                var xml = XElement.Parse(responseContent);
                XNamespace tt = "http://www.onvif.org/ver10/schema";
                XNamespace trt = "http://www.onvif.org/ver10/media/wsdl";

                var osdElements = xml.Descendants(trt + "OSDs").ToList();
                if (!osdElements.Any())
                {
                    Console.WriteLine($"通道 [{profileToken}] 未发现OSD配置");
                    return osdDictList;
                }

                foreach (var osd in osdElements)
                {
                    var osdDict = new Dictionary<string, string>
                    {
                        ["OSDToken"] = osd.Attribute("token")?.Value ?? "未知",
                        ["Type"] = osd.Element(tt + "Type")?.Value ?? "未知",
                        ["IsVisible"] = "true", // 响应默认可见
                        ["Text"] = "无文本",
                        ["FontSize"] = "未知",
                        ["PositionX"] = "0",
                        ["PositionY"] = "0",
                        ["DateFormat"] = "未知",
                        ["TimeFormat"] = "未知"
                    };

                    // 解析文本内容
                    var textString = osd.Element(tt + "TextString");
                    if (textString != null)
                    {
                        var textType = textString.Element(tt + "Type")?.Value;
                        if (textType == "DateAndTime")
                        {
                            osdDict["DateFormat"] = textString.Element(tt + "DateFormat")?.Value ?? "未知";
                            osdDict["TimeFormat"] = textString.Element(tt + "TimeFormat")?.Value ?? "未知";
                            osdDict["Text"] = $"日期时间（格式: {osdDict["DateFormat"]} {osdDict["TimeFormat"]}）";
                        }
                        else if (textType == "Plain")
                        {
                            osdDict["Text"] = textString.Element(tt + "PlainText")?.Value ?? "无文本";
                        }

                        // 解析字体大小
                        osdDict["FontSize"] = textString.Element(tt + "FontSize")?.Value ?? "未知";
                    }

                    // 解析位置
                    var position = osd.Element(tt + "Position");
                    if (position != null)
                    {
                        var pos = position.Element(tt + "Pos");
                        if (pos != null)
                        {
                            osdDict["PositionX"] = pos.Attribute("x")?.Value ?? "0";
                            osdDict["PositionY"] = pos.Attribute("y")?.Value ?? "0";
                        }
                    }

                    osdDictList.Add(osdDict);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析OSD响应失败：{ex.Message}");
            }
            return osdDictList;
        }

        /// <summary>
        /// 精简打印OSD摘要信息
        /// </summary>
        private void PrintOSDSummary(string channelToken, List<Dictionary<string, string>> osdDictList)
        {
            Console.WriteLine($"\n=== 通道 [{channelToken}] OSD信息摘要 ===");
            foreach (var osdDict in osdDictList)
            {
                Console.WriteLine($"OSDToken：{osdDict["OSDToken"]} | 类型：{osdDict["Type"]} | 文本：{osdDict["Text"]} | 字体大小：{osdDict["FontSize"]} | 位置(X:{osdDict["PositionX"]}, Y:{osdDict["PositionY"]})");
            }
            Console.WriteLine("===================\n");
        }
        #endregion

        #region 原有基础方法（保留不变）
        /// <summary>
        /// 获取设备UTC时间
        /// </summary>
        private async Task<DateTime> GetDeviceUtcTime()
        {
            var deviceServiceUrl = $"http://{DeviceIp}:{DevicePort}/onvif/device_service";
            var requestBody = @"<tds:GetSystemDateAndTime xmlns:tds=""http://www.onvif.org/ver10/device/wsdl""/>";

            var response = await SendOnvifRequest(deviceServiceUrl, requestBody, "text/xml");
            if (response == null || !response.IsSuccessStatusCode)
            {
                Console.WriteLine($"获取设备时间失败，状态码：{response?.StatusCode}");
                return DateTime.UtcNow;
            }

            try
            {
                var responseContent = Encoding.UTF8.GetString(response.Data);
                var xml = XElement.Parse(responseContent);
                var tt = XNamespace.Get("http://www.onvif.org/ver10/schema");
                var utcElement = xml.Descendants(tt + "UTCDateTime").FirstOrDefault();

                if (utcElement == null) return DateTime.UtcNow;

                var date = utcElement.Element(tt + "Date");
                var time = utcElement.Element(tt + "Time");
                if (date == null || time == null) return DateTime.UtcNow;

                return new DateTime(
                    int.Parse(date.Element(tt + "Year").Value),
                    int.Parse(date.Element(tt + "Month").Value),
                    int.Parse(date.Element(tt + "Day").Value),
                    int.Parse(time.Element(tt + "Hour").Value),
                    int.Parse(time.Element(tt + "Minute").Value),
                    int.Parse(time.Element(tt + "Second").Value),
                    DateTimeKind.Utc
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析设备时间失败：{ex.Message}");
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 获取Media服务地址
        /// </summary>
        private async Task<string> GetMediaServiceUrl()
        {
            var deviceServiceUrl = $"http://{DeviceIp}:{DevicePort}/onvif/device_service";
            var requestBody = @"<tds:GetCapabilities xmlns:tds=""http://www.onvif.org/ver10/device/wsdl"">
                <tds:Category>Media</tds:Category>
            </tds:GetCapabilities>";

            var response = await SendOnvifRequest(deviceServiceUrl, requestBody, "text/xml");
            if (response == null || !response.IsSuccessStatusCode)
            {
                Console.WriteLine($"获取设备能力失败，状态码：{response?.StatusCode}");
                return null;
            }

            try
            {
                var responseContent = Encoding.UTF8.GetString(response.Data);
                var xml = XElement.Parse(responseContent);
                XNamespace tds = "http://www.onvif.org/ver10/device/wsdl";
                XNamespace tt = "http://www.onvif.org/ver10/schema";

                var capabilities = xml.Descendants(tds + "Capabilities").FirstOrDefault();
                if (capabilities == null) return null;

                var mediaElement = capabilities.Element(tt + "Media");
                if (mediaElement == null) return null;

                return mediaElement.Element(tt + "XAddr")?.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析Media服务地址失败：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 通用ONVIF请求发送方法
        /// </summary>
        private async Task<HttpResponse> SendOnvifRequest(string serviceUrl, string requestBody, string contentType)
        {
            try
            {
                var authHeader = OnvifAuth.GetHeadToken(Username, Password, DeviceUtcTime);
                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://www.w3.org/2003/05/soap-envelope"" 
    xmlns:trt=""http://www.onvif.org/ver10/media/wsdl""
    xmlns:tds=""http://www.onvif.org/ver10/device/wsdl""
    xmlns:tt=""http://www.onvif.org/ver10/schema"">
    <SOAP-ENV:Header>{authHeader}</SOAP-ENV:Header>
    <SOAP-ENV:Body>{requestBody}</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var httpContent = new StringContent(soapEnvelope, Encoding.UTF8, contentType);
                    var response = await client.PostAsync(serviceUrl, httpContent);
                    var responseData = await response.Content.ReadAsByteArrayAsync();

                    return new HttpResponse
                    {
                        Response = response,
                        Data = responseData,
                        StatusCode = response.StatusCode,
                        IsSuccessStatusCode = response.IsSuccessStatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送请求异常：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 同步发送请求（用于默认Token验证）
        /// </summary>
        private HttpResponse SendOnvifRequestSync(string serviceUrl, string requestBody, string contentType)
        {
            try
            {
                var authHeader = OnvifAuth.GetHeadToken(Username, Password, DeviceUtcTime);
                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://www.w3.org/2003/05/soap-envelope"" 
    xmlns:trt=""http://www.onvif.org/ver10/media/wsdl""
    xmlns:tt=""http://www.onvif.org/ver10/schema"">
    <SOAP-ENV:Header>{authHeader}</SOAP-ENV:Header>
    <SOAP-ENV:Body>{requestBody}</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = contentType;
                    var responseData = client.UploadData(serviceUrl, Encoding.UTF8.GetBytes(soapEnvelope));
                    return new HttpResponse
                    {
                        StatusCode = HttpStatusCode.OK,
                        Data = responseData,
                        IsSuccessStatusCode = true
                    };
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                return new HttpResponse
                {
                    StatusCode = response?.StatusCode ?? HttpStatusCode.BadRequest,
                    Data = null,
                    IsSuccessStatusCode = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步请求异常：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 辅助类：封装HTTP响应信息
        /// </summary>
        private class HttpResponse
        {
            public HttpResponseMessage Response { get; set; }
            public byte[] Data { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public bool IsSuccessStatusCode { get; set; }
        }
        #endregion
    }

    /// <summary>
    /// ONVIF认证工具类
    /// </summary>
    public static class OnvifAuth
    {
        public static string GetHeadToken(string username, string password, DateTime utcTime)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return "";

            var nonce = Guid.NewGuid().ToString("N");
            var created = utcTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var passwordDigest = ComputePasswordDigest(nonce, created, password);

            return $@"<wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
                <wsse:UsernameToken>
                    <wsse:Username>{username}</wsse:Username>
                    <wsse:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"">{passwordDigest}</wsse:Password>
                    <wsse:Nonce EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"">{Convert.ToBase64String(Encoding.UTF8.GetBytes(nonce))}</wsse:Nonce>
                    <wsu:Created xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">{created}</wsu:Created>
                </wsse:UsernameToken>
            </wsse:Security>";
        }

        private static string ComputePasswordDigest(string nonce, string created, string password)
        {
            var nonceBytes = Encoding.UTF8.GetBytes(nonce);
            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            var combined = new byte[nonceBytes.Length + createdBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(nonceBytes, 0, combined, 0, nonceBytes.Length);
            Buffer.BlockCopy(createdBytes, 0, combined, nonceBytes.Length, createdBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, nonceBytes.Length + createdBytes.Length, passwordBytes.Length);

            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                return Convert.ToBase64String(sha1.ComputeHash(combined));
            }
        }
    }
}