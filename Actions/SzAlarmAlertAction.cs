using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Notifications;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin.Actions;

/// <summary>
/// 行动：手动触发时弹出深圳气象预警提醒（不自动弹窗）。
/// </summary>
[ActionInfo("szalarm.alert", "深圳气象预警提醒", "\ue7ba", defaultGroupToMenu: "深圳气象预警")]
public class SzAlarmAlertAction : ActionBase<SzAlarmAlertActionSettings>
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();

        // 提供方只被注册为 IHostedService（非可注入单例），故通过静态 Current 访问，
        // 与组件/规则中用 SzAlarmService.Current 的方式保持一致。
        var svc = SzAlarmService.Current;
        var provider = SzAlarmNotificationProvider.Current;
        if (svc is null || provider is null)
            return;

        var items = Filter(svc.RawItems, Settings);
        if (items.Count == 0)
        {
            await provider.SendNoAlarmAsync();
            return;
        }

        foreach (var it in items)
            await provider.SendAlertAsync(it, Settings.AnnounceMode, Settings);
    }

    private static List<SzAlarmItem> Filter(IEnumerable<SzAlarmItem> all, SzAlarmAlertActionSettings s)
    {
        var regions = Split(s.Regions);
        var types = Split(s.Types);
        var levels = Split(s.Levels).Select(CoreColor).ToHashSet();

        return all.Where(it =>
            (regions.Count == 0 || regions.Any(r => (it.AlarmArea ?? "").Contains(r))) &&
            (types.Count == 0 || types.Any(t => (it.AlarmType ?? "").Contains(t))) &&
            (levels.Count == 0 || levels.Contains(CoreColor(it.AlarmColor)))
        ).ToList();
    }

    private static List<string> Split(string? s) =>
        (s ?? "").Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).ToList();

    private static string? CoreColor(string? c)
    {
        if (string.IsNullOrEmpty(c)) return null;
        foreach (var k in new[] { "白", "蓝", "黄", "橙", "红" })
            if (c.Contains(k)) return k;
        return null;
    }
}
