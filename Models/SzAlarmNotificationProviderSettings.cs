using CommunityToolkit.Mvvm.ComponentModel;

namespace SzAlarmPlugin.Models;

/// <summary>
/// 预警提醒提供方的设置（在「提醒」设置里配置，而非行动设置）。
/// 控制提醒中预警图标的来源（官方远程 / 本地文件夹）与图标圆角。
/// </summary>
public class SzAlarmNotificationProviderSettings : ObservableObject
{
    /// <summary>
    /// 是否使用本地文件夹的预警图标（按 icon 字段名自动匹配 *.png）。
    /// </summary>
    private bool _useLocalIcons;
    public bool UseLocalIcons
    {
        get => _useLocalIcons;
        set { if (value == _useLocalIcons) return; _useLocalIcons = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 本地图标文件夹路径（相对插件目录，或填绝对路径）。
    /// </summary>
    private string? _localIconFolder = "Icons";
    public string? LocalIconFolder
    {
        get => _localIconFolder;
        set { if (value == _localIconFolder) return; _localIconFolder = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 提醒中预警图标的圆角半径（像素，0 为方角）。
    /// </summary>
    private double _cornerRadius = 8;
    public double CornerRadius
    {
        get => _cornerRadius;
        set { if (value == _cornerRadius) return; _cornerRadius = value; OnPropertyChanged(); }
    }
}
