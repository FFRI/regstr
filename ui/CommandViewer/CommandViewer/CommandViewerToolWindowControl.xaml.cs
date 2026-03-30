/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CommandViewer;

/// <summary>
/// CommandViewerToolWindowControl.xaml の相互作用ロジック
/// </summary>
public partial class CommandViewerToolWindowControl
{
    private bool _isCommandOutputTextBoxView;
    private readonly CommandViewerToolWindowViewModel _viewModel;
    public CommandViewerToolWindowControl(CommandViewerToolWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void CommandOutputTextBlock_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SwitchToTextBox();
        e.Handled = true;
    }

    private void SwitchToTextBlock()
    {
        if (!_isCommandOutputTextBoxView) return;
        CommandOutputTextBlock.Visibility = Visibility.Visible;
        CommandOutputTextBox.Visibility = Visibility.Collapsed;
        CommandOutputTextBox.SelectionLength = 0;
        CommandOutputTextBox.CaretIndex = 0;
        _isCommandOutputTextBoxView = false;
    }
    private void SwitchToTextBox()
    {
        if (_isCommandOutputTextBoxView) return;
        CommandOutputTextBlock.Visibility = Visibility.Collapsed;
        CommandOutputTextBox.Visibility = Visibility.Visible;
        _isCommandOutputTextBoxView = true;
    }

    private void CommandOutputTextBox_OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        SwitchToTextBlock();
    }

    /// <summary>
    /// コマンドが更新されたらすぐ実行
    /// </summary>
    private void CommandInputTextBox_OnSourceUpdated(object? sender, DataTransferEventArgs e)
    {
        _viewModel.CancelAndRefreshAsync();
    }
}
