using System.Numerics;

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

    public struct ChainHolderChange((int, int, int) tilePos, Vector2i? oldChainPos, Vector2i? newChainPos)
    {
        public int X = tilePos.Item2;
        public int Y = tilePos.Item3;
        public int Layer = tilePos.Item1;
        public Vector2i? OldChainPos = oldChainPos;
        public Vector2i? NewChainPos = newChainPos;
    }

    public int EditMode;
    public List<CellChange> CellChanges = [];
    public List<ChainHolderChange> ChainHolderChanges = [];

    public SelectionRecord OldSelection;
    public SelectionRecord NewSelection;

    public CellChangeRecord()
    {}
    
    public void Apply(bool useNew)
    {
        var level = RainEd.Instance.Level;
        RainEd.Instance.LevelView.EditMode = EditMode;

        foreach (CellChange change in CellChanges)
        {
            level.Layers[change.Layer, change.X, change.Y] = useNew ? change.NewState : change.OldState;
            RainEd.Instance.LevelView.Renderer.InvalidateGeo(change.X, change.Y, change.Layer);
            RainEd.Instance.LevelView.Renderer.InvalidateTileHead(change.X, change.Y, change.Layer);
        }

        SelectionRecord selectionRec = useNew ? NewSelection : OldSelection;
        
        if (EditMode == (int)EditModeEnum.Geometry)
        {
            var geoEditor = RainEd.Instance.LevelView.GetEditor<GeometryEditor>();
            geoEditor.SetSelection(selectionRec);
        }

        foreach (var change in ChainHolderChanges)
        {
            var chainPos = useNew ? change.NewChainPos : change.OldChainPos;
            
            if (chainPos is null)
            {
                RainEd.Instance.Level.RemoveChainData(change.Layer, change.X, change.Y);
            }
            else
            {
                var vec = chainPos.Value;
                RainEd.Instance.Level.SetChainData(change.Layer, change.X, change.Y, vec.X, vec.Y);
            }
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
    
    private Dictionary<(int, int, int), Vector2i>? snapshotChains = null;

    public void BeginChange()
    {
        if (snapshotLayers != null)
            throw new Exception("CellChangeRecorder.BeginChange() called twice");
        
        var level = RainEd.Instance.Level;

        snapshotLayers = (LevelCell[,,]) level.Layers.Clone();

        // account for level rendering overlay
        var geoRenderer = RainEd.Instance.LevelView.Renderer.Geometry;
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
                    if (!level.IsInBounds(x, y)) continue;
                    
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

        snapshotChains = [];
        foreach (var (k, v) in RainEd.Instance.Level.ChainData)
            snapshotChains[k] = v;
    }

    public void TryPushChange()
    {
        if (snapshotLayers is null || snapshotChains is null)
            return;
        
        var changes = new CellChangeRecord()
        {
            EditMode = RainEd.Instance.LevelView.EditMode,
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
        var geoRenderer = RainEd.Instance.LevelView.Renderer.Geometry;

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

        // check if a chain was removed or changed
        foreach (var (oldCell, oldPos) in snapshotChains)
        {
            if (RainEd.Instance.Level.TryGetChainData(oldCell.Item1, oldCell.Item2, oldCell.Item3, out Vector2i newPos))
            {
                // still exists and is changed
                if (oldPos != newPos)
                    changes.ChainHolderChanges.Add(new CellChangeRecord.ChainHolderChange(oldCell, oldPos, newPos));
            }
            else
            {
                // removed
                changes.ChainHolderChanges.Add(new CellChangeRecord.ChainHolderChange(oldCell, oldPos, null));
            }
        }

        // check if a chain was added
        foreach (var (cellPos, chainPos) in RainEd.Instance.Level.ChainData)
        {
            if (!snapshotChains.ContainsKey(cellPos))
            {
                changes.ChainHolderChanges.Add(new CellChangeRecord.ChainHolderChange(cellPos, null, chainPos));
            }
        }

        if (changes.CellChanges.Count > 0 || changes.ChainHolderChanges.Count > 0 || changes.NewSelection != changes.OldSelection)
            RainEd.Instance.ChangeHistory.Push(changes);

        snapshotLayers = null;
        snapshotChains = null;
    }

    public void PushChange()
    {
        if (snapshotLayers is null || snapshotChains is null)
            throw new Exception("CellChangeRecorder.PushChange() called twice");
        
        TryPushChange();
    }
}