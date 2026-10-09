using CommunityToolkit.Mvvm.ComponentModel;

namespace SzAlarmPlugin.Models;

/// <summary>
/// 组件设置。
/// </summary>
public class SzAlarmComponentSettings : ObservableObject
{
    /// <summary>
    /// 显示模式（在按地区过滤之后生效）：
    /// 0 = 所有预警；1 = 最高级别（含并列）；2 = 最新一条；3 = 每种预警中级别最高的一条。
    /// </summary>
    private int _displayMode = 0;
    public int DisplayMode
    {
        get => _displayMode;
        set { if (value == _displayMode) return; _displayMode = value; OnPropertyChanged(); }
    }

    /// <summary>是否使用本地文件夹的预警图标（按 icon 字段名自动匹配 *.png）。</summary>
    private bool _useLocalIcons;
    public bool UseLocalIcons
    {
        get => _useLocalIcons;
        set { if (value == _useLocalIcons) return; _useLocalIcons = value; OnPropertyChanged(); }
    }

    /// <summary>本地图标文件夹路径（相对插件目录，或填绝对路径）。</summary>
    private string? _localIconFolder = "Icons";
    public string? LocalIconFolder
    {
        get => _localIconFolder;
        set { if (value == _localIconFolder) return; _localIconFolder = value; OnPropertyChanged(); }
    }

    /// <summary>是否显示预警图标。</summary>
    private bool _showIcon = true;
    public bool ShowIcon
    {
        get => _showIcon;
        set { if (value == _showIcon) return; _showIcon = value; OnPropertyChanged(); }
    }

    /// <summary>是否显示预警类型文字（如：台风）。</summary>
    private bool _showText = true;
    public bool ShowText
    {
        get => _showText;
        set { if (value == _showText) return; _showText = value; OnPropertyChanged(); }
    }

    /// <summary>是否显示预警级别文字（如：橙色）。</summary>
    private bool _showLevel = true;
    public bool ShowLevel
    {
        get => _showLevel;
        set { if (value == _showLevel) return; _showLevel = value; OnPropertyChanged(); }
    }

    /// <summary>图标尺寸（像素）。</summary>
    private double _iconSize = 22;
    public double IconSize
    {
        get => _iconSize;
        set { if (value == _iconSize) return; _iconSize = value; OnPropertyChanged(); }
    }

    /// <summary>图标圆角半径（像素，0 为方角）。</summary>
    private double _cornerRadius;
    public double CornerRadius
    {
        get => _cornerRadius;
        set { if (value == _cornerRadius) return; _cornerRadius = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 区域过滤关键词，空格分隔。仅当预警的 alarmArea 包含其中任一关键词时才显示；
    /// 留空表示不过滤，显示所有区域。
    /// </summary>
    private string? _regionFilter;
    public string? RegionFilter
    {
        get => _regionFilter;
        set { if (value == _regionFilter) return; _regionFilter = value; OnPropertyChanged(); }
    }

    /// <summary>无预警时是否显示占位文字。</summary>
    private bool _showPlaceholderText;
    public bool ShowPlaceholderText
    {
        get => _showPlaceholderText;
        set { if (value == _showPlaceholderText) return; _showPlaceholderText = value; OnPropertyChanged(); }
    }

    /// <summary>占位文字内容（默认"无预警"）。</summary>
    private string? _placeholderText = "无预警";
    public string? PlaceholderText
    {
        get => _placeholderText;
        set { if (value == _placeholderText) return; _placeholderText = value; OnPropertyChanged(); }
    }

    /// <summary>无预警时是否显示占位图片。</summary>
    private bool _showPlaceholderImage;
    public bool ShowPlaceholderImage
    {
        get => _showPlaceholderImage;
        set { if (value == _showPlaceholderImage) return; _showPlaceholderImage = value; OnPropertyChanged(); }
    }

    /// <summary>占位图片的本地路径（png/jpg 等）。</summary>
    private string? _placeholderImagePath;
    public string? PlaceholderImagePath
    {
        get => _placeholderImagePath;
        set { if (value == _placeholderImagePath) return; _placeholderImagePath = value; OnPropertyChanged(); }
    }
}
