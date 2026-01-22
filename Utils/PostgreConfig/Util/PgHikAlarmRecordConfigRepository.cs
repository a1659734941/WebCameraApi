using HikAlarmEndPoints;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreConfig
{
    internal class PgHikAlarmRecordConfigRepository
    {
        public PgHikAlarmRecordConfigRepository()
        {
        }

        public async Task CreateTableIfNotExistsAsync(string _connectionString)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS hik_alarm_record (
                    Id UUID PRIMARY KEY,
                    EventType VARCHAR(100) NOT NULL,
                    EventTime TIMESTAMP NOT NULL,
                    DeviceName VARCHAR(100) NOT NULL,
                    ChannelName VARCHAR(100) NOT NULL,
                    TaskName VARCHAR(100) NOT NULL,
                    SnapshotBase64Path TEXT,
                    RawData TEXT
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

        //时间筛选：仅返回EventTime在_startTime和_endTime之间的记录（参数为空则不限制）；
        //类型筛选：仅返回EventType等于_eventType的记录（参数为空则不限制）；
        //设备筛选：仅返回DeviceName等于deviceName的记录（参数为空则不限制）；
        //分页筛选：仅返回分页后的记录（比如pageNumber=1、pageSize=15则返回最新的 15 条匹配记录）；
        //排序规则：所有返回的记录按EventTime降序排列（最新的记录排在字典中靠前位置）；
        //空结果：如果没有匹配任何记录，方法会返回空字典（new Dictionary<...>()），而非null，避免调用方空指针异常。
        //返回结果示例:
        //{
        //  "550e8400-e29b-41d4-a716-446655440000": {
        //    "Id": "550e8400-e29b-41d4-a716-446655440000",
        //    "EventType": "入侵报警",
        //    "EventTime": "2026-01-20 10:00:00",
        //    "DeviceName": "摄像头01",
        //    "ChannelName": "通道1",
        //    "TaskName": "安防监控",
        //    "SnapshotBase64Path": null,
        //    "RawData": "{...}"
        //  },
        //  "660e8400-e29b-41d4-a716-446655440001": {
        //    "Id": "660e8400-e29b-41d4-a716-446655440001",
        //    "EventType": "越界报警",
        //    "EventTime": "2026-01-20 09:50:00",
        //    "DeviceName": "摄像头01",
        //    "ChannelName": "通道1",
        //    "TaskName": "安防监控",
        //    "SnapshotBase64Path": "/9j/4AAQSkZJRgABAQE...",
        //    "RawData": "{...}"
        //  }
        //}
        /// <summary>
        /// 查询数据库
        /// </summary>
        /// <param name="_connectionString"></param>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="_eventType"></param>
        /// <param name="deviceName"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<HikAlarmRecordDto>> SelectHikAlarmRecordAsync(
            string _connectionString,
            string? _startTime,
            string? _endTime,
            string? _eventType,
            string? deviceName,
            int pageNumber,
            int pageSize)
        {
            // 初始化返回结果
            var resultList = new List<HikAlarmRecordDto>();

            // 校验基础参数（避免无效分页）
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);
            int offset = (pageNumber - 1) * pageSize;

            // 构建参数集合
            var parameters = new List<NpgsqlParameter>();

            // 执行查询并映射数据
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                // 解析真实列名（兼容大小写/引号）
                var idCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "id", "Id");
                var eventTypeCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "eventtype", "EventType");
                var eventTimeCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "eventtime", "EventTime");
                var deviceNameCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "devicename", "DeviceName");
                var channelNameCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "channelname", "ChannelName");
                var taskNameCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "taskname", "TaskName");
                var snapshotCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "snapshotbase64path", "SnapshotBase64Path");
                var rawDataCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "rawdata", "RawData");

                if (string.IsNullOrWhiteSpace(idCol) ||
                    string.IsNullOrWhiteSpace(eventTypeCol) ||
                    string.IsNullOrWhiteSpace(eventTimeCol) ||
                    string.IsNullOrWhiteSpace(deviceNameCol) ||
                    string.IsNullOrWhiteSpace(channelNameCol) ||
                    string.IsNullOrWhiteSpace(taskNameCol))
                {
                    throw new Exception("hik_alarm_record 表结构缺失必要字段，请检查列名大小写是否一致");
                }

                var idExpr = QuoteIdentifierIfNeeded(idCol);
                var eventTypeExpr = QuoteIdentifierIfNeeded(eventTypeCol);
                var eventTimeExpr = QuoteIdentifierIfNeeded(eventTimeCol);
                var deviceNameExpr = QuoteIdentifierIfNeeded(deviceNameCol);
                var channelNameExpr = QuoteIdentifierIfNeeded(channelNameCol);
                var taskNameExpr = QuoteIdentifierIfNeeded(taskNameCol);
                var snapshotExpr = string.IsNullOrWhiteSpace(snapshotCol) ? "NULL" : QuoteIdentifierIfNeeded(snapshotCol);
                var rawDataExpr = string.IsNullOrWhiteSpace(rawDataCol) ? "NULL" : QuoteIdentifierIfNeeded(rawDataCol);

                // 构建SQL查询语句（动态拼接条件，参数化防注入）
                var sqlBuilder = new System.Text.StringBuilder();
                sqlBuilder.Append($@"
                    SELECT {idExpr} AS ""Id"",
                           {eventTypeExpr} AS ""EventType"",
                           {eventTimeExpr} AS ""EventTime"",
                           {deviceNameExpr} AS ""DeviceName"",
                           {channelNameExpr} AS ""ChannelName"",
                           {taskNameExpr} AS ""TaskName"",
                           {snapshotExpr} AS ""SnapshotBase64Path"",
                           {rawDataExpr} AS ""RawData""
                    FROM hik_alarm_record
                    WHERE 1=1 "); // 占位条件，方便拼接AND

                // 1. 时间范围条件（开始时间）
                if (!string.IsNullOrWhiteSpace(_startTime) && DateTime.TryParse(_startTime, out DateTime startTime))
                {
                    sqlBuilder.Append($" AND {eventTimeExpr} >= @StartTime ");
                    parameters.Add(new NpgsqlParameter("@StartTime", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = startTime });
                }

                // 2. 时间范围条件（结束时间）
                if (!string.IsNullOrWhiteSpace(_endTime) && DateTime.TryParse(_endTime, out DateTime endTime))
                {
                    sqlBuilder.Append($" AND {eventTimeExpr} <= @EndTime ");
                    parameters.Add(new NpgsqlParameter("@EndTime", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = endTime });
                }

                // 3. 事件类型条件
                if (!string.IsNullOrWhiteSpace(_eventType))
                {
                    sqlBuilder.Append($" AND {eventTypeExpr} = @EventType ");
                    parameters.Add(new NpgsqlParameter("@EventType", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = _eventType.Trim() });
                }

                // 4. 设备名称条件
                if (!string.IsNullOrWhiteSpace(deviceName))
                {
                    sqlBuilder.Append($" AND {deviceNameExpr} = @DeviceName ");
                    parameters.Add(new NpgsqlParameter("@DeviceName", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = deviceName.Trim() });
                }

                // 排序（按事件时间降序，保证最新的记录在前）
                sqlBuilder.Append($" ORDER BY {eventTimeExpr} DESC ");

                // 分页（LIMIT限制条数，OFFSET偏移量）
                sqlBuilder.Append(" LIMIT @PageSize OFFSET @Offset ");
                parameters.Add(new NpgsqlParameter("@PageSize", NpgsqlTypes.NpgsqlDbType.Integer) { Value = pageSize });
                parameters.Add(new NpgsqlParameter("@Offset", NpgsqlTypes.NpgsqlDbType.Integer) { Value = offset });

                using (var cmd = new NpgsqlCommand(sqlBuilder.ToString(), conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());

                    // 读取查询结果
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // 映射数据库字段到HikAlarmRecordDto
                            resultList.Add(new HikAlarmRecordDto
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("Id")), // 直接赋值 Guid 类型
                                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                                EventTime = reader.GetDateTime(reader.GetOrdinal("EventTime")),
                                DeviceName = reader.GetString(reader.GetOrdinal("DeviceName")),
                                ChannelName = reader.GetString(reader.GetOrdinal("ChannelName")),
                                TaskName = reader.GetString(reader.GetOrdinal("TaskName")),
                                // 处理可空字段
                                SnapshotBase64Path = reader.IsDBNull(reader.GetOrdinal("SnapshotBase64Path"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("SnapshotBase64Path")),
                                RawData = reader.IsDBNull(reader.GetOrdinal("RawData"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("RawData"))
                            });
                        }
                    }
                }
            }

            return resultList;
        }

        /// <summary>
        /// 获取符合条件的报警记录总数（不分页）
        /// </summary>
        public async Task<int> CountHikAlarmRecordAsync(
            string _connectionString,
            string? _startTime,
            string? _endTime,
            string? _eventType,
            string? deviceName)
        {
            var parameters = new List<NpgsqlParameter>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var eventTypeCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "eventtype", "EventType");
                var eventTimeCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "eventtime", "EventTime");
                var deviceNameCol = await ResolveColumnNameAsync(conn, "hik_alarm_record", "devicename", "DeviceName");

                if (string.IsNullOrWhiteSpace(eventTypeCol) ||
                    string.IsNullOrWhiteSpace(eventTimeCol) ||
                    string.IsNullOrWhiteSpace(deviceNameCol))
                {
                    throw new Exception("hik_alarm_record 表结构缺失必要字段，请检查列名大小写是否一致");
                }

                var eventTypeExpr = QuoteIdentifierIfNeeded(eventTypeCol);
                var eventTimeExpr = QuoteIdentifierIfNeeded(eventTimeCol);
                var deviceNameExpr = QuoteIdentifierIfNeeded(deviceNameCol);

                var sqlBuilder = new System.Text.StringBuilder();
                sqlBuilder.Append($@"
                    SELECT COUNT(*)
                    FROM hik_alarm_record
                    WHERE 1=1 ");

                if (!string.IsNullOrWhiteSpace(_startTime) && DateTime.TryParse(_startTime, out DateTime startTime))
                {
                    sqlBuilder.Append($" AND {eventTimeExpr} >= @StartTime ");
                    parameters.Add(new NpgsqlParameter("@StartTime", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = startTime });
                }

                if (!string.IsNullOrWhiteSpace(_endTime) && DateTime.TryParse(_endTime, out DateTime endTime))
                {
                    sqlBuilder.Append($" AND {eventTimeExpr} <= @EndTime ");
                    parameters.Add(new NpgsqlParameter("@EndTime", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = endTime });
                }

                if (!string.IsNullOrWhiteSpace(_eventType))
                {
                    sqlBuilder.Append($" AND {eventTypeExpr} = @EventType ");
                    parameters.Add(new NpgsqlParameter("@EventType", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = _eventType.Trim() });
                }

                if (!string.IsNullOrWhiteSpace(deviceName))
                {
                    sqlBuilder.Append($" AND {deviceNameExpr} = @DeviceName ");
                    parameters.Add(new NpgsqlParameter("@DeviceName", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = deviceName.Trim() });
                }

                using (var cmd = new NpgsqlCommand(sqlBuilder.ToString(), conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        /// <summary>
        /// 写入单条报警记录到数据库
        /// </summary>
        /// <param name="_connectionString">数据库连接字符串</param>
        /// <param name="alarmDto">待写入的报警记录DTO</param>
        /// <returns>是否写入成功（true=成功，false=失败）</returns>
        /// <exception cref="ArgumentNullException">DTO为空时抛出</exception>
        /// <exception cref="ArgumentException">必填字段为空时抛出</exception>
        public async Task<bool> InsertHikAlarmRecordAsync(string _connectionString, HikAlarmRecordDto alarmDto)
        {
            // 1. 基础参数校验
            if (alarmDto == null)
                throw new ArgumentNullException(nameof(alarmDto), "待写入的报警记录不能为空");

            // 2. 校验必填字段（数据库表定义为NOT NULL的字段）
            if (string.IsNullOrWhiteSpace(alarmDto.EventType))
                throw new ArgumentException("事件类型不能为空", nameof(alarmDto.EventType));
            if (string.IsNullOrWhiteSpace(alarmDto.DeviceName))
                throw new ArgumentException("设备名称不能为空", nameof(alarmDto.DeviceName));
            if (string.IsNullOrWhiteSpace(alarmDto.ChannelName))
                throw new ArgumentException("通道名称不能为空", nameof(alarmDto.ChannelName));
            if (string.IsNullOrWhiteSpace(alarmDto.TaskName))
                throw new ArgumentException("任务名称不能为空", nameof(alarmDto.TaskName));
            // EventTime如果未赋值，默认使用当前时间（也可根据业务调整）
            if (alarmDto.EventTime == default)
                alarmDto.EventTime = DateTime.Now;
            // Id如果未赋值，自动生成新的UUID（避免主键冲突）
            if (alarmDto.Id == default)
                alarmDto.Id = Guid.NewGuid();

            // 3. 构建插入SQL（参数化防SQL注入）
            var insertSql = @"
                INSERT INTO hik_alarm_record (
                    Id, EventType, EventTime, DeviceName, ChannelName, 
                    TaskName, SnapshotBase64Path, RawData
                ) VALUES (
                    @Id, @EventType, @EventTime, @DeviceName, @ChannelName, 
                    @TaskName, @SnapshotBase64Path, @RawData
                );";

            // 4. 构建参数集合（严格匹配字段类型）
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("@Id", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = alarmDto.Id },
                new NpgsqlParameter("@EventType", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = alarmDto.EventType.Trim() },
                new NpgsqlParameter("@EventTime", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = alarmDto.EventTime },
                new NpgsqlParameter("@DeviceName", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = alarmDto.DeviceName.Trim() },
                new NpgsqlParameter("@ChannelName", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = alarmDto.ChannelName.Trim() },
                new NpgsqlParameter("@TaskName", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = alarmDto.TaskName.Trim() },
                // 处理可空字段（DBNull.Value适配数据库NULL）
                new NpgsqlParameter("@SnapshotBase64Path", NpgsqlTypes.NpgsqlDbType.Text)
                    { Value = string.IsNullOrWhiteSpace(alarmDto.SnapshotBase64Path) ? DBNull.Value : alarmDto.SnapshotBase64Path },
                new NpgsqlParameter("@RawData", NpgsqlTypes.NpgsqlDbType.Text)
                    { Value = string.IsNullOrWhiteSpace(alarmDto.RawData) ? DBNull.Value : alarmDto.RawData }
            };

            // 5. 执行插入操作
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        // 执行插入，返回受影响行数（1=成功，0=失败）
                        int affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows == 1;
                    }
                }
            }
            catch (PostgresException ex)
            {
                // 捕获PostgreSQL特定异常（如主键冲突）
                throw new InvalidOperationException($"写入数据库失败：{ex.Message} (错误码：{ex.SqlState})", ex);
            }
            catch (Exception ex)
            {
                // 捕获其他通用异常
                throw new InvalidOperationException($"写入数据库失败：{ex.Message}", ex);
            }
        }
        /// <summary>
        /// 获取所有报警记录，查看不同事件的出现次数
        /// </summary>
        /// <param name="_connectionString">数据库连接字符串</param>
        /// <returns>每一种事件出现了多少次</returns>
        //返回结果示例:
        //[
        //  { "Name": "入侵报警", "Value": 10 },
        //  { "Name": "越界报警", "Value": 5 },
        //  { "Name": "其他事件", "Value": 3 }
        //]
        public async Task<List<AlarmCountDto>> GetAllAlarmRecordCountAsync(string _connectionString)
        {
            var result = new List<AlarmCountDto>();
            try
            {
                using(var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var eventTypeColumn = await ResolveColumnNameAsync(
                        conn,
                        "hik_alarm_record",
                        "eventtype",
                        "EventType");
                    if (string.IsNullOrWhiteSpace(eventTypeColumn))
                    {
                        throw new Exception("hik_alarm_record 表中未找到 EventType/eventtype 字段，请检查表结构");
                    }

                    var eventTypeExpr = QuoteIdentifierIfNeeded(eventTypeColumn);
                    var sql = $@"
                        SELECT {eventTypeExpr} AS ""EventType"", COUNT(*) AS ""Count""
                        FROM hik_alarm_record
                        GROUP BY {eventTypeExpr};";
                    using(var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using(var reader = await cmd.ExecuteReaderAsync())
                        {
                            while(await reader.ReadAsync())
                            {
                                result.Add(new AlarmCountDto
                                {
                                    Name = reader.GetString(reader.GetOrdinal("EventType")),
                                    Value = reader.GetInt32(reader.GetOrdinal("Count"))
                                });
                            }
                        }
                    }
                }
            }
            catch(Exception ex){
                throw new Exception("获取所有报警记录，查看不同事件的出现次数时发生异常：" + ex.Message);
            }
            return result;
        }

        private static async Task<string?> ResolveColumnNameAsync(
            NpgsqlConnection conn,
            string tableName,
            params string[] candidates)
        {
            const string sql = @"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = @TableName
                  AND column_name = ANY(@Candidates)
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@Candidates", candidates);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        private static string QuoteIdentifierIfNeeded(string identifier)
        {
            if (identifier.Any(ch => char.IsUpper(ch) || ch == ' ' || ch == '-'))
            {
                return $"\"{identifier.Replace("\"", "\"\"")}\"";
            }
            return identifier;
        }
    }
}