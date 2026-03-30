/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
namespace CommandViewer;

/// <summary>
/// CommandViewerTabButtonControl.xaml の相互作用ロジック
/// </summary>
public partial class CommandViewerTabButtonControl
{
    public CommandViewerTabButtonControl(CommandViewerTabButtonViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
