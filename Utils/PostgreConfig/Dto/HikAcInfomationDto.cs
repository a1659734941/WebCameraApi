namespace PostgreConfig
{
    public class HikAcInfomationDto
    {
        /// <summary>
        /// 门禁名称（如 HikAc_1）
        /// </summary>
        public string HikAcName { get; set; }

        /// <summary>
        /// 门禁IP
        /// </summary>
        public string HikAcIP { get; set; }

        /// <summary>
        /// 门禁登录用户名
        /// </summary>
        public string HikAcUser { get; set; }

        /// <summary>
        /// 门禁登录密码
        /// </summary>
        public string HikAcPassword { get; set; }

        /// <summary>
        /// 门禁端口
        /// </summary>
        public int HikAcPort { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int HikAcRetryCount { get; set; }

        /// <summary>
        /// 等待毫秒数（注意字段名按你提供的拼写）
        /// </summary>
        public int HikAcWaitmillisecounds { get; set; }
    }
}