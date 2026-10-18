using System;

namespace SzAlarmPlugin.Models;

/// <summary>
/// 统一预警项（“指定格式”）。
/// 任何城市 / 任何预警 API 的数据源，最终都映射成这个结构交给插件使用。
/// 组件、规则、提醒只依赖这个统一格式，完全不感知具体接口。
/// 以后接新城市 / 新 API，照数据源文件（如 ShenZhenAlarmSource）写一个即可。
/// </summary>
public class AlarmItem
{
    /// <summary>数据源名称（城市 / 来源），如 “深圳”。</summary>
    public string Source { get; set; } = "";

    /// <summary>预警地区（分区），如 “全市”、“福田区”。</summary>
    public string Area { get; set; } = "";

    /// <summary>灾害种类，如 “台风”、“暴雨”、“高温”。</summary>
    public string Type { get; set; } = "";

    /// <summary>预警级别，如 “蓝色”、“黄色”、“橙色”、“红色”（可为空表示无级别）。</summary>
    public string Color { get; set; } = "";

    /// <summary>
    /// 预警正文（完整文本，用于滚动显示与语音播报）。
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// 完整预警名（可选）。用于「灾种 + 级别 + 预警」模板无法表达的信号体系，
    /// 例如港澳的 “三号强风信号”、“一号戒备信号”、“山泥倾泻警告”、“雷暴警告” 等。
    /// 填了就直接用它；留空则回退为 “{Type}{Color}预警” 的大陆模板（见 DisplayName）。
    /// </summary>
    public string FullName { get; set; } = "";

    /// <summary>预警发布方名称，如 “深圳市气象台”。数据源未提供时回退为 “预警方”。</summary>
    public string IssuerName { get; set; } = "预警方";

    /// <summary>图标名（无扩展名），用于本地 *.png 匹配，如 “taifengchengse”。</summary>
    public string IconName { get; set; } = "";

    /// <summary>图标完整地址（远程官方图标 URL，用于自动下载并缓存）。</summary>
    public string IconUrl { get; set; } = "";

    /// <summary>发布时间（可选，用于“最新一条”等排序）。</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>源内优先级（可选，越小越优先）。</summary>
    public int Order { get; set; }

    /// <summary>级别权重（白0 蓝1 黄2 橙3 红4），用于排序与“最高级别”筛选。</summary>
    public int Level => AlarmLevels.Of(Color);

    /// <summary>核心色字（白/蓝/黄/橙/红），用于关键词匹配与染色。</summary>
    public string CoreColor => AlarmLevels.CoreColor(Color) ?? "";

    /// <summary>
    /// 展示用预警名：优先用 FullName（兼容港澳信号/警告体系）；
    /// 否则回退为 “{Type}{Color}预警” 的大陆模板（Type/Color 缺失时自动省略对应部分）。
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FullName)) return FullName.Trim();

            var t = (Type ?? "").Trim();
            var c = (Color ?? "").Trim();

            if (t.Length == 0 && c.Length == 0) return "预警";
            if (t.Length == 0) return c + "预警";
            if (c.Length == 0) return t + "预警";
            return t + c + "预警";
        }
    }
}

/// <summary>
/// 级别 / 颜色归一化与权重。统一逻辑，各数据源与各消费方共用，避免到处重复 switch。
/// </summary>
public static class AlarmLevels
{
    private static readonly string[] Colors = { "白", "蓝", "黄", "橙", "红" };

    /// <summary>级别 → 权重：白0 蓝1 黄2 橙3 红4；未知/空 → 0。</summary>
    public static int Of(string? color)
    {
        if (string.IsNullOrEmpty(color)) return 0;
        for (var i = 0; i < Colors.Length; i++)
            if (color.Contains(Colors[i], StringComparison.Ordinal)) return i;
        return 0;
    }

    /// <summary>把 “红色”/“红”/“橙色” 等归一到核心字：白/蓝/黄/橙/红；匹配不到返回 null。</summary>
    public static string? CoreColor(string? color)
    {
        if (string.IsNullOrEmpty(color)) return null;
        foreach (var k in Colors)
            if (color.Contains(k, StringComparison.Ordinal)) return k;
        return null;
    }
}
