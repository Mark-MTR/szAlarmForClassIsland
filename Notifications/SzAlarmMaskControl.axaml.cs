using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Notifications;

/// <summary>
/// 预警提醒遮罩（顶部横幅）内容：小图标 + 居中粗体文字（使用强调字号，不染色）。
/// </summary>
public partial class SzAlarmMaskControl : UserControl
{
    public SzAlarmMaskControl()
    {
        InitializeComponent();
    }

    public void SetData(SzAlarmItem item, Bitmap? icon)
    {
        if (icon is not null)
        {
            Icon.Source = icon;
            Icon.IsVisible = true;
        }
        else
        {
            Icon.IsVisible = false;
        }

        // 大陆标准顺序：灾种 + 级别 + 预警；文字保持默认前景色，不按级别染色
        Text.Text = $"深圳市气象台发布 {item.AlarmType}{item.AlarmColor}预警";
    }
}
