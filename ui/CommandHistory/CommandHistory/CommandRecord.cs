/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandHistory;

// コマンドと UI 表示用の ID を持つクラス
public record CommandRecord : INotifyPropertyChanged
{
    public CommandRecord(int id, string command)
    {
        Id = id;
        Command = command;
    }

    public int Id { get; }

    public int ViewId => Id + 1;

    public string Command { get; init; }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
