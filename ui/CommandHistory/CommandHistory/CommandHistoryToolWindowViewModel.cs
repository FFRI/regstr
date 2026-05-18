/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Interfaces.Events;
using DbgX.Interfaces.Listeners;
using DbgX.Interfaces.Services;
using DbgX.Interfaces.Target.Options;

namespace CommandHistory;

[Export]
[Export(typeof(IDbgStartupListener))] // エクスポートを追加
[Export(typeof(IDbgToolWindow))]
[NamedPartMetadata("CommandHistoryToolWindow")]
public class CommandHistoryToolWindowViewModel : INotifyPropertyChanged, IDbgToolWindow, IDbgStartupListener
{
    [Import] private IDbgEventBus? _eventBus = null;

    [Import] private IDbgConsole? _console = null;

    [Import] private CommandHistorySettings? _settings = null;

    // 通常履歴
    public CommandHistory History { get; } = new();
    // ローカル履歴
    public CommandHistory LocalHistory { get; } = new();
    // グローバル履歴
    public CommandHistory GlobalHistory { get; } = new();
    // グローバル Unique の変更用
    public bool GlobalUnique
    {
        get => GlobalHistory.Unique;
        set
        {
            if (GlobalHistory.Unique == value) return;

            GlobalHistory.Unique = value;
            _settings?.Unique = value; // 設定の Unique も更新す
            OnPropertyChanged();
        }
    }

    public FrameworkElement? GetToolWindowView(object parameter)
    {
        return new CommandHistoryToolWindowControl(this);
    }

    private void OnTargetInitialized(object? sender, TargetInitializedEventArgs e)
    {
        // ターゲット初期化時にターゲット設定から通常履歴を復元する
        // RestoreCommandHistory オプションを探す
        var option = e.TargetConfiguration.TargetOptions.OfType<RestoreCommandHistoryOption>().FirstOrDefault(opt =>
            string.Equals(opt.OptionName, "RestoreCommandHistory", StringComparison.Ordinal));

        var histories = option?.History;

        if (histories == null) return;

        // 通常履歴があれば復元
        foreach (var command in histories)
        {
            History.Add(command);
        }
    }

    private void OnActiveTargetConfigurationChanged(object? sender, ActiveTargetConfigurationChangedEventArgs e)
    {
        // ターゲットが変更されたら通常履歴とローカル履歴を消去する
        History.Clear();
        LocalHistory.Clear();
    }

    private void OnCommandExecuted(object? sender, CommandExecutedEventArgs e)
    {
        if (!CheckCommandBefore(e.Command)) return;

        _curCommand = e.Command;
        _curTextOutput = "";
        _curDmlOutput = "";
    }

    // 現在実行中のコマンド
    private string? _curCommand;
    // 現在実行中のコマンド出力 (テキスト)
    private string? _curTextOutput;
    // 現在実行中のコマンド出力 (DML)
    private string? _curDmlOutput;
    // 再実行したコマンドを実行中か？
    private bool _isExecutingReplayCommand;

    // コマンドからコマンド名を取り出す
    private static string GetCommandName(string s)
    {
        s = s.TrimStart();
        var p = s.IndexOf(' ');
        return p == -1 ? s : s[..p];
    }

    // コマンド実行前に登録する必要が無いとわかるものは除外する
    public bool CheckCommandBefore(string command)
    {
        // 再実行されたコマンドは登録しない
        if (_isExecutingReplayCommand) return false;

        command = command.Trim();
        // 空文字列(直前のコマンドの再実行)は登録しない
        if (command == "") return false;

        var commandName = GetCommandName(command);
        if (commandName is "h" or "$PinCommand" or "$ShowHistory")
        {
            // この拡張機能の持つコマンドは登録しない
            return false;
        }

        return true;
    }

    private void OnTextOutput(object? sender, TextOutputEventArgs e)
    {
        // コマンド出力 (テキスト) を傍受
        _curTextOutput = e.Text;

        if (_curCommand == null || _curTextOutput == null) return;

        // コマンド出力を考慮して履歴に登録
        History.Add(_curCommand, _curTextOutput);

        _curCommand = null;
        _curTextOutput = null;
        _curDmlOutput = null;
    }

    private void OnDmlOutput(object? sender, DmlOutputEventArgs e)
    {
        // コマンド出力 (DML) を傍受
        _curDmlOutput += e.Dml;
        if (!e.IsCommandCompletion) return;

        if (_curCommand == null || _curDmlOutput == null) return;

        // コマンド出力を考慮して履歴に登録
        History.Add(_curCommand, _curDmlOutput);

        _curCommand = null;
        _curTextOutput = null;
        _curDmlOutput = null;
    }


    public void RemoveFromGlobalHistory(int id)
    {
        if (GlobalHistory.Remove(id)) _settings?.Global?.RemoveAt(id);
    }

    public bool AddToGlobalHistory(string command)
    {
        var succeeded = GlobalHistory.Add(command);
        if (succeeded) _settings?.Global?.Add(command);
        return succeeded;
    }

    public async Task ReplayCommand(string? command)
    {
        if (_console == null || command == null) return;

        command = command.Trim();
        if (command == "") return;
        // 再実行フラグを立てておく
        _isExecutingReplayCommand = true;
        await _console.ExecuteCommandAsync(command, false, ExecuteSource.Event);
        _isExecutingReplayCommand = false;
    }

    public async Task ReplayCommand(CommandRecord? record)
    {
        if (record == null) return;
        await ReplayCommand(record.Command);
    }

    private static int? GetId(string s)
    {
        if (s == "") return null;

        var num = s;
        if (s[0] == 'l' || s[0] == 'g')
        {
            if (s.Length == 1) return null;
            num = s[1..];
        }

        return int.TryParse(num, out var value) ? value : null;
    }

    // 再実行コマンド
    [ClientCommand(Name = "h",
        Description = "Execute a command from command history")]
    private async Task ClientCommandExecute(string s)
    {

        if (_console == null) return;

        if (GetId(s) is not { } id)
        {
            _console.PrintTextToConsole("Invalid command ID\n");
            return;
        }

        var command = s[0] switch
        {
            'l' => LocalHistory.GetCommand(id - 1),
            'g' => GlobalHistory.GetCommand(id - 1),
            _ => History.GetCommand(id - 1),
        };

        if (command == null)
        {
            _console.PrintTextToConsole("Invalid command ID\n");
            return;
        }

        await ReplayCommand(command);
    }

    // 履歴表示コマンド
    [ClientCommand(Name = "$ShowHistory",
        Description = "Show command history")]
    private void ShowHistory(bool l, bool g)
    {
        if (_console == null) return;

        if (l && g)
        {
            // l と g は排他
            _console.PrintTextToConsole("-l と -g are mutually exclusive.\n");
            return;
        }

        var commands = History.Commands;
        if (l) commands = LocalHistory.Commands;
        else if (g) commands = GlobalHistory.Commands;

        if (commands.Count == 0) return;

        var sb = new StringBuilder(commands.Count * 16);

        foreach (var command in commands)
        {
            if (command == null) continue;
            sb.Append($"{command.ViewId,3}: {command.Command}").AppendLine();
        }
        _console.PrintTextToConsole(sb.ToString());
    }

    // ピン留めコマンド
    [ClientCommand(Name = "$PinCommand",
        Description = "Add a command to the history")]
    private void PinCommand(bool l, bool g, string command)
    {
        if (_console == null) return;

        if (!l && !g)
        {
            _console.PrintTextToConsole("Specify -l, -g, or both.\n");
            return;
        }

        if (l)
        {
            _console.PrintTextToConsole(LocalHistory.Add(command)
                ? "Pinned to local history.\n"
                : "Failed to pin to local history.\n");
        }

        if (g)
        {
            _console.PrintTextToConsole(AddToGlobalHistory(command)
                ? "Pinned to global history.\n"
                : "Failed to pin to global history.\n");
        }
    }

    public void OnStartup()
    {
        // 起動時にイベントバスへ登録
        _eventBus?.Subscribe<TargetInitializedEventArgs>(OnTargetInitialized);
        _eventBus?.Subscribe<ActiveTargetConfigurationChangedEventArgs>(OnActiveTargetConfigurationChanged);
        _eventBus?.Subscribe<CommandExecutedEventArgs>(OnCommandExecuted);
        _eventBus?.Subscribe<TextOutputEventArgs>(OnTextOutput);
        _eventBus?.Subscribe<DmlOutputEventArgs>(OnDmlOutput);

        // グローバル履歴の復元
        if (_settings != null)
        {
            GlobalHistory.Unique = _settings.Unique;
            if (_settings.Global != null)
            {
                // グローバル履歴があれば復元
                foreach (var command in _settings.Global)
                {
                    GlobalHistory.Add(command);
                }
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
