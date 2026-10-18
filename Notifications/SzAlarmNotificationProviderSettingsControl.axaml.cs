using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Notifications;

/// <summary>
/// 预警提醒提供方的设置面板（图标来源与圆角）。
/// </summary>
public partial class SzAlarmNotificationProviderSettingsControl : NotificationProviderControlBase<SzAlarmNotificationProviderSettings>
{
    public SzAlarmNotificationProviderSettingsControl()
    {
        InitializeComponent();
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择预警图标文件夹",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(path))
                Settings.LocalIconFolder = path;
        }
    }
}
