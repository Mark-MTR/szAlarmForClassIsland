using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
/// 由 SzAlarmAlertAction 在手动触发时调用 SendAlertAsync。
/// 图标来源（官方远程 / 本地文件夹）与图标圆角由「提醒」设置面板配置。
/// 依赖统一格式 AlarmItem。
/// </summary>
[NotificationProviderInfo("4B2D1A9E-7C5F-4E3B-8A1D-3F6C9B2E5D71", "深圳气象预警提醒", "",
    "手动触发时弹出深圳市气象台实时预警提醒。")]
public class SzAlarmNotificationProvider : NotificationProviderBase<SzAlarmNotificationProviderSettings>
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
        // 切换图标来源/文件夹后清本地图标缓存，下次弹提醒时按新路径重新加载。
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SzAlarmNotificationProviderSettings.UseLocalIcons)
            or nameof(SzAlarmNotificationProviderSettings.LocalIconFolder))
        {
            SzAlarmService.Current?.ClearLocalCache();
        }
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

    /// <summary>
    /// 弹出一条预警提醒（自动按去重策略处理；滚动时长由卡片按“速度优先”算定并回传）。
    /// 返回已入队的提醒请求，调用方可选择 await 其播放完成（WaitForCompletionAsync）。
    ///
    /// 关键：在 UI 线程一次性构造控件并【同步入队】。此前分三次 InvokeAsync 且走
    /// ShowNotificationAsync（其内部对入队是 fire-and-forget 派发），不等待时入队被延后
    /// 多个调度周期，队列序号偏大，导致预警弹窗排到行动组末尾才显示。
    /// </summary>
    public async Task<NotificationRequest?> SendAlertAsync(AlarmItem item, int mode, SzAlarmAlertActionSettings? settings = null)
    {
        var id = $"{item.Type}|{item.Color}|{item.Area}|{item.DisplayName}";
        if (!ShouldAnnounce(id, mode)) return null;

        // 图标来源与圆角由「提醒」设置（本提供方 Settings）决定。
        var icon = SzAlarmService.Current?.GetIcon(item, Settings.UseLocalIcons, Settings.LocalIconFolder);

        var scrollSpeed = settings?.ScrollSpeed ?? 5;
        var maxDuration = settings?.MaxDuration ?? 15;
        var cornerRadius = Settings.CornerRadius;

        // UserControl 必须在 UI 线程构造，否则 InitializeComponent 会失败。
        // SetData 里会一次性算好最终时长 DurationSeconds = min(滚完所需时间, 最大显示时长)，
        // 作为提醒倒计时(Duration)的唯一来源——倒计时与滚动动画严格同步。
        var request = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var control = new SzAlarmNotificationControl();
            control.SetData(item, icon, scrollSpeed, maxDuration, cornerRadius);

            var maskControl = new SzAlarmMaskControl();
            maskControl.SetData(item, icon, cornerRadius);

            var overlay = new NotificationContent(control)
            {
                SpeechContent = item.Text ?? item.DisplayName,
                Duration = TimeSpan.FromSeconds(control.DurationSeconds),
                Color = LevelBrush(item.Color)
            };

            var mask = new NotificationContent(maskControl);

            var req = new NotificationRequest
            {
                MaskContent = mask,
                OverlayContent = overlay
            };

            // 同步入队：与内置“显示提醒”走的是同一个底层路径，但跳过
            // ShowNotificationAsync 的 fire-and-forget 派发，队列顺序立即确定。
            ShowNotification(req);
            return req;
        });

        MarkAnnounced(id, mode);
        return request;
    }

    /// <summary>等待提醒播放完成（用于“等待预警播报结束”模式）。</summary>
    public async Task WaitForCompletionAsync(NotificationRequest? request)
    {
        if (request is null) return;
        // CompletedToken 是 public 的（CompletedTokenSource 是 internal，插件不可访问）。
        await Task.Run(() => request.CompletedToken.WaitHandle.WaitOne());
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
