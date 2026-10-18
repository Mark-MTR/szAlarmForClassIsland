using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin;

/// <summary>
/// 规则“存在深圳气象预警”的设置控件。
/// 注意：基类 RuleSettingsControlBase.Settings 只是个只读 getter，且框架会复用单例控件、
/// 随时替换内部 Settings 对象而不发通知，因此不能在 XAML 里直接 TwoWay 绑 Settings.X
/// （会绑到被丢弃的旧对象，导致输入一刷新就清空）。
/// 改为代码驱动：面板打开时从 Settings 同步进输入框，输入时实时写回 Settings。
/// </summary>
public partial class SzAlarmRuleSettingsControl : RuleSettingsControlBase<SzAlarmRuleSettings>
{
    private readonly DispatcherTimer _timer;

    public SzAlarmRuleSettingsControl()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (_, _) => UpdatePreview();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _timer.Stop();
		CbExclusive.Click += OnExclusiveChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 每次面板打开/复用都从当前 Settings 重新同步（这是修复“输入被清空”的关键）
        if (Settings is not null)
        {
            TbRegions.Text = Settings.Regions ?? "";
            TbTypes.Text = Settings.Types ?? "";
            TbLevels.Text = Settings.Levels ?? "";
            CbExclusive.IsChecked = Settings.ExclusiveType;
        }
        UpdatePreview();
        _timer.Start();
    }

    private void OnInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (Settings is null) return;
        // 实时写回框架持有的 Settings 对象（即规则存档对象），保证持久化
        Settings.Regions = TbRegions.Text;
        Settings.Types = TbTypes.Text;
        Settings.Levels = TbLevels.Text;
        UpdatePreview();
    }
	
	private void OnExclusiveChanged(object? sender, RoutedEventArgs e)
	{
		if (Settings is null) return;
		Settings.ExclusiveType = CbExclusive.IsChecked == true;
		UpdatePreview();
	}

    private void UpdatePreview()
    {
        if (Settings is null) return;

        var regions = Split(Settings.Regions);
        var types = Split(Settings.Types);
        var levels = Split(Settings.Levels);

        var regionPart = regions.Count == 0 ? "任意地区" : "地区含「" + string.Join("、", regions) + "」";
        var typePart = types.Count == 0 ? "任意灾害" : "种类含「" + string.Join("、", types) + "」";
        var levelPart = levels.Count == 0 ? "任意级别" : "级别为「" + string.Join("、", levels) + "」";

        if (Settings.ExclusiveType)
        {
            PreviewSentence.Text =
                $"当所有生效预警的种类都属于「{string.Join("、", types)}」，且其中至少一条满足 {regionPart}、{levelPart} 时，判定为「存在预警」（有且仅有）。";
        }
        else
        {
            PreviewSentence.Text =
                $"当 {regionPart} 有 {levelPart} 的 {typePart} 预警时，判定为「存在预警」。三者需同时满足。";
        }

        var count = SzAlarmRules.CountMatches(Settings);
        if (SzAlarmService.Current is null)
        {
            MatchInfo.Text = "预警服务未就绪";
            MatchBadge.Background = Brushes.Gray;
            MatchInfo.Foreground = Brushes.White;
        }
        else if (count > 0)
        {
            MatchInfo.Text = $"当前满足条件：{count} 条";
            MatchBadge.Background = new SolidColorBrush(Color.Parse("#2E7D32"));
            MatchInfo.Foreground = Brushes.White;
        }
        else
        {
            MatchInfo.Text = "当前满足条件：0 条";
            MatchBadge.Background = new SolidColorBrush(Color.Parse("#9E9E9E"));
            MatchInfo.Foreground = Brushes.White;
        }
    }

    private static List<string> Split(string? s) =>
        (s ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
