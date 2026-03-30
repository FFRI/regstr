/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DbgX.Interfaces.Dml;
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

    public CommandViewerToolWindowViewModel(ICompositionService? compositionService)
    {
        // コンストラクタで compositionService を受け取り、合成
        compositionService?.SatisfyImportsOnce(this);
        CommandInputEnterCommand = new DelegateCommand(ExecuteInputCommand);
        PrevHistoryCommand = new DelegateCommand(PrevHistory, PrevHistoryCanExecute);
        NextHistoryCommand = new DelegateCommand(NextHistory, NextHistoryCanExecute);
        DeleteHistoryCommand = new DelegateCommand(DeleteHistory, DeleteHistoryCanExecute);
        _eventBus?.Subscribe<TargetInitializedEventArgs>(OnTargetInitialized);
        _eventBus?.Subscribe<TargetRefreshEventArgs>(OnTargetRefresh);
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

    private DiffResult GetDiffResult(string oldText, string newText)
    {
        return SelectedDiffChunkerKind switch
        {
            DiffChunkerKind.None => new DiffResult([], [newText], []),
            DiffChunkerKind.Char => Differ.Instance.CreateCharacterDiffs(oldText, newText, true),
            DiffChunkerKind.Word => Differ.Instance.CreateWordDiffs(oldText, newText, true, [' ', '\n']),
            DiffChunkerKind.Line => Differ.Instance.CreateLineDiffs(oldText, newText, true),
            _ => throw new UnreachableException()
        };
    }

    public string CommandOutput
    {
        get;
        set
        {
            CommandOutputDiff = new DiffInfo(GetDiffResult(field, value), SelectedDiffChunkerKind);
            field = value;
            RaiseCanExecuteChanged();
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
        // ターゲットの初期化後に更新する
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
            CommandOutput = DmlToText(await _console.ExecuteCommandAndCaptureOutputAsync(CommandInput));
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
        CancelAndRefreshAsync();
    }

    private void OnTargetRefresh(object? sender, TargetRefreshEventArgs e)
    {
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

    // 簡易的な禁止コマンドの確認を行う。複文等には対応しない。
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

    /// <summary>
    /// DML 文字列からタグを除去した文字列を返す。
    /// </summary>
    /// <param name="s">DML 文字列</param>
    /// <returns>タグを除去した文字列</returns>
    private static string DmlToText(string s)
    {
        // パーサーを作成して DML 文字列を渡す
        var parser = new DmlParser();
        parser.AppendDml(s);
        var ret = new StringBuilder(s.Length);
        // ノードがある限り取得し続ける
        while (parser.HasMoreNodes)
        {
            // 1 つノードを取り出す
            var node = parser.ReadNode();
            // 構文エラーの場合は元文字列を返す
            if (node == null) return s;
            // テキストノードでない場合は次へ
            if (node.Type != DmlNodeType.Text) continue;
            // テキストノードであれば追加
            ret.Append(node.Text);
        }

        return ret.ToString();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}
