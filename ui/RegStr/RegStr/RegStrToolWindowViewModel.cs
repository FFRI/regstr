/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Interfaces.Enums;
using DbgX.Interfaces.Events;
using DbgX.Util;

namespace RegStr;

/// <summary>
/// RegStrToolWindow のビューモデル。
/// </summary>
[Export(typeof(IDbgToolWindow))]
[NamedPartMetadata("RegStrToolWindow")]
public class RegStrToolWindowViewModel : IDbgToolWindow, INotifyPropertyChanged, IDisposable
{
    // 最大バイト数
    private const ulong N = 32;

    // 表示するレジスタ
    internal readonly List<string> RegNames =
    [
        "rax",
        "rbx",
        "rcx",
        "rdx",
        "rdi",
        "rsi",
        "rbp",
        "rsp",
        "r8",
        "r9",
        "r10",
        "r11",
        "r12",
        "r13",
        "r14",
        "r15"
    ];

    // タスクをキャンセルするためのトークンソース
    private CancellationTokenSource? _cancellationTokenSource;

    // レジスタを読み取るために使用
    [Import] private IDbgEngineQuery? _engineQuery = null;

    // デバッギーイベントを捕捉するために使用
    [Import] private IDbgEventBus? _eventBus = null;

    private bool _isInitialized;

    // メモリを読み取るために使用
    [Import] private IDbgMemoryControl? _memoryControl = null;

    public RegStrToolWindowViewModel()
    {
        // コンストラクタでコマンドを登録
        UpdateValueCommand = new AsyncDelegateCommand<RegStrItem>(UpdateRegValue);
    }

    // ビューに反映させるためのプロパティ
    public ObservableCollection<RegStrItem> RegStrItems
    {
        get;
        set
        {
            field = value;
            // 代入時に反映するようにする
            OnPropertyChanged();
        }
    } = [];

    public FrameworkElement GetToolWindowView(object parameter)
    {
        if (!_isInitialized)
        {
            // イベントバスにハンドラーを登録
            _eventBus?.Subscribe<TargetInitializedEventArgs>(OnTargetInitialized);
            _eventBus?.Subscribe<TargetRefreshEventArgs>(OnTargetRefresh);
            _isInitialized = true;
        }

        // ウィンドウを開くときも更新する
        CancelAndRefreshAsync();
        // ツールウィンドウを開く
        return new RegStrToolWindowControl(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void CancelAndRefreshAsync()
    {
        // ターゲットの初期化後に更新する
        _cancellationTokenSource?.Cancel(); // 既に更新中だった場合、前のタスクはキャンセル
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        RefreshAsync(_cancellationTokenSource.Token);
    }

    private void OnTargetInitialized(object? sender, TargetInitializedEventArgs e)
    {
        // ターゲット初期化後に更新する
        CancelAndRefreshAsync();
    }

    private void OnTargetRefresh(object? sender, TargetRefreshEventArgs e)
    {
        if (e.ConnectionStateChanged)
            // ターゲットの接続状態が変化した場合、アイテムの中身を全削除する
            RegStrItems.Clear();

        if (e.Kinds.HasFlag(RefreshKind.Registers) || e.Kinds.HasFlag(RefreshKind.Memory) ||
            e.Kinds.HasFlag(RefreshKind.Scope))
            // ターゲットのレジスタ、メモリ、スコープのいずれかが変化した場合、更新する
            CancelAndRefreshAsync();
    }

    /// <summary>
    ///     データを取得してビューを更新する。
    /// </summary>
    private async void RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var regStrItems = new List<RegStrItem>(RegNames.Count);

            // モデルの更新
            foreach (var regName in RegNames)
            {
                var item = await GetRegStrItem(regName, cancellationToken);
                if (item == null) break;
                regStrItems.Add(item);
            }

            // ビューの更新は最後に行う
            RegStrItems = new ObservableCollection<RegStrItem>(regStrItems);
        }
        catch (OperationCanceledException)
        {
            // キャンセルが発生した場合、何もせずにリターンする
        }
        catch (Exception)
        {
            // キャンセル以外の例外発生時も何もしない
        }
    }

    private static string MakeString(byte[]? buf)
    {
        if (buf == null) return "";
        var ret = new StringBuilder(buf.Length);
        foreach (var ch in buf)
        {
            // 0x80 以上は ASCII でない
            if (ch >= 0x80) return ret.ToString();
            if (ch < 0x20)
            {
                // 0x20 未満はエスケープシーケンス
                // 以下のエスケープシーケンス以外は文字とみなさないこととする
                switch (ch)
                {
                    case 0x09:
                        ret.Append("\\t");
                        break;
                    case 0x0a:
                        ret.Append("\\n");
                        break;
                    case 0x0b:
                        ret.Append("\\v");
                        break;
                    case 0x0d:
                        ret.Append("\\r");
                        break;
                    default:
                        return ret.ToString();
                }
            }
            else
            {
                if (ch is 0x22 or 0x5c) ret.Append('\\');
                ret.Append(Convert.ToChar(ch));
            }
        }

        // ここまで来た場合、文字列の途中で切れた可能性があるため、... を付けて続きを示唆しておく。
        ret.Append("...");
        return ret.ToString();
    }


    private async Task<RegStrItem?> GetRegStrItem(string regName, CancellationToken cancellationToken)
    {
        // キャンセルを要求されていないか確認
        cancellationToken.ThrowIfCancellationRequested();
        // 使用するコンポーネントが null の場合は何もせずリターン
        if (_engineQuery == null || _memoryControl == null) return null;

        // 指定したレジスタのモデルオブジェクトを取得
        var register = await _engineQuery.QueryDynamicModelAsync($"@$curthread.Registers.User.{regName}",
            ModelQueryFlags.Default, 1, cancellationToken);
        // IDbgModelObject にキャストし、値の取得の成否を確認
        if (register is not IDbgModelObject regModel || regModel.Failed)
            // モデルオブジェクトの取得に失敗した場合、何もせずリターン (デバッグ実行前など)
            return null;

        // モデルオブジェクトを ulong 型に変換
        var regValue = Convert.ToUInt64(regModel.PrimitiveValue);

        // メモリからデータを読み取る
        var res = await _memoryControl.ReadVirtualMemoryAsync(regValue, N, cancellationToken);
        var buf = res?.Data;
        var s = MakeString(buf);

        return new RegStrItem(regName, regValue, s);
    }

    public AsyncDelegateCommand<RegStrItem> UpdateValueCommand { get; }

    // レジスタ値の更新
    private async Task UpdateRegValue(RegStrItem reg)
    {
        try
        {
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync(); // 既に更新中だった場合、前のタスクはキャンセル
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            if (_engineQuery == null) return;
            await _engineQuery.WriteModelAsync($"@$curthread.Registers.User.{reg.Name}", reg.Value.ToString(),
                _cancellationTokenSource.Token);
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

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
