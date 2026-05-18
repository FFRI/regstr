/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using System.Windows.Controls;
using DbgX.Interfaces;

namespace RegStr;

/// <summary>
/// リボンタブのビューモデル。
/// 常に右に配置するため、order を小さい値に設定。
/// </summary>
[Export(typeof(IDbgRibbonTab))]
[RibbonTabMetadata("RegStrRibbonTab", -1000)]
internal class RegStrRibbonTabViewModel : IDbgRibbonTab
{
    // RegStrRibbonTab を返す
    public Control Tab => new RegStrRibbonTabControl();
}
