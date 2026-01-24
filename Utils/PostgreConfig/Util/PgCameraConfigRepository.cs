using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
namespace PostgreConfig
{
    class PgOnvifCameraInfomationRepository
    {

        public PgOnvifCameraInfomationRepository()
        {
        
        }
        /// <summary>
        /// 创建摄像头配置表（不存在则创建）
        /// </summary>
        public async Task CreateTableIfNotExistsAsync(string _connectionString)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS camera_config (
                    CameraName VARCHAR(50) PRIMARY KEY,  -- 摄像头名称作为主键
                    CameraIP VARCHAR(50) NOT NULL,
                    CameraUser VARCHAR(50) NOT NULL,
                    CameraPassword VARCHAR(100) NOT NULL,
                    CameraPort INT NOT NULL,
                    CameraRetryCount INT NOT NULL,
                    CameraWaitmillisecounds INT NOT NULL
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
        /// 查询所有摄像头配置，返回字典（键：CameraName，值：OnvifCameraInfomation）
        /// </summary>
        /// <returns>包含所有摄像头配置的字典</returns>
        /// <exception cref="Exception">数据库操作异常</exception>
        public async Task<Dictionary<string, OnvifCameraInfomation>> GetAllOnvifCameraInfomationsAsync(string _connectionString)
        {
            // 初始化字典，键为摄像头名称（主键）
            var OnvifCameraInfomations = new Dictionary<string, OnvifCameraInfomation>();

            // 查询所有记录的SQL
            var sql = "SELECT * FROM camera_config;";

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
                                // 将数据库记录封装为OnvifCameraInfomation实体
                                var config = new OnvifCameraInfomation
                                {
                                    CameraName = reader.GetString(reader.GetOrdinal("CameraName")),
                                    CameraIP = reader.GetString(reader.GetOrdinal("CameraIP")),
                                    CameraUser = reader.GetString(reader.GetOrdinal("CameraUser")),
                                    CameraPassword = reader.GetString(reader.GetOrdinal("CameraPassword")),
                                    CameraPort = reader.GetInt32(reader.GetOrdinal("CameraPort")),
                                    CameraRetryCount = reader.GetInt32(reader.GetOrdinal("CameraRetryCount")),
                                    CameraWaitmillisecounds = reader.GetInt32(reader.GetOrdinal("CameraWaitmillisecounds"))
                                };

                                // 添加到字典（主键保证不会重复）
                                OnvifCameraInfomations.Add(config.CameraName, config);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("查询摄像头配置失败：" + ex.Message, ex);
            }

            return OnvifCameraInfomations;
        }

        /// <summary>
        /// 批量新增/更新摄像头配置（按CameraName主键）
        /// </summary>
        public async Task<int> UpsertOnvifCameraInfomationsAsync(string _connectionString, IEnumerable<OnvifCameraInfomation> configs)
        {
            if (configs == null)
            {
                return 0;
            }

            var sql = @"
                INSERT INTO camera_config
                (CameraName, CameraIP, CameraUser, CameraPassword, CameraPort, CameraRetryCount, CameraWaitmillisecounds)
                VALUES
                (@CameraName, @CameraIP, @CameraUser, @CameraPassword, @CameraPort, @CameraRetryCount, @CameraWaitmillisecounds)
                ON CONFLICT (CameraName) DO UPDATE SET
                    CameraIP = EXCLUDED.CameraIP,
                    CameraUser = EXCLUDED.CameraUser,
                    CameraPassword = EXCLUDED.CameraPassword,
                    CameraPort = EXCLUDED.CameraPort,
                    CameraRetryCount = EXCLUDED.CameraRetryCount,
                    CameraWaitmillisecounds = EXCLUDED.CameraWaitmillisecounds;";

            var count = 0;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tx = await conn.BeginTransactionAsync())
                using (var cmd = new NpgsqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("CameraName", NpgsqlTypes.NpgsqlDbType.Varchar));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraIP", NpgsqlTypes.NpgsqlDbType.Varchar));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraUser", NpgsqlTypes.NpgsqlDbType.Varchar));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraPassword", NpgsqlTypes.NpgsqlDbType.Varchar));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraPort", NpgsqlTypes.NpgsqlDbType.Integer));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraRetryCount", NpgsqlTypes.NpgsqlDbType.Integer));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraWaitmillisecounds", NpgsqlTypes.NpgsqlDbType.Integer));

                    foreach (var config in configs)
                    {
                        cmd.Parameters["CameraName"].Value = config.CameraName;
                        cmd.Parameters["CameraIP"].Value = config.CameraIP;
                        cmd.Parameters["CameraUser"].Value = config.CameraUser;
                        cmd.Parameters["CameraPassword"].Value = config.CameraPassword;
                        cmd.Parameters["CameraPort"].Value = config.CameraPort;
                        cmd.Parameters["CameraRetryCount"].Value = config.CameraRetryCount;
                        cmd.Parameters["CameraWaitmillisecounds"].Value = config.CameraWaitmillisecounds;

                        count += await cmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                }
            }

            return count;
        }
    }
}
