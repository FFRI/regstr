/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using DbgX.Interfaces;

namespace CommandViewer;

[Export(typeof(IDbgToolWindow))]
[NamedPartMetadata("CommandViewerToolWindow")]
public class CommandViewerToolWindow : IDbgToolWindow
{
    [Import] private ICompositionService? _compositionService = null;

    public FrameworkElement? GetToolWindowView(object parameter)
    {
        try
        {
            return new CommandViewerToolWindowControl(new CommandViewerToolWindowViewModel(_compositionService));
        }
        catch (FileNotFoundException e)
        {
            // 依存 DLL を入れ忘れた場合のメッセージ表示
            MessageBox.Show($"{e.FileName} not found.");
            return null;
        }
    }
}
