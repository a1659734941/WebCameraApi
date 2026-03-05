using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebCameraApi.Dto
{
    /// <summary>
    /// 办案区入区出区人员信息表 - 东海
    /// </summary>
    [Table("T_ZFBAGL_RQCQRYXXB", Schema = "ZFBAGL_LYGDH")]
    public class DH_T_ZFBAGL_RQCQRYXXB
    {
        /// <summary>
        /// 信息主键编号
        /// </summary>
        [Key]
        [Column("XXZJBH")]
        public string XXZJBH { get; set; } = string.Empty;

        /// <summary>
        /// 嫌疑人_信息主键编号
        /// </summary>
        [Column("RQRY_XXZJBH")]
        public string? RQRY_XXZJBH { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Column("RQRY_XM")]
        public string? RQRY_XM { get; set; }

        /// <summary>
        /// 证件号码
        /// </summary>
        [Column("ZJHM")]
        public string? ZJHM { get; set; }

        /// <summary>
        /// 性别代码
        /// </summary>
        [Column("XBDM")]
        public string? XBDM { get; set; }

        /// <summary>
        /// 人员属性
        /// </summary>
        [Column("RYSXDM")]
        public string? RYSXDM { get; set; }

        /// <summary>
        /// 案件编号(如果人员属性为嫌疑人请填写警综平台案件编号，对应此次入区的唯一一条案件编号)
        /// </summary>
        [Column("AJBH")]
        public string? AJBH { get; set; }

        /// <summary>
        /// 入区时间
        /// </summary>
        [Column("RQSJ")]
        public DateTime? RQSJ { get; set; }

        /// <summary>
        /// 出区时间
        /// </summary>
        [Column("CQSJ")]
        public DateTime? CQSJ { get; set; }

        /// <summary>
        /// 出区类型代码
        /// </summary>
        [Column("CQLXDM")]
        public string? CQLXDM { get; set; }

        /// <summary>
        /// 全流程视频访问地址
        /// </summary>
        [Column("FWDZ")]
        public string? FWDZ { get; set; }

        /// <summary>
        /// 办案区_行政区划代码
        /// </summary>
        [Column("BAQ_XZQHDM")]
        public string? BAQ_XZQHDM { get; set; }

        /// <summary>
        /// 办案区编号
        /// </summary>
        [Column("BAQBH")]
        public string? BAQBH { get; set; }

        /// <summary>
        /// 办案区_名称
        /// </summary>
        [Column("BAQ_MC")]
        public string? BAQ_MC { get; set; }

        /// <summary>
        /// 带入民警_公民身份号码
        /// </summary>
        [Column("DRMJ_GMSFHM")]
        public string? DRMJ_GMSFHM { get; set; }

        /// <summary>
        /// 带入民警_姓名
        /// </summary>
        [Column("DRMJ_XM")]
        public string? DRMJ_XM { get; set; }

        /// <summary>
        /// 带入民警_联系电话
        /// </summary>
        [Column("DRMJ_LXDH")]
        public string? DRMJ_LXDH { get; set; }

        /// <summary>
        /// 带入民警_公安机关机构代码
        /// </summary>
        [Column("DRMJ_GAJGJGDM")]
        public string? DRMJ_GAJGJGDM { get; set; }

        /// <summary>
        /// 带入民警_单位名称
        /// </summary>
        [Column("DRMJ_DWMC")]
        public string? DRMJ_DWMC { get; set; }

        /// <summary>
        /// 带出民警_公民身份号码
        /// </summary>
        [Column("DCMJ_GMSFHM")]
        public string? DCMJ_GMSFHM { get; set; }

        /// <summary>
        /// 带出民警_姓名
        /// </summary>
        [Column("DCMJ_XM")]
        public string? DCMJ_XM { get; set; }

        /// <summary>
        /// 带出民警_联系电话
        /// </summary>
        [Column("DCMJ_LXDH")]
        public string? DCMJ_LXDH { get; set; }

        /// <summary>
        /// 带出民警_公安机关机构代码
        /// </summary>
        [Column("DCMJ_GAJGJGDM")]
        public string? DCMJ_GAJGJGDM { get; set; }

        /// <summary>
        /// 带出民警_单位名称
        /// </summary>
        [Column("DCMJ_DWMC")]
        public string? DCMJ_DWMC { get; set; }

        /// <summary>
        /// 登记(变更)标识
        /// </summary>
        [Column("DJBGBS")]
        public string? DJBGBS { get; set; }

        /// <summary>
        /// 数据上传时间
        /// </summary>
        [Column("SJSCSJ")]
        public DateTime? SJSCSJ { get; set; }

        /// <summary>
        /// 违法犯罪嫌疑人入区原因
        /// </summary>
        [Column("RQYY")]
        public string? RQYY { get; set; }
    }

    /// <summary>
    /// 办案区入区出区人员信息表 - 广南
    /// </summary>
    [Table("T_ZFBAGL_RQCQRYXXB", Schema = "ZFBAGL_LYGGN")]
    public class GN_T_ZFBAGL_RQCQRYXXB
    {
        /// <summary>
        /// 信息主键编号
        /// </summary>
        [Key]
        [Column("XXZJBH")]
        public string XXZJBH { get; set; } = string.Empty;

        /// <summary>
        /// 嫌疑人_信息主键编号
        /// </summary>
        [Column("RQRY_XXZJBH")]
        public string? RQRY_XXZJBH { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Column("RQRY_XM")]
        public string? RQRY_XM { get; set; }

        /// <summary>
        /// 证件号码
        /// </summary>
        [Column("ZJHM")]
        public string? ZJHM { get; set; }

        /// <summary>
        /// 性别代码
        /// </summary>
        [Column("XBDM")]
        public string? XBDM { get; set; }

        /// <summary>
        /// 人员属性
        /// </summary>
        [Column("RYSXDM")]
        public string? RYSXDM { get; set; }

        /// <summary>
        /// 案件编号(如果人员属性为嫌疑人请填写警综平台案件编号，对应此次入区的唯一一条案件编号)
        /// </summary>
        [Column("AJBH")]
        public string? AJBH { get; set; }

        /// <summary>
        /// 入区时间
        /// </summary>
        [Column("RQSJ")]
        public DateTime? RQSJ { get; set; }

        /// <summary>
        /// 出区时间
        /// </summary>
        [Column("CQSJ")]
        public DateTime? CQSJ { get; set; }

        /// <summary>
        /// 出区类型代码
        /// </summary>
        [Column("CQLXDM")]
        public string? CQLXDM { get; set; }

        /// <summary>
        /// 全流程视频访问地址
        /// </summary>
        [Column("FWDZ")]
        public string? FWDZ { get; set; }

        /// <summary>
        /// 办案区_行政区划代码
        /// </summary>
        [Column("BAQ_XZQHDM")]
        public string? BAQ_XZQHDM { get; set; }

        /// <summary>
        /// 办案区编号
        /// </summary>
        [Column("BAQBH")]
        public string? BAQBH { get; set; }

        /// <summary>
        /// 办案区_名称
        /// </summary>
        [Column("BAQ_MC")]
        public string? BAQ_MC { get; set; }

        /// <summary>
        /// 带入民警_公民身份号码
        /// </summary>
        [Column("DRMJ_GMSFHM")]
        public string? DRMJ_GMSFHM { get; set; }

        /// <summary>
        /// 带入民警_姓名
        /// </summary>
        [Column("DRMJ_XM")]
        public string? DRMJ_XM { get; set; }

        /// <summary>
        /// 带入民警_联系电话
        /// </summary>
        [Column("DRMJ_LXDH")]
        public string? DRMJ_LXDH { get; set; }

        /// <summary>
        /// 带入民警_公安机关机构代码
        /// </summary>
        [Column("DRMJ_GAJGJGDM")]
        public string? DRMJ_GAJGJGDM { get; set; }

        /// <summary>
        /// 带入民警_单位名称
        /// </summary>
        [Column("DRMJ_DWMC")]
        public string? DRMJ_DWMC { get; set; }

        /// <summary>
        /// 带出民警_公民身份号码
        /// </summary>
        [Column("DCMJ_GMSFHM")]
        public string? DCMJ_GMSFHM { get; set; }

        /// <summary>
        /// 带出民警_姓名
        /// </summary>
        [Column("DCMJ_XM")]
        public string? DCMJ_XM { get; set; }

        /// <summary>
        /// 带出民警_联系电话
        /// </summary>
        [Column("DCMJ_LXDH")]
        public string? DCMJ_LXDH { get; set; }

        /// <summary>
        /// 带出民警_公安机关机构代码
        /// </summary>
        [Column("DCMJ_GAJGJGDM")]
        public string? DCMJ_GAJGJGDM { get; set; }

        /// <summary>
        /// 带出民警_单位名称
        /// </summary>
        [Column("DCMJ_DWMC")]
        public string? DCMJ_DWMC { get; set; }

        /// <summary>
        /// 登记(变更)标识
        /// </summary>
        [Column("DJBGBS")]
        public string? DJBGBS { get; set; }

        /// <summary>
        /// 数据上传时间
        /// </summary>
        [Column("SJSCSJ")]
        public DateTime? SJSCSJ { get; set; }

        /// <summary>
        /// 违法犯罪嫌疑人入区原因
        /// </summary>
        [Column("RQYY")]
        public string? RQYY { get; set; }
    }

    /// <summary>
    /// 测试表
    /// </summary>
    [Table("TEST", Schema = "ZFBAGL_LYGDH")]
    public class TEST
    {
        [Key]
        [Column("ID")]
        public string ID { get; set; } = string.Empty;

        [Column("TYPE")]
        public string? TYPE { get; set; }
    }

    /// <summary>
    /// 分页查询参数
    /// </summary>
    public class PageQueryInfo
    {
        /// <summary>
        /// 入区人员姓名
        /// </summary>
        public string? RQRY_XM { get; set; }

        /// <summary>
        /// 信息主键编号
        /// </summary>
        public string? XXZJBH { get; set; }

        /// <summary>
        /// 入区人员信息主键编号
        /// </summary>
        public string? RQRY_XXZJBH { get; set; }

        /// <summary>
        /// 案件编号
        /// </summary>
        public string? AJBH { get; set; }

        /// <summary>
        /// 页面大小
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageNum { get; set; } = 1;
    }
}
