using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ConfigGet
{
    class Appsettings_Get
    {
        private static readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        /// <summary>
        /// 读取JSON中指定Key对应的配置（动态类型）
        /// </summary>
        /// <param name="configKey">目标配置的Key（如DatabaseConfig）</param>
        /// <returns>目标配置的动态对象</returns>
        public static dynamic GetConfigByKey(string configKey)
        {
            // 1. 校验文件是否存在
            if (!File.Exists(_jsonFilePath))
            {
                throw new FileNotFoundException("JSON配置文件不存在", _jsonFilePath);
            }

            // 2. 读取JSON文件内容
            string jsonContent = File.ReadAllText(_jsonFilePath);

            // 3. 解析JSON并提取目标配置
            JObject jsonObject = JObject.Parse(jsonContent);
            var targetConfig = jsonObject[configKey];

            // 4. 校验目标配置是否存在
            if (targetConfig == null)
            {
                throw new KeyNotFoundException($"未找到配置项：{configKey}");
            }

            return targetConfig;
        }

        /// <summary>
        /// 读取JSON中指定Key对应的配置（强类型）
        /// </summary>
        /// <typeparam name="T">目标配置的强类型</typeparam>
        /// <param name="configKey">目标配置的Key</param>
        /// <returns>目标配置的强类型对象</returns>
        public static T GetConfigByKey<T>(string configKey)
        {
            dynamic targetConfig = GetConfigByKey(configKey);
            // 将动态对象转换为指定的强类型
            return targetConfig.ToObject<T>();
        }
    }
}
