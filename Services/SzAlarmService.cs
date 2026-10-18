using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Services;

/// <summary>
/// 预警聚合服务：定时拉取所有已注册的数据源，合并成统一格式（AlarmItem），
/// 对外提供预警列表与图标加载。本身不关心任何具体城市 / 接口的细节。
///
/// 接新城市 / 新 API 时，只需：
///   1. 实现 IAlarmSource（照 ShenZhenAlarmSource 抄一份改改）；
///   2. 在构造函数里 new 一个加进 _sources 列表。
/// 组件、规则、提醒会自动合并展示，无需再改其它地方。
/// </summary>
public class SzAlarmService : ObservableObject, IHostedService
{
    /// <summary>当前服务实例（供规则处理器等静态场景访问实时预警数据）。</summary>
    public static SzAlarmService? Current { get; private set; }

    private readonly HttpClient _http = new();
    private readonly List<IAlarmSource> _sources;
    private Timer? _timer;

    // 远程图标缓存：key = 图标完整地址 IconUrl（不同源可能同名不同图，故用地址区分）。
    private readonly Dictionary<string, Bitmap?> _remoteCache = new();
    // 本地图标缓存：key = 图标名 IconName（按 {IconName}.png 从本地文件夹加载）。
    private readonly Dictionary<string, Bitmap?> _localCache = new();

    public SzAlarmService()
    {
        Current = this;
        // 数据源清单：以后新增城市 / API 在这里 new 一个加进来即可。
        _sources = new List<IAlarmSource>
        {
            new ShenZhenAlarmSource(),
        };
    }

    private List<AlarmItem> _rawItems = new();
    /// <summary>当前所有数据源合并后的生效预警（按优先级 + 级别排序）。</summary>
    public List<AlarmItem> RawItems
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
        // 并发拉取所有数据源，各自失败互不影响。
        var all = new List<AlarmItem>();
        var anySuccess = false;
        foreach (var src in _sources)
        {
            try
            {
                var items = await src.FetchAsync();
                if (items is not null) all.AddRange(items);
                anySuccess = true;
            }
            catch (Exception)
            {
                // 单个源失败，跳过，保留其它源结果
            }
        }

        // 所有数据源都拉取失败时，保留上次的预警信息，绝不用空内容覆盖。
        if (!anySuccess)
            return;

        // 统一排序：源内优先级 → 级别。
        var sorted = all
            .OrderBy(a => a.Order)
            .ThenByDescending(a => a.Level)
            .ToList();

        // 预下载官方远程图标（按完整地址缓存，避免重复下载）。
        foreach (var it in sorted)
        {
            if (string.IsNullOrEmpty(it.IconUrl) || _remoteCache.ContainsKey(it.IconUrl))
                continue;
            try
            {
                var bytes = await _http.GetByteArrayAsync(it.IconUrl);
                using var ms = new MemoryStream(bytes);
                _remoteCache[it.IconUrl] = new Bitmap(ms);
            }
            catch (Exception)
            {
                _remoteCache[it.IconUrl] = null; // 下载失败则留空，不阻断显示
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RawItems = sorted;
            HasAlarm = sorted.Count > 0;
        });
    }

    /// <summary>
    /// 按设置返回图标位图：本地优先或官方远程。
    /// </summary>
    public Bitmap? GetIcon(AlarmItem item, bool useLocal, string? localFolder)
    {
        if (string.IsNullOrEmpty(item.IconName)) return null;

        if (useLocal)
        {
            if (_localCache.TryGetValue(item.IconName, out var local)) return local;
            try
            {
                var folder = ResolveFolder(localFolder);
                var path = Path.Combine(folder, item.IconName + ".png");
                if (File.Exists(path))
                {
                    using var fs = File.OpenRead(path);
                    var bm = new Bitmap(fs);
                    _localCache[item.IconName] = bm;
                    return bm;
                }
            }
            catch (Exception)
            {
                // 忽略，回退到无图标
            }
            _localCache[item.IconName] = null;
            return null;
        }

        if (string.IsNullOrEmpty(item.IconUrl)) return null;
        return _remoteCache.TryGetValue(item.IconUrl, out var remote) ? remote : null;
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
}
