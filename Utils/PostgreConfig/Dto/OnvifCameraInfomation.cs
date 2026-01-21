using System;
using System.Collections.Generic;
using System.Text;

namespace PostgreConfig
{
    /// <summary>
    /// 摄像头配置类
    /// </summary>
    public class OnvifCameraInfomation
    {
        /// <summary>
        /// 摄像头名称（如 camera_1）
        /// </summary>
        public string CameraName { get; set; }

        /// <summary>
        /// 摄像头IP
        /// </summary>
        public string CameraIP { get; set; }

        /// <summary>
        /// 摄像头登录用户名
        /// </summary>
        public string CameraUser { get; set; }

        /// <summary>
        /// 摄像头登录密码
        /// </summary>
        public string CameraPassword { get; set; }

        /// <summary>
        /// 摄像头端口
        /// </summary>
        public int CameraPort { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int CameraRetryCount { get; set; }

        /// <summary>
        /// 等待毫秒数（注意字段名按你提供的拼写）
        /// </summary>
        public int CameraWaitmillisecounds { get; set; }

    }
}
