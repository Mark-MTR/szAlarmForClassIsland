using System.Text.Json.Serialization;

namespace SzAlarmPlugin.Models;

/// <summary>接口返回的根对象。</summary>
public class SzAlarmRoot
{
    public string? AlarmInfo { get; set; }
    public string? AlarmDate { get; set; }

    /// <summary>根节点 ingnalNum 接口返回字符串（如 "548"）。</summary>
    public string? IngnalNum { get; set; }

    /// <summary>生效中的预警列表（全市各分区）。</summary>
    public List<SzAlarmItem>? SubAlarm { get; set; }

    /// <summary>深汕特别合作区预警列表。</summary>
    public List<SzAlarmItem>? SshzqAlarm { get; set; }
}

/// <summary>单条预警信号。</summary>
public class SzAlarmItem
{
    public string? Pic { get; set; }
    public string? PicS { get; set; }
    public int Ingnalnum { get; set; }
    public string? Date { get; set; }
    public long RecId { get; set; }
    public string? AlarmArea { get; set; }
    public string? AlarmType { get; set; }
    public string? AlarmColor { get; set; }

    /// <summary>预警图标名，如 "taifengchengse"，拼成 https://weather.sz.gov.cn/alarmIcon/{icon}.png</summary>
    public string? Icon { get; set; }

    /// <summary>预警正文（带【标题】的完整文本）。接口字段名为 str。</summary>
    [JsonPropertyName("str")]
    public string? Str { get; set; }

    public bool IsShenShan { get; set; }
    public int IfTyphoon { get; set; }

    /// <summary>官方优先级，越小越优先。</summary>
    public int Order { get; set; }
}
