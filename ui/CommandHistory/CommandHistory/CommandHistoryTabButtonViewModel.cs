/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Util;

namespace CommandHistory;

[Export(typeof(IDbgRibbonTabGroupExtension))]
[RibbonTabGroupExtensionMetadata("ViewRibbonTab", "Windows", 1001)]
public class CommandHistoryTabButtonViewModel : IDbgRibbonTabGroupExtension
{
    [Import] private IDbgToolWindowManager? _toolWindowManager = null;

    public CommandHistoryTabButtonViewModel()
    {
        OpenCommandHistoryToolWindow = new DelegateCommand(delegate
        {
            _toolWindowManager?.OpenToolWindow("CommandHistoryToolWindow");
        });
    }
    
    // リボンタブグループにボタンを追加する
    public IEnumerable<FrameworkElement> Controls =>
        new List<FrameworkElement>
        {
            new CommandHistoryTabButtonControl(this)
        };

    public DelegateCommand OpenCommandHistoryToolWindow { get; }
}
