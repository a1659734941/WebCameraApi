namespace HikAlarmEndPoints
{
    /// <summary>
    /// 报警记录表的数据传输对象（DTO）
    /// </summary>
    public class HikAlarmRecordDto
    {
        /// <summary>
        /// 报警记录的唯一标识符
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 报警的具体类型
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 报警事件发生的时间
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// 触发报警的设备名称
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 触发报警的通道名称
        /// </summary>
        public string? ChannelName { get; set; }

        /// <summary>
        /// 报警任务的名称
        /// </summary>
        public string? TaskName { get; set; }

        /// <summary>
        /// 报警事件的快照图片的Base64编码路径
        /// </summary>
        public string SnapshotBase64Path { get; set; }

        /// <summary>
        /// 报警事件的原始数据
        /// </summary>
        public string RawData { get; set; }
    }

    /// <summary>
    /// 报警统计返回对象（用于图表展示）
    /// </summary>
    public class AlarmCountDto
    {
        /// <summary>
        /// 名称（事件类型中文名）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数值（出现次数）
        /// </summary>
        public int Value { get; set; }
    }
}
