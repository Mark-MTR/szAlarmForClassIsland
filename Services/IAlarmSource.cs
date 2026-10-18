using System.Collections.Generic;
using System.Threading.Tasks;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Services;

/// <summary>
/// 预警数据源接口（“照葫芦画瓢”的葫芦）。
/// 每个城市 / 每个预警 API 各实现一份：
///   1. 拉取自己的接口；
///   2. 把原始 JSON 映射成统一的 AlarmItem（含图标完整地址 IconUrl）。
/// 之后在 SzAlarmService 构造函数里 new 一个加进 _sources 列表即可，
/// 组件、规则、提醒会自动合并展示，无需再改其它地方。
/// </summary>
public interface IAlarmSource
{
    /// <summary>数据源名称（用于区分来源），如 “深圳”。</summary>
    string Name { get; }

    /// <summary>拉取该数据源当前生效预警，返回统一格式列表（含图标地址 IconUrl）。</summary>
    Task<IReadOnlyList<AlarmItem>> FetchAsync();
}
