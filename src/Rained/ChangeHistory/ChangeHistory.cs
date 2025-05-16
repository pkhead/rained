namespace Rained.ChangeHistory;

interface IChangeRecord
{
    void Apply(bool useNew);
}

class ChangeHistory
{
    private readonly Stack<IChangeRecord> undoStack = new();
    private readonly Stack<IChangeRecord> redoStack = new();
    private IChangeRecord? upToDate = null;
    private bool dirty = false; // force flag for dirty status

    public event Action? Cleared = null;
    public event Action? UndidOrRedid = null; // WTF DO I CALL THIS!?!?

    public int UndoStackCount => undoStack.Count;
    public int RedoStackCount => redoStack.Count;

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();

        Cleared?.Invoke();
    }
    
    public void Push(IChangeRecord record)
    {
        redoStack.Clear();
        undoStack.Push(record);
    }

    public bool Undo()
    {
        if (undoStack.Count == 0) return false;
        var record = undoStack.Pop();
        redoStack.Push(record);
        record.Apply(false);
        UndidOrRedid?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (redoStack.Count == 0) return false;
        var record = redoStack.Pop();
        undoStack.Push(record);
        record.Apply(true);
        UndidOrRedid?.Invoke();
        return true;
    }

    public void MarkUpToDate()
    {
        upToDate = undoStack.Count == 0 ? null : undoStack.Peek();
        dirty = false;
    }

    public void ForceMarkDirty()
    {
        dirty = true;
    }

    public bool HasChanges {
        get
        {
            if (dirty)
                return true;
            else if (upToDate is null)
                return undoStack.Count > 0;
            else
                return undoStack.Count == 0 || undoStack.Peek() != upToDate;
        }
    }
}