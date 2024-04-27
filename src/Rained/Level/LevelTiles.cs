using RainEd.Tiles;
namespace RainEd;

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

partial class Level
{
    public TilePlacementStatus ValidateTilePlacement(Tile tile, int tileLeft, int tileTop, int layer, bool force)
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
                if (IsInBounds(gx, gy))
                {
                    // placing it on a tile head can introduce a bugged state,
                    // soo... even when forced... no
                    ref var cellAtPos = ref Layers[layer, gx, gy];

                    if (specInt >= 0 && cellAtPos.TileHead is not null)
                        return TilePlacementStatus.Overlap;
                    
                    // check on first layer
                    var isHead = x == tile.CenterX && y == tile.CenterY;

                    if ((isHead || specInt >= 0) && !force && cellAtPos.HasTile())
                        return TilePlacementStatus.Overlap;

                    // check on second layer
                    if (layer < 2)
                    {
                        if (spec2Int >= 0 && !force && Layers[layer + 1, gx, gy].HasTile())
                            return TilePlacementStatus.Overlap;
                    }
                }

                if (!force)
                {
                    // check first layer geometry
                    if (specInt == -1) continue;
                    if (GetClamped(layer, gx, gy).Geo != (GeoType) specInt)
                        return TilePlacementStatus.Geometry;

                    // check second layer geometry
                    // if we are on layer 3, there is no second layer
                    // all checks pass
                    if (layer == 2) continue;
                    
                    if (spec2Int == -1) continue;
                    if (GetClamped(layer + 1, gx, gy).Geo != (GeoType) spec2Int)
                        return TilePlacementStatus.Geometry;
                }
            }
        }
        
        return TilePlacementStatus.Success;
    }

    // check that a potential placement isn't intersecting a specific already placed tile
    public bool IsIntersectingTile(Tile tile, int tileLeft, int tileTop, int layer, int testX, int testY, int testL)
    {
        var testTilePos = GetTileHead(testL, testX, testY);
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
                if (IsInBounds(gx, gy))
                {
                    ref var cellAtPos = ref Layers[layer, gx, gy];

                    // check on first layer
                    var isHead = x == tile.CenterX && y == tile.CenterY;

                    if ((isHead || specInt >= 0) && GetTileHead(layer, gx, gy) == testTilePos)
                        return true;

                    // check on second layer
                    if (layer < 2)
                    {
                        if (spec2Int >= 0 && GetTileHead(layer+1, gx, gy) == testTilePos)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public TilePlacementStatus SafePlaceTile(
        Tile tile,
        int layer, int tileRootX, int tileRootY,
        TilePlacementMode placeMode
    )
    {
        // check if requirements are satisfied
        TilePlacementStatus validationStatus;

        if (IsInBounds(tileRootX, tileRootY))
            validationStatus = ValidateTilePlacement(
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
            PlaceTile(
                tile,
                layer, tileRootX, tileRootY,
                placeMode == TilePlacementMode.Geometry
            );
        }

        return validationStatus;
    }

    public void PlaceTile(
        Tile tile,
        int layer, int tileRootX, int tileRootY,
        bool placeGeometry
    )
    {
        int tileLeft = tileRootX - tile.CenterX;
        int tileTop = tileRootY - tile.CenterY;

        var levelRenderer = RainEd.Instance.Window.LevelRenderer;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                if (!IsInBounds(gx, gy)) continue;

                int specInt = tile.Requirements[x,y];
                int spec2Int = tile.Requirements2[x,y];

                if (placeGeometry)
                {
                    // place first layer    
                    if (specInt >= 0)
                    {
                        Layers[layer, gx, gy].Geo = (GeoType) specInt;
                        levelRenderer.MarkNeedsRedraw(gx, gy, layer);
                    }

                    // place second layer
                    if (layer < 2 && spec2Int >= 0)
                    {
                        Layers[layer+1, gx, gy].Geo = (GeoType) spec2Int;
                        levelRenderer.MarkNeedsRedraw(gx, gy, layer+1);
                    }
                }

                // tile first 
                if (specInt >= 0)
                {
                    Layers[layer, gx, gy].TileRootX = tileRootX;
                    Layers[layer, gx, gy].TileRootY = tileRootY;
                    Layers[layer, gx, gy].TileLayer = layer;
                }

                // tile second layer
                if (spec2Int >= 0 && layer < 2)
                {
                    Layers[layer+1, gx, gy].TileRootX = tileRootX;
                    Layers[layer+1, gx, gy].TileRootY = tileRootY;
                    Layers[layer+1, gx, gy].TileLayer = layer;
                }
            }
        }

        // place tile root
        Layers[layer, tileRootX, tileRootY].TileHead = tile;
    }

    /// <summary>
    /// Remove a tile head at a given position
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="tileRootX"></param>
    /// <param name="tileRootY"></param>
    /// <param name="removeGeometry"></param>
    /// <exception cref="Exception">Thrown if the tile at the given position is not a tile head</exception>
    public void RemoveTile(int layer, int tileRootX, int tileRootY, bool removeGeometry)
    {
        var levelRenderer = RainEd.Instance.Window.LevelRenderer;

        var tile = Layers[layer, tileRootX, tileRootY].TileHead
            ?? throw new Exception("Attempt to remove unknown tile");
        int tileLeft = tileRootX - tile.CenterX;
        int tileTop = tileRootY - tile.CenterY;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                if (!IsInBounds(gx, gy)) continue;

                int specInt = tile.Requirements[x,y];
                int spec2Int = tile.Requirements2[x,y];
                
                // remove tile bodies
                if (specInt >= 0)
                {
                    Layers[layer, gx, gy].TileRootX = -1;
                    Layers[layer, gx, gy].TileRootY = -1;
                    Layers[layer, gx, gy].TileLayer = -1;
                }

                if (spec2Int >= 0 && layer < 2)
                {
                    Layers[layer+1, gx, gy].TileRootX = -1;
                    Layers[layer+1, gx, gy].TileRootY = -1;
                    Layers[layer+1, gx, gy].TileLayer = -1;
                }

                // remove geometry
                if (removeGeometry)
                {
                    if (specInt >= 0)
                    {
                        Layers[layer, gx, gy].Geo = GeoType.Air;
                        levelRenderer.MarkNeedsRedraw(gx, gy, layer);
                    }

                    if (spec2Int >= 0 && layer < 2)
                    {
                        Layers[layer+1, gx, gy].Geo = GeoType.Air;
                        levelRenderer.MarkNeedsRedraw(gx, gy, layer+1);
                    }
                }
            }
        }

        // remove tile root
        Layers[layer, tileRootX, tileRootY].TileHead = null;
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
    public bool RemoveTileCell(int layer, int x, int y, bool removeGeometry)
    {
        ref var cell = ref Layers[layer, x, y];

        // if this is a tile body, find referenced tile head
        if (cell.HasTile() && cell.TileHead is null)
        {
            layer = cell.TileLayer;
            x = cell.TileRootX;
            y = cell.TileRootY;
        }

        if (Layers[layer, x, y].TileHead is not null)
        {
            RemoveTile(layer, x, y, removeGeometry);
            return false;
        }
        else
        {
            RainEd.Logger.Information("Removed detached tile body");

            cell.TileLayer = -1;
            cell.TileRootX = -1;
            cell.TileRootY = -1;
            return true;
        }
    }
}