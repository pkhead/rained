namespace Rained.EditorGui.Editors;
using Raylib_cs;
using ImGuiNET;
using System.Numerics;
using Rained.LevelData;
using System.Diagnostics;

/// <summary>
/// Operations for copying, pasting, and moving of cells.
/// </summary>
class CellSelection
{
    public static CellSelection? Instance { get; set; } = null;

    public bool Active { get; private set; } = true;
    public bool PasteMode { get; set; } = false;
    public bool AffectTiles { get; set; } = true;

    private static RlManaged.Texture2D icons = null!;
    enum IconName
    {
        SelectRect,
        MoveSelected,
        MoveSelection,
        LassoSelect,
        MagicWand,
        TileSelect,
        OpReplace,
        OpAdd,
        OpSubtract,
        OpIntersect
    };

    enum SelectionTool
    {
        Rect,
        Lasso,
        MagicWand,
        TileSelect,
        MoveSelection,
        MoveSelected,
    };
    private SelectionTool curTool = SelectionTool.Rect;
    static readonly (IconName icon, string name)[] toolInfo = [
        (IconName.SelectRect, "Rectangle Select"),
        (IconName.LassoSelect, "Lasso Select"),
        (IconName.MagicWand, "Magic Wand"),
        (IconName.TileSelect, "Tile Select"),
        (IconName.MoveSelection, "Move Selection"),
        (IconName.MoveSelected, "Move Selected"),
    ];

    enum SelectionOperator
    {
        Replace,
        Add,
        Subtract,
        Intersect
    }

    // this is set by ui
    private SelectionOperator curOp = SelectionOperator.Replace;

    // this is set by keyboard controls
    private SelectionOperator? curOpOverride = null;

    static readonly (IconName icon, string name)[] operatorInfo = [
        (IconName.OpReplace, "Replace"),
        (IconName.OpAdd, "Add"),
        (IconName.OpSubtract, "Subtract"),
        (IconName.OpIntersect, "Intersect"),
    ];

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

    private readonly LayerSelection?[] selections = new LayerSelection?[Level.LayerCount];
    private readonly LayerSelection?[] tmpSelections = new LayerSelection?[Level.LayerCount];

    private int movingW = 0;
    private int movingH = 0;
    private (bool mask, LevelCell cell)[,,]? movingGeometry = null;

    private int cancelOrigX = 0;
    private int cancelOrigY = 0;
    private (bool mask, LevelCell cell)[,,]? cancelGeoData = null;

    // used for mouse drag
    private bool mouseWasDragging = false;
    private Tool? mouseDragState = null;
    abstract class Tool
    {
        public abstract void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask);
        //public virtual void Submit() {}
    }

    interface ISelectionTool
    {
        //public bool ApplySelection(out int minX, out int minY, out int maxX, out int maxY, out bool[,] mask);
        public bool ApplySelection(Span<LayerSelection?> dstSelections, ReadOnlySpan<bool> layerMask);
    }

    public CellSelection()
    {
        icons ??= RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "selection-icons.png"));
    }

    private static Rectangle GetIconRect(IconName icon)
    {
        return new Rectangle((int)icon * 16f, 0, 16f, 16f);
    }

    private static bool IconButton(IconName icon)
    {
        var framePadding = ImGui.GetStyle().FramePadding;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, framePadding);
        var buttonSize = 16 * Boot.PixelIconScale;
        var desiredHeight = ImGui.GetFrameHeight();
        
        // sz + pad*2 = w
        // pad = (w - sz) / 2
        ImGui.GetStyle().FramePadding = new Vector2(
            MathF.Floor( (desiredHeight - buttonSize) / 2f ),
            MathF.Floor( (desiredHeight - buttonSize) / 2f )
        );

        var textColorVec4 = ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * 255f;
        bool pressed = ImGuiExt.ImageButtonRect(
            "##test",
            icons,
            buttonSize, buttonSize,
            GetIconRect(icon),
            new Color((int)textColorVec4.X, (int)textColorVec4.Y, (int)textColorVec4.Z, (int)textColorVec4.W)
        );

        ImGui.PopStyleVar();
        return pressed;
    }

    public void DrawStatusBar()
    {
        if (PasteMode)
        {
            if (ImGui.Button("OK") || EditorWindow.IsKeyPressed(ImGuiKey.Enter))
            {
                SubmitMove();
                Active = false;
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Cancel") || EditorWindow.IsKeyPressed(ImGuiKey.Escape))
            {
                CancelMove();
                Active = false;
            }
            
            return;
        }

        // selection mode options
        // for tile editor mode, Tile Select button appears.
        using (var group = ImGuiExt.ButtonGroup.Begin("Selection Mode", AffectTiles ? 5 : 4, 0))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            for (int i = 0; i < toolInfo.Length; i++)
            {
                if (toolInfo[i].icon == IconName.TileSelect && !AffectTiles)
                    continue;
                
                group.BeginButton(i, (int)curTool == i);

                ref var info = ref toolInfo[i];
                if (IconButton(info.icon))
                {
                    SubmitMove();
                    curTool = (SelectionTool)i;
                }
                ImGui.SetItemTooltip(info.name);

                group.EndButton();
            }
            ImGui.PopStyleVar();
        }

        // operator mode options
        ImGui.SameLine();
        using (var group = ImGuiExt.ButtonGroup.Begin("Operator Mode", 4, 0))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            for (int i = 0; i < operatorInfo.Length; i++)
            {
                group.BeginButton(i, (int)(curOpOverride ?? curOp) == i);

                ref var info = ref operatorInfo[i];
                if (IconButton(info.icon))
                {
                    curOp = (SelectionOperator)i;
                }
                ImGui.SetItemTooltip(info.name);

                group.EndButton();
            }
            ImGui.PopStyleVar();
        }

        ImGui.SameLine();
        if (ImGui.Button("Done") || EditorWindow.IsKeyPressed(ImGuiKey.Enter))
        {
            SubmitMove();
            Active = false;
        }
        
        ImGui.SameLine();
        ImGui.BeginDisabled(movingGeometry is null);
        if (ImGui.Button("Cancel"))
        {
            CancelMove();
            ClearSelection();
        }
        ImGui.EndDisabled();

        if (EditorWindow.IsKeyPressed(ImGuiKey.Escape))
        {
            bool doExit = movingGeometry is null;
            CancelMove();
            ClearSelection();
            if (doExit) Active = false;
        }
    }

    public void Update(ReadOnlySpan<bool> layerMask, int activeLayer)
    {
        // TODO: crosshair cursor
        Debug.Assert(layerMask.Length == Level.LayerCount);

        if (PasteMode)
        {
            curTool = SelectionTool.MoveSelected;
        }
        
        var view = RainEd.Instance.LevelView;
        view.Renderer.OverlayAffectTiles = AffectTiles;

        curOpOverride = null;
        if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
        {
            curOpOverride = SelectionOperator.Add;
        }

        var activeOp = curOpOverride ?? curOp;
        if (curTool == SelectionTool.MagicWand)
        {
            if (view.IsViewportHovered && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
            {
                var layerSelection = MagicWand(view.MouseCx, view.MouseCy, activeLayer);
                if (layerSelection is not null)
                {
                    Array.Fill(tmpSelections, null);
                    tmpSelections[activeLayer] = layerSelection;
                    CombineMasks(tmpSelections);
                }
                else if (activeOp is SelectionOperator.Replace or SelectionOperator.Intersect) selections[activeLayer] = null;
            }
        }
        else if (curTool == SelectionTool.TileSelect)
        {
            if (view.IsViewportHovered && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
            {
                Array.Fill(tmpSelections, null);
                if (TileSelect(
                    view.MouseCx, view.MouseCy, activeLayer, tmpSelections
                ))
                {
                    CombineMasks(tmpSelections);
                }
                else if (activeOp is SelectionOperator.Replace or SelectionOperator.Intersect) ClearSelection();
            }
        }
        else
        {
            if (view.IsViewportHovered && EditorWindow.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (!mouseWasDragging)
                {
                    mouseDragState = curTool switch
                    {
                        SelectionTool.Rect => new RectDragState(view.MouseCx, view.MouseCy),
                        SelectionTool.Lasso => new LassoDragState(view.MouseCx, view.MouseCy),
                        SelectionTool.MoveSelection => new SelectionMoveDragState(this, view.MouseCx, view.MouseCy, layerMask),
                        SelectionTool.MoveSelected => new SelectedMoveDragState(this, view.MouseCx, view.MouseCy, layerMask),
                        _ => throw new UnreachableException("Invalid curTool")
                    };

                    if (activeOp is SelectionOperator.Replace or SelectionOperator.Intersect && mouseDragState is ISelectionTool)
                        ClearSelection(layerMask);
                }

                mouseDragState!.Update(view.MouseCx, view.MouseCy, layerMask);
            }
            else if (mouseWasDragging && mouseDragState is not null)
            {
                if (mouseDragState is ISelectionTool selTool)
                {
                    Array.Fill(tmpSelections, null);
                    if (selTool.ApplySelection(tmpSelections, layerMask))
                    {
                        CombineMasks(tmpSelections);
                    }
                    else if (activeOp is SelectionOperator.Replace or SelectionOperator.Intersect) ClearSelection(layerMask);
                }
            }
        }

        mouseWasDragging = EditorWindow.IsMouseDragging(ImGuiMouseButton.Left);

        // update layer colors
        Span<Color> layerColors = stackalloc Color[3];
        {
            var layerCol1 = RainEd.Instance.Preferences.LayerColor1;
            var layerCol2 = RainEd.Instance.Preferences.LayerColor2;
            var layerCol3 = RainEd.Instance.Preferences.LayerColor3;
            layerColors[0] = new Color(255 - layerCol1.R, 255 - layerCol1.G, 255 - layerCol1.B, (byte)255);
            layerColors[1] = new Color(layerCol2.R, layerCol2.G, layerCol2.B, (byte)255);
            layerColors[2] = new Color(layerCol3.R, layerCol3.G, layerCol3.B, (byte)255);
        }

        // draw
        Raylib.BeginShaderMode(Shaders.OutlineMarqueeShader);

        Shaders.OutlineMarqueeShader.GlibShader.SetUniform("time", (float)Raylib.GetTime());
        RainEd.Instance.NeedScreenRefresh();

        // draw selection outline
        bool isSelectionActive = false;
        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var selection = ref selections[l];
            if (selection is not null)
            {
                isSelectionActive = true;

                var w = selection.maxX - selection.minX + 1;
                var h = selection.maxY - selection.minY + 1;
                Debug.Assert(w > 0 && h > 0);

                var col = layerColors[l];
                int offsetX = l - activeLayer;
                int offsetY = l - activeLayer;

                for (int y = 0; y < h; y++)
                {
                    var gy = selection.minY + y;
                    for (int x = 0; x < w; x++)
                    {
                        if (!selection.mask[y,x]) continue;
                        var gx = selection.minX + x;

                        bool left = x == 0 || !selection.mask[y,x-1];
                        bool right = x == w-1 || !selection.mask[y,x+1];
                        bool top = y == 0 || !selection.mask[y-1,x];
                        bool bottom = y == h-1 || !selection.mask[y+1,x];

                        if (left) Raylib.DrawLine(
                            offsetX + gx * Level.TileSize,
                            offsetY + gy * Level.TileSize,
                            offsetX + gx * Level.TileSize,
                            offsetY + (gy+1) * Level.TileSize,
                            col
                        );

                        if (right) Raylib.DrawLine(
                            offsetX + (gx+1) * Level.TileSize,
                            offsetY + gy * Level.TileSize,
                            offsetX + (gx+1) * Level.TileSize,
                            offsetY + (gy+1) * Level.TileSize,
                            col
                        );

                        if (top) Raylib.DrawLine(
                            offsetX + gx * Level.TileSize,
                            offsetY + gy * Level.TileSize,
                            offsetX + (gx+1) * Level.TileSize,
                            offsetY + gy * Level.TileSize,
                            col
                        );

                        if (bottom) Raylib.DrawLine(
                            offsetX + gx * Level.TileSize,
                            offsetY + (gy+1) * Level.TileSize,
                            offsetX + (gx+1) * Level.TileSize,
                            offsetY + (gy+1) * Level.TileSize,
                            col
                        );
                    }
                }
            }
        }

        Raylib.EndShaderMode();

        // copy
        // (paste is handled by GeometryEditor, since paste can be done without first entering selection mode)
        if (KeyShortcuts.Activated(KeyShortcut.Copy) && isSelectionActive)
        {
            CopySelectedGeometry();
        }
    }

    private void CopySelectedGeometry()
    {
        if (!IsSelectionActive()) return;

        int selX, selY, selW, selH;
        (bool mask, LevelCell cell)[,,] geometryData;

        if (movingGeometry is not null)
        {
            var renderer = RainEd.Instance.LevelView.Renderer;
            geometryData = movingGeometry;
            (selX, selY) = (renderer.OverlayX, renderer.OverlayY);
            (selW, selH) = (movingW, movingH);
        }
        else
        {
            geometryData = MakeCellGroup(out selX, out selY, out selW, out selH, false);
        }

        var serializedData = CellSerialization.SerializeCells(selX, selY, selW, selH, geometryData);
        if (!Platform.SetClipboard(Boot.Window, Platform.ClipboardDataType.LevelCells, serializedData))
        {
            EditorWindow.ShowNotification("Could not copy!");
        }
    }

    public bool PasteGeometry(byte[] serializedData)
    {
        SubmitMove();

        var data = CellSerialization.DeserializeCells(serializedData, out int origX, out int origY, out int width, out int height);
        if (data is null) return false;

        // set selection data
        for (int l = 0; l < Level.LayerCount; l++)
        {
            var selLayer = new LayerSelection(
                minX: origX,
                minY: origY,
                maxX: origX + width - 1,
                maxY: origY + height - 1
            );

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    selLayer.mask[y,x] = data[l,x,y].mask;
                }
            }

            selections[l] = selLayer;
        }
        CropSelection();

        // set move data
        movingGeometry = data;
        movingW = width;
        movingH = height;

        // send overlay to renderer
        var rndr = RainEd.Instance.LevelView.Renderer;
        rndr.OverlayX = origX;
        rndr.OverlayY = origY;
        rndr.SetOverlay(
            width: width,
            height: height,
            geometry: movingGeometry
        );

        //File.WriteAllBytes("test.rwc", serializedData);
        return true;
    }

    public static void BeginPaste(ref CellSelection? inst)
    {
        if (Platform.GetClipboard(Boot.Window, Platform.ClipboardDataType.LevelCells, out var serializedCells))
        {
            inst ??= new CellSelection()
            {
                PasteMode = true
            };
            inst.curTool = SelectionTool.MoveSelected;
            inst.PasteGeometry(serializedCells);
        }
    }

    private void CombineMasks(ReadOnlySpan<LayerSelection?> dstSelections)
    {
        for (int l = 0; l < Level.LayerCount; l++)
        {
            var srcSel = selections[l];
            var dstSel = dstSelections[l];
            if (dstSel is null) continue;

            var op = curOpOverride ?? curOp;

            switch (op)
            {
                case SelectionOperator.Replace:
                case SelectionOperator.Add:
                    if (op == SelectionOperator.Replace || srcSel is null)
                    {
                        selections[l] = new LayerSelection(
                            dstSel.minX, dstSel.minY,
                            dstSel.maxX, dstSel.maxY,
                            dstSel.mask
                        );
                    }
                    else if (op == SelectionOperator.Add)
                    {
                        var newSel = new LayerSelection(
                            minX: Math.Min(srcSel.minX, dstSel.minX),
                            minY: Math.Min(srcSel.minY, dstSel.minY),
                            maxX: Math.Max(srcSel.maxX, dstSel.maxX),
                            maxY: Math.Max(srcSel.maxY, dstSel.maxY)
                        );

                        // source
                        int ox = srcSel.minX - newSel.minX;
                        int oy = srcSel.minY - newSel.minY;
                        int w = srcSel.maxX - srcSel.minX + 1;
                        int h = srcSel.maxY - srcSel.minY + 1;
                        for (int y = 0; y < h; y++)
                        {
                            var y2 = y + oy;
                            for (int x = 0; x < w; x++)
                            {
                                var x2 = x + ox;
                                newSel.mask[y2,x2] = srcSel.mask[y,x];
                            }
                        }

                        // dest
                        ox = dstSel.minX - newSel.minX;
                        oy = dstSel.minY - newSel.minY;
                        w = dstSel.maxX - dstSel.minX + 1;
                        h = dstSel.maxY - dstSel.minY + 1;
                        for (int y = 0; y < h; y++)
                        {
                            var y2 = y + oy;
                            for (int x = 0; x < w; x++)
                            {
                                var x2 = x + ox;
                                newSel.mask[y2, x2] |= dstSel.mask[y,x];
                            }
                        }

                        selections[l] = newSel;
                    }

                    break;

                case SelectionOperator.Subtract:
                {
                    if (srcSel is null) break;

                    var newSel = new LayerSelection(
                        minX: srcSel.minX,
                        minY: srcSel.minY,
                        maxX: srcSel.maxX,
                        maxY: srcSel.maxY
                    );

                    // dest
                    var oldW = srcSel.maxX - srcSel.minX + 1;
                    var oldH = srcSel.maxY - srcSel.minY + 1;
                    var ox = newSel.minX - dstSel.minX;
                    var oy = newSel.minY - dstSel.minY;
                    var w = dstSel.maxX - dstSel.minX + 1;
                    var h = dstSel.maxY - dstSel.minY + 1;

                    // in source bounds
                    for (int y = 0; y < oldH; y++)
                    {
                        for (int x = 0; x < oldW; x++)
                        {
                            var lx = x + ox;
                            var ly = y + oy;
                            if (lx >= 0 && ly >= 0 && lx < w && ly < h)
                            {
                                // A  B  OUT
                                // 0  0  0
                                // 0  1  0
                                // 1  0  1
                                // 1  1  0
                                newSel.mask[y,x] = srcSel.mask[y,x] & (srcSel.mask[y,x] ^ dstSel.mask[ly,lx]);
                            }
                        }
                    }

                    selections[l] = newSel;
                    break;
                }

                case SelectionOperator.Intersect:
                {
                    if (srcSel is null) break;

                    var newSel = new LayerSelection(
                        minX: int.Max(srcSel.minX, dstSel.minX),
                        minY: int.Max(srcSel.minY, dstSel.minY),
                        maxX: int.Max(srcSel.maxX, dstSel.maxX),
                        maxY: int.Max(srcSel.maxY, dstSel.maxY)
                    );

                    if (newSel.maxX < newSel.minX || newSel.maxY < newSel.minY)
                    {
                        selections[l] = null;
                        break;
                    }
                    
                    // source
                    var ox0 = newSel.minX - srcSel.minX;
                    var oy0 = newSel.minY - srcSel.minY;
                    var w0 = srcSel.maxX - srcSel.minX + 1;
                    var h0 = srcSel.maxY - srcSel.minY + 1;

                    // dest
                    var ox1 = newSel.minX - dstSel.minX;
                    var oy1 = newSel.minY - dstSel.minY;
                    var w1 = dstSel.maxX - dstSel.minX + 1;
                    var h1 = dstSel.maxY - dstSel.minY + 1;

                    // in dest bounds
                    var newW = newSel.maxX - newSel.minX + 1;
                    var newH = newSel.maxY - newSel.minY + 1;
                    for (int y = 0; y < newH; y++)
                    {
                        for (int x = 0; x < newW; x++)
                        {
                            var x0 = x + ox0;
                            var y0 = y + oy0;
                            var x1 = x + ox1;
                            var y1 = y + oy1;

                            if (!(x0 >= 0 && y0 >= 0 && x1 < w0 && y1 < h0)) continue;
                            if (!(x1 >= 0 && y1 >= 0 && x1 < w1 && y1 < h1)) continue;
                            newSel.mask[y,x] = srcSel.mask[y0,x0] & dstSel.mask[y1,x1];
                        }
                    }

                    selections[l] = newSel;
                    break;
                }
            }
        }

        CropSelection();
    }

    private static bool TileSelect(int mouseX, int mouseY, int layer, LayerSelection?[] dstSelections)
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

    private static LayerSelection? MagicWand(int mouseX, int mouseY, int layer)
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

    public void CropSelection()
    {
        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var selection = ref selections[l];
            if (selection is null) continue;

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            bool hasValue = false;

            for (int gy = selection.minY; gy <= selection.maxY; gy++)
            {
                for (int gx = selection.minX; gx <= selection.maxX; gx++)
                {
                    var x = gx - selection.minX;
                    var y = gy - selection.minY;
                    if (selection.mask[y,x])
                    {
                        hasValue = true;
                        minX = Math.Min(minX, gx);
                        minY = Math.Min(minY, gy);
                        maxX = Math.Max(maxX, gx);
                        maxY = Math.Max(maxY, gy);
                    }
                }
            }

            if (!hasValue)
            {
                selection = null;
                return;
            }

            var newW = maxX - minX + 1;
            var newH = maxY - minY + 1;
            var newMask = new bool[newH, newW];

            for (int y = 0; y < newH; y++)
            {
                for (int x = 0; x < newW; x++)
                {
                    var lx = x + minX - selection.minX;
                    var ly = y + minY - selection.minY;
                    newMask[y,x] = selection.mask[ly,lx];
                }
            }

            selection.minX = minX;
            selection.minY = minY;
            selection.maxX = maxX;
            selection.maxY = maxY;
            selection.mask = newMask;
        }
    }

    public void SubmitMove()
    {
        cancelGeoData = null;
        if (movingGeometry is null)
            return;

        var level = RainEd.Instance.Level;
        var rndr = RainEd.Instance.LevelView.Renderer;

        RainEd.Instance.LevelView.CellChangeRecorder.BeginChange();

        // apply moved geometry
        for (int y = 0; y < movingH; y++)
        {
            var gy = rndr.OverlayY + y;
            for (int x = 0; x < movingW; x++)
            {
                var gx = rndr.OverlayX + x;
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    if (!level.IsInBounds(gx, gy)) continue;

                    ref var srcCell = ref movingGeometry[l,x,y];
                    if (!srcCell.mask) continue;

                    ref var dstCell = ref level.Layers[l,gx,gy];
                    dstCell.Geo = srcCell.cell.Geo;
                    dstCell.Objects = srcCell.cell.Objects;
                    dstCell.Material = srcCell.cell.Material;

                    if (AffectTiles)
                    {
                        if (srcCell.cell.TileHead is not null)
                        {
                            dstCell.TileHead = srcCell.cell.TileHead;
                            dstCell.TileRootX = gx;
                            dstCell.TileRootY = gy;
                            dstCell.TileLayer = l;
                            rndr.InvalidateTileHead(gx, gy, l);
                        }
                        else if (srcCell.cell.HasTile())
                        {
                            dstCell.TileRootX = srcCell.cell.TileRootX + rndr.OverlayX;
                            dstCell.TileRootY = srcCell.cell.TileRootY + rndr.OverlayY;
                            dstCell.TileLayer = srcCell.cell.TileLayer;
                        }
                    }

                    rndr.InvalidateGeo(gx, gy, l);
                    if (l == 0)
                        RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(gx, gy);
                }
            }
        }

        movingGeometry = null;
        rndr.ClearOverlay();

        RainEd.Instance.LevelView.CellChangeRecorder.PushChange();
    }

    public void CancelMove()
    {
        if (movingGeometry is null)
            return;
        
        movingGeometry = null;
        RainEd.Instance.LevelView.Renderer.ClearOverlay();

        if (cancelGeoData is not null)
        {
            var level = RainEd.Instance.Level;
            var renderer = RainEd.Instance.LevelView.Renderer;
            var nodeData = RainEd.Instance.CurrentTab!.NodeData;
            
            for (int y = 0; y < movingH; y++)
            {
                var gy = cancelOrigY + y;
                for (int x = 0; x < movingW; x++)
                {
                    var gx = cancelOrigX + x;
                    for (int l = 0; l < Level.LayerCount; l++)
                    {
                        if (!level.IsInBounds(gx, gy)) continue;
                        if (!cancelGeoData[l,x,y].mask) continue;
                        level.Layers[l,gx,gy] = cancelGeoData[l,x,y].cell;

                        renderer.InvalidateGeo(gx, gy, l);
                        if (l == 0) nodeData.InvalidateCell(gx, gy);
                        if (level.Layers[l,gx,gy].TileHead is not null)
                            renderer.InvalidateTileHead(gx, gy, l);
                    }
                }
            }

            cancelGeoData = null;
        }
    }

    private (bool mask, LevelCell cell)[,,] MakeCellGroup(out int selX, out int selY, out int selW, out int selH, bool eraseSource)
    {
        // (selX, selY) = furthest top-left of layer selection
        selX = int.MaxValue;
        selY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var sel = ref selections[l];
            if (sel is null) continue;

            selX = int.Min(selX, sel.minX);
            selY = int.Min(selY, sel.minY);
            maxX = int.Max(maxX, sel.maxX);
            maxY = int.Max(maxY, sel.maxY);
        }

        selW = maxX - selX + 1;
        selH = maxY - selY + 1;
        var level = RainEd.Instance.Level;
        var renderer = RainEd.Instance.LevelView.Renderer;

        if (eraseSource)
        {
            RainEd.Instance.LevelView.CellChangeRecorder.BeginChange();
        }

        static bool GetMaskFromGlobalCoords(ref readonly LayerSelection sel, int x, int y)
        {
            if (x >= sel.minX && y >= sel.minY && x <= sel.maxX && y <= sel.maxY)
                return sel.mask[y - sel.minY, x - sel.minX];
            else
                return false;
        }

        var geometry = new (bool mask, LevelCell cell)[Level.LayerCount, selW, selH];
        for (int y = 0; y < selH; y++)
        {
            var gy = selY + y;
            for (int x = 0; x < selW; x++)
            {
                var gx = selX + x;

                for (int l = 0; l < Level.LayerCount; l++)
                {
                    ref readonly var sel = ref selections[l];
                    if (sel is null) continue;

                    if (!level.IsInBounds(gx, gy))
                        continue;

                    ref var srcCell = ref level.Layers[l,gx,gy];
                    ref var dstCell = ref geometry[l,x,y];
                    bool selMask = GetMaskFromGlobalCoords(in sel, gx, gy);
                    dstCell.mask = selMask;
                    dstCell.cell = srcCell;

                    // change tile head references to be relative to the origin
                    // of the overlay
                    if (dstCell.cell.HasTile() && dstCell.cell.TileHead is null)
                    {
                        dstCell.cell.TileRootX -= selX;
                        dstCell.cell.TileRootY -= selY;

                        // if the tile head is outside of the selection,
                        // then erase this tile body.
                        var tx = dstCell.cell.TileRootX;
                        var ty = dstCell.cell.TileRootY;
                        var tLayerSel = selections[dstCell.cell.TileLayer];
                        if (tx < 0 || ty < 0 ||
                            tx >= selW || ty >= selH ||
                            !(tLayerSel is not null && GetMaskFromGlobalCoords(in tLayerSel, tx + selX, ty + selY))
                        )
                        {
                            dstCell.cell.TileRootX = -1;
                            dstCell.cell.TileRootY = -1;
                            dstCell.cell.TileLayer = -1;
                        }
                    }

                    if (eraseSource && selMask)
                    {
                        srcCell.Geo = GeoType.Air;
                        srcCell.Objects = LevelObject.None;
                        srcCell.Material = 0;

                        if (AffectTiles)
                        {
                            bool hadHead = srcCell.TileHead is not null;
                            srcCell.TileRootX = -1;
                            srcCell.TileRootY = -1;
                            srcCell.TileLayer = -1;
                            srcCell.TileHead = null;

                            if (hadHead)
                                renderer.InvalidateTileHead(gx, gy, l);
                        }

                        renderer.InvalidateGeo(gx, gy, l);
                        if (l == 0)
                            RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(gx, gy);
                    }
                }
            }
        }

        if (eraseSource)
        {
            RainEd.Instance.LevelView.CellChangeRecorder.PushChange();
        }

        return geometry;
    }

    public bool IsSelectionActive()
    {
        for (int l = 0; l < Level.LayerCount; l++)
        {
            if (selections[l] is not null)
                return true;
        }

        return false;
    }

    public void ClearSelection()
    {
        selections[0] = null;
        selections[1] = null;
        selections[2] = null;
    }

    public void ClearSelection(ReadOnlySpan<bool> layerMasks)
    {
        for (int l = 0; l < Level.LayerCount; l++)
        {
            if (layerMasks[l]) selections[l] = null;
        }
    }

    private void BeginMove()
    {
        if (!IsSelectionActive())
        {
            cancelGeoData = null;
            movingGeometry = null;
            return;
        }

        // create separate copy of selected cells in order for the Cancel operation
        // to work properly. can't use movingGeometry because it may modify the
        // data of cells (i.e. tile head references)
        var level = RainEd.Instance.Level;

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        for (int l = 0; l < Level.LayerCount; l++)
        {
            ref var sel = ref selections[l];
            if (sel is null) continue;

            minX = int.Min(minX, sel.minX);
            minY = int.Min(minY, sel.minY);
            maxX = int.Max(maxX, sel.maxX);
            maxY = int.Max(maxY, sel.maxY);
        }

        var selW = maxX - minX + 1;
        var selH = maxY - minY + 1;
        cancelGeoData = new (bool mask, LevelCell cell)[Level.LayerCount, selW, selH];
        cancelOrigX = minX;
        cancelOrigY = minY;

        for (int y = 0; y < selH; y++)
        {
            var gy = cancelOrigY + y;
            for (int x = 0; x < selW; x++)
            {
                var gx = cancelOrigX + x;
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    ref var sel = ref selections[l];
                    if (sel is null) continue;

                    if (gx >= sel.minX && gy >= sel.minY && gx <= sel.maxX && gy <= sel.maxY && level.IsInBounds(gx, gy))
                    {
                        var ly = gy - sel.minY;
                        var lx = gx - sel.minX;
                        cancelGeoData[l,x,y] = (sel.mask[ly,lx], level.Layers[l,gx,gy]);
                    }
                    else
                    {
                        cancelGeoData[l,x,y] = (false, new LevelCell());
                    }
                }
            }
        }

        var renderer = RainEd.Instance.LevelView.Renderer;
        movingGeometry = MakeCellGroup(out int movingX, out int movingY, out movingW, out movingH, true);

        // send it to geo renderer
        renderer.OverlayX = movingX;
        renderer.OverlayY = movingY;
        renderer.SetOverlay(
            width: movingW,
            height: movingH,
            geometry: movingGeometry
        );
    }

    class RectDragState : Tool, ISelectionTool
    {
        private int selectionStartX = -1;
        private int selectionStartY = -1;
        private int mouseMinX = 0;
        private int mouseMaxX = 0;
        private int mouseMinY = 0;
        private int mouseMaxY = 0;

        public RectDragState(int startX, int startY)
        {
            selectionStartX = startX;
            selectionStartY = startY;
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

    class LassoDragState : Tool, ISelectionTool
    {
        private List<Vector2i> points = [];

        public LassoDragState(int startX, int startY)
        {
            points.Add(new Vector2i(startX, startY));
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
                var ptA = points[i-1];
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

    class SelectionMoveDragState : Tool
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
            layerInfo = new LayerInfo[Level.LayerCount];

            for (int l = 0; l < Level.LayerCount; l++)
            {
                ref var sel = ref controller.selections[l];
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

        public override void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask)
        {
            for (int l = 0; l < Level.LayerCount; l++)
            {
                if (!layerInfo[l].active) continue;
                ref var sel = ref controller.selections[l];
                if (sel is null) continue;

                sel.minX = mouseX - layerInfo[l].offsetX;
                sel.minY = mouseY - layerInfo[l].offsetY;
                sel.maxX = sel.minX + layerInfo[l].selW - 1;
                sel.maxY = sel.minY + layerInfo[l].selH - 1;
            }
        }
    }

    class SelectedMoveDragState : Tool
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
                    ref var sel = ref controller.selections[l];
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
                ref var sel = ref controller.selections[l];
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
            if (controller.movingGeometry is null)
            {
                controller.BeginMove();
            }
        }

        public override void Update(int mouseX, int mouseY, ReadOnlySpan<bool> layerMask)
        {
            var rndr = RainEd.Instance.LevelView.Renderer;
            rndr.OverlayX = mouseX - offsetX;
            rndr.OverlayY = mouseY - offsetY;

            for (int l = 0; l < Level.LayerCount; l++)
            {
                ref var sel = ref controller.selections[l];
                if (sel is null) continue;

                sel.minX = rndr.OverlayX + layerInfo[l].offsetX;
                sel.minY = rndr.OverlayY + layerInfo[l].offsetY;
                sel.maxX = sel.minX + layerInfo[l].selW - 1;
                sel.maxY = sel.minY + layerInfo[l].selH - 1;
            }
        }
    }
}