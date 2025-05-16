namespace Rained.EditorGui.Editors.CellEditing;
using Raylib_cs;
using Rained.LevelData;
using System.Numerics;

enum SelectionTool
{
    Rect,
    Lasso,
    MagicWand,
    TileSelect,
    MoveSelection,
    MoveSelected,
};

record class LayerSelection
{
    public int minX;
    public int minY;
    public int maxX;
    public int maxY;
    public bool[,] mask;

    public LayerSelection(int minX, int minY, int maxX, int maxY, bool[,]? mask = null)
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
        this.mask = mask ?? new bool[maxY - minY + 1, maxX - minX + 1];
    }
}

abstract class SelectToolState
{
    public abstract void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask);
    public abstract void Close();
}

interface IApplySelection
{
    public bool ApplySelection(Span<LayerSelection?> dstSelections, ReadOnlySpan<bool> layerMask);
}

static class StaticSelectionTools
{
    public static bool TileSelect(int mouseX, int mouseY, int layer, LayerSelection?[] dstSelections)
    {
        Array.Fill(dstSelections, null);
        var level = RainEd.Instance.Level;

        if (level.IsInBounds(mouseX, mouseY) && level.Layers[layer, mouseX, mouseY].HasTile())
        {
            var tileHeadPos = level.GetTileHead(layer, mouseX, mouseY);
            Assets.Tile? tile = level.Layers[tileHeadPos.Layer, tileHeadPos.X, tileHeadPos.Y].TileHead;
            if (tile is null)
            {
                return false;
            }

            var minX = tileHeadPos.X - tile.CenterX;
            var minY = tileHeadPos.Y - tile.CenterY;
            var maxX = minX + tile.Width - 1;
            var maxY = minY + tile.Height - 1;
            // var mask = new bool[tile.Height, tile.Width];
            // mask[tile.CenterY, tile.CenterX] = true;

            dstSelections[layer] = new LayerSelection(
                minX, minY,
                maxX, maxY
            );
            dstSelections[layer]!.mask[tileHeadPos.Y - minY, tileHeadPos.X - minX] = true;

            if (tile.HasSecondLayer)
            {
                dstSelections[layer+1] = new LayerSelection(
                    minX, minY,
                    maxX, maxY
                );
            }

            for (int l = 0; l < Level.LayerCount; l++)
            {
                ref var selLayer = ref dstSelections[l];
                if (selLayer is null) continue;

                for (int x = 0; x < tile.Width; x++)
                {
                    for (int y = 0; y < tile.Height; y++)
                    {
                        ref var c = ref level.Layers[l, x + minX, y + minY];
                        if (c.TileRootX == tileHeadPos.X && c.TileRootY == tileHeadPos.Y && c.TileLayer == tileHeadPos.Layer)
                        {
                            selLayer.mask[y,x] = true;
                        }
                    }
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public static LayerSelection? MagicWand(int mouseX, int mouseY, int layer)
    {
        var level = RainEd.Instance.Level;
        if (!level.IsInBounds(mouseX, mouseY)) 
        {
            return null;
        }

        var levelMask = new bool[level.Height,level.Width];

        bool isSolidGeo(int x, int y, int l)
        {
            return level.Layers[l, x, y].Geo is
                GeoType.Solid or
                GeoType.SlopeRightUp or
                GeoType.SlopeLeftUp or
                GeoType.SlopeRightDown or
                GeoType.SlopeLeftDown or
                GeoType.Platform;
        }
        bool selectGeo = isSolidGeo(mouseX, mouseY, layer);

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        var hasValue = false;
        bool success = Rasterization.FloodFill(
            mouseX, mouseY, level.Width, level.Height,
            isSimilar: (int x, int y) =>
            {
                return isSolidGeo(x, y, layer) == selectGeo && !levelMask[y,x];
                //return false;
            },
            plot: (int x, int y) =>
            {
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                levelMask[y,x] = true;
                hasValue = true;
            }
        );

        if (!success)
        {
            EditorWindow.ShowNotification("Magic wand selection too large!");
            return null;
        }

        if (!hasValue)
        {
            return null;
        }

        var aabbW = maxX - minX + 1;
        var aabbH = maxY - minY + 1;
        var mask = new bool[aabbH,aabbW];

        for (int y = 0; y < aabbH; y++)
        {
            for (int x = 0; x < aabbW; x++)
            {
                var gx = minX + x;
                var gy = minY + y;
                mask[y,x] = levelMask[gy,gx];
            }
        }

        return new LayerSelection(
            minX, minY,
            maxX, maxY,
            mask
        );
    }
}

class RectDragState : SelectToolState, IApplySelection
{
    private int selectionStartX = -1;
    private int selectionStartY = -1;
    private int mouseMinX = 0;
    private int mouseMaxX = 0;
    private int mouseMinY = 0;
    private int mouseMaxY = 0;
    private readonly CellSelection controller;

    public RectDragState(CellSelection controller, int startX, int startY)
    {
        this.controller = controller;
        controller.ChangeRecorder.BeginChange();
        selectionStartX = startX;
        selectionStartY = startY;
    }

    public override void Close()
    {
        controller.ChangeRecorder.PushChange();
    }

    public override void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMasks)
    {
        mouseMinX = Math.Min(selectionStartX, mouseX);
        mouseMaxX = Math.Max(selectionStartX, mouseX);
        mouseMinY = Math.Min(selectionStartY, mouseY);
        mouseMaxY = Math.Max(selectionStartY, mouseY);
        var w = mouseMaxX - mouseMinX + 1;
        var h = mouseMaxY - mouseMinY + 1;

        Raylib.DrawRectangleLines(
            mouseMinX * Level.TileSize,
            mouseMinY * Level.TileSize,
            w * Level.TileSize,
            h * Level.TileSize,
            Color.White
        );
    }
    
    public bool ApplySelection(Span<LayerSelection?> dstSelections, ReadOnlySpan<bool> layerMask)
    {
        var minX = mouseMinX;
        var maxX = mouseMaxX;
        var minY = mouseMinY;
        var maxY = mouseMaxY;

        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        var mask = new bool[h,w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                mask[y,x] = true;
            }
        }

        for (int l = 0; l < Level.LayerCount; l++)
        {
            if (layerMask[l])
            {
                dstSelections[l] = new LayerSelection(
                    minX, minY,
                    maxX, maxY,
                    mask
                );
            }
            else
            {
                dstSelections[l] = null;
            }
        }

        return true;
    }
}

class LassoDragState : SelectToolState, IApplySelection
{
    private readonly List<Vector2i> points = [];
    private readonly CellSelection controller;

    public LassoDragState(CellSelection controller, int startX, int startY)
    {
        this.controller = controller;
        points.Add(new Vector2i(startX, startY));
        controller.ChangeRecorder.BeginChange();
    }

    public override void Close()
    {
        controller.ChangeRecorder.PushChange();
    }
    
    public override void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask)
    {
        var newPoint = new Vector2i(mouseX, mouseY);
        if (newPoint != points[^1])
        {
            //Rasterization.Bresenham(points[^1].X, points[^1].Y, newPoint.X, newPoint.Y, (x, y) =>
            //{
            points.Add(newPoint);
            //});
        }

        // draw points
        var rctx = RainEd.RenderContext;
        rctx.UseGlLines = true;
        rctx.DrawColor = Glib.Color.White;

        for (int i = 1; i < points.Count; i++)
        {
            var ptA = points[i - 1];
            var ptB = points[i];

            rctx.DrawLine(
                (ptA.X + 0.5f) * Level.TileSize,
                (ptA.Y + 0.5f) * Level.TileSize,
                (ptB.X + 0.5f) * Level.TileSize,
                (ptB.Y + 0.5f) * Level.TileSize
            );
        }

        {
            var ptA = points[^1];
            var ptB = points[0];

            rctx.DrawLine(
                (ptA.X + 0.5f) * Level.TileSize,
                (ptA.Y + 0.5f) * Level.TileSize,
                (ptB.X + 0.5f) * Level.TileSize,
                (ptB.Y + 0.5f) * Level.TileSize
            );
        }

        /*var lastX = 0;
        var lastY = 0;
        bool first = true;
        Rasterization.Bresenham(points[^1].X, points[^1].Y, points[0].X, points[0].Y, (x, y) =>
        {
            if (!first)
            {
                rctx.DrawLine(
                    (lastX + 0.5f) * Level.TileSize,
                    (lastY + 0.5f) * Level.TileSize,
                    (x + 0.5f) * Level.TileSize,
                    (y + 0.5f) * Level.TileSize
                );
            }

            first = false;
            lastX = x;
            lastY = y;
        });*/
    }

    public bool ApplySelection(Span<LayerSelection?> dstSelections, ReadOnlySpan<bool> layerMask)
    {
        // calc bounding box of points
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        if (points.Count == 0)
        {
            for (int l = 0; l < Level.LayerCount; l++)
                dstSelections[l] = null;
            
            return false;
        }

        foreach (var pt in points)
        {
            minX = Math.Min(pt.X, minX);
            minY = Math.Min(pt.Y, minY);
            maxX = Math.Max(pt.X, maxX);
            maxY = Math.Max(pt.Y, maxY);
        }

        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        var mask = new bool[h,w];

        // polygon rasterization:
        // a point is inside a polygon if, given an infinitely long horizontal
        // line that intersects with the point, the line intersects with the polygon's
        // edges an even number of times.
        // so, to rasterize a polygon, for each scanline we can cast a horizontal ray from
        // the left side of the polygon's AABB to the right side, and then move along each
        // point along that ray. if so far, that ray intersected with an even number of edges,
        // draw a pixel.
        
        // gather lines
        var lines = new List<(Vector2 a, Vector2 b)>();
        for (int i = 1; i < points.Count; i++)
        {
            var ptA = (Vector2) points[i-1];
            var ptB = (Vector2) points[i];
            lines.Add((ptA, ptB));
        }

        if (points[^1] != points[0])
            lines.Add(((Vector2) points[^1], (Vector2) points[0]));
        
        // this function casts a ray towards the right from (rx, ry)
        // and fills the distances array to the distance from the ray
        // to each intersectin segment. it is also sorted.
        List<float> distances = [];
        void CalcIntersections(float rx, float ry)
        {
            distances.Clear();
            var gy = ry;
            var gx = rx;
            foreach (var (ptA, ptB) in lines)
            {
                var lineMinY = Math.Min(ptA.Y, ptB.Y);
                var lineMaxY = Math.Max(ptA.Y, ptB.Y);
                if (!(gy >= lineMinY && gy <= lineMaxY)) continue;
                
                float dist;
                if (ptA.X == ptB.X) // line is completely vertical
                {
                    dist = ptA.X - gx;
                }
                else
                {
                    var slope = (ptB.Y - ptA.Y) / (ptB.X - ptA.X);
                    if (slope == 0) continue; // line is parallel with ray
                    dist = (gy - ptA.Y) / slope + ptA.X - gx;
                }

                distances.Add(dist);
            }
            distances.Sort();
        }

        // use this distances array to fill a scanline
        // we don't actually need to keep track of even/odd intersections,
        // we just iterate through each pair of intersections in the list.
        // it does the same thing.
        void Scanline(int y)
        {
            for (int i = 0; i < distances.Count; i += 2)
            {
                var xEnd = i+1 < distances.Count ? (int)distances[i+1] : w;
                for (int x = (int)distances[i]; x < xEnd; x++)
                {
                    mask[y,x] = true;
                }
            }
        }

        for (int y = 0; y < h; y++)
        {
            // check for all four corners of each pixel in the polygon, not just one.
            // otherwise, the selection mask will be "offset".
            CalcIntersections(minX, y + minY + 0.05f); // top-left
            Scanline(y);
            CalcIntersections(minX - 1.0f, y + minY + 0.05f); // top-right
            Scanline(y);
            CalcIntersections(minX, y + minY - 0.05f); // bottom-left
            Scanline(y);
            CalcIntersections(minX - 1.0f, y + minY - 0.05f); // bottom-right
            Scanline(y);
        }

        for (int l = 0; l < Level.LayerCount; l++)
        {
            if (layerMask[l])
            {
                dstSelections[l] = new LayerSelection(
                    minX, minY,
                    maxX, maxY,
                    mask
                );
            }
            else
            {
                dstSelections[l] = null;
            }
        }
        
        return true;
    }
}

class SelectionMoveDragState : SelectToolState
{
    struct LayerInfo
    {
        public bool active;
        public int offsetX, offsetY;
        public int selW, selH;
    }

    private readonly LayerInfo[] layerInfo;
    private readonly CellSelection controller;

    public SelectionMoveDragState(CellSelection controller, int startX, int startY, ReadOnlySpan<bool> layerMask)
    {
        this.controller = controller;
        controller.ChangeRecorder.BeginChange();

        layerInfo = new LayerInfo[Level.LayerCount];

        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var sel = ref controller.Selections[l];
            var li = new LayerInfo
            {
                active = layerMask[l] && sel is not null
            };

            if (sel is not null)
            {
                li.offsetX = startX - sel.minX;
                li.offsetY = startY - sel.minY;
                li.selW = sel.maxX - sel.minX + 1;
                li.selH = sel.maxY - sel.minY + 1;
            }

            layerInfo[l] = li;
        }
    }

    public override void Close()
    {
        controller.ChangeRecorder.PushChange();
    }

    public override void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask)
    {
        for (int l = 0; l < Level.LayerCount; l++)
        {
            if (!layerInfo[l].active) continue;
            ref var sel = ref controller.Selections[l];
            if (sel is null) continue;

            sel.minX = mouseX - layerInfo[l].offsetX;
            sel.minY = mouseY - layerInfo[l].offsetY;
            sel.maxX = sel.minX + layerInfo[l].selW - 1;
            sel.maxY = sel.minY + layerInfo[l].selH - 1;
        }
    }
}

class SelectedMoveDragState : SelectToolState
{
    struct LayerInfo
    {
        public bool active;
        public int offsetX, offsetY; // local min's offset from global min
        public int selW, selH; // local size
    }

    private readonly LayerInfo[] layerInfo;
    private readonly int offsetX, offsetY; // offset from global min
    private readonly CellSelection controller;

    public SelectedMoveDragState(CellSelection controller, int startX, int startY, ReadOnlySpan<bool> layerMask)
    {
        this.controller = controller;

        layerInfo = new LayerInfo[Level.LayerCount];

        int gMinX, gMinY;

        if (controller.IsSelectionActive())
        {
            gMinX = gMinY = int.MaxValue;
            for (int l = 0; l < Level.LayerCount; l++)
            {
                ref var sel = ref controller.Selections[l];
                if (sel is null) continue;

                gMinX = int.Min(gMinX, sel.minX);
                gMinY = int.Min(gMinY, sel.minY);
            }
        }
        else
        {
            gMinX = gMinY = 0;
        }

        offsetX = startX - gMinX;
        offsetY = startY - gMinY;
        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var sel = ref controller.Selections[l];
            var li = new LayerInfo
            {
                active = layerMask[l] && sel is not null
            };

            if (sel is not null)
            {
                li.offsetX = sel.minX - gMinX;
                li.offsetY = sel.minY - gMinY;
                li.selW = sel.maxX - sel.minX + 1;
                li.selH = sel.maxY - sel.minY + 1;
            }

            layerInfo[l] = li;
        }

        // create geometry overlay array
        // and clear out selection
        if (!controller.IsGeometryMoveActive)
        {
            controller.BeginMove();
        }
    }

    public override void Close()
    {}

    public override void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask)
    {
        var rndr = RainEd.Instance.LevelView.Renderer;
        rndr.OverlayX = mouseX - offsetX;
        rndr.OverlayY = mouseY - offsetY;

        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var sel = ref controller.Selections[l];
            if (sel is null) continue;

            sel.minX = rndr.OverlayX + layerInfo[l].offsetX;
            sel.minY = rndr.OverlayY + layerInfo[l].offsetY;
            sel.maxX = sel.minX + layerInfo[l].selW - 1;
            sel.maxY = sel.minY + layerInfo[l].selH - 1;
        }
    }
}