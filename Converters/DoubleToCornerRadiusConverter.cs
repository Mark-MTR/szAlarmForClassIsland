using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SzAlarmPlugin.Converters;

/// <summary>
/// 把 double 转换为 Avalonia 的 CornerRadius，用于绑定设置里的圆角半径到 Border。
/// </summary>
public class DoubleToCornerRadiusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var r = value is double d ? d : 0;
        return new CornerRadius(r);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is CornerRadius cr ? cr.TopLeft : 0;
}
