/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
namespace CommandHistory;

/// <summary>
/// CommandViewerTabButtonControl.xaml の相互作用ロジック
/// </summary>
public partial class CommandHistoryTabButtonControl
{
    public CommandHistoryTabButtonControl(CommandHistoryTabButtonViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
