using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using PostgreConfig;

namespace HikAcessControl  // 与Dto类保持相同的命名空间
{
    internal class PgHikAccessControlRepository
    {
        public PgHikAccessControlRepository()
        {
        }

        /// <summary>
        /// 创建门禁配置表（不存在则创建）
        /// </summary>
        /// <param name="_connectionString">PostgreSQL连接字符串</param>
        public async Task CreateTableIfNotExistsAsync(string _connectionString)
        {
            // 适配HikAcInfomationDto的表结构，主键为门禁名称
            var sql = @"
                CREATE TABLE IF NOT EXISTS hikac_config (
                    HikAcName VARCHAR(50) PRIMARY KEY,  -- 门禁名称作为主键
                    HikAcIP VARCHAR(50) NOT NULL,
                    HikAcUser VARCHAR(50) NOT NULL,
                    HikAcPassword VARCHAR(100) NOT NULL,
                    HikAcPort INT NOT NULL,
                    HikAcRetryCount INT NOT NULL,
                    HikAcWaitmillisecounds INT NOT NULL
                );";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 查询所有门禁配置，返回字典（键：HikAcName，值：HikAcInfomationDto）
        /// </summary>
        /// <param name="_connectionString">PostgreSQL连接字符串</param>
        /// <returns>包含所有门禁配置的字典</returns>
        /// <exception cref="Exception">数据库操作异常</exception>
        public async Task<Dictionary<string, HikAcInfomationDto>> GetAllHikAcInfomationsAsync(string _connectionString)
        {
            // 初始化字典，键为门禁名称（主键）
            var hikAcInfomations = new Dictionary<string, HikAcInfomationDto>();

            // 查询所有门禁配置记录的SQL
            var sql = "SELECT * FROM hikac_config;";

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        // 执行查询并获取数据读取器
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // 遍历每一条记录
                            while (await reader.ReadAsync())
                            {
                                // 将数据库记录封装为HikAcInfomationDto实体
                                var config = new HikAcInfomationDto
                                {
                                    HikAcName = reader.GetString(reader.GetOrdinal("HikAcName")),
                                    HikAcIP = reader.GetString(reader.GetOrdinal("HikAcIP")),
                                    HikAcUser = reader.GetString(reader.GetOrdinal("HikAcUser")),
                                    HikAcPassword = reader.GetString(reader.GetOrdinal("HikAcPassword")),
                                    HikAcPort = reader.GetInt32(reader.GetOrdinal("HikAcPort")),
                                    HikAcRetryCount = reader.GetInt32(reader.GetOrdinal("HikAcRetryCount")),
                                    HikAcWaitmillisecounds = reader.GetInt32(reader.GetOrdinal("HikAcWaitmillisecounds"))
                                };

                                // 添加到字典（主键保证不会重复）
                                hikAcInfomations.Add(config.HikAcName, config);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("查询门禁配置失败：" + ex.Message, ex);
            }

            return hikAcInfomations;
        }

        /// <summary>
        /// 通过门禁IP或门禁名称查询对应的门禁信息（基于外部传入的内存数据）
        /// </summary>
        /// <param name="hikAcInfomations">外部传入的门禁信息字典</param>
        /// <param name="queryValue">门禁IP地址或门禁名称</param>
        /// <returns>匹配的门禁信息，无匹配则返回null</returns>
        /// <exception cref="ArgumentNullException">查询条件为空/传入的字典为空时抛出</exception>
        /// <exception cref="Exception">数据筛选异常时抛出</exception>
        public async Task<HikAcInfomationDto> GetHikAcInfomationByIpOrNameAsync(
            Dictionary<string, HikAcInfomationDto> hikAcInfomations,  // 新增：外部传入的字典参数
            string queryValue)
        {
            // 1. 空值校验：新增对传入字典的校验
            if (hikAcInfomations == null)
            {
                throw new ArgumentNullException(nameof(hikAcInfomations), "传入的门禁信息字典不能为空");
            }
            if (string.IsNullOrWhiteSpace(queryValue))
            {
                throw new ArgumentNullException(nameof(queryValue), "查询条件（IP/门禁名称）不能为空");
            }

            try
            {
                // 2. 直接使用外部传入的字典，移除内部数据库查询逻辑
                var matchedInfo = hikAcInfomations.Values
                    .FirstOrDefault(dto =>
                        dto.HikAcName.Equals(queryValue, StringComparison.OrdinalIgnoreCase)  // 名称匹配（忽略大小写）
                        || dto.HikAcIP.Equals(queryValue, StringComparison.OrdinalIgnoreCase)    // IP匹配（忽略大小写）
                    );

                // 保持async签名兼容，返回已完成的任务
                return await Task.FromResult(matchedInfo);
            }
            catch (Exception ex)
            {
                // 3. 调整异常信息，适配新逻辑
                throw new Exception($"根据{queryValue}筛选门禁配置失败：{ex.Message}", ex);
            }
        }
    }
}