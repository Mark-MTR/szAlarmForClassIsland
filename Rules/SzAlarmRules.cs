using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin;

/// <summary>
/// 自定义规则的处理逻辑。供 ClassIsland 行动/规则集系统调用。
/// </summary>
public static class SzAlarmRules
{
    /// <summary>
    /// 规则“存在深圳气象预警”的处理程序：
    /// 判断是否存在至少一条满足（地区 ∩ 种类 ∩ 级别）条件的生效预警。
    /// </summary>
    public static bool HasAlarmHandler(object? settings)
    {
        var s = Parse(settings);
        if (s is null) return false;

        var svc = SzAlarmService.Current;
        if (svc is null) return false;

        return svc.RawItems.Any(it => Matches(s, it));
    }

    /// <summary>
    /// 单条预警是否匹配给定规则设置（地区 ∩ 种类 ∩ 级别，三者为“与”）。
    /// 供规则处理与设置页实时预览共用。
    /// </summary>
    public static bool Matches(SzAlarmRuleSettings s, SzAlarmItem it)
    {
        var regions = Split(s.Regions);
        var types = Split(s.Types);
        var levels = Split(s.Levels).Select(CoreColor).ToHashSet();

        return (regions.Count == 0 || regions.Any(r => (it.AlarmArea ?? "").Contains(r))) &&
               (types.Count == 0 || types.Any(t => (it.AlarmType ?? "").Contains(t))) &&
               (levels.Count == 0 || levels.Contains(CoreColor(it.AlarmColor)));
    }

    /// <summary>当前生效预警中匹配给定设置的数量（用于设置页实时预览）。</summary>
    public static int CountMatches(SzAlarmRuleSettings s)
    {
        var svc = SzAlarmService.Current;
        if (svc is null) return 0;
        return svc.RawItems.Count(it => Matches(s, it));
    }

    public static SzAlarmRuleSettings? Parse(object? settings)
    {
        if (settings is SzAlarmRuleSettings direct) return direct;
        if (settings is JsonElement je)
        {
            try { return je.Deserialize<SzAlarmRuleSettings>(); }
            catch { return null; }
        }
        return null;
    }

    private static List<string> Split(string? s) =>
        (s ?? "")
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
            .ToList();

    /// <summary>
    /// 把预警颜色（如“红色”）或用户输入（如“红”/“红色”）归一到核心字：白/蓝/黄/橙/红。
    /// </summary>
    private static string? CoreColor(string? c)
    {
        if (string.IsNullOrEmpty(c)) return null;
        foreach (var k in new[] { "白", "蓝", "黄", "橙", "红" })
            if (c.Contains(k)) return k;
        return null;
    }
}
