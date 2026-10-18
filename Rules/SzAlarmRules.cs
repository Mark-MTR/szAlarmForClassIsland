using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin;

/// <summary>
/// 自定义规则的处理逻辑。供 ClassIsland 行动/规则集系统调用。
/// 依赖统一格式 AlarmItem，与具体数据源无关。
/// </summary>
public static class SzAlarmRules
{
    /// <summary>
    /// 规则“存在深圳气象预警”的处理程序。
    /// 普通模式：是否存在至少一条满足（地区 ∩ 种类 ∩ 级别）条件的生效预警。
    /// 排他模式（ExclusiveType=true）：当前所有生效预警的类型都必须命中「灾害种类」，
    /// 且其中至少一条同时满足地区/级别过滤——即“有且仅有该（些）类型预警”。
    /// </summary>
    public static bool HasAlarmHandler(object? settings)
    {
        var s = Parse(settings);
        if (s is null) return false;

        var svc = SzAlarmService.Current;
        if (svc is null) return false;

        var items = svc.RawItems;
        if (items.Count == 0) return false;

        if (s.ExclusiveType)
        {
            var types = Split(s.Types);
            // 排他模式要求至少一个类型关键词，否则无法判定“仅有某类”
            if (types.Count == 0) return false;

            // 有且仅有：不允许出现任何未命中的预警类型
            var allInTypes = items.All(it => types.Any(t => TypeEquals(it.Type, t)));
            if (!allInTypes) return false;

            // 且其中至少一条完整满足（地区 ∩ 级别）过滤
            return items.Any(it => MatchesRegionAndLevel(s, it));
        }

        return items.Any(it => Matches(s, it));
    }

    /// <summary>
    /// 单条预警是否匹配给定规则设置（地区 ∩ 种类 ∩ 级别，三者为“与”）。
    /// 灾害种类为严格匹配：类型与关键词整段相等才命中（“雷暴”不会命中“雷暴大风”）。
    /// </summary>
    public static bool Matches(SzAlarmRuleSettings s, AlarmItem it)
    {
        var regions = Split(s.Regions);
        var types = Split(s.Types);
        var levels = Split(s.Levels).Select(CoreColor).ToHashSet();

        return (regions.Count == 0 || regions.Any(r => (it.Area ?? "").Contains(r))) &&
               (types.Count == 0 || types.Any(t => TypeEquals(it.Type, t))) &&
               (levels.Count == 0 || levels.Contains(CoreColor(it.Color)));
    }

    /// <summary>仅地区 ∩ 级别匹配（供排他模式使用，种类已整体判断）。</summary>
    private static bool MatchesRegionAndLevel(SzAlarmRuleSettings s, AlarmItem it)
    {
        var regions = Split(s.Regions);
        var levels = Split(s.Levels).Select(CoreColor).ToHashSet();

        return (regions.Count == 0 || regions.Any(r => (it.Area ?? "").Contains(r))) &&
               (levels.Count == 0 || levels.Contains(CoreColor(it.Color)));
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
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    /// <summary>灾害种类严格匹配（忽略首尾空白、区分大小写）。</summary>
    private static bool TypeEquals(string? type, string keyword) =>
        string.Equals((type ?? "").Trim(), keyword, StringComparison.Ordinal);

    /// <summary>
    /// 把预警颜色（如“红色”）或用户输入（如“红”/“红色”）归一到核心字：白/蓝/黄/橙/红。
    /// </summary>
    private static string? CoreColor(string? c) => AlarmLevels.CoreColor(c);
}
