using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Notifications;

/// <summary>
/// 预警提醒主卡片（覆盖层内容）。直接赋值，不依赖 DataContext 绑定。
/// 布局：图标 | 灾种+级别标题(不染色) | 单行横向滚动正文。
///
/// 滚动时长计算（速度优先、滚完即走）：
///   · 速度固定 = 滑块 × 25 像素/秒（滑块步进 0.5）。
///   · 单遍完整位移 = 滚动区宽(从右外进入) + 两遍文字宽(滚出左侧外)。
///   · 滚完所需时间 fullTime = 位移 / 速度。
///   · 最终时长 = min(fullTime, 最大显示时长)：
///       - fullTime ≤ 上限 → 用计算结果，滚完恰好到点，不会空等上限；
///       - fullTime > 上限 → 用上限，速度不变，只滚“上限内能走的距离”（可能滚不完，但绝不拉慢）。
///
/// 关键：时长在 SetData（Provider 弹窗前）就一次性算好，Provider 把它作为提醒倒计时(Duration)，
/// 动画也严格用同一个时长，因此“倒计时”与“滚动动画”天然同步——不需要任何提前关闭的旁路逻辑。
/// 依赖统一格式 AlarmItem。
/// </summary>
public partial class SzAlarmNotificationControl : UserControl
{
    private const string GapStr = "　　　　"; // 两遍之间的全角空格间隔
    private const double ClipExtra = 200;     // 纵向裁切余量（远大于字号，确保纵向永不裁）
    private const double MaxCardWidth = 720;  // 与 XAML Border.MaxWidth 一致，用于挂树前估算滚动区宽

    private string _cleanText = "";
    private double _scrollSpeed = 5;
    private double _maxDuration = 15;
    private bool _attached;
    private CancellationTokenSource? _cts;

    /// <summary>最终卡片时长（秒）= min(滚完所需时间, 最大显示时长)。Provider 据此设置提醒倒计时。</summary>
    public double DurationSeconds { get; private set; }

    public SzAlarmNotificationControl()
    {
        InitializeComponent();
        ScrollText.RenderTransform = new TranslateTransform();
        Loaded += (_, _) => { _attached = true; SetupScroll(); };
        Unloaded += (_, _) => { _attached = false; _cts?.Cancel(); };
        ScrollViewport.SizeChanged += (_, _) => { if (_attached) SetupScroll(); };
    }

    /// <summary>设置只裁横向的 Clip 几何：宽=可视区宽，高=足够大（纵向不裁）。</summary>
    private void ApplyHorizontalClip(double w)
    {
        if (w <= 0) return;
        ScrollViewport.Clip = new RectangleGeometry(new Rect(0, -ClipExtra, w, ClipExtra * 2));
    }

    /// <summary>
    /// 填充卡片内容，并立即算好最终时长 DurationSeconds。
    /// 字号已写死为显式值，脱离视觉树也能正确测量文本宽度。
    /// cornerRadius 由「提醒」设置传入，控制左侧图标的圆角。
    /// </summary>
    public void SetData(AlarmItem item, Bitmap? icon, double scrollSpeed = 5, double maxDuration = 15,
        double cornerRadius = 8)
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

        IconBorder.CornerRadius = new CornerRadius(cornerRadius);

        Title.Text = item.DisplayName;

        _cleanText = (item.Text ?? "").Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
        _scrollSpeed = scrollSpeed;
        _maxDuration = maxDuration;

        DurationSeconds = ComputeDuration();

        if (_attached) SetupScroll();
    }

    /// <summary>测量给定文本的宽度（显式字号，脱离视觉树也准确）。</summary>
    private double MeasureTextWidth(string text)
    {
        ScrollText.Text = text;
        ScrollText.Width = double.NaN;
        ScrollText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return ScrollText.DesiredSize.Width;
    }

    /// <summary>估算滚动区宽度：挂树用真实值；未挂树按最大卡片宽(720)强制布局后读取。</summary>
    private double GetViewportWidth()
    {
        if (_attached) return ScrollViewport.Bounds.Width;

        // 脱离视觉树时按最大宽度布局一次，得到 * 列（滚动区）的真实宽度。
        Measure(new Size(MaxCardWidth, double.PositiveInfinity));
        var h = DesiredSize.Height > 0 ? DesiredSize.Height : 80.0;
        Arrange(new Rect(0, 0, MaxCardWidth, h));
        return ScrollViewport.Bounds.Width;
    }

    /// <summary>计算最终时长 = min(滚完所需时间, 最大显示时长)。</summary>
    private double ComputeDuration()
    {
        var single = MeasureTextWidth(_cleanText);

        var viewport = GetViewportWidth();
        if (viewport <= 0)
        {
            // 布局失败兜底：按最大卡片宽估算滚动区（偏大 → 时长偏长 → 文字必然滚完，绝不截断）。
            viewport = 500;
        }

        // 放得下：静态显示，无需滚动，按最大显示时长停留。
        if (single <= viewport + 4)
        {
            return _maxDuration;
        }

        var total = MeasureTextWidth(_cleanText + GapStr + _cleanText);
        var speed = Math.Clamp(_scrollSpeed, 1, 20) * 25.0; // 像素/秒，恒定
        var fullTime = (viewport + total) / speed;          // 滚完所需时间
        return Math.Min(fullTime, _maxDuration);            // 小于上限用计算结果，否则用上限
    }

    private void SetupScroll()
    {
        if (!_attached) return;
        var viewport = ScrollViewport.Bounds.Width;
        if (viewport <= 0) return;
        ApplyHorizontalClip(viewport);

        var tf = (TranslateTransform)ScrollText.RenderTransform!;

        var single = MeasureTextWidth(_cleanText);
        if (single <= viewport + 4)
        {
            tf.X = 0;       // 放得下：左对齐静态显示
            _cts?.Cancel();
            return;
        }

        var total = MeasureTextWidth(_cleanText + GapStr + _cleanText);
        ScrollText.Width = total;

        _ = StartScrollAsync(viewport, total);
    }

    private async Task StartScrollAsync(double viewport, double total)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        try
        {
            var speed = Math.Clamp(_scrollSpeed, 1, 20) * 25.0; // 像素/秒，恒定（速度由滑块决定，不拉慢）
            var durationSec = DurationSeconds;                  // 与 Provider 设置的倒计时完全一致
            var distance = speed * durationSec;                 // 该时长内能走的距离（速度恒定）

            var anim = new Animation
            {
                Duration = TimeSpan.FromSeconds(durationSec),
                FillMode = FillMode.Forward,                    // 结束后停在最后位置（已滚出视口，不可见）
                Children =
                {
                    new KeyFrame
                    {
                        KeyTime = TimeSpan.Zero,
                        Setters = { new Setter(TranslateTransform.XProperty, viewport) }
                    },
                    new KeyFrame
                    {
                        KeyTime = TimeSpan.FromSeconds(durationSec),
                        Setters = { new Setter(TranslateTransform.XProperty, viewport - distance) }
                    }
                }
            };

            await anim.RunAsync(ScrollText, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 控件卸载或重新 setup，忽略
        }
    }
}
