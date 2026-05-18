/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommandHistory;

/// <summary>
/// CommandHistoryToolWindowControl.xaml の相互作用ロジック
/// </summary>
public partial class CommandHistoryToolWindowControl
{
    private readonly CommandHistoryToolWindowViewModel _viewModel;

    public CommandHistoryToolWindowControl(CommandHistoryToolWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void HistoryListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (item.DataContext is not CommandRecord record) return;

        _ = _viewModel.ReplayCommand(record.Command);
        e.Handled = true;
    }

    private void HistoryListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not CommandRecord record) return;

        switch (e.Key)
        {
            case Key.Enter:
                _ = _viewModel.ReplayCommand(record);
                e.Handled = true;
                break;
            case Key.Delete:
                MoveSelectionNext(listBox);
                _viewModel.History.Remove(record.Id);
                e.Handled = true;
                break;
        }
    }

    private void LocalHistoryListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not CommandRecord record) return;

        switch (e.Key)
        {
            case Key.Enter:
                _ = _viewModel.ReplayCommand(record);
                e.Handled = true;
                break;
            case Key.Delete:
                MoveSelectionNext(listBox);
                _viewModel.LocalHistory.Remove(record.Id);
                e.Handled = true;
                break;
        }
    }

    private void GlobalHistoryListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not CommandRecord record) return;

        switch (e.Key)
        {
            case Key.Enter:
                _ = _viewModel.ReplayCommand(record);
                e.Handled = true;
                break;
            case Key.Delete:
                MoveSelectionNext(listBox);
                _viewModel.RemoveFromGlobalHistory(record.Id);
                e.Handled = true;
                break;
        }
    }

    private void HistoryListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;

        item.IsSelected = true;
        item.Focus();
    }

    private void HistoryRemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetRecordFromMenuItem(sender) is not { } record) return;

        MoveSelectionNext(HistoryListBox);
        _viewModel.History.Remove(record.Id);
    }

    private void LocalHistoryRemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetRecordFromMenuItem(sender) is not { } record) return;

        MoveSelectionNext(LocalHistoryListBox);
        _viewModel.LocalHistory.Remove(record.Id);
    }

    private void GlobalHistoryRemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetRecordFromMenuItem(sender) is not { } record) return;

        MoveSelectionNext(GlobalHistoryListBox);
        _viewModel.RemoveFromGlobalHistory(record.Id);
    }

    private void LocalPinMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetRecordFromMenuItem(sender) is not { } record) return;

        _viewModel.LocalHistory.Add(record.Command);
    }

    private void GlobalPinMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetRecordFromMenuItem(sender) is not { } record) return;

        _viewModel.AddToGlobalHistory(record.Command);
    }

    // ListBox の選択中の要素を一つ後ろにずらす
    private static void MoveSelectionNext(ListBox listBox)
    {
        if (listBox.Items.Count == 0)
        {
            // 要素が無い場合は選択解除
            listBox.SelectedIndex = -1;
        }
        // 後ろにずらす
        if (listBox.SelectedIndex == listBox.Items.Count - 1)
        {
            // 末尾の要素を消す場合、2 つ前を選択する
            listBox.SelectedIndex = listBox.Items.Count - 2;
            return;
        }
        listBox.SelectedIndex += 1;
    }

    // コンテキストメニューから選択中の CommandRecord を取得する
    private static CommandRecord? GetRecordFromMenuItem(object sender)
    {
        if (sender is not MenuItem menuItem) return null;

        if (menuItem.Parent is not ContextMenu contextMenu) return null;

        if (contextMenu.PlacementTarget is not ListBoxItem listBoxItem) return null;

        return listBoxItem.DataContext as CommandRecord;
    }
}
