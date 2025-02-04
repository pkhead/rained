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
/// <p>
/// This is the change recorder used by Lua scripts.
/// Unlike the non-universal change recorders, it can track changes
/// for every aspect of the level rather than just one, done by unifying changes
/// from different change recorders into one conglomerate class.
/// </p>
/// 
/// <p>
/// In addition, it uses a more generic implementation of a prop and effects change recorder,
/// because the one I wrote earlier was too bound to the user interface, though they do require
/// the user to explictly flag when a prop was changed.
/// </p>
/// 
/// </summary>
class UniversalChangeRecorder : ChangeRecorder
{
    public UniversalEffectChangeRecorder EffectRecorder { get; private set; } = new();
    public UniversalPropChangeRecorder PropRecorder { get; private set; } = new();

    private readonly CameraChangeRecorder camRecorder = new();
    private LevelComponents activeComponents;

    private bool _active = false;
    public override bool Active => _active;

    public void BeginChange(LevelComponents components)
    {
        activeComponents = components;

        if (components.HasFlag(LevelComponents.Cells))
            RainEd.Instance.LevelView.CellChangeRecorder.BeginChange(false);

        if (components.HasFlag(LevelComponents.Effects))
            EffectRecorder.BeginChange();

        if (components.HasFlag(LevelComponents.Cameras))
            camRecorder.BeginChange(false);
        
        if (components.HasFlag(LevelComponents.Props))
            PropRecorder.BeginChange();

        if (components.HasFlag(LevelComponents.Properties))
            throw new NotImplementedException();
        
        _active = true;
    }

    public override IChangeRecord? EndChange()
    {
        var components = activeComponents;
        var res = new ConglomerateChange();

        if (components.HasFlag(LevelComponents.Cells))
            res.cells = (CellChangeRecord?) RainEd.Instance.LevelView.CellChangeRecorder.EndChange();

        if (components.HasFlag(LevelComponents.Effects))
            res.effects = (UniversalEffectChangeRecord?) EffectRecorder.EndChange();

        if (components.HasFlag(LevelComponents.Cameras))
            res.cameras = (CameraChangeRecord?) camRecorder.EndChange();
        
        if (components.HasFlag(LevelComponents.Props))
            res.props = (UniversalPropChangeRecord?) PropRecorder.EndChange();
        
        _active = false;
        if (res.cells is null && res.effects is null && res.cameras is null && res.props is null)
            return null;
        
        return res;
    }

    record class ConglomerateChange : IChangeRecord
    {
        public CellChangeRecord? cells;
        public UniversalEffectChangeRecord? effects;
        public CameraChangeRecord? cameras;
        public UniversalPropChangeRecord? props;

        public void Apply(bool useNew)
        {
            cells?.Apply(useNew);
            effects?.Apply(useNew);
            cameras?.Apply(useNew);
            props?.Apply(useNew);
        }
    }
}