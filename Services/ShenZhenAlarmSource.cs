using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Services;

/// <summary>
/// 深圳气象预警数据源：拉取深圳市气象台接口，映射为统一的 AlarmItem。
/// 这一份就是“专门获取预警信息 + 图标地址”的文件——
/// 以后接新城市 / 新 API，复制本文件改一改即可。
/// </summary>
public class ShenZhenAlarmSource : IAlarmSource
{
    public string Name => "深圳";

    private static readonly Uri ApiUri = new("https://weather.121.com.cn/data_cache/szWeather/alarm/szAlarm.json");

    /// <summary>官方图标源基址：图标完整地址 = IconBase + 图标名 + ".png"。</summary>
    private const string IconBase = "https://weather.sz.gov.cn/alarmIcon/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // 接口部分数字字段以字符串返回（如 ingnalNum:"548"），允许从字符串读数字
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _http = new();

    public async Task<IReadOnlyList<AlarmItem>> FetchAsync()
    {
        var json = await _http.GetStringAsync(ApiUri);
        var root = JsonSerializer.Deserialize<SzAlarmRoot>(json, JsonOptions);
        if (root?.SubAlarm is null) return Array.Empty<AlarmItem>();

        // 映射成统一格式，再按源内优先级 + 级别排序
        return root.SubAlarm
            .Where(a => !string.IsNullOrWhiteSpace(a.AlarmType))
            .Select(ToUnified)
            .OrderBy(a => a.Order)
            .ThenByDescending(a => a.Level)
            .ToList();
    }

    /// <summary>深圳接口单条记录 → 统一格式（含图标完整地址）。</summary>
    private static AlarmItem ToUnified(SzAlarmItemDto d)
    {
        var iconName = d.Icon ?? "";
        return new AlarmItem
        {
            Source = "深圳",
            Area = d.AlarmArea ?? "",
            Type = d.AlarmType ?? "",
            Color = d.AlarmColor ?? "",
            Text = d.Str ?? "",
            IssuerName = "深圳市气象台",
            IconName = iconName,
            IconUrl = string.IsNullOrEmpty(iconName) ? "" : IconBase + iconName + ".png",
            PublishedAt = DateTime.TryParse(d.Date, out var dt) ? dt : null,
            Order = d.Order
        };
    }

    // ---- 深圳接口原始结构（仅本数据源内部使用，不对外暴露） ----
    private class SzAlarmRoot
    {
        public string? AlarmInfo { get; set; }
        public string? AlarmDate { get; set; }
        public string? IngnalNum { get; set; }
        public List<SzAlarmItemDto>? SubAlarm { get; set; }
        public List<SzAlarmItemDto>? SshzqAlarm { get; set; }
    }

    private class SzAlarmItemDto
    {
        public string? Pic { get; set; }
        public string? PicS { get; set; }
        public int Ingnalnum { get; set; }
        public string? Date { get; set; }
        public long RecId { get; set; }
        public string? AlarmArea { get; set; }
        public string? AlarmType { get; set; }
        public string? AlarmColor { get; set; }
        public string? Icon { get; set; }

        /// <summary>预警正文，接口字段名为 str。</summary>
        [JsonPropertyName("str")]
        public string? Str { get; set; }

        public bool IsShenShan { get; set; }
        public int IfTyphoon { get; set; }
        public int Order { get; set; }
    }
}
