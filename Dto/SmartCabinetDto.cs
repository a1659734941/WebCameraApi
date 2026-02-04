namespace WebCameraApi.Dto
{
    /// <summary>
    /// 志信锁控板请求参数（智能柜）
    /// </summary>
    public class SmartCabinetZhiXinRequestDto
    {
        /// <summary>
        /// IP+端口，格式：IP:端口，如 192.168.1.100:9000
        /// </summary>
        public string IP { get; set; } = string.Empty;

        /// <summary>
        /// 动作：单开、全开、单查
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 地址，默认从 0 开始
        /// </summary>
        public byte Address { get; set; }

        /// <summary>
        /// 门号，默认从 1 开始
        /// </summary>
        public byte Door { get; set; }
    }

    /// <summary>
    /// 志信锁控板响应数据
    /// </summary>
    public class SmartCabinetZhiXinResponseDto
    {
        /// <summary>
        /// 结果描述，如 "成功"、"失败"
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
}
