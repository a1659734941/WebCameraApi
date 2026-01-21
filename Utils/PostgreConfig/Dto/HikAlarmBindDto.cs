namespace WebCameraApi.Utils.PostgreConfig.Dto
{
    public class HikAlarmBindDto
    {
        /// <summary>
        /// 计数摄像头房间名称
        /// </summary>
        public string HikAlarmCameraRoomName { get; set; }
        /// <summary>
        /// 计数摄像头IP
        /// </summary>
        public string HikAlarmCameraIP { get; set; }
        /// <summary>
        /// 被绑定的电脑IP
        /// </summary>
        public string BlockComputerIP { get; set; }
    }
}
