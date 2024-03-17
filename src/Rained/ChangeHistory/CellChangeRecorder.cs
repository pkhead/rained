namespace RainEd.ChangeHistory;

struct SelectionRecord
{
    public bool IsActive;
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public readonly override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (SelectionRecord) obj;
        return IsActive == other.IsActive && X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }
    
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(IsActive.GetHashCode(), X.GetHashCode(), Y.GetHashCode(), Width.GetHashCode(), Height.GetHashCode());
    }

    public static bool operator==(SelectionRecord a, SelectionRecord b)
    {
        return a.IsActive == b.IsActive && a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
    }

    public static bool operator!=(SelectionRecord a, SelectionRecord b)
    {
        return !(a == b);
    }
}

class CellChangeRecord : IChangeRecord
{
    public struct CellChange
    {
        public int X, Y, Layer;
        public LevelCell OldState, NewState;
    };

    public int EditMode;
    public List<CellChange> CellChanges = new();

    public SelectionRecord OldSelection;
    public SelectionRecord NewSelection;

    public CellChangeRecord()
    {}
    
    public void Apply(bool useNew)
    {
        var level = RainEd.Instance.Level;
        RainEd.Instance.Window.EditMode = EditMode;

        foreach (CellChange change in CellChanges)
        {
            level.Layers[change.Layer, change.X, change.Y] = useNew ? change.NewState : change.OldState;
            RainEd.Instance.Window.LevelRenderer.Geometry.MarkNeedsRedraw(change.X, change.Y, change.Layer);
        }

        SelectionRecord selectionRec = useNew ? NewSelection : OldSelection;
        
        if (EditMode == (int)EditModeEnum.Geometry)
        {
            var geoEditor = RainEd.Instance.Window.GetEditor<GeometryEditor>();
            geoEditor.SetSelection(selectionRec);
        }
    }
}

class CellChangeRecorder
{
    private LevelCell[,,]? snapshotLayers = null;
    public SelectionRecord selectionSnapshot;
    
    public bool IsSelectionActive = false;
    public int SelectionX;
    public int SelectionY;
    public int SelectionWidth;
    public int SelectionHeight;

    public void BeginChange()
    {
        if (snapshotLayers != null)
            throw new Exception("CellChangeRecorder.BeginChange() called twice");

        snapshotLayers = (LevelCell[,,]) RainEd.Instance.Level.Layers.Clone();

        // account for level rendering overlay
        var geoRenderer = RainEd.Instance.Window.LevelRenderer.Geometry;
        if (geoRenderer.Overlay is not null)
        {
            int sx = geoRenderer.OverlayX;
            int sy = geoRenderer.OverlayY;
            int width = geoRenderer.OverlayWidth;
            int height = geoRenderer.OverlayHeight;

            for (int x = sx; x < sx + width; x++)
            {
                for (int y = sy; y < sy + height; y++)
                {
                    for (int l = 0; l < 3; l++)
                    {
                        snapshotLayers[l,x,y] = geoRenderer.GetDrawnCell(l, x, y);
                    }
                }
            }
        }

        selectionSnapshot.IsActive = IsSelectionActive;
        selectionSnapshot.X = SelectionX;
        selectionSnapshot.Y = SelectionY;
        selectionSnapshot.Width = SelectionWidth;
        selectionSnapshot.Height = SelectionHeight;
    }

    public void TryPushChange()
    {
        if (snapshotLayers is null)
            return;
        
        var changes = new CellChangeRecord()
        {
            EditMode = RainEd.Instance.Window.EditMode,
            OldSelection = selectionSnapshot,
            NewSelection = new SelectionRecord()
            {
                IsActive = selectionSnapshot.IsActive,
                X = SelectionX,
                Y = SelectionY,
                Width = SelectionWidth,
                Height = SelectionHeight
            }
        };

        var level = RainEd.Instance.Level;
        var geoRenderer = RainEd.Instance.Window.LevelRenderer.Geometry;

        for (int l = 0; l < Level.LayerCount; l++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    var cell = geoRenderer.GetDrawnCell(l,x,y);

                    if (!snapshotLayers[l,x,y].Equals(cell))
                    {
                        changes.CellChanges.Add(new CellChangeRecord.CellChange()
                        {
                            X = x, Y = y, Layer = l,
                            OldState = snapshotLayers[l,x,y],
                            NewState = cell
                        });
                    }
                }
            }
        }

        if (changes.CellChanges.Count > 0 || changes.NewSelection != changes.OldSelection)
            RainEd.Instance.ChangeHistory.Push(changes);

        snapshotLayers = null;
    }

    public void PushChange()
    {
        if (snapshotLayers is null)
            throw new Exception("CellChangeRecorder.PushChange() called twice");
        
        TryPushChange();
    }
}