/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
namespace RegStr;

/// <summary>
/// RegStrTabButtonControl.xaml の相互作用ロジック
/// </summary>
public partial class RegStrTabButtonControl
{
    public RegStrTabButtonControl(RegStrTabButtonViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
