using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Notifications;

namespace SzAlarmPlugin.Actions;

/// <summary>
/// 行动“深圳气象预警提醒”的设置控件。
/// 数值通过 XAML 双向绑定到 Settings（= 框架的 actionItem.Settings），框架在保存时据此序列化，
/// 这是 ClassIsland 内置行动设置控件使用的同一套持久化机制，稳定可靠。
///
/// 行动设置存于 Automations/*.json，框架仅在“关闭应用设置窗口”或退出时统一 SaveConfig。
/// 若只在抽屉里改完、没关整个设置窗口就重启，配置会丢失；因此这里订阅 Settings.PropertyChanged，
/// 任一属性变化（Settings 已实现 INPC）就主动 SaveConfig 立即落盘，重启也不再归一。
/// </summary>
public partial class SzAlarmAlertActionSettingsControl : ActionSettingsControlBase<SzAlarmAlertActionSettings>
{
    public SzAlarmAlertActionSettingsControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Settings is SzAlarmAlertActionSettings s)
        {
            s.PropertyChanged += OnSettingsChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Settings is SzAlarmAlertActionSettings s)
        {
            s.PropertyChanged -= OnSettingsChanged;
        }
        base.OnDetachedFromVisualTree(e);
    }

    // 行动设置靠 AutomationService.SaveConfig 序列化整个 Workflows 落盘；值一变就保存，不依赖窗口关闭时机。
    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        IAppHost.GetService<IAutomationService>()?.SaveConfig();
    }

    private void ClearBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SzAlarmNotificationProvider.Current?.ClearAnnounced();
    }
}
