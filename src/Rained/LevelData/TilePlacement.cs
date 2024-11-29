using Rained.Assets;
namespace Rained.LevelData;

enum TilePlacementStatus
{
    Success,
    OutOfBounds,
    Overlap,
    Geometry
};

enum TilePlacementMode : int
{
    Normal = 0,
    Force = 1,
    Geometry = 2
};

static class TilePlacement
{
    public static TilePlacementStatus ValidateTilePlacement(this Level level, Tile tile, int tileLeft, int tileTop, int layer, bool force)
    {
        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                var specInt = tile.Requirements[x,y];
                var spec2Int = tile.Requirements2[x,y];

                // check that there is not already a tile here
                if (level.IsInBounds(gx, gy))
                {
                    // placing it on a tile head can introduce a bugged state,
                    // soo... even when forced... no
                    ref var cellAtPos = ref level.Layers[layer, gx, gy];

                    if (specInt >= 0 && cellAtPos.TileHead is not null)
                        return TilePlacementStatus.Overlap;
                    
                    // check on first layer
                    var isHead = x == tile.CenterX && y == tile.CenterY;

                    if ((isHead || specInt >= 0) && !force && cellAtPos.HasTile())
                        return TilePlacementStatus.Overlap;

                    // check on second layer
                    if (layer < 2)
                    {
                        if (spec2Int >= 0 && !force && level.Layers[layer + 1, gx, gy].HasTile())
                            return TilePlacementStatus.Overlap;
                    }
                }

                if (!force)
                {
                    // check first layer geometry
                    if (specInt == -1) continue;
                    if (level.GetClamped(layer, gx, gy).Geo != (GeoType) specInt)
                        return TilePlacementStatus.Geometry;

                    // check second layer geometry
                    // if we are on layer 3, there is no second layer
                    // all checks pass
                    if (layer == 2) continue;
                    
                    if (spec2Int == -1) continue;
                    if (level.GetClamped(layer + 1, gx, gy).Geo != (GeoType) spec2Int)
                        return TilePlacementStatus.Geometry;
                }
            }
        }
        
        return TilePlacementStatus.Success;
    }

    // check that a potential placement isn't intersecting a specific already placed tile
    public static bool IsIntersectingTile(this Level level, Tile tile, int tileLeft, int tileTop, int layer, int testX, int testY, int testL)
    {
        var testTilePos = level.GetTileHead(testL, testX, testY);
        if (testTilePos.X == -1) return false;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                var specInt = tile.Requirements[x,y];
                var spec2Int = tile.Requirements2[x,y];

                // check that there is not already a tile here
                if (level.IsInBounds(gx, gy))
                {
                    ref var cellAtPos = ref level.Layers[layer, gx, gy];

                    // check on first layer
                    var isHead = x == tile.CenterX && y == tile.CenterY;

                    if ((isHead || specInt >= 0) && level.GetTileHead(layer, gx, gy) == testTilePos)
                        return true;

                    // check on second layer
                    if (layer < 2)
                    {
                        if (spec2Int >= 0 && level.GetTileHead(layer+1, gx, gy) == testTilePos)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public static TilePlacementStatus SafePlaceTile(
        this Level level,
        Tile tile,
        int layer, int tileRootX, int tileRootY,
        TilePlacementMode placeMode
    )
    {
        // check if requirements are satisfied
        TilePlacementStatus validationStatus;

        if (level.IsInBounds(tileRootX, tileRootY))
            validationStatus = level.ValidateTilePlacement(
                tile,
                tileRootX, tileRootY, layer,
                placeMode != TilePlacementMode.Normal
            );
        else
        {
            return TilePlacementStatus.OutOfBounds;
        }

        if (validationStatus == TilePlacementStatus.Success)
        {
            level.PlaceTile(
                tile,
                layer, tileRootX, tileRootY,
                placeMode == TilePlacementMode.Geometry
            );
        }

        return validationStatus;
    }

    public static void PlaceTile(
        this Level level,
        Tile tile,
        int layer, int tileRootX, int tileRootY,
        bool placeGeometry
    )
    {
        int tileLeft = tileRootX - tile.CenterX;
        int tileTop = tileRootY - tile.CenterY;

        var view = RainEd.Instance.LevelView;
        var levelRenderer = view.Renderer;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                if (!level.IsInBounds(gx, gy)) continue;

                int specInt = tile.Requirements[x,y];
                int spec2Int = tile.Requirements2[x,y];

                if (placeGeometry)
                {
                    // place first layer    
                    if (specInt >= 0)
                    {
                        level.Layers[layer, gx, gy].Geo = (GeoType) specInt;
                        view.InvalidateGeo(gx, gy, layer);
                    }

                    // place second layer
                    if (layer < 2 && spec2Int >= 0)
                    {
                        level.Layers[layer+1, gx, gy].Geo = (GeoType) spec2Int;
                        view.InvalidateGeo(gx, gy, layer+1);
                    }
                }

                // tile first 
                if (specInt >= 0)
                {
                    ref var cell = ref level.Layers[layer, gx, gy];
                    if (cell.HasTile())
                        levelRenderer.InvalidateTileHead(gx, gy, layer);
                    
                    cell.TileRootX = tileRootX;
                    cell.TileRootY = tileRootY;
                    cell.TileLayer = layer;
                }

                // tile second layer
                if (spec2Int >= 0 && layer < 2)
                {
                    ref var cell = ref level.Layers[layer+1, gx, gy];
                    if (cell.HasTile())
                        levelRenderer.InvalidateTileHead(gx, gy, layer+1);
                    
                    cell.TileRootX = tileRootX;
                    cell.TileRootY = tileRootY;
                    cell.TileLayer = layer;
                }
            }
        }

        // place tile root
        level.Layers[layer, tileRootX, tileRootY].TileHead = tile;
        levelRenderer.InvalidateTileHead(tileRootX, tileRootY, layer);
    }

    /// <summary>
    /// Remove a tile head at a given position
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="tileRootX"></param>
    /// <param name="tileRootY"></param>
    /// <param name="removeGeometry"></param>
    /// <exception cref="Exception">Thrown if the tile at the given position is not a tile head</exception>
    public static void RemoveTile(this Level level, int layer, int tileRootX, int tileRootY, bool removeGeometry)
    {
        var view = RainEd.Instance.LevelView;
        var levelRenderer = view.Renderer;

        var tile = level.Layers[layer, tileRootX, tileRootY].TileHead
            ?? throw new Exception("Attempt to remove unknown tile");
        int tileLeft = tileRootX - tile.CenterX;
        int tileTop = tileRootY - tile.CenterY;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                if (!level.IsInBounds(gx, gy)) continue;

                int specInt = tile.Requirements[x,y];
                int spec2Int = tile.Requirements2[x,y];
                
                // remove tile bodies
                if (specInt >= 0)
                {
                    level.Layers[layer, gx, gy].TileRootX = -1;
                    level.Layers[layer, gx, gy].TileRootY = -1;
                    level.Layers[layer, gx, gy].TileLayer = -1;
                }

                if (spec2Int >= 0 && layer < 2)
                {
                    level.Layers[layer+1, gx, gy].TileRootX = -1;
                    level.Layers[layer+1, gx, gy].TileRootY = -1;
                    level.Layers[layer+1, gx, gy].TileLayer = -1;
                }

                // remove geometry
                if (removeGeometry)
                {
                    if (specInt >= 0)
                    {
                        level.Layers[layer, gx, gy].Geo = GeoType.Air;
                        view.InvalidateGeo(gx, gy, layer);
                    }

                    if (spec2Int >= 0 && layer < 2)
                    {
                        level.Layers[layer+1, gx, gy].Geo = GeoType.Air;
                        view.InvalidateGeo(gx, gy, layer+1);
                    }
                }
            }
        }

        // remove tile root
        level.Layers[layer, tileRootX, tileRootY].TileHead = null;

        level.RemoveChainData(layer, tileRootX, tileRootY);
        levelRenderer.InvalidateTileHead(tileRootX, tileRootY, layer);
    }

    /// <summary>
    /// Find the tile head at a given location and remove it.
    /// It will remove the cell's tile body is the tile head is
    /// detached or non-existent.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="removeGeometry"></param>
    /// <returns>True if the tile head was not found, false if removed normally</returns>
    public static bool RemoveTileCell(this Level level, int layer, int x, int y, bool removeGeometry)
    {
        ref var cell = ref level.Layers[layer, x, y];

        // if this is a tile body, find referenced tile head
        if (cell.HasTile() && cell.TileHead is null)
        {
            layer = cell.TileLayer;
            x = cell.TileRootX;
            y = cell.TileRootY;
        }

        if (level.Layers[layer, x, y].TileHead is not null)
        {
            level.RemoveTile(layer, x, y, removeGeometry);
            return false;
        }
        else
        {
            Log.Information("Removed detached tile body");

            cell.TileLayer = -1;
            cell.TileRootX = -1;
            cell.TileRootY = -1;
            level.RemoveChainData(layer, x, y);
            
            return true;
        }
    }

    /// <summary>
    /// Set the tile head data for a given cell.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tile"></param>
    public static void SetTileHead(this Level level, int layer, int x, int y, Tile? tile)
    {
        ref var cell = ref level.Layers[layer, x, y];

        if (tile is null)
        {
            cell.TileHead = null;
            if (cell.TileRootX == x && cell.TileRootY == y && cell.TileLayer == layer)
            {
                cell.TileRootX = -1;
                cell.TileRootY = -1;
                cell.TileLayer = -1;
                level.RemoveChainData(layer, x, y);
            }
        }
        else
        {
            cell.TileHead = tile;
            cell.TileRootX = x;
            cell.TileRootY = y;
            cell.TileLayer = layer;
        }
    }

    /// <summary>
    /// Set tile root data for a given cell.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="rootX"></param>
    /// <param name="rootY"></param>
    /// <param name="rootL"></param>
    public static void SetTileRoot(this Level level, int layer, int x, int y, int rootX, int rootY, int rootL)
    {
        ref var cell = ref level.Layers[layer, x, y];
        if (rootX != x || rootY != y || rootL != layer)
        {
            cell.TileHead = null;
            level.RemoveChainData(layer, x, y);
        }
        
        cell.TileRootX = rootX;
        cell.TileRootY = rootY;
        cell.TileLayer = rootL;
    }

    /// <summary>
    /// Clear tile root data for a given cell.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public static void ClearTileRoot(this Level level, int layer, int x, int y)
    {
        ref var cell = ref level.Layers[layer, x, y];
        cell.TileRootX = -1;
        cell.TileRootY = -1;
        cell.TileLayer = -1;
    }
}