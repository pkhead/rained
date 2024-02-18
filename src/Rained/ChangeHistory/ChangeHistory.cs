namespace RainEd.ChangeHistory;

interface IChangeRecord
{
    bool HasChange();
    void Apply(bool useNew);
}

class ChangeHistory
{
    public Level Level { get => RainEd.Instance.Level; }

    private readonly Stack<IChangeRecord> undoStack = new();
    private readonly Stack<IChangeRecord> redoStack = new();
    private IChangeRecord? upToDate = null;

    public event Action? Cleared = null;

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();

        Cleared?.Invoke();
    }
    
    public void PushCustom(IChangeRecord record)
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
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;
        var record = redoStack.Pop();
        undoStack.Push(record);
        record.Apply(true);
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