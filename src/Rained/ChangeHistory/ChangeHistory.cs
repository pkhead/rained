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
        foreach (var v in undoStack)
        {
            if (v is IDisposable a) a.Dispose();
        }
        foreach (var v in redoStack)
        {
            if (v is IDisposable a) a.Dispose();
        }

        undoStack.Clear();
        redoStack.Clear();

        Cleared?.Invoke();
        RainEd.Instance?.UpdateTitle();
    }
    
    public void Push(IChangeRecord record)
    {
        foreach (var v in redoStack)
        {
            if (v is IDisposable a) a.Dispose();
        }
        
        redoStack.Clear();
        undoStack.Push(record);
        RainEd.Instance?.UpdateTitle();
    }

    public bool Undo()
    {
        if (undoStack.Count == 0) return false;
        var record = undoStack.Pop();
        redoStack.Push(record);
        record.Apply(false);
        UndidOrRedid?.Invoke();

        RainEd.Instance?.UpdateTitle();
        return true;
    }

    public bool Redo()
    {
        if (redoStack.Count == 0) return false;
        var record = redoStack.Pop();
        undoStack.Push(record);
        record.Apply(true);
        UndidOrRedid?.Invoke();

        RainEd.Instance?.UpdateTitle();
        return true;
    }

    public void MarkUpToDate()
    {
        upToDate = undoStack.Count == 0 ? null : undoStack.Peek();
        dirty = false;
        RainEd.Instance?.UpdateTitle();
    }

    public void ForceMarkDirty()
    {
        dirty = true;
        RainEd.Instance?.UpdateTitle();
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