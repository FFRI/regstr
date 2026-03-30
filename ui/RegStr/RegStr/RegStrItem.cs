/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RegStr;

/// <summary>
/// ObservableCollection で管理しやすくするためのタプル代わりのクラス。
/// </summary>
public class RegStrItem(string name, ulong value, string str) : INotifyPropertyChanged
{
    public string Name { get; } = name;

    public ulong Value
    {
        get;
        set
        {
            if (field == value) return;
            field = value;

            OnPropertyChanged();
        }
    } = value;

    public string Str { get; } = str;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
