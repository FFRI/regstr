/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
namespace RegStr;

/// <summary>
///     RegStrToolWindowControl.xaml の相互作用ロジック
/// </summary>
public partial class RegStrToolWindowControl
{
    public RegStrToolWindowControl(RegStrToolWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
