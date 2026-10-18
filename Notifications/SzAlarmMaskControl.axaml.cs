using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Notifications;

/// <summary>
/// 预警提醒遮罩（顶部横幅）内容：小图标 + 居中粗体文字（使用强调字号，不染色）。
/// 依赖统一格式 AlarmItem。
/// </summary>
public partial class SzAlarmMaskControl : UserControl
{
    public SzAlarmMaskControl()
    {
        InitializeComponent();
    }

    public void SetData(AlarmItem item, Bitmap? icon, double cornerRadius = 8)
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

        // 遮罩图标较小（22），圆角按图标尺寸的一半封顶，避免圆角过大导致整图标变圆。
        var r = System.Math.Min(cornerRadius, 11);
        IconBorder.CornerRadius = new CornerRadius(r);

        // 发布方来自标准预警格式（IssuerName），数据源未提供时回退为“预警方”。
        var issuer = string.IsNullOrWhiteSpace(item.IssuerName) ? "预警方" : item.IssuerName;

        // 预警名用 DisplayName：优先 FullName（兼容港澳“三号强风信号”等信号体系），
        // 留空则回退为“{Type}{Color}预警”的大陆模板。文字保持默认前景色，不按级别染色。
        Text.Text = $"{issuer}发布{item.DisplayName}";
    }
}
