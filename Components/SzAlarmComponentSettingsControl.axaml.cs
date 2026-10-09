using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Components;

/// <summary>
/// 组件设置面板。
/// </summary>
public partial class SzAlarmComponentSettingsControl : ComponentBase<SzAlarmComponentSettings>
{
    public SzAlarmComponentSettingsControl()
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

    private async void BrowsePlaceholderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择占位图片",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp" }
                }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(path))
                Settings.PlaceholderImagePath = path;
        }
    }
}
