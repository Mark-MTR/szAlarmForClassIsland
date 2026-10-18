using Avalonia.Media.Imaging;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Services;

/// <summary>
/// 预警展示项：包装一条统一格式预警及其已加载的图标，供 XAML 直接绑定。
/// </summary>
public class AlarmEntry
{
    public AlarmItem Item { get; }
    public Bitmap? IconBitmap { get; }

    /// <summary>是否显示图标（= 设置开启图标 且 成功加载到图标）。</summary>
    public bool ShowIcon { get; }

    public AlarmEntry(AlarmItem item, Bitmap? icon, bool showIcon)
    {
        Item = item;
        IconBitmap = icon;
        ShowIcon = showIcon && icon != null;
    }
}
