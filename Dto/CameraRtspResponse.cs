namespace WebCameraApi.Dto
{
    public class CameraRtspResponse
    {
        /// <summary>
        /// 摄像头名称
        /// </summary>
        public string CameraName { get; set; }
        /// <summary>
        /// 摄像头RTSP地址
        /// </summary>
        public string RtspUrl { get; set; }

        /// <summary>
        /// 获取状态
        /// </summary>
        public class StatusInfo
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool IsSuccess { get; set; }
            /// <summary>
            /// 消息内容
            /// </summary>
            public string Message { get; set; }
        }
    }
}
