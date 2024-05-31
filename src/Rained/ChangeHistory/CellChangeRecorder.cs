using System.Numerics;

namespace RainEd.ChangeHistory;

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
    private Dictionary<(int, int, int), Vector2i>? snapshotChains = null;

    public void BeginChange()
    {
        if (snapshotLayers != null)
            throw new Exception("CellChangeRecorder.BeginChange() called twice");

        snapshotLayers = (LevelCell[,,]) RainEd.Instance.Level.Layers.Clone();

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
            EditMode = RainEd.Instance.LevelView.EditMode
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

        if (changes.CellChanges.Count > 0 || changes.ChainHolderChanges.Count > 0)
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