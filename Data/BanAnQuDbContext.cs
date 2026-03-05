using Microsoft.EntityFrameworkCore;
using WebCameraApi.Dto;

namespace WebCameraApi.Data
{
    /// <summary>
    /// 东海办案区数据库上下文
    /// </summary>
    public class DH_AppDbContext : DbContext
    {
        public DbSet<DH_T_ZFBAGL_RQCQRYXXB> T_ZFBAGL_RQCQRYXXB { get; set; }
        public DbSet<TEST> TEST { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "User Id=ZFBAGL_LYGDH;Password=zfbazx_lygdhgafz_20210728;Data Source=10.32.208.193:1521/orcl;Pooling=false";
            
            optionsBuilder.UseOracle(connectionString, opt =>
            {
                opt.UseOracleSQLCompatibility("11");
                opt.CommandTimeout(60);
            })
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        }
    }

    /// <summary>
    /// 广南办案区数据库上下文
    /// </summary>
    public class GN_AppDbContext : DbContext
    {
        public DbSet<GN_T_ZFBAGL_RQCQRYXXB> GN_T_ZFBAGL_RQCQRYXXB { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "User Id=ZFBAGL_LYGGN;Password=zfbazx_lyggngafz_20210915;Data Source=10.32.208.193:1521/orcl;Pooling=false";
            
            optionsBuilder.UseOracle(connectionString, opt =>
            {
                opt.UseOracleSQLCompatibility("11");
                opt.CommandTimeout(60);
            })
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        }
    }
}
