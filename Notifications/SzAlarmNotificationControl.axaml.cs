using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Notifications;

/// <summary>
/// 预警提醒主卡片（覆盖层内容）。直接赋值，不依赖 DataContext 绑定。
/// 布局：图标 | 灾种+级别标题(不染色) | 单行横向滚动正文(两遍后停开头)。
/// 滚动用 Avalonia 原生 Animation（走渲染循环，与官方 RollingTextTemplate 同机制），
/// 从视口右侧外进入、向左滚出，播完两遍后回到左对齐开头静止。
///
/// 注意：横向滚动的“窗口裁切”不能用 Border.ClipToBounds（它会把 XY 两个方向都裁，
/// 导致文字顶部被切），故改用 ScrollViewport.Clip 的矩形几何，宽度=可视区宽、高度给足，
/// 从而只裁横向、纵向完全不裁。
/// </summary>
public partial class SzAlarmNotificationControl : UserControl
{
    private const string GapStr = "　　　　"; // 两遍之间的全角空格间隔
    private const double ClipExtra = 200;     // 纵向裁切余量（远大于字号，确保纵向永不裁）

    private string _cleanText = "";
    private int _scrollSpeed = 5;
    private double _viewport;
    private double _total;
    private bool _setupDone;
    private CancellationTokenSource? _cts;

    public SzAlarmNotificationControl()
    {
        InitializeComponent();
        ScrollText.RenderTransform = new TranslateTransform();
        Loaded += (_, _) => SetupScroll();
        Unloaded += (_, _) => _cts?.Cancel();
        ScrollViewport.SizeChanged += (_, _) =>
        {
            ApplyHorizontalClip();
            _setupDone = false;
            SetupScroll();
        };
    }

    /// <summary>设置只裁横向的 Clip 几何：宽=可视区宽，高=足够大（纵向不裁）。</summary>
    private void ApplyHorizontalClip()
    {
        var w = ScrollViewport.Bounds.Width;
        if (w <= 0) return;
        // 矩形覆盖视口的全部纵向范围（上下各留 ClipExtra），仅左右边界裁切
        ScrollViewport.Clip = new RectangleGeometry(new Rect(0, -ClipExtra, w, ClipExtra * 2));
    }

    public void SetData(SzAlarmItem item, Bitmap? icon, int scrollSpeed = 5)
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

        // 大陆标准顺序：灾种 + 级别 + 预警（如「暴雨黄色预警」），不染色
        Title.Text = $"{item.AlarmType}{item.AlarmColor}预警";

        // 把换行/回车压成空格，确保正文严格单行
        _cleanText = (item.Str ?? "").Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
        _scrollSpeed = scrollSpeed;
        _setupDone = false;
        SetupScroll();
    }

    private void SetupScroll()
    {
        if (_setupDone) return;

        _viewport = ScrollViewport.Bounds.Width;
        // 视口尚未布局完成（宽度为 0）时先退出，等 SizeChanged 再次触发
        if (_viewport <= 0) return;

        ApplyHorizontalClip();

        var tf = (TranslateTransform)ScrollText.RenderTransform!;

        // 先按单遍测量宽度
        ScrollText.Width = double.NaN;
        ScrollText.Text = _cleanText;
        ScrollText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var single = ScrollText.DesiredSize.Width;

        // 放得下：静态显示，左对齐，不滚动
        if (single <= _viewport + 4)
        {
            tf.X = 0;
            _setupDone = true;
            return;
        }

        // 超宽：正文 + 间隔 + 正文，两遍；TextBlock 宽度锁为全文宽
        ScrollText.Text = _cleanText + GapStr + _cleanText;
        ScrollText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _total = ScrollText.DesiredSize.Width;
        ScrollText.Width = _total;

        _setupDone = true;
        _ = StartScrollAsync();
    }

    private async Task StartScrollAsync()
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        try
        {
            var pxPerSec = Math.Clamp(_scrollSpeed, 1, 20) * 25.0; // 速度滑块 → 像素/秒
            var duration = TimeSpan.FromSeconds(_total / pxPerSec);

            // 从右侧外进入（X=视口宽），向左滚出（X=-全文宽），单次播放两遍
            var anim = new Animation
            {
                Duration = duration,
                FillMode = FillMode.None, // 播完回到基础值 X=0，停在开头
                Children =
                {
                    new KeyFrame
                    {
                        KeyTime = TimeSpan.Zero,
                        Setters = { new Setter(TranslateTransform.XProperty, _viewport) }
                    },
                    new KeyFrame
                    {
                        KeyTime = duration,
                        Setters = { new Setter(TranslateTransform.XProperty, -_total) }
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
