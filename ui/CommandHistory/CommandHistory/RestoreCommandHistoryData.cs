/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using DbgX.Interfaces.Services;
using DbgX.Interfaces.Target;
using DbgX.Interfaces.Target.Options;
using DbgX.Util;

namespace CommandHistory;

// ローカル履歴、通常履歴とローカル履歴の Unique 設定を扱うオプション
[TargetOption(OptionName = "RestoreCommandHistoryData", OptionDescription = "Restore saved normal/local command history", Phase = TargetInitializationPhase.PreLaunch)]
public class RestoreCommandHistoryDataOption : TargetOption
{
    // ローカル履歴
    public List<string>? Local { get; set; }
    // 通常履歴の Unique
    public bool Unique { get; set; }
    // ローカル履歴の Unique
    public bool LocalUnique { get; set; }
}

// ↑のオプションのハンドラ
[TargetOptionHandler(typeof(RestoreCommandHistoryDataOption))]
public class RestoreCommandHistoryDataOptionHandler : TargetOptionHandler<RestoreCommandHistoryDataOption>, IPartImportsSatisfiedNotification
{
    [Import] private CommandHistoryToolWindowViewModel? _viewModel = null;

    public void OnImportsSatisfied()
    {
        /* 何もしない */
    }

    protected override Task ProcessOptionAsync(RestoreCommandHistoryDataOption option, EngineOptions engineOptions)
    {
        if (_viewModel == null) return Task.CompletedTask;
        // Unique の復元
        _viewModel.History.Unique = option.Unique;
        _viewModel.LocalHistory.Unique = option.LocalUnique;

        if (option.Local == null) return Task.CompletedTask;

        // ローカル履歴があれば復元
        foreach (var command in option.Local)
        {
            if (!_viewModel.CheckCommandBefore(command)) continue;

            _viewModel.LocalHistory.Add(command);
        }

        return Task.CompletedTask;
    }

    protected override string GetDescription(RestoreCommandHistoryDataOption option)
    {
        return "Restore normal/local command history";
    }

    protected override bool CanBeMerged(RestoreCommandHistoryDataOption first, RestoreCommandHistoryDataOption second)
    {
        return false;
    }

    public override void UpdateTargetConfigFromCurrentTarget(IDbgTargetConfiguration config)
    {
        if (_viewModel == null) return;

        var option =
            GetOrCreateTargetOptionForTargetConfig<RestoreCommandHistoryDataOption>(config);
        // 一旦全消去
        option.Local = [];
        // Unique の保存
        option.Unique = _viewModel.History.Unique;
        option.LocalUnique = _viewModel.LocalHistory.Unique;

        // ローカル履歴の保存
        foreach (var record in _viewModel.LocalHistory.Commands)
        {
            if (record != null) option.Local.Add(record.Command);
        }
    }
}
