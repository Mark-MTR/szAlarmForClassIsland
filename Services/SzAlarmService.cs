using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Services;

/// <summary>
/// 定时从深圳市气象台拉取预警信号，并对外暴露当前生效预警及图标。
/// </summary>
public class SzAlarmService : ObservableObject, IHostedService
{
    /// <summary>当前服务实例（供规则处理器等静态场景访问实时预警数据）。</summary>
    public static SzAlarmService? Current { get; private set; }

    public SzAlarmService()
    {
        Current = this;
    }

    private static readonly Uri ApiUri = new("https://weather.121.com.cn/data_cache/szWeather/alarm/szAlarm.json");
    // 预警图标按 icon 字段名拼接（官方图标源）
    private static readonly string IconBase = "https://weather.sz.gov.cn/alarmIcon/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // 接口部分数字字段会以字符串形式返回（如 ingnalNum:"548"），允许从字符串读数字
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _http = new();
    private Timer? _timer;
    private readonly Dictionary<string, Bitmap?> _remoteCache = new();
    private readonly Dictionary<string, Bitmap?> _localCache = new();

    private List<SzAlarmItem> _rawItems = new();
    /// <summary>当前生效预警（已按官方优先级排序）。</summary>
    public List<SzAlarmItem> RawItems
    {
        get => _rawItems;
        private set => SetProperty(ref _rawItems, value);
    }

    private bool _hasAlarm;
    /// <summary>是否有生效预警。</summary>
    public bool HasAlarm
    {
        get => _hasAlarm;
        set => SetProperty(ref _hasAlarm, value);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = RefreshAsync();
        _timer = new Timer(_ => _ = RefreshAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        ClearCache(_remoteCache);
        ClearCache(_localCache);
        return Task.CompletedTask;
    }

    /// <summary>切换本地图标后清缓存，下次重建时按新路径重新加载。</summary>
    public void ClearLocalCache() => ClearCache(_localCache);

    public async Task RefreshAsync()
    {
        SzAlarmRoot? root = null;
        try
        {
            var json = await _http.GetStringAsync(ApiUri);
            root = JsonSerializer.Deserialize<SzAlarmRoot>(json, JsonOptions);
        }
        catch (Exception)
        {
            return; // 网络异常时保留上次数据，静默处理
        }

        if (root?.SubAlarm is null) return;

        // 按官方优先级排序，同级按颜色级别兜底
        var items = root.SubAlarm
            .OrderBy(a => a.Order)
            .ThenByDescending(Level)
            .ToList();

        // 预下载官方远程图标（按 icon 名缓存，避免重复下载）
        foreach (var it in items)
        {
            if (it.Icon is { } iconName && !_remoteCache.ContainsKey(iconName))
            {
                try
                {
                    var bytes = await _http.GetByteArrayAsync(IconBase + iconName + ".png");
                    using var ms = new MemoryStream(bytes);
                    _remoteCache[iconName] = new Bitmap(ms);
                }
                catch (Exception)
                {
                    _remoteCache[iconName] = null; // 下载失败则留空，不阻断显示
                }
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RawItems = items;
            HasAlarm = items.Count > 0;
        });
    }

    /// <summary>
    /// 按当前设置返回图标位图：本地优先或官方远程。
    /// </summary>
    public Bitmap? GetIcon(SzAlarmItem item, bool useLocal, string? localFolder)
    {
        if (item.Icon is not { } iconName) return null;

        if (useLocal)
        {
            if (_localCache.TryGetValue(iconName, out var local)) return local;
            try
            {
                var folder = ResolveFolder(localFolder);
                var path = Path.Combine(folder, iconName + ".png");
                if (File.Exists(path))
                {
                    using var fs = File.OpenRead(path);
                    var bm = new Bitmap(fs);
                    _localCache[iconName] = bm;
                    return bm;
                }
            }
            catch (Exception)
            {
                // 忽略，回退到无图标
            }
            _localCache[iconName] = null;
            return null;
        }

        return _remoteCache.TryGetValue(iconName, out var remote) ? remote : null;
    }

    private static string ResolveFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) folder = "Icons";
        if (Path.IsPathRooted(folder)) return folder;
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                      ?? AppContext.BaseDirectory;
        return Path.Combine(baseDir, folder);
    }

    private static void ClearCache(Dictionary<string, Bitmap?> cache)
    {
        foreach (var b in cache.Values) b?.Dispose();
        cache.Clear();
    }

    private static int Level(SzAlarmItem a) => a.AlarmColor switch
    {
        "红色" => 4,
        "橙色" => 3,
        "黄色" => 2,
        "蓝色" => 1,
        _ => 0
    };
}
