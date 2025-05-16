using System.Numerics;
using Rained.EditorGui.Editors;
using Rained.EditorGui.Editors.CellEditing;

namespace Rained.ChangeHistory;

struct CellSelectionData(Vector2i cutoutPos, Vector2i cutoutSize, MaskedCell[,,]? cutout, LayerSelection?[]? selections)
{
    public Vector2i cutoutPos = cutoutPos;
    public Vector2i cutoutSize = cutoutSize;
    public MaskedCell[,,]? cutout = cutout;
    public LayerSelection?[] selections = selections is null ? new LayerSelection?[3] : selections;
}

// usually the pattern was to store both the old state and the state at which the change record was pushed.
// however I realized i can just make it only store one snapshot of the state, and swap the current state
// and the stored state when apply is called. since, you know, you can't undo/redo something more than once.
class CellSelectionChangeRecord(
    CellSelectionData data
) : IChangeRecord
{
    public CellSelectionData data = data;
    public int? editMode;
    public CellChangeRecord? cellChangeRecord;

    public void Apply(bool useNew)
    {
        CellSelection.Instance ??= new CellSelection();
        CellSelection.Instance.AffectTiles = editMode == (int)EditModeEnum.Tile;

        // update edit mode
        if (editMode is not null)
            (RainEd.Instance.LevelView.EditMode, editMode) = (editMode.Value, RainEd.Instance.LevelView.EditMode);

        // swap selection state between current and stored
        Vector2i tmpCutoutPos = new Vector2i();
        Vector2i tmpCutoutSize = new Vector2i();
        MaskedCell[,,]? tmpCutout;
        LayerSelection?[] tmpSelections = new LayerSelection?[3];

        CellSelection.Instance.CopyState(
            tmpSelections,
            out tmpCutout, out tmpCutoutPos.X, out tmpCutoutPos.Y, out tmpCutoutSize.X, out tmpCutoutSize.Y
        );

        CellSelection.Instance.ApplyState(
            data.selections,
            data.cutout, data.cutoutPos.X, data.cutoutPos.Y,
            data.cutoutSize.X, data.cutoutSize.Y
        );

        data.selections = tmpSelections;
        data.cutout = tmpCutout;
        data.cutoutPos = tmpCutoutPos;
        data.cutoutSize = tmpCutoutSize;

        if (cellChangeRecord is not null)
        {
            cellChangeRecord.Apply(useNew);
        }
    }
}

class CellSelectionChangeRecorder : ChangeRecorder
{
    LayerSelection?[] tempSelections = new LayerSelection?[3];
    MaskedCell[,,]? tempCutout;
    int cutoutX, cutoutY, cutoutW, cutoutH;
    bool _active = false;

    int? editMode = null;
    bool trackGeo;

    public void BeginChange(bool saveEditMode = false)
    {
        if (_active)
        {
            ValidationError("CellSelectionChangeRecorder.BeginChange when already active");
            return;
        }

        var cellSel = CellSelection.Instance;
        if (cellSel is null)
        {
            ValidationError("Selection mode is not active!");
            return;
        }

        editMode = saveEditMode ? RainEd.Instance.LevelView.EditMode : null;

        cellSel.CopyState(tempSelections, out tempCutout, out cutoutX, out cutoutY, out cutoutW, out cutoutH);
        trackGeo = false;
        _active = true;
    }

    public void BeginChangeWithGeo()
    {
        if (_active)
        {
            ValidationError("CellSelectionChangeRecorder.BeginChange when already active");
            return;
        }

        var cellSel = CellSelection.Instance;
        if (cellSel is null)
        {
            ValidationError("Selection mode is not active!");
            return;
        }

        editMode = null;

        cellSel.CopyState(tempSelections, out tempCutout, out cutoutX, out cutoutY, out cutoutW, out cutoutH);
        RainEd.Instance.LevelView.CellChangeRecorder.BeginChange();
        trackGeo = true;
        _active = true;
    }

    public void TryPushChange()
    {
        if (_active)
        {
            var data = new CellSelectionData(
                new Vector2i(cutoutX, cutoutY), new Vector2i(cutoutW, cutoutH),
                tempCutout,
                (LayerSelection?[])tempSelections.Clone()
            );

            RainEd.Instance.ChangeHistory.Push(new CellSelectionChangeRecord(data)
            {
                editMode = editMode,
                cellChangeRecord = trackGeo ? RainEd.Instance.LevelView.CellChangeRecorder.EndChange() : null
            });
            _active = false;
        }
    }

    public void PushChange()
    {
        if (!_active)
        {
            ValidationError("CellSelectionChangeRecorder.PushChange when not active");
            return;
        }

        TryPushChange();
    }

    public void CancelChange()
    {
        _active = false;
    }
}