using System.Diagnostics;
using System.Numerics;
using Rained.EditorGui.Editors.CellEditing;
using Rained.LevelData;

namespace Rained.ChangeHistory;

class CellSelectionChangeRecord : IChangeRecord
{
    public Vector2i oldCutoutPos;
    public Vector2i newCutoutPos;

    public MaskedCell[,,]? cutout;
    public int cutoutWidth, cutoutHeight;

    public LayerSelection?[] oldSelections = new LayerSelection?[3];
    public LayerSelection?[] newSelections = new LayerSelection?[3];

    public CellSelectionChangeRecord()
    {
        
    }

    public void Apply(bool useNew)
    {

    }
}

class CellSelectionChangeRecorder : ChangeRecorder
{
    LayerSelection?[] tempSelections = new LayerSelection?[3];
    MaskedCell[,,]? tempCutout;
    int cutoutX, cutoutY, cutoutW, cutoutH;

    private void CopyState(CellSelection cellSel, LayerSelection?[] selections, out MaskedCell[,,]? cutout, out int cutoutX, out int cutoutY, out int cutoutW, out int cutoutH)
    {
        ArgumentNullException.ThrowIfNull(cellSel);

        for (int i = 0; i < Level.LayerCount; i++)
        {
            selections[i] = null;
            var srcSel = cellSel.Selections[i];

            if (srcSel is not null)
            {
                selections[i] = new LayerSelection(
                    srcSel.minX, srcSel.minY,
                    srcSel.maxX, srcSel.maxY,
                    (bool[,]) srcSel.mask.Clone()
                );
            }
        }

        // copy geometry cutout, if active
        MaskedCell[,,]? srcCutout = cellSel.ActiveCutout;
        cutout = null;
        cutoutX = 0;
        cutoutY = 0;
        cutoutW = 0;
        cutoutH = 0;

        if (srcCutout is not null)
        {
            cutoutX = cellSel.CutoutX;
            cutoutY = cellSel.CutoutY;
            cutoutW = cellSel.CutoutWidth;
            cutoutH = cellSel.CutoutHeight;
            cutout = (MaskedCell[,,]) srcCutout.Clone();
        }
    }

    public void BeginChange()
    {
        var cellSel = CellSelection.Instance;
        if (cellSel is null)
        {
            ValidationError("Selection mode is not active!");
            return;
        }

        CopyState(cellSel, tempSelections, out tempCutout, out cutoutX, out cutoutY, out cutoutW, out cutoutH);
    }

    public void TryPushChange()
    {
        //CopyState(cellSel, tempSelections, out tempCutout);
    }
}