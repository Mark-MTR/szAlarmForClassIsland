using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Components;

/// <summary>
/// 深圳气象预警组件：图标 + 文字，多预警横排，行为由组件设置控制。
/// 数据来自 SzAlarmService（统一格式 AlarmItem）。
/// </summary>
[ComponentInfo("5C3E1F2A-9B4D-4E6A-8C7B-1D2E3F4A5B6C", "深圳气象预警", "\uE7BA",
    "显示深圳市气象台发布的实时预警信号。")]
public partial class SzAlarmComponent : ComponentBase<SzAlarmComponentSettings>
{
    public SzAlarmService AlarmService { get; }

    private Bitmap? _placeholderBitmap;

    public SzAlarmComponent(SzAlarmService alarmService)
    {
        AlarmService = alarmService;
        InitializeComponent();
    }

    private void Control_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (Settings is not null)
            Settings.PropertyChanged += OnSettingsChanged;
        AlarmService.PropertyChanged += OnServiceChanged;

        _ = AlarmService.RefreshAsync(); // 兜底刷新一次
        Rebuild();
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SzAlarmComponentSettings.UseLocalIcons)
            or nameof(SzAlarmComponentSettings.LocalIconFolder))
        {
            AlarmService.ClearLocalCache();
        }
        Rebuild();
    }

    private void OnServiceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SzAlarmService.RawItems) or nameof(SzAlarmService.HasAlarm))
            Rebuild();
    }

    private void Rebuild()
    {
        if (Settings is null) return;

        // 1) 区域过滤（空格分隔关键词）
        var keywords = (Settings.RegionFilter ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // 1.5) 预警类型排除（空格分隔关键词，黑名单：类型与关键词严格相等才忽略该条）
        var typeKeywords = (Settings.TypeFilter ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var filtered = AlarmService.RawItems
            .Where(it => keywords.Count == 0 ||
                         keywords.Any(k => (it.Area ?? "").Contains(k)))
            .Where(it => typeKeywords.Count == 0 ||
                         !typeKeywords.Any(k => string.Equals((it.Type ?? "").Trim(), k, StringComparison.Ordinal)))
            .ToList();

        // 2) 过滤后再按显示模式选择
        var displayed = filtered;
        switch (Settings.DisplayMode)
        {
            case 1: // 最高级别（含并列）
                if (filtered.Count > 0)
                {
                    var max = filtered.Max(a => a.Level);
                    displayed = filtered.Where(a => a.Level == max).ToList();
                }
                break;
            case 2: // 最新一条
                displayed = filtered
                    .OrderByDescending(a => a.PublishedAt ?? DateTime.MinValue)
                    .Take(1)
                    .ToList();
                break;
            case 3: // 每种预警中级别最高的一条
                displayed = filtered
                    .GroupBy(a => a.Type)
                    .Select(g => g.OrderByDescending(a => a.Level).First())
                    .ToList();
                break;
            default: // 0 = 所有
                displayed = filtered;
                break;
        }

        AlarmItems.ItemsSource = displayed
            .Select(it => new AlarmEntry(it,
                AlarmService.GetIcon(it, Settings.UseLocalIcons, Settings.LocalIconFolder),
                Settings.ShowIcon))
            .ToList();

        var hasVisible = displayed.Count > 0;
        AlarmItems.IsVisible = hasVisible;

        // 3) 无预警占位
        PlaceholderPanel.IsVisible = !hasVisible &&
                                     (Settings.ShowPlaceholderText || Settings.ShowPlaceholderImage);
        PlaceholderTextBlock.IsVisible = !hasVisible && Settings.ShowPlaceholderText;
        PlaceholderTextBlock.Text = Settings.PlaceholderText;
        PlaceholderImageBorder.IsVisible = !hasVisible && Settings.ShowPlaceholderImage;
        UpdatePlaceholderImage();
    }

    private void UpdatePlaceholderImage()
    {
        _placeholderBitmap?.Dispose();
        _placeholderBitmap = null;
        if (!string.IsNullOrWhiteSpace(Settings?.PlaceholderImagePath) &&
            File.Exists(Settings.PlaceholderImagePath))
        {
            try
            {
                using var fs = File.OpenRead(Settings.PlaceholderImagePath);
                _placeholderBitmap = new Bitmap(fs);
            }
            catch (Exception)
            {
                _placeholderBitmap = null;
            }
        }

        PlaceholderImage.Source = _placeholderBitmap;
        var size = Settings?.IconSize ?? 22;
        PlaceholderImage.Height = size;
        PlaceholderImage.Width = size;
    }
}
