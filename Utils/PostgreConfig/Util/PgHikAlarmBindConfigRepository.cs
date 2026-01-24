
using Npgsql;
using WebCameraApi.Utils.PostgreConfig.Dto;
namespace PostgreConfig
{
    internal class PgHikAlarmBindConfigRepository
    {
        public PgHikAlarmBindConfigRepository()
        {
        }
        /// <summary>
        /// 创建海康报警和双人审讯的sql表（不存在则创建）
        /// </summary>
        /// <param name="_connectionString">PostgreSQL连接字符串</param>
        public async Task CreateTableIfNotExistsAsync(string _connectionString)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS hik_alarm_bind (
                    AlarmCameraIP VARCHAR(50) PRIMARY KEY,  
                    BlockComputerIP VARCHAR(50) NOT NULL,
                    HikAlarmCameraRoomName VARCHAR(50)
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
        /// 查询所有海康报警绑定配置，返回字典（键：AlarmCameraIP，值：HikAlarmBindDto）
        /// </summary>
        /// <param name="_connectionString">PostgreSQL连接字符串</param>
        /// <returns>包含所有海康报警摄像头和电脑配置</returns>
        public async Task<Dictionary<string, HikAlarmBindDto>> GetAllHikAlarmBindsAsync(string _connectionString)
        {
            var sql = @"SELECT * FROM hik_alarm_bind";
            var result = new Dictionary<string, HikAlarmBindDto>();
            try
            {
                using(var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using(var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using(var reader = await cmd.ExecuteReaderAsync())
                        {
                            while(await reader.ReadAsync())
                            {
                                var dto = new HikAlarmBindDto
                                {
                                    HikAlarmCameraRoomName = reader.GetString(reader.GetOrdinal("HikAlarmCameraRoomName")),
                                    HikAlarmCameraIP = reader.GetString(reader.GetOrdinal("AlarmCameraIP")),
                                    BlockComputerIP = reader.GetString(reader.GetOrdinal("BlockComputerIP"))
                                };
                                result[dto.HikAlarmCameraIP] = dto;
                            }
                        }
                    }
                }
            }catch(Exception ex)
            {
                throw new Exception("查询海康报警绑定配置时发生异常：" + ex.Message);
            }
            return result;
        }
    }
}