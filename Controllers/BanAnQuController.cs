using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebCameraApi.Data;
using WebCameraApi.Dto;

namespace WebCameraApi.Controllers
{
    /// <summary>
    /// 办案区上传接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BanAnQuController : ControllerBase
    {
        private readonly ILogger<BanAnQuController> _logger;

        public BanAnQuController(ILogger<BanAnQuController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 东海 - 查询入区出区人员信息列表
        /// </summary>
        [HttpPost("DH/List")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> DH_List([FromBody] PageQueryInfo query)
        {
            using var context = new DH_AppDbContext();
            
            var dbQuery = context.T_ZFBAGL_RQCQRYXXB.AsQueryable();

            if (!string.IsNullOrEmpty(query.XXZJBH))
                dbQuery = dbQuery.Where(x => x.XXZJBH == query.XXZJBH);

            if (!string.IsNullOrEmpty(query.RQRY_XXZJBH))
                dbQuery = dbQuery.Where(x => x.RQRY_XXZJBH == query.RQRY_XXZJBH);

            if (!string.IsNullOrEmpty(query.AJBH))
                dbQuery = dbQuery.Where(x => x.AJBH == query.AJBH);

            if (!string.IsNullOrEmpty(query.RQRY_XM))
                dbQuery = dbQuery.Where(x => x.RQRY_XM != null && x.RQRY_XM.Contains(query.RQRY_XM));

            int total = await dbQuery.CountAsync();
            var data = await dbQuery
                .OrderByDescending(x => x.RQSJ)
                .Skip((query.PageNum - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return Ok(new
            {
                code = 200,
                msg = "查询成功",
                data = new
                {
                    list = data,
                    total,
                    pageNum = query.PageNum,
                    pageSize = query.PageSize
                }
            });
        }

        /// <summary>
        /// 东海 - 新增或更新入区出区人员信息
        /// </summary>
        [HttpPost("DH/Upsert")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> DH_Upsert([FromBody] DH_T_ZFBAGL_RQCQRYXXB model)
        {
            using var context = new DH_AppDbContext();
            
            try
            {
                _logger.LogInformation("东海Upsert: {Model}", System.Text.Json.JsonSerializer.Serialize(model));
                
                if (string.IsNullOrEmpty(model.XXZJBH))
                    return Ok(new { code = 400, msg = "信息主键编号不能为空" });

                var existing = await context.T_ZFBAGL_RQCQRYXXB
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.XXZJBH == model.XXZJBH);

                if (existing != null)
                {
                    context.Update(model);
                    await context.SaveChangesAsync();
                    return Ok(new { code = 200, msg = "更新成功" });
                }
                else
                {
                    context.Add(model);
                    await context.SaveChangesAsync();
                    return Ok(new { code = 200, msg = "新增成功" });
                }
            }
            catch (DbUpdateException ex)
            {
                return Ok(new { code = 500, msg = $"数据库操作失败：{ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// 广南 - 查询入区出区人员信息列表
        /// </summary>
        [HttpPost("GN/List")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GN_List([FromBody] PageQueryInfo query)
        {
            using var context = new GN_AppDbContext();
            
            var dbQuery = context.GN_T_ZFBAGL_RQCQRYXXB.AsQueryable();

            if (!string.IsNullOrEmpty(query.XXZJBH))
                dbQuery = dbQuery.Where(x => x.XXZJBH == query.XXZJBH);

            if (!string.IsNullOrEmpty(query.RQRY_XXZJBH))
                dbQuery = dbQuery.Where(x => x.RQRY_XXZJBH == query.RQRY_XXZJBH);

            if (!string.IsNullOrEmpty(query.AJBH))
                dbQuery = dbQuery.Where(x => x.AJBH == query.AJBH);

            if (!string.IsNullOrEmpty(query.RQRY_XM))
                dbQuery = dbQuery.Where(x => x.RQRY_XM != null && x.RQRY_XM.Contains(query.RQRY_XM));

            int total = await dbQuery.CountAsync();
            var data = await dbQuery
                .OrderByDescending(x => x.RQSJ)
                .Skip((query.PageNum - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return Ok(new
            {
                code = 200,
                msg = "查询成功",
                data = new
                {
                    list = data,
                    total,
                    pageNum = query.PageNum,
                    pageSize = query.PageSize
                }
            });
        }

        /// <summary>
        /// 广南 - 新增或更新入区出区人员信息
        /// </summary>
        [HttpPost("GN/Upsert")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GN_Upsert([FromBody] GN_T_ZFBAGL_RQCQRYXXB model)
        {
            using var context = new GN_AppDbContext();
            
            try
            {
                _logger.LogInformation("广南Upsert: {Model}", System.Text.Json.JsonSerializer.Serialize(model));
                
                if (string.IsNullOrEmpty(model.XXZJBH))
                    return Ok(new { code = 400, msg = "信息主键编号不能为空" });

                var existing = await context.GN_T_ZFBAGL_RQCQRYXXB
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.XXZJBH == model.XXZJBH);

                if (existing != null)
                {
                    context.Update(model);
                    await context.SaveChangesAsync();
                    return Ok(new { code = 200, msg = "更新成功" });
                }
                else
                {
                    context.Add(model);
                    await context.SaveChangesAsync();
                    return Ok(new { code = 200, msg = "新增成功" });
                }
            }
            catch (DbUpdateException ex)
            {
                return Ok(new { code = 500, msg = $"数据库操作失败：{ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// 测试接口 - 查询测试表
        /// </summary>
        [HttpPost("Test/List")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Test_List()
        {
            using var context = new DH_AppDbContext();
            
            var data = await context.TEST.ToListAsync();
            
            return Ok(new { code = 200, msg = "", data });
        }

        /// <summary>
        /// 测试接口 - 新增或更新测试数据
        /// </summary>
        [HttpPost("Test/Upsert")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Test_Upsert([FromBody] TEST model)
        {
            using var context = new DH_AppDbContext();
            
            try
            {
                _logger.LogInformation("Test Upsert: {Model}", System.Text.Json.JsonSerializer.Serialize(model));
                
                if (string.IsNullOrEmpty(model.ID))
                    return Ok(new { code = 400, msg = "信息主键编号不能为空" });

                var existing = await context.TEST
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ID == model.ID);

                if (existing != null)
                {
                    context.Update(model);
                    await context.SaveChangesAsync();
                    return Ok(new { code = 200, msg = "更新成功" });
                }
                else
                {
                    context.Add(model);
                    await context.SaveChangesAsync();
                    return Ok(new { code = 200, msg = "新增成功" });
                }
            }
            catch (DbUpdateException ex)
            {
                return Ok(new { code = 500, msg = $"数据库操作失败：{ex.InnerException?.Message ?? ex.Message}" });
            }
        }
    }
}
