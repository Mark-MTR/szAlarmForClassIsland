using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Notifications;

/// <summary>
/// 预警提醒提供方：仅提供“按原生风格弹提醒”的能力，本身不自动触发。
/// 由 SzAlarmAlertAction 在手动触发时调用 SendAlertAsync / SendNoAlarmAsync。
/// </summary>
[NotificationProviderInfo("4B2D1A9E-7C5F-4E3B-8A1D-3F6C9B2E5D71", "深圳气象预警提醒", "\ue7ba",
    "手动触发时弹出深圳市气象台实时预警提醒。")]
public class SzAlarmNotificationProvider : NotificationProviderBase
{
    /// <summary>当前提供方实例（供行动等静态访问）。</summary>
    public static SzAlarmNotificationProvider? Current { get; private set; }

    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SzAlarmPlugin");
    private static readonly string FilePath = Path.Combine(Dir, "announced.json");

    private readonly object _lock = new();
    private HashSet<string> _announced = Load();

    public SzAlarmNotificationProvider()
    {
        Current = this;
    }

    /// <summary>是否应当播报该预警（去重逻辑）。</summary>
    public bool ShouldAnnounce(string id, int mode)
    {
        if (mode == 0) return true;            // 每次都播报
        lock (_lock) return !_announced.Contains(id);
    }

    /// <summary>记录该预警已播报（仅“仅首次”模式生效）。</summary>
    public void MarkAnnounced(string id, int mode)
    {
        if (mode != 1) return;
        lock (_lock)
        {
            _announced.Add(id);
            Save();
        }
    }

    /// <summary>清除已播报记录（行动设置里的“清除已播记录”按钮调用）。</summary>
    public void ClearAnnounced()
    {
        lock (_lock)
        {
            _announced.Clear();
            Save();
        }
    }

    /// <summary>弹出一条预警提醒（自动按去重策略 + 计算时长处理）。</summary>
    public async Task SendAlertAsync(SzAlarmItem item, int mode, SzAlarmAlertActionSettings? settings = null)
    {
        var id = $"{item.AlarmType}|{item.AlarmColor}|{item.AlarmArea}";
        if (!ShouldAnnounce(id, mode)) return;

        var icon = SzAlarmService.Current?.GetIcon(item, false, null);

        // UserControl 必须在 UI 线程构造，否则 InitializeComponent 会失败。
        var control = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var c = new SzAlarmNotificationControl();
            c.SetData(item, icon, settings?.ScrollSpeed ?? 5);
            return c;
        });

        var maskControl = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var m = new SzAlarmMaskControl();
            m.SetData(item, icon);
            return m;
        });

        // 时长：先算滚动所需时间；小于上限用计算值，超过用上限。
        var clean = CleanText(item.Str);
        var maxDuration = settings?.MaxDuration ?? 15;
        var duration = ComputeDuration(clean, settings?.ScrollSpeed ?? 5, maxDuration);

        var overlay = new NotificationContent(control)
        {
            SpeechContent = item.Str ?? $"{item.AlarmType}{item.AlarmColor}预警",
            Duration = duration,
            Color = LevelBrush(item.AlarmColor)
        };

        var mask = new NotificationContent(maskControl);

        var request = new NotificationRequest
        {
            MaskContent = mask,
            OverlayContent = overlay
        };

        await ShowNotificationAsync(request);
        MarkAnnounced(id, mode);
    }

    /// <summary>无预警时的友好提示。</summary>
    public async Task SendNoAlarmAsync()
    {
        var request = new NotificationRequest
        {
            MaskContent = NotificationContent.CreateSimpleTextContent("当前深圳市无气象预警"),
            OverlayContent = null
        };
        await ShowNotificationAsync(request);
    }

    private static string CleanText(string? s) =>
        (s ?? "").Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

    /// <summary>
    /// 计算提醒正文时长：
    /// 放得下（静态）→ 用上限时长；放不下（需滚动两遍）→ 用滚动所需时长，但与上限取小。
    /// </summary>
    private static TimeSpan ComputeDuration(string clean, int speed, double maxDuration)
    {
        if (string.IsNullOrEmpty(clean)) return TimeSpan.FromSeconds(maxDuration);

        var typeface = Typeface.Default;
        double WidthOf(string t)
        {
            var ft = new FormattedText(t, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 14, Brushes.Black);
            return ft.Width;
        }

        var single = WidthOf(clean);
        // 滚动区可用宽度估算：卡片最大 640，减图标(44)+间距+标题+内边距 ≈ 420
        const double estViewport = 420;
        if (single <= estViewport + 4)
            return TimeSpan.FromSeconds(maxDuration); // 放得下：静态显示，按上限停留

        var total = WidthOf(clean + "　　　　" + clean); // 两遍（全角空格间隔）
        var pxPerSec = Math.Clamp(speed, 1, 20) * 25.0; // 速度滑块 → 像素/秒
        var scroll = total / pxPerSec;
        return TimeSpan.FromSeconds(Math.Min(scroll, maxDuration));
    }

    private static IBrush LevelBrush(string? color) => color switch
    {
        "红色" => new SolidColorBrush(Color.Parse("#E53935")),
        "橙色" => new SolidColorBrush(Color.Parse("#FB8C00")),
        "黄色" => new SolidColorBrush(Color.Parse("#FDD835")),
        "蓝色" => new SolidColorBrush(Color.Parse("#1E88E5")),
        "白色" => new SolidColorBrush(Color.Parse("#90A4AE")),
        _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
    };

    private static HashSet<string> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* 忽略损坏文件 */ }
        return new HashSet<string>();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_announced));
        }
        catch { /* 写入失败不阻断 */ }
    }
}
