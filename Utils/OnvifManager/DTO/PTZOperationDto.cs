namespace Onvif_GetPhoto.DTO
{
    public enum PTZOperation
{
    /// <summary>
    /// 获取云台状态
    /// </summary>
    GetStatus,
    /// <summary>
    /// 绝对移动（指定目标位置）
    /// </summary>
    AbsoluteMove,
    /// <summary>
    /// 停止移动
    /// </summary>
    Stop,
    /// <summary>
    /// 连续移动（指定速度）
    /// </summary>
    ContinuousMove,
    /// <summary>
    /// 设置_home位置
    /// </summary>
    SetHomePosition,
    /// <summary>
    /// 移动到_home位置
    /// </summary>
    GotoHomePosition,
    /// <summary>
    /// 相对移动（基于当前位置偏移）
    /// </summary>
    RelativeMove
}
}
