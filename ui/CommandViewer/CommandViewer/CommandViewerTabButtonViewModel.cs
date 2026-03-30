/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Util;

namespace CommandViewer;

[Export(typeof(IDbgRibbonTabGroupExtension))]
[RibbonTabGroupExtensionMetadata("ViewRibbonTab", "Windows", 1000)]
public class CommandViewerTabButtonViewModel : IDbgRibbonTabGroupExtension
{
    [Import] private IDbgToolWindowManager? _toolWindowManager = null;

    public CommandViewerTabButtonViewModel()
    {
        OpenCommandViewerToolWindow = new DelegateCommand(delegate
        {
            _toolWindowManager?.OpenToolWindow("CommandViewerToolWindow");
        });
    }
    // リボンタブグループにボタンを追加する
    public IEnumerable<FrameworkElement> Controls =>
        new List<FrameworkElement>
        {
            new CommandViewerTabButtonControl(this)
        };

    public DelegateCommand OpenCommandViewerToolWindow { get; }
}
