/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
/*
 * CommandViewer ツールウィンドウは複数作成でき、それぞれが独立した設定を持つ。
 * ウィンドウ毎の設定はレジストリで管理する。
 */
using System.ComponentModel.Composition;
using DbgX.Interfaces.Events;
using DbgX.Interfaces.Services;
using DbgX.Interfaces.Target;
using DbgX.Interfaces.Target.Options;
using DbgX.Util;

namespace CommandViewer;

// CommandViewer 毎の設定
public class CommandViewerSetting
{
    // ウィンドウ毎の GUID
    public string WindowId { get; set; } = "";
    public string CommandInput { get; set; } = "";
    public RefreshKind RefreshKindMask { get; set; } = RefreshKind.Memory | RefreshKind.Registers | RefreshKind.Scope;
    public DiffChunkerKind SelectedDiffChunkerKind { get; set; } = DiffChunkerKind.Char;
    public bool IsAlwaysRecord { get; set; }
    public bool IsPause { get; set; }
}

// すべての CommandViewer の設定を保持する TargetOption
[TargetOption(OptionName = "RestoreCommandViewerOption",
    OptionDescription = "Restore CommandViewer settings",
    Phase = TargetInitializationPhase.PostLaunch)]
public class RestoreCommandViewerOption : TargetOption
{
    public List<CommandViewerSetting>? Settings { get; set; }
}

[TargetOptionHandler(typeof(RestoreCommandViewerOption))]
public class RestoreCommandViewerOptionHandler
    : TargetOptionHandler<RestoreCommandViewerOption>, IPartImportsSatisfiedNotification
{
    [Import(AllowDefault = true)] private CommandViewerRegistry? _registry = null;

    public void OnImportsSatisfied()
    {
        /* 何もしない */
    }

    protected override Task ProcessOptionAsync(RestoreCommandViewerOption option, EngineOptions engineOptions)
    {
        _registry?.LoadRestoredStates(option.Settings);
        return Task.CompletedTask;
    }

    protected override string GetDescription(RestoreCommandViewerOption option)
    {
        return "Restore CommandViewer settings";
    }

    protected override bool CanBeMerged(RestoreCommandViewerOption first, RestoreCommandViewerOption second)
    {
        return false;
    }

    public override void UpdateTargetConfigFromCurrentTarget(IDbgTargetConfiguration config)
    {
        if (_registry == null) return;

        var option = GetOrCreateTargetOptionForTargetConfig<RestoreCommandViewerOption>(config);
        option.Settings = _registry.GetActiveWindows();
    }
}

// CommandViewer 全体の設定を扱うレジストリ
[Export]
public class CommandViewerRegistry
{
    private readonly Lock _lock = new();
    // 生存している ViewModel
    private readonly List<WeakReference<CommandViewerToolWindowViewModel>> _activeVMs = [];
    // 現在のターゲットについて復元された状態
    private readonly Dictionary<string, CommandViewerSetting> _restoredSettings = [];

    // 生存中の ViewModel をレジストリに登録
    public void Register(CommandViewerToolWindowViewModel viewModel)
    {
        CommandViewerSetting? setting;
        lock (_lock)
        {
            Prune();
            _activeVMs.Add(new WeakReference<CommandViewerToolWindowViewModel>(viewModel));

            // 設定が復元済みであれば、登録時点で適用する
            _restoredSettings.TryGetValue(viewModel.WindowId, out setting);
        }

        // UI 更新・非同期実行を伴うため、ロックの外で設定を適用
        if (setting != null) viewModel.ApplySettings(setting);
    }

    // 登録解除
    public void Unregister(CommandViewerToolWindowViewModel viewModel)
    {
        lock (_lock)
        {
            _activeVMs.RemoveAll(vm => !vm.TryGetTarget(out var ret) || ReferenceEquals(ret, viewModel));
        }
    }

    // CommandViewer の設定をレジストリに保存
    public void LoadRestoredStates(IEnumerable<CommandViewerSetting>? settings)
    {
        // 現在の CommandViewer ツールウィンドウおよびそれに対応する設定情報のタプル
        List<(CommandViewerToolWindowViewModel vm, CommandViewerSetting setting)> vms = [];
        lock (_lock)
        {
            _restoredSettings.Clear();
            if (settings != null)
            {
                foreach (var s in settings)
                {
                    if (!string.IsNullOrEmpty(s.WindowId)) _restoredSettings[s.WindowId] = s;
                }
            }

            foreach (var vm in GetActiveViewModels())
            {
                if (_restoredSettings.TryGetValue(vm.WindowId, out var state))
                {
                    vms.Add((vm, state));
                }
            }
        }

        // UI 更新・非同期実行を伴うため、ロックの外で設定を適用
        foreach (var (vm, state) in vms) vm.ApplySettings(state);
    }

    // 現在生存している CommandViewer の設定を保存
    public List<CommandViewerSetting> GetActiveWindows()
    {
        lock (_lock)
        {
            var ret = new List<CommandViewerSetting>();
            foreach (var vm in GetActiveViewModels())
            {
                ret.Add(new CommandViewerSetting
                {
                    WindowId = vm.WindowId,
                    CommandInput = vm.CommandInput,
                    RefreshKindMask = vm.RefreshKindMask,
                    SelectedDiffChunkerKind = vm.SelectedDiffChunkerKind,
                    IsAlwaysRecord = vm.IsAlwaysRecord,
                    IsPause = vm.IsPause
                });
            }
            return ret;
        }
    }

    private IEnumerable<CommandViewerToolWindowViewModel> GetActiveViewModels()
    {
        Prune();

        foreach (var vm in _activeVMs)
        {
            if (vm.TryGetTarget(out var ret)) yield return ret;
        }
    }

    private void Prune()
    {
        _activeVMs.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
}
