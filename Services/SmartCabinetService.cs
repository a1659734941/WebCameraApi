using WebCameraApi.Dto;

namespace WebCameraApi.Services
{
    /// <summary>
    /// 志信锁控板协议常量
    /// </summary>
    internal static class ZhiXinData
    {
        public const byte 前导 = 0x5a;
        public const byte 返回 = 0x55;
        public const byte 存物 = 0xa1;
        public const byte 取物 = 0xa2;
        public const byte 查询 = 0xa3;
        public const byte 应答 = 0xa4;
        public const byte 结束 = 0xa5;
        public const byte 错误 = 0xa6;
    }

    /// <summary>
    /// 智能柜（志信锁控板）业务服务
    /// </summary>
    public class SmartCabinetService
    {
        private readonly UdpClientService _udpClientService;
        private readonly ILogger<SmartCabinetService> _logger;

        public SmartCabinetService(UdpClientService udpClientService, ILogger<SmartCabinetService> logger)
        {
            _udpClientService = udpClientService;
            _logger = logger;
        }

        /// <summary>
        /// 志信锁控板指令：单开、全开、单查
        /// </summary>
        public async Task<(int code, string msg, SmartCabinetZhiXinResponseDto data)> ZhiXinAsync(SmartCabinetZhiXinRequestDto request)
        {
            var data = new SmartCabinetZhiXinResponseDto { Data = "" };

            if (request == null)
            {
                _logger.LogWarning("志信锁控板请求为空");
                return (400, "请求参数不能为空", data);
            }

            _logger.LogInformation("志信锁控板<-- IP:{IP}, Action:{Action}, Address:{Address}, Door:{Door}",
                request.IP, request.Action, request.Address, request.Door);

            byte[]? payload = request.Action switch
            {
                "单开" => [ZhiXinData.前导, request.Address, ZhiXinData.存物, request.Door],
                "全开" => [ZhiXinData.前导, request.Address, ZhiXinData.存物, 0x00],
                "单查" => [ZhiXinData.前导, request.Address, ZhiXinData.查询, request.Door],
                _ => null
            };

            if (payload == null)
            {
                _logger.LogWarning("志信锁控板未知指令: {Action}", request.Action);
                return (400, "未知指令，支持：单开、全开、单查", data);
            }

            var packet = new byte[payload.Length + 2];
            Buffer.BlockCopy(payload, 0, packet, 0, payload.Length);
            var crc = CrcCheck(payload);
            packet[payload.Length] = crc[0];
            packet[payload.Length + 1] = crc[1];

            _udpClientService.Send(request.IP, packet);
            var response = await _udpClientService.GetUDPDataAsync();

            if (response != null && response.Length > 4)
            {
                return response[4] switch
                {
                    0x01 => (200, "指令: 单开, 全开, 单查", new SmartCabinetZhiXinResponseDto { Data = "成功" }),
                    0x00 => (400, "指令: 单开, 全开, 单查", new SmartCabinetZhiXinResponseDto { Data = "失败" }),
                    _ => (400, "未知响应", new SmartCabinetZhiXinResponseDto { Data = $"返回状态码: 0x{response[4]:X2}" })
                };
            }

            return (400, "UDP 超时，没有返回数据", data);
        }

        private static byte[] CrcCheck(byte[] msg)
        {
            if (msg == null || msg.Length == 0) return [0, 0];
            byte c0 = 0, c1 = 0;
            foreach (var b in msg)
            {
                c0 ^= b;
                c1 += b;
            }
            return [c0, c1];
        }
    }
}
