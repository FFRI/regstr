/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Globalization;
using System.Windows.Data;
using DbgX.Interfaces.Events;

namespace CommandViewer;

public class RefreshKindValueConverter : IValueConverter
{
    private RefreshKind _refreshKind;

    // RefreshKind -> bool
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null || value == null) return _refreshKind;
        var mask = (RefreshKind)parameter;
        _refreshKind = (RefreshKind)value;
        return _refreshKind.HasFlag(mask);
    }

    // bool -> RefreshKind
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter == null) return _refreshKind;
        _refreshKind ^= (RefreshKind)parameter;
        return _refreshKind;
    }
}
