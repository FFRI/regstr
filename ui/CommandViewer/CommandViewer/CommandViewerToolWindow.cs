/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Interfaces.UI;

namespace CommandViewer;

[Export(typeof(IDbgToolWindow))]
[NamedPartMetadata("CommandViewerToolWindow")]
public class CommandViewerToolWindow : IDbgToolWindow
{
    [Import] private ICompositionService? _compositionService = null;

    public FrameworkElement? GetToolWindowView(object parameter)
    {
        // ウィンドウ毎に固有のウィンドウ ID を作成
        var windowId = parameter as string;
        if (string.IsNullOrEmpty(windowId)) windowId = Guid.NewGuid().ToString("N");

        try
        {
            var control = new CommandViewerToolWindowControl(new CommandViewerToolWindowViewModel(_compositionService, windowId));

            // ウィンドウ ID を永続的に保持し、設定の復元で使用する
            ToolWindowView.SetPersistedWindowSettings(control, windowId);
            ToolWindowView.SetIsWindowPersisted(control, true);

            return control;
        }
        catch (FileNotFoundException e)
        {
            // 依存 DLL を入れ忘れた場合のメッセージ表示
            MessageBox.Show($"{e.FileName} not found.");
            return null;
        }
    }
}
