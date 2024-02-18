namespace RainEd.ChangeHistory;

class CellChangeRecord : IChangeRecord
{
    public struct CellChange
    {
        public int X, Y, Layer;
        public LevelCell OldState, NewState;
    };

    public int EditMode;
    public List<CellChange> CellChanges = new();

    public CellChangeRecord()
    {}

    public bool HasChange() => true;
    public void Apply(bool useNew)
    {
        var level = RainEd.Instance.Level;
        RainEd.Instance.Window.EditMode = EditMode;

        foreach (CellChange change in CellChanges)
        {
            level.Layers[change.Layer, change.X, change.Y] = useNew ? change.NewState : change.OldState;
        }
    }
}

class CellChangeRecorder
{
    private LevelCell[,,] snapshotLayers = null!;
    private int editMode;

    public CellChangeRecorder()
    {
        UpdateSnapshot();
    }

    private void UpdateSnapshot()
    {
        editMode = RainEd.Instance.Window.EditMode;
        snapshotLayers = (LevelCell[,,]) RainEd.Instance.Level.Layers.Clone();
    }

    public void PushChange()
    {
        var changes = new CellChangeRecord()
        {
            EditMode = editMode
        };

        var level = RainEd.Instance.Level;

        for (int l = 0; l < Level.LayerCount; l++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    if (!snapshotLayers[l,x,y].Equals(level.Layers[l,x,y]))
                    {
                        changes.CellChanges.Add(new CellChangeRecord.CellChange()
                        {
                            X = x, Y = y, Layer = l,
                            OldState = snapshotLayers[l,x,y],
                            NewState = level.Layers[l,x,y]
                        });
                    }
                }
            }
        }

        if (changes.CellChanges.Count > 0)
            RainEd.Instance.ChangeHistory.PushCustom(changes);

        UpdateSnapshot();
    }
}