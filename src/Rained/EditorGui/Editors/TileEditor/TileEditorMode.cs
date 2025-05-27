namespace Rained.EditorGui.Editors;
using Raylib_cs;
using System.Numerics;
using Rained.LevelData;
using Rained.Assets;
using ImGuiNET;

abstract class TileEditorCatalog : CatalogWidget
{
    protected override bool HasSearch => true;
    protected override bool Dual => true;
}

abstract class TileEditorMode(TileEditor editor)
{
    public enum RectMode { Inactive, Place, Remove };
    protected RectMode rectMode = 0;
    protected CellPosition rectStart;

    protected readonly TileEditor editor = editor;
    protected bool LeftMouseDown { get; private set; } = false;
    protected bool RightMouseDown { get; private set; } = false;

    abstract public string TabName { get; }
    public RectMode CurrentRectMode => rectMode;

    public void ResetInput()
    {
        LeftMouseDown = false;
        RightMouseDown = false;
    }

    virtual public void Process()
    {
        if (EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
            LeftMouseDown = true;
        
        if (KeyShortcuts.Activated(KeyShortcut.RightMouse))
            RightMouseDown = true;
        
        if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left))
            LeftMouseDown = false;
        
        if (KeyShortcuts.Deactivated(KeyShortcut.RightMouse))
            RightMouseDown = false;
    }

    virtual public void IdleProcess() {}

    abstract public void DrawToolbar();

    public virtual void Focus()
    {
        rectMode = RectMode.Inactive;
        LeftMouseDown = false;
        RightMouseDown = false;
    }
    
    public virtual void Unfocus()
    {
        rectMode = RectMode.Inactive;
        LeftMouseDown = false;
        RightMouseDown = false;

        RainEd.Instance.LevelView.CellChangeRecorder.TryPushChange();
    }

    public virtual void UndidOrRedid() {}
}