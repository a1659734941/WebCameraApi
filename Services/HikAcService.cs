using ConfigGet;
using HikAcessControl;
using PostgreConfig;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using WebCameraApi.Dto;

namespace WebCameraApi.Services
{
    public class HikAcService
    {
        private Dictionary<string, HikAcInfomationDto> HikAcConfigs = new Dictionary<string, HikAcInfomationDto>();
        private readonly ILogger<HikAcService> _logger;
        private string _connectionString;

        public HikAcService(ILogger<HikAcService> logger)
        {
            _logger = logger;
            InitPostgreAsync().Wait();
        }

        public async Task InitPostgreAsync()
        {
            try
            {
                // 1. 获取appsetting里的配置
                dynamic dbConfigDynamic = Appsettings_Get.GetConfigByKey("PostresSQLConfig");
                PgConnectionOptions pgConnectionOptions = new PgConnectionOptions();
                pgConnectionOptions.Host = dbConfigDynamic.host;
                pgConnectionOptions.Port = dbConfigDynamic.port;
                pgConnectionOptions.Username = dbConfigDynamic.username;
                pgConnectionOptions.Password = dbConfigDynamic.password;
                pgConnectionOptions.Database = dbConfigDynamic.database;
                _connectionString = pgConnectionOptions.BuildConnectionString();
                // 实例化仓储类（自动拼接连接字符串）
                var repository = new PgHikAccessControlRepository();

                await repository.CreateTableIfNotExistsAsync(_connectionString);

                HikAcConfigs = await repository.GetAllHikAcInfomationsAsync(_connectionString);
                if (HikAcConfigs.Count == 0)
                {
                    _logger.LogError("CameraConfigSql 初始化完成，但未加载到任何摄像头配置");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CameraConfigSql 初始化失败");
            }
        }

        public async Task<int> BatchUpsertHikAcConfigsAsync(IEnumerable<HikAcInfomationDto> configs)
        {
            if (configs == null || !configs.Any())
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                dynamic dbConfigDynamic = Appsettings_Get.GetConfigByKey("PostresSQLConfig");
                PgConnectionOptions pgConnectionOptions = new PgConnectionOptions();
                pgConnectionOptions.Host = dbConfigDynamic.host;
                pgConnectionOptions.Port = dbConfigDynamic.port;
                pgConnectionOptions.Username = dbConfigDynamic.username;
                pgConnectionOptions.Password = dbConfigDynamic.password;
                pgConnectionOptions.Database = dbConfigDynamic.database;
                _connectionString = pgConnectionOptions.BuildConnectionString();
            }

            var repository = new PgHikAccessControlRepository();
            await repository.CreateTableIfNotExistsAsync(_connectionString);
            var count = await repository.UpsertHikAcInfomationsAsync(_connectionString, configs);

            HikAcConfigs = await repository.GetAllHikAcInfomationsAsync(_connectionString);
            return count;
        }

        public async Task<(AcResponseDto response, StatusDto status)> OpenHikAccessGetway(string AcName = null, string AcIp = null)
        {
            var response = new AcResponseDto { AcName = AcName };
            var status = new StatusDto { isSuccess = false }; // 默认设为失败，避免误判
            string AcValue = string.Empty;
            if (AcName == null && AcIp == null)
            {
                _logger.LogError("至少传入一个名称或者IP");
                return (response, status);
            }
            else if (AcName == null && AcIp != null)
            {
                AcValue = AcIp;
            }
            else
            {
                AcValue = AcName;
            }
            try
            {
                var repository = new PgHikAccessControlRepository();
                var hikAcInfo = await repository.GetHikAcInfomationByIpOrNameAsync(HikAcConfigs, AcValue);
                response.AcName = hikAcInfo.HikAcName;
                HikAC hikAc = new HikAC(_logger);
                bool loginResult = hikAc.LoginAC(hikAcInfo.HikAcIP, (ushort)hikAcInfo.HikAcPort, hikAcInfo.HikAcUser, hikAcInfo.HikAcPassword);
                if (loginResult)
                {
                    // 登录成功后开门
                    bool openResult = hikAc.OpenGetway();
                    if (openResult)
                    {
                        status.isSuccess = true;
                        status.message = $"门禁 : {hikAcInfo.HikAcName}开门成功!";
                        return (response, status);
                    }
                    else
                    {
                        status.message = $"门禁 : {hikAcInfo.HikAcName}开门失败!";
                        return (response, status);
                    }
                }
                else
                {
                    status.message = $"门禁 : {hikAcInfo.HikAcName}登录失败!";
                    return (response, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取摄像头配置失败");
                return (response, status);
            }
        }
    }
}