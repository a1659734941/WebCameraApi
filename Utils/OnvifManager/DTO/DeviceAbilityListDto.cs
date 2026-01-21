namespace Onvif_GetPhoto.DTO
{
    public class DeviceAbilityListDto
    {
          /// <summary>
        /// 服务名称（如 Device_Service、Analytics 等）
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 功能是否开启（存在该服务即视为开启）
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 服务地址
        /// </summary>
        public string Url { get; set; }

        // 构造函数：通过 URL 初始化实体
        public DeviceAbilityListDto(string url)
        {
            Url = url;
            IsEnabled = true; // 存在该 URL 即视为功能开启
            ServiceName = FormatServiceName(ExtractServiceName(url)); // 解析并格式化服务名称
        }

        /// <summary>
        /// 从 URL 中提取服务名称（如从 /onvif/ 后获取）
        /// </summary>
        private string ExtractServiceName(string url)
        {
            var splitParts = url.Split(new[] { "/onvif/" }, StringSplitOptions.None);
            return splitParts.Length > 1 ? splitParts[1] : "UnknownService";
        }
        /// <summary>
        /// 格式化服务名称（首字母大写，如 device_service → Device_Service）
        /// </summary>
        private string FormatServiceName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return rawName;

            var parts = rawName.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    // 首字母大写，其余字符保持原样
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join("_", parts);
        }

        /// <summary>
        /// 重写ToString，返回指定格式的字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"({ServiceName}功能状态 ({IsEnabled}),地址：{Url})";
        }

    }
}