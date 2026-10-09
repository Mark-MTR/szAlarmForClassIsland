using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Notifications;

namespace SzAlarmPlugin.Actions;

/// <summary>
/// 行动“深圳气象预警提醒”的设置控件。
/// Settings 为只读 getter（框架注入 actionItem.Settings），故采用代码驱动：
/// 加载时从 Settings 同步进控件，输入时实时写回 Settings，避免绑定到被替换的旧对象。
/// </summary>
public partial class SzAlarmAlertActionSettingsControl : ActionSettingsControlBase<SzAlarmAlertActionSettings>
{
    public SzAlarmAlertActionSettingsControl()
    {
        InitializeComponent();
    }

    // 基类要求不要在构造函数里访问 Settings。加载后再访问。
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Settings is null) return;
        ModeBox.SelectedIndex = Settings.AnnounceMode;
        TbRegions.Text = Settings.Regions ?? "";
        TbTypes.Text = Settings.Types ?? "";
        TbLevels.Text = Settings.Levels ?? "";
        SpeedSlider.Value = Settings.ScrollSpeed;
        DurSlider.Value = Settings.MaxDuration;
        UpdateLabels();
    }

    /// <summary>把所有控件当前值写回 Settings（持久化）。</summary>
    private void SyncSettings()
    {
        if (Settings is null) return;
        Settings.AnnounceMode = ModeBox.SelectedIndex;
        Settings.Regions = TbRegions.Text;
        Settings.Types = TbTypes.Text;
        Settings.Levels = TbLevels.Text;
        Settings.ScrollSpeed = (int)SpeedSlider.Value;
        Settings.MaxDuration = (int)DurSlider.Value;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        SpeedVal.Text = $"{SpeedSlider.Value:F0}";
        DurVal.Text = $"{DurSlider.Value:F0} 秒";
    }

    private void OnInputChanged(object? sender, TextChangedEventArgs e) => SyncSettings();
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => SyncSettings();
    private void OnSliderChanged(object? sender, RangeBaseValueChangedEventArgs e) => SyncSettings();

    private void ClearBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        SzAlarmNotificationProvider.Current?.ClearAnnounced();
    }
}
