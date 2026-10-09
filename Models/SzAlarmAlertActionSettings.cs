namespace SzAlarmPlugin.Models;

/// <summary>
/// 行动“深圳气象预警提醒”的设置。
/// </summary>
public class SzAlarmAlertActionSettings
{
    /// <summary>
    /// 播报策略：0 = 每次触发都播报；1 = 同一预警仅首次播报（去重）。
    /// </summary>
    public int AnnounceMode { get; set; } = 0;

    /// <summary>地名关键词（空格分隔，留空=所有地区）。</summary>
    public string Regions { get; set; } = "";

    /// <summary>灾害种类关键词（空格分隔，留空=所有种类）。</summary>
    public string Types { get; set; } = "";

    /// <summary>预警级别关键词（白 蓝 黄 橙 红，留空=所有级别）。</summary>
    public string Levels { get; set; } = "";

    /// <summary>正文文字横向滚动速度（1=慢，20=快）。</summary>
    public int ScrollSpeed { get; set; } = 5;

    /// <summary>提醒最大显示时长（秒，10~90）。</summary>
    public int MaxDuration { get; set; } = 15;
}
