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

    // 重複判定用
    private readonly Dictionary<string, int> _commandByCommand = new(StringComparer.Ordinal);

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

    private void AddCommand(string command)
    {
        var record = new CommandRecord(Commands.Count, command);
        Commands.Add(record);
        IncrementCommandCount(record.Command);
    }

    // コマンド履歴にコマンドを追加する。追加できなかった場合、false を返す
    public bool Add(string command)
    {
        command = command.Trim();
        if (command == "") return false;

        if (Unique && _commandByCommand.ContainsKey(command)) return false;

        AddCommand(command);
        return true;
    }

    // コマンドの出力結果を考慮したうえで追加する
    public bool Add(string command, string output)
    {
        command = command.Trim();
        if (command == "") return false;

        if (Unique && _commandByCommand.ContainsKey(command)) return false;

        output = output.Trim();

        var e = output.AsSpan().EnumerateLines();
        // 1 行目はエコーであるため読み飛ばす
        if (!e.MoveNext() || !e.MoveNext())
        {
            AddCommand(command);
            return true;
        }

        // コマンドの成否を確認する方法はないため、不正なコマンド名と拡張コマンド名を入力したときの出力のみ確認する
        var l2 = e.Current.Trim();

        // 不正なコマンド名
        if (l2.StartsWith($"^ Syntax error in '{command}'"))
        {
            return false;
        }

        var commandName = GetCommandName(command);
        // 不正な拡張コマンド名
        if (commandName.StartsWith('!') && l2.StartsWith($"{commandName[1..]} is not extension gallery command"))
        {
            return false;
        }

        AddCommand(command);
        return true;
    }

    // 履歴を全消去する
    public void Clear()
    {
        Commands.Clear();
        _commandByCommand.Clear();
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

        var record = Commands[index];
        if (record == null) return false;

        // index を変えないために null 消去を行う
        Commands[index] = null;
        DecrementCommandCount(record.Command);
        return true;
    }

    private void IncrementCommandCount(string command)
    {
        _commandByCommand.TryGetValue(command, out var count);
        _commandByCommand[command] = count + 1;
    }

    private void DecrementCommandCount(string command)
    {
        if (!_commandByCommand.TryGetValue(command, out var count)) return;

        if (count <= 1)
        {
            _commandByCommand.Remove(command);
        }
        else
        {
            _commandByCommand[command] = count - 1;
        }
    }

    // コマンドからコマンド名を取り出す
    public static string GetCommandName(string s)
    {
        s = s.TrimStart();
        var p = s.IndexOf(' ');
        var commandName = p == -1 ? s : s[..p];
        return commandName.ToLowerInvariant();
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
