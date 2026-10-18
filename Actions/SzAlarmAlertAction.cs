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
/// 数据来自 SzAlarmService（统一格式 AlarmItem）。
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
            // 无预警：直接略过，不弹任何提示。
            return;
        }

        // SendAlertAsync 现在【先同步入队，再返回】，await 它可保证预警在本行动内就排好队，
        // 不会延后到行动组末尾。是否继续等待“播报结束”由复选框决定。
        foreach (var it in items)
        {
            var request = await provider.SendAlertAsync(it, Settings.AnnounceMode, Settings);
            if (Settings.WaitForCompletion)
                await provider.WaitForCompletionAsync(request);
        }
    }

    private static List<AlarmItem> Filter(IEnumerable<AlarmItem> all, SzAlarmAlertActionSettings s)
    {
        var regions = Split(s.Regions);
        var types = Split(s.Types);
        var levels = Split(s.Levels).Select(AlarmLevels.CoreColor).ToHashSet();

        return all.Where(it =>
            (regions.Count == 0 || regions.Any(r => (it.Area ?? "").Contains(r))) &&
            (types.Count == 0 || types.Any(t => (it.Type ?? "").Contains(t))) &&
            (levels.Count == 0 || levels.Contains(AlarmLevels.CoreColor(it.Color)))
        ).ToList();
    }

    private static List<string> Split(string? s) =>
        (s ?? "").Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).ToList();
}
