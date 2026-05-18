/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace CommandHistory;

// コマンド履歴
public class CommandHistory : INotifyPropertyChanged
{
    // コマンド履歴
    public ObservableCollection<CommandRecord?> Commands { get; } = [];
    // 重複を排除するか？
    public bool Unique
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    } = true;

    // UI 表示用のビュー。 null を除外する
    public ICollectionView CommandsView { get; }

    public CommandHistory()
    {
        CommandsView = CollectionViewSource.GetDefaultView(Commands);
        CommandsView.Filter = x => x != null;
    }

    // コマンド履歴にコマンドを追加する。追加できなかった場合、false を返す
    public bool Add(string command)
    {
        command = command.Trim();
        if (command == "") return false;

        if (Unique && Commands.Any(record => command == record?.Command)) return false;

        Commands.Add(new CommandRecord(Commands.Count, command));
        return true;
    }

    // コマンドの出力結果を考慮したうえで追加する
    public bool Add(string command, string output)
    {
        command = command.Trim();
        if (command == "") return false;

        if (Unique)
        {
            if (Commands.Any(record => command == record?.Command)) return false;
        }

        var trimmedCommand = command.Trim();
        output = output.Trim();

        var e = output.AsSpan().EnumerateLines();
        // 1 行目はエコーであるため読み飛ばす
        if (!e.MoveNext() || !e.MoveNext())
        {
            Commands.Add(new CommandRecord(Commands.Count, command));
            return true;
        }

        // コマンドの成否を確認する方法はないため、不正なコマンド名と拡張コマンド名を入力したときの出力のみ確認する
        var l2 = e.Current.Trim();

        // 不正なコマンド名
        if (l2.StartsWith($"^ Syntax error in '{command}'"))
        {
            return false;
        }

        var commandName = GetCommandName(trimmedCommand);
        // 不正な拡張コマンド名
        if (commandName.StartsWith('!') && l2.StartsWith($"{commandName[1..]} is not extension gallery command"))
        {
            return false;
        }

        Commands.Add(new CommandRecord(Commands.Count, command));
        return true;
    }

    // 履歴を消去する
    public void Clear()
    {
        Commands.Clear();
    }

    // index に対応するコマンドを取得する
    public string? GetCommand(int index)
    {
        if (CheckIndex(index))
        {
            var command = Commands[index];
            if (command != null) return command.Command;
        }
        return null;
    }

    private bool CheckIndex(int index)
    {
        return 0 <= index && index < Commands.Count;
    }

    // 指定した index のコマンドを消去する
    public bool Remove(int index)
    {
        if (!CheckIndex(index)) return false;

        var command = Commands[index];
        if (command == null) return false;

        // index を変えないために null 消去を行う
        Commands[index] = null;
        CommandsView.Refresh();
        return true;

    }

    private static string GetCommandName(string s)
    {
        s = s.TrimStart();
        var p = s.IndexOf(' ');
        return p == -1 ? s : s[..p];
    }

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
