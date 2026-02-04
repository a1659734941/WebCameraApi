using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebCameraApi.Services
{
    /// <summary>
    /// UDP 客户端服务，用于向指定 IP:端口 发送数据并接收响应（如志信锁控板等）
    /// </summary>
    public class UdpClientService
    {
        private UdpClient? _client;
        private IPEndPoint? _remoteEndPoint;
        private readonly ILogger<UdpClientService> _logger;
        private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(2);

        public UdpClientService(ILogger<UdpClientService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 发送 UDP 数据。IP 格式为 "主机:端口"，如 "192.168.1.100:9000"
        /// </summary>
        public void Send(string ip, byte[] data)
        {
            try
            {
                var (host, port) = ParseIpAndPort(ip);
                if (host == null || port == 0)
                {
                    _logger.LogError("无效的 IP 和端口格式: {IP}", ip);
                    return;
                }

                _client?.Dispose();
                _client = new UdpClient(0);
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

                _client.Send(data, data.Length, _remoteEndPoint);

                var sb = new StringBuilder();
                foreach (var b in data) sb.Append(b.ToString("X2") + " ");
                _logger.LogInformation("UDP-> {Data}", sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UDP 发送异常: {IP}", ip);
                _client?.Dispose();
                _client = null;
                _remoteEndPoint = null;
            }
        }

        /// <summary>
        /// 异步等待并获取 UDP 响应数据，超时后返回 null
        /// </summary>
        public async Task<byte[]?> GetUDPDataAsync(CancellationToken cancellationToken = default)
        {
            if (_client == null)
            {
                _logger.LogWarning("UDP 客户端未初始化，请先调用 Send");
                return null;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ReceiveTimeout);

            try
            {
                var result = await _client.ReceiveAsync(cts.Token);
                var data = result.Buffer;
                var sb = new StringBuilder();
                foreach (var b in data) sb.Append(b.ToString("X2") + " ");
                _logger.LogInformation("UDP<- {Data}", sb.ToString());
                return data;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UDP 接收超时，没有返回数据");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUDPDataAsync 失败: {Message}", ex.Message);
                return null;
            }
            finally
            {
                _client?.Dispose();
                _client = null;
                _remoteEndPoint = null;
            }
        }

        private static (string? host, ushort port) ParseIpAndPort(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return (null, 0);
            var parts = ip.Trim().Split(':');
            if (parts.Length != 2) return (null, 0);
            if (!ushort.TryParse(parts[1].Trim(), out var port)) return (null, 0);
            var host = parts[0].Trim();
            return string.IsNullOrEmpty(host) ? (null, 0) : (host, port);
        }
    }
}
