namespace SzAlarmPlugin.Models;

/// <summary>
/// 自定义规则“存在深圳气象预警”的设置。
/// 三个过滤条件留空均表示“不限制（匹配所有）”。
/// </summary>
public class SzAlarmRuleSettings
{
    /// <summary>
    /// 地名关键词，空格分隔（如：南山 福田 宝安）。
    /// 仅当某条预警的发布区域包含其中任一关键词时才计入；留空表示所有地区。
    /// </summary>
    public string Regions { get; set; } = "";

    /// <summary>
    /// 灾害种类关键词，空格分隔（如：台风 暴雨 雷雨大风）。
    /// 仅当某条预警的类型包含其中任一关键词时才计入；留空表示所有种类。
    /// </summary>
    public string Types { get; set; } = "";

    /// <summary>
    /// 预警级别关键词，空格分隔（白 蓝 黄 橙 红）。
    /// 仅当某条预警的级别命中其中任一关键词时才计入；留空表示所有级别。
    /// </summary>
    public string Levels { get; set; } = "";
}
