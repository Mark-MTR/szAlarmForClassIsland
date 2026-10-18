using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SzAlarmPlugin.Models;

/// <summary>
/// 自定义规则“存在深圳气象预警”的设置。
/// 三个过滤条件留空均表示“不限制（匹配所有）”。
/// 手动实现 INotifyPropertyChanged（不依赖 CommunityToolkit 源码生成器）：
/// 属性变更会抛出 PropertyChanged，冒泡到 Profile 触发 ClassIsland 的保存，
/// 解决“设置不保存”问题。同时避免 partial + 源码生成器在某些环境下不生成类型导致的 CS0246。
/// </summary>
public class SzAlarmRuleSettings : INotifyPropertyChanged
{
    /// <summary>
    /// 地名关键词，空格分隔（如：南山 福田 宝安）。
    /// 仅当某条预警的发布区域包含其中任一关键词时才计入；留空表示所有地区。
    /// </summary>
    public string Regions
    {
        get => _regions;
        set => Set(ref _regions, value ?? "");
    }

    /// <summary>
    /// 灾害种类关键词，空格分隔（如：台风 暴雨 雷雨大风）。
    /// 严格匹配：预警类型与关键词整段相等才命中（“雷暴”不会命中“雷暴大风”）。
    /// 留空表示所有种类。
    /// </summary>
    public string Types
    {
        get => _types;
        set => Set(ref _types, value ?? "");
    }

    /// <summary>
    /// 预警级别关键词，空格分隔（白 蓝 黄 橙 红）。
    /// 仅当某条预警的级别命中其中任一关键词时才计入；留空表示所有级别。
    /// </summary>
    public string Levels
    {
        get => _levels;
        set => Set(ref _levels, value ?? "");
    }

    /// <summary>
    /// “有且仅有该类型”排他模式。开启后，规则为真的条件变为：
    /// 当前所有生效预警的类型都必须命中「灾害种类」（不允许出现其它类型），
    /// 且其中至少有一条同时满足地区/级别过滤。
    /// 用于表达“只有当全市只有台风预警（没有别的预警）时才触发”这类需求。
    /// 注意：开启时「灾害种类」必须至少填一个关键词，否则永远不成立。
    /// </summary>
    public bool ExclusiveType
    {
        get => _exclusiveType;
        set => Set(ref _exclusiveType, value);
    }

    private string _regions = "";
    private string _types = "";
    private string _levels = "";
    private bool _exclusiveType;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
