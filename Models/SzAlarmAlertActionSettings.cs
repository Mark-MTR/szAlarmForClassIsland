using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SzAlarmPlugin.Models;

/// <summary>
/// 行动“深圳气象预警提醒”的设置。
/// 手动实现 INotifyPropertyChanged（不依赖 CommunityToolkit 源码生成器）：
/// 属性变更会抛出 PropertyChanged，冒泡到 Profile 触发 ClassIsland 的保存，
/// 解决“设置不保存”问题。同时避免 partial + 源码生成器在某些环境下不生成类型导致的 CS0246。
/// </summary>
public class SzAlarmAlertActionSettings : INotifyPropertyChanged
{
    /// <summary>
    /// 播报策略：0 = 每次触发都播报；1 = 同一预警仅首次播报（去重）。
    /// </summary>
    public int AnnounceMode
    {
        get => _announceMode;
        set => Set(ref _announceMode, value);
    }

    /// <summary>
    /// 地名关键词（空格分隔，留空=所有地区）。
    /// </summary>
    public string Regions
    {
        get => _regions;
        set => Set(ref _regions, value ?? "");
    }

    /// <summary>
    /// 灾害种类关键词（空格分隔，留空=所有种类）。
    /// </summary>
    public string Types
    {
        get => _types;
        set => Set(ref _types, value ?? "");
    }

    /// <summary>
    /// 预警级别关键词（白 蓝 黄 橙 红，留空=所有级别）。
    /// </summary>
    public string Levels
    {
        get => _levels;
        set => Set(ref _levels, value ?? "");
    }

    /// <summary>
    /// 正文文字横向滚动速度（1=慢，20=快，步进 0.5）。
    /// </summary>
    public double ScrollSpeed
    {
        get => _scrollSpeed;
        set => Set(ref _scrollSpeed, value);
    }

    /// <summary>
    /// 提醒最大显示时长（秒，10~90，步进 1）。
    /// </summary>
    public int MaxDuration
    {
        get => _maxDuration;
        set => Set(ref _maxDuration, value);
    }

    /// <summary>
    /// 是否等待预警播报结束再继续执行后续行动。
    /// 勾选（默认）：SendAlertAsync 会 await 到提醒播放完成，行动 OnInvoke 才会返回；
    /// 不勾选：立即返回，后续行动与预警播报并行执行。
    /// </summary>
    public bool WaitForCompletion
    {
        get => _waitForCompletion;
        set => Set(ref _waitForCompletion, value);
    }

    private int _announceMode = 0;
    private string _regions = "";
    private string _types = "";
    private string _levels = "";
    private double _scrollSpeed = 5;
    private int _maxDuration = 15;
    private bool _waitForCompletion = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
