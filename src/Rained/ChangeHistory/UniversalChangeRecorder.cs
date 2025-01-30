namespace Rained.ChangeHistory;

[Flags]
public enum LevelComponents
{
    None = 0,
    Properties = 1,
    Cells = 2,
    Cameras = 4,
    Effects = 8,
    Props = 16,
    All = Properties | Cells | Cameras | Effects | Props
};

/// <summary>
/// This is the change recorder used by Lua scripts.
/// </summary>
class UniversalChangeRecorder : ChangeRecorder
{
    public UniversalEffectChangeRecorder EffectRecorder => effectRecorder;

    private readonly UniversalEffectChangeRecorder effectRecorder = new();
    private readonly CameraChangeRecorder camRecorder = new();
    private LevelComponents activeComponents;

    public override bool Active {
        get
        {
            return RainEd.Instance.LevelView.CellChangeRecorder.Active || effectRecorder.Active;
        }
    }

    public void BeginChange(LevelComponents components)
    {
        activeComponents = components;

        if (components.HasFlag(LevelComponents.Cells))
            RainEd.Instance.LevelView.CellChangeRecorder.BeginChange(false);

        if (components.HasFlag(LevelComponents.Effects))
            effectRecorder.BeginChange();

        if (components.HasFlag(LevelComponents.Cameras))
            camRecorder.BeginChange(false);

        if (components.HasFlag(LevelComponents.Properties | LevelComponents.Props))
            throw new NotImplementedException();
    }

    public override IChangeRecord? EndChange()
    {
        var components = activeComponents;
        var res = new ConglomerateChange();

        if (components.HasFlag(LevelComponents.Cells))
            res.cells = (CellChangeRecord?) RainEd.Instance.LevelView.CellChangeRecorder.EndChange();

        if (components.HasFlag(LevelComponents.Effects))
            res.effects = (UniversalEffectChangeRecord?) effectRecorder.EndChange();

        if (components.HasFlag(LevelComponents.Cameras))
            res.cameras = (CameraChangeRecord?) camRecorder.EndChange();
        
        if (res.cells is null && res.effects is null && res.cameras is null)
            return null;
        
        return res;
    }

    record class ConglomerateChange : IChangeRecord
    {
        public CellChangeRecord? cells;
        public UniversalEffectChangeRecord? effects;
        public CameraChangeRecord? cameras;

        public void Apply(bool useNew)
        {
            cells?.Apply(useNew);
            effects?.Apply(useNew);
            cameras?.Apply(useNew);
        }
    }
}