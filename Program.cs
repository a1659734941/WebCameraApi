// 引入Serilog日志组件命名空间，用于实现结构化日志记录
using Serilog;
// 引入WebCameraApi项目中的服务层命名空间，包含自定义业务服务
using WebCameraApi.Services;
using System.Text;
using System.Linq;
// 定义WebCameraApi项目的根命名空间
namespace WebCameraApi
{
    /// <summary>
    /// 应用程序入口点类
    /// 负责配置和启动ASP.NET Core Web API应用
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 应用程序主入口方法
        /// 程序启动时首先执行此方法
        /// </summary>
        /// <param name="args">命令行参数</param>
        public static void Main(string[] args)
        {
            // 创建Web应用程序构建器，负责配置应用的所有服务和中间件
            var builder = WebApplication.CreateBuilder(args);
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            #region Serilog 日志配置
            // 配置Serilog日志记录器，替代默认的Microsoft.Extensions.Logging
            Log.Logger = new LoggerConfiguration()
                // 设置日志最低级别为Information，低于此级别的日志（如Debug）不会被记录
                .MinimumLevel.Information()
                // 将日志输出到控制台，方便开发调试时实时查看
                .WriteTo.Console()
                // 将日志输出到文件系统
                .WriteTo.File(
                    path: "logs/Log-.txt",                // 日志文件存储路径，文件名前缀为Log-
                    rollingInterval: RollingInterval.Day, // 按天滚动生成新日志文件（如Log-20260121.txt）
                    retainedFileCountLimit: 180,          // 保留最近180天的日志文件，自动清理过期文件
                                                          // 日志输出模板：包含时间戳、日志级别、日志来源、消息内容、异常信息
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}")
                // 从日志上下文（LogContext）中添加丰富的日志属性
                .Enrich.FromLogContext()
                // 创建最终的日志记录器实例
                .CreateLogger();
            #endregion

            #region 生产环境URL配置
            // 判断当前是否为生产环境（通过环境变量ASPNETCORE_ENVIRONMENT判断）
            if (builder.Environment.IsProduction())
            {
                // 从配置文件（如appsettings.json）中读取自定义的URL配置
                var customUrls = builder.Configuration["Urls"];
                // 如果配置了自定义URL，则使用该URL启动应用（替代默认的5000/5001端口）
                if (!string.IsNullOrEmpty(customUrls))
                {
                    var urls = customUrls
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(url => url.Trim())
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .ToList();

                    // 未配置证书时移除 https 监听，避免 Windows 下权限/证书绑定错误
                    if (!HasHttpsCertificate(builder.Configuration))
                    {
                        urls = urls
                            .Where(url => !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (urls.Count > 0)
                    {
                        builder.WebHost.UseUrls(string.Join(';', urls));
                    }
                }
            }
            #endregion

            #region CORS跨域配置
            // 添加CORS（跨域资源共享）服务配置
            builder.Services.AddCors(options =>
            {
                // 定义名为"AllowAll"的跨域策略（生产环境建议按需配置，不要允许所有来源）
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()   // 允许所有来源的跨域请求
                          .AllowAnyMethod()   // 允许所有HTTP方法（GET/POST/PUT/DELETE等）
                          .AllowAnyHeader();  // 允许所有请求头
                });
            });
            #endregion

            #region HttpClient配置
            // 注册命名的HttpClient实例，名称为"hik"，用于调用海康威视相关接口
            builder.Services.AddHttpClient("hik", client =>
            {
                // 设置HttpClient的请求超时时间为3秒，与业务层超时保持一致
                client.Timeout = TimeSpan.FromMilliseconds(3000);
            })
            // 配置HttpClient的底层消息处理器
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false, // 禁用自动重定向，避免请求被意外重定向
                UseCookies = false         // 禁用Cookie，避免跨请求携带Cookie导致的问题
            });
            #endregion

            #region 依赖注入服务注册
            // 将Serilog集成到ASP.NET Core的日志系统中，替代默认日志提供程序
            builder.Host.UseSerilog();

            // 添加控制器服务，启用API控制器功能（支持[ApiController]、[Route]等特性）
            builder.Services.AddControllers();

            // 添加API端点探索服务，用于Swagger文档生成
            builder.Services.AddEndpointsApiExplorer();

            // 添加Swagger生成器服务，用于生成API文档和调试界面
            builder.Services.AddSwaggerGen();

            // 注册摄像头RTSP服务为Scoped生命周期
            builder.Services.AddScoped<CameraRtspService>();

            // 注册海康AC服务为Scoped生命周期
            builder.Services.AddScoped<HikAcService>();

            // 注册海康报警服务（HikAlarmService）为Scoped生命周期
            // Scoped：每个HTTP请求创建一个新的服务实例
            builder.Services.AddScoped<HikAlarmService>();
            #endregion

            // 构建Web应用程序实例，应用所有配置的服务和中间件
            var app = builder.Build();

            #region HTTP请求管道配置
            // 启用Swagger中间件，提供Swagger JSON端点（默认路径：/swagger/v1/swagger.json）
            app.UseSwagger();

            // 启用Swagger UI中间件，提供可视化的API调试界面（默认路径：/swagger）
            app.UseSwaggerUI();

            // 应用CORS中间件，使用名为"AllowAll"的跨域策略
            // 注意：CORS中间件必须在UseRouting和UseAuthorization之前
            app.UseCors("AllowAll");

            // 启用HTTPS重定向中间件，将HTTP请求重定向到HTTPS
            app.UseHttpsRedirection();

            // 启用路由中间件，解析请求URL并匹配到对应的控制器/动作
            app.UseRouting();

            // 启用授权中间件，处理基于策略的授权
            app.UseAuthorization();

            // 映射控制器路由，将请求路由到对应的控制器和动作方法
            app.MapControllers();
            #endregion

            // 启动Web应用程序，开始监听HTTP请求
            app.Run();
        }

        private static bool HasHttpsCertificate(IConfiguration configuration)
        {
            var certPath = configuration["Kestrel:Certificates:Default:Path"];
            var certPassword = configuration["Kestrel:Certificates:Default:Password"];
            return !string.IsNullOrWhiteSpace(certPath) && !string.IsNullOrWhiteSpace(certPassword);
        }
    }
}