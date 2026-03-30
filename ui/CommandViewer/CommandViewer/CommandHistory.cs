/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandViewer;

public class CommandHistory : INotifyPropertyChanged
{
    private int CommandHistoryPos
    {
        get;
        set
        {
            if (field == value) return;
            // -1 未満にはならない
            if (value < -1) return;
            // Count より大きくはならない
            if (value > CommandHistoryNodes.Count) return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CommandHistoryNodes));
            OnPropertyChanged(nameof(CommandHistoryPosView));
        }
    } = -1;

    // ビュー用の pos は 1-origin
    public int CommandHistoryPosView => CommandHistoryPos + 1;

    public int Count => CommandHistoryNodes.Count;

    private LinkedList<string> CommandHistoryNodes { get; } = [];

    public LinkedListNode<string>? CurCommandHistoryNode
    {
        get;
        set
        {
            if (field?.Value == value?.Value) return;

            field = value;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void AddLast(string commandOutput)
    {
        var node = CommandHistoryNodes.AddLast(commandOutput);
        CurCommandHistoryNode = node;
        SetPosLast();
        OnPropertyChanged(nameof(Count));
    }

    public void SetPosLast()
    {
        CommandHistoryPos = CommandHistoryNodes.Count - 1;
    }

    public void Delete()
    {
        if (CurCommandHistoryNode == null) return;
        LinkedListNode<string>? node;
        // 削除後は前の要素に移動する
        // 先頭要素を消す場合は後の要素に移動する
        if (CommandHistoryPos != 0)
        {
            node = CurCommandHistoryNode.Previous;
            CommandHistoryPos--;
        }
        else
        {
            node = CurCommandHistoryNode.Next;
            // 空になる場合
            if (CommandHistoryNodes.Count == 1) CommandHistoryPos--;
        }
        CommandHistoryNodes.Remove(CurCommandHistoryNode);
        CurCommandHistoryNode = node;
        OnPropertyChanged(nameof(CommandHistoryNodes));
        OnPropertyChanged(nameof(Count));
    }

    public bool CanDelete()
    {
        return CommandHistoryNodes.Count != 0;
    }

    public void Next()
    {
        CommandHistoryPos++;
        CurCommandHistoryNode = CurCommandHistoryNode?.Next;
    }

    public void Prev()
    {
        CommandHistoryPos--;
        CurCommandHistoryNode = CurCommandHistoryNode?.Previous;
    }

    public string GetCurrentCommandOutput()
    {
        return CurCommandHistoryNode == null ? "" : CurCommandHistoryNode.Value;
    }

    public string? Last()
    {
        return CommandHistoryNodes.Last?.Value;
    }

    // next できるか？
    public bool CanNext()
    {
        return CommandHistoryPos < CommandHistoryNodes.Count - 1;
    }

    // prev できるか？
    public bool CanPrev()
    {
        return CommandHistoryPos > 0;
    }
}
