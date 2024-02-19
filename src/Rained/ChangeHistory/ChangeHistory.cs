namespace RainEd.ChangeHistory;

interface IChangeRecord
{
    void Apply(bool useNew);
}

class ChangeHistory
{
    private readonly Stack<IChangeRecord> undoStack = new();
    private readonly Stack<IChangeRecord> redoStack = new();
    private IChangeRecord? upToDate = null;

    public event Action? Cleared = null;
    public event Action? UndidOrRedid = null; // WTF DO I CALL THIS!?!?

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

    public void Undo()
    {
        if (undoStack.Count == 0) return;
        var record = undoStack.Pop();
        redoStack.Push(record);
        record.Apply(false);
        UndidOrRedid?.Invoke();
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;
        var record = redoStack.Pop();
        undoStack.Push(record);
        record.Apply(true);
        UndidOrRedid?.Invoke();
    }

    public void MarkUpToDate()
    {
        upToDate = undoStack.Count == 0 ? null : undoStack.Peek();
    }

    public bool HasChanges {
        get
        {
            if (upToDate is null)
                return undoStack.Count > 0;
            else
                return undoStack.Count == 0 || undoStack.Peek() != upToDate;
        }
    }
}