using System;
namespace PostgreConfig
{
    /// <summary>
    /// PostgreSQL 连接配置类
    /// </summary>
    public class PgConnectionOptions
    {
        /// <summary>
        /// 数据库主机地址
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// 数据库端口
        /// </summary>
        public int Port { get; set; } = 5432;

        /// <summary>
        /// 数据库用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 数据库密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// 命令超时时间（秒）
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// 校验配置有效性
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new ArgumentNullException(nameof(Host), "数据库主机地址不能为空");
            if (Port < 1 || Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "数据库端口必须在1-65535之间");
            if (string.IsNullOrWhiteSpace(Username))
                throw new ArgumentNullException(nameof(Username), "数据库用户名不能为空");
            if (string.IsNullOrWhiteSpace(Database))
                throw new ArgumentNullException(nameof(Database), "数据库名称不能为空");
        }

        /// <summary>
        /// 构建连接字符串
        /// </summary>
        public string BuildConnectionString()
        {
            Validate();
            return $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Database};CommandTimeout={CommandTimeout};";
        }
    }
}