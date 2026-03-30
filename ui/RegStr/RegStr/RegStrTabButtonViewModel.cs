/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using System.Windows;
using DbgX.Interfaces;
using DbgX.Util;

namespace RegStr;

/// <summary>
/// リボンタブに追加するボタンのビューモデル。
/// 常に右に配置するため、order を小さい値に設定。
/// </summary>
[Export(typeof(IDbgRibbonTabGroupExtension))]
[RibbonTabGroupExtensionMetadata("RegStrRibbonTab", "Register", 0)]
public class RegStrTabButtonViewModel : IDbgRibbonTabGroupExtension
{
    // ツールウィンドウを開くために使用
    [Import] private IDbgToolWindowManager? _toolWindowManager = null;

    public RegStrTabButtonViewModel()
    {
        OpenRegStrToolWindow = new DelegateCommand(delegate
        {
            // OpenRegStrToolWindow が呼ばれたらツールウィンドウを開く
            _toolWindowManager?.OpenToolWindow("RegStrToolWindow");
        });
    }

    // ツールウィンドウを開くコマンド
    public DelegateCommand OpenRegStrToolWindow { get; }

    // リボンタブグループにボタンを追加する
    public IEnumerable<FrameworkElement> Controls =>
        new List<FrameworkElement>
        {
            new RegStrTabButtonControl(this)
        };
}
