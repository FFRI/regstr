/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel.Composition;
using DbgX.Interfaces.Attributes;

namespace CommandHistory;

// グローバル履歴を設定するクラス
[Export]
[SerializedSettingsObject("CommandHistory")]
public class CommandHistorySettings
{
    // グローバル履歴
    public List<string>? Global { get; set; } = [];
    // グローバルの Unique
    public bool Unique { get; set; } = false;
}
