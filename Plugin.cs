using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SzAlarmPlugin.Actions;
using SzAlarmPlugin.Components;
using SzAlarmPlugin.Models;
using SzAlarmPlugin.Notifications;
using SzAlarmPlugin.Services;

namespace SzAlarmPlugin;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册预警拉取服务（单例 + 后台定时刷新）
        services.AddSingleton<SzAlarmService>();
        services.AddHostedService(sp => sp.GetRequiredService<SzAlarmService>());

        // 注册主界面组件，并关联设置控件（第二个泛型即设置控件类型）
        services.AddComponent<SzAlarmComponent, SzAlarmComponentSettingsControl>();

        // 注册自定义规则：存在符合条件的深圳气象预警
        services.AddRule<SzAlarmRuleSettings, SzAlarmRuleSettingsControl>(
            "szalarm.hasAlarm", "存在深圳气象预警", "\ue7ba", SzAlarmRules.HasAlarmHandler);

        // 注册预警提醒提供方（仅提供弹窗能力，不自动触发）
        services.AddNotificationProvider<SzAlarmNotificationProvider>();

        // 注册行动：手动触发时弹出深圳气象预警提醒
        services.AddAction<SzAlarmAlertAction, SzAlarmAlertActionSettingsControl>();
    }
}
