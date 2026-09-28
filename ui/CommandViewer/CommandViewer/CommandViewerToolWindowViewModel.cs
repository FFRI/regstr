/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Windows.UI;
using DbgX.Interfaces.Events;
using DbgX.Interfaces.Services;
using DbgX.Services.Console;
using DbgX.Util;
using DiffPlex;
using DiffPlex.Model;

namespace CommandViewer;

public class CommandViewerToolWindowViewModel : INotifyPropertyChanged, IDisposable
{
    [Import] private IDbgEventBus? _eventBus = null;

    [Import] private IDbgConsole? _console = null;

    [Import(AllowDefault = true)] private CommandViewerRegistry? _registry = null;

    private bool _disposed;

    public string WindowId { get; }

    public CommandViewerToolWindowViewModel(ICompositionService? compositionService, string windowId)
    {
        WindowId = windowId;
        // コンストラクタで compositionService を受け取り、合成
        compositionService?.SatisfyImportsOnce(this);
        CommandInputEnterCommand = new DelegateCommand(ExecuteInputCommand);
        PrevHistoryCommand = new DelegateCommand(PrevHistory, PrevHistoryCanExecute);
        NextHistoryCommand = new DelegateCommand(NextHistory, NextHistoryCanExecute);
        DeleteHistoryCommand = new DelegateCommand(DeleteHistory, DeleteHistoryCanExecute);
        _eventBus?.Subscribe<TargetInitializedEventArgs>(OnTargetInitialized);
        _eventBus?.Subscribe<TargetRefreshEventArgs>(OnTargetRefresh);

        _registry?.Register(this);
    }

    // 設定を適用する
    public void ApplySettings(CommandViewerSetting state)
    {
        RefreshKindMask = state.RefreshKindMask;
        OnPropertyChanged(nameof(RefreshKindMask));
        SelectedDiffChunkerKind = state.SelectedDiffChunkerKind;
        IsAlwaysRecord = state.IsAlwaysRecord;
        IsPause = state.IsPause;
        OnPropertyChanged(nameof(IsAlwaysRecord));
        OnPropertyChanged(nameof(IsPause));

        // CommandInput は IsBadCommand を通す。保存時に検証済みのため通常は問題無い
        CommandInput = state.CommandInput;
        OnPropertyChanged(nameof(CommandInput));

        if (!string.IsNullOrEmpty(CommandInput)) CancelAndRefreshAsync();
    }

    public CommandHistory History { get; set; } = new();

    public string CommandInput
    {
        get;
        set
        {
            if (field == value) return;


            if (IsBadCommand(value))
            {
                MessageBox.Show($"Bad command: {value}");
                field = "";
            }
            else
            {
                field = value;
            }
            OnPropertyChanged();
        }
    } = "";

    private static DiffResult GetDiffResult(string oldText, string newText, DiffChunkerKind kind)
    {
        return kind switch
        {
            DiffChunkerKind.None => new DiffResult([], [newText], []),
            DiffChunkerKind.Char => Differ.Instance.CreateCharacterDiffs(oldText, newText, true),
            DiffChunkerKind.Word => Differ.Instance.CreateWordDiffs(oldText, newText, true, [' ', '\n']),
            DiffChunkerKind.Line => Differ.Instance.CreateLineDiffs(oldText, newText, true),
            _ => throw new UnreachableException()
        };
    }

    private CancellationTokenSource? _diffCancellationTokenSource;

    private void StartDiff(string oldText, string newText, DiffChunkerKind kind)
    {
        _diffCancellationTokenSource?.Cancel();
        _diffCancellationTokenSource?.Dispose();
        _diffCancellationTokenSource = new CancellationTokenSource();
        _ = ComputeDiffAsync(oldText, newText, kind, _diffCancellationTokenSource.Token);
    }

    // 非同期的に Diff を計算
    private async Task ComputeDiffAsync(string oldText, string newText, DiffChunkerKind kind, CancellationToken token)
    {
        try
        {
            var result = await Task.Run(() => GetDiffResult(oldText, newText, kind), token);
            // 新しいタスクが既にあれば更新しない
            token.ThrowIfCancellationRequested();
            CommandOutputDiff = new DiffInfo(result, kind);
        }
        catch (OperationCanceledException)
        {
            // 何もしない
        }
    }

    public string CommandOutput
    {
        get;
        set
        {
            var oldText = field;
            field = value;
            RaiseCanExecuteChanged();
            StartDiff(oldText, value, SelectedDiffChunkerKind);
        }
    } = "";

    public RefreshKind RefreshKindMask { get; set; } = RefreshKind.Memory | RefreshKind.Registers | RefreshKind.Scope;

    // タスクをキャンセルするためのトークンソース
    private CancellationTokenSource? _cancellationTokenSource;

    private void RaiseCanExecuteChanged()
    {
        PrevHistoryCommand.RaiseCanExecuteChanged();
        NextHistoryCommand.RaiseCanExecuteChanged();
        DeleteHistoryCommand.RaiseCanExecuteChanged();
    }

    public Array DiffChunkerKinds { get; } = Enum.GetValues(typeof(DiffChunkerKind));

    public DiffChunkerKind SelectedDiffChunkerKind
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = DiffChunkerKind.Char;

    public DiffInfo CommandOutputDiff
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new(new DiffResult([], [], []), DiffChunkerKind.None);

    // コマンド実行を一時停止するか？
    public bool IsPause { get; set; } = false;

    // 変更が無くてもコマンド出力を記録するか？
    public bool IsAlwaysRecord { get; set; } = false;

    // コマンド出力を text wrap するか？
    public bool IsWrapText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WrapTextType));
            OnPropertyChanged(nameof(WrapTextTypeScrollBar));
        }
    } = false;

    // TextBlock, TextBox 反映用
    public TextWrapping WrapTextType => IsWrapText ? TextWrapping.Wrap : TextWrapping.NoWrap;

    // ScrollBar 反映用
    public ScrollBarVisibility WrapTextTypeScrollBar => IsWrapText ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

    // 入力確定ボタン
    public DelegateCommand CommandInputEnterCommand { get; }

    // ←ボタン
    public DelegateCommand PrevHistoryCommand { get; }

    // →ボタン
    public DelegateCommand NextHistoryCommand { get; }

    // delete ボタン
    public DelegateCommand DeleteHistoryCommand { get; }

    private void ExecuteInputCommand()
    {
        CancelAndRefreshAsync();
    }

    private void PrevHistory()
    {
        History.Prev();
        CommandOutput = History.GetCurrentCommandOutput();
        OnPropertyChanged(nameof(History));
        RaiseCanExecuteChanged();
    }

    private bool PrevHistoryCanExecute()
    {
        return History.CanPrev();
    }

    private void NextHistory()
    {
        History.Next();
        CommandOutput = History.GetCurrentCommandOutput();
        OnPropertyChanged(nameof(History));
        RaiseCanExecuteChanged();
    }

    private bool NextHistoryCanExecute()
    {
        return History.CanNext();
    }

    private void DeleteHistory()
    {
        History.Delete();
        CommandOutput = History.Count == 0 ? "" : History.GetCurrentCommandOutput();
        OnPropertyChanged(nameof(History));
        RaiseCanExecuteChanged();
    }

    private bool DeleteHistoryCanExecute()
    {
        return History.CanDelete();
    }

    internal void CancelAndRefreshAsync()
    {
        if (IsPause) return;

        _cancellationTokenSource?.Cancel(); // 既に更新中だった場合、前のタスクはキャンセル
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        RefreshAsync(_cancellationTokenSource.Token);
    }

    private async void RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_console == null || CommandInput == "")
            {
                CommandOutput = "";
                return;
            }

            var last = History.Last();
            var output = await _console.ExecuteCommandAndCaptureOutputAsync(CommandInput);

            if (cancellationToken.IsCancellationRequested) return;

            CommandOutput = DmlHelper.StripDmlTags(output);
            if (IsAlwaysRecord || last != CommandOutput)
            {
                // 常時記録状態か出力が前回と異なる場合は履歴に追加
                History.AddLast(CommandOutput);
                RaiseCanExecuteChanged();
            }
        }
        catch (OperationCanceledException)
        {
            // 何もしない
        }
        catch (Exception)
        {
            // 何もしない
        }
    }

    private void OnTargetInitialized(object? sender, TargetInitializedEventArgs e)
    {
        if (_disposed) return;

        CancelAndRefreshAsync();
    }

    private void OnTargetRefresh(object? sender, TargetRefreshEventArgs e)
    {
        if (_disposed) return;

        if ((e.Kinds & RefreshKindMask) != 0)
        {
            CancelAndRefreshAsync();
        }
    }

    // 禁止コマンド一覧
    // とりあえず実行系コマンドのみ禁止
    private readonly List<string> _badCommands = [
        "g", "gc", "gh", "gn", "gu",
        "p", "pa", "pc", "pct", "ph", "pr", "pt",
        "t", "ta", "tb", "tc", "tct", "th", "tr", "tt",
        "wt"
    ];

    // 簡易的な禁止コマンドの確認を行う。複文等には対応しない
    private bool IsBadCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) return false;

        var cmd = command.TrimStart().ToLowerInvariant();

        foreach (var bad in _badCommands)
        {
            if (cmd.StartsWith(bad) && (cmd.Length == bad.Length || cmd[bad.Length] == ' '))
            {
                return true;
            }
        }

        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<TargetInitializedEventArgs>(OnTargetInitialized);
            _eventBus.Unsubscribe<TargetRefreshEventArgs>(OnTargetRefresh);
        }

        _registry?.Unregister(this);
    }
}
