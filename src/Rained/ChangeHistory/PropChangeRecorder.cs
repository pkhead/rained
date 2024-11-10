using Rained.LevelData;
using Rained.EditorGui.Editors;
using System.Numerics;

namespace Rained.ChangeHistory;

record PropTransformExt
{
    public PropTransform Transform;
    public Vector2[] RopePoints;
    public Vector2[] RopeVelocities;

    public PropTransformExt(Prop prop)
    {
        Transform = prop.Transform.Clone();

        var ropeModel = prop.Rope?.Model;
        if (ropeModel is not null)
        {
            RopePoints = new Vector2[ropeModel.SegmentCount];
            RopeVelocities = new Vector2[ropeModel.SegmentCount];
            for (int i = 0; i < ropeModel.SegmentCount; i++)
            {
                RopePoints[i] = ropeModel.GetSegmentPos(i);
                RopeVelocities[i] = ropeModel.GetSegmentVel(i);
            }
        }
        else
        {
            RopePoints = [];
            RopeVelocities = [];
        }
    }

    public void Apply(Prop prop)
    {
        prop.Transform = Transform.Clone();

        var rope = prop.Rope;
        if (rope is not null)
        {
            rope.IgnoreMovement();
            rope.ResetModel();
            
            rope.Model!.SetSegmentPositions(RopePoints);

            for (int i = 0; i < RopePoints.Length; i++)
            {
                rope.Model!.SetSegmentVel(i, RopeVelocities[i]);
            }
        }
    }
}

struct PropSettings(Prop prop)
{
    public int DepthOffset = prop.DepthOffset;
    public int CustomDepth = prop.CustomDepth;
    public int CustomColor = prop.CustomColor;
    public int RenderOrder = prop.RenderOrder;
    public int Variation = prop.Variation;
    public int Seed = prop.Seed;
    public PropRenderTime RenderTime = prop.RenderTime;
    public bool ApplyColor = prop.ApplyColor;

    public RopeReleaseMode ReleaseMode = prop.Rope?.ReleaseMode ?? RopeReleaseMode.None;
    public float PropHeight = prop.IsAffine ? prop.Rect.Size.Y : 0f; // a.k.a. rope flexibility
    public float RopeThickness = prop.Rope?.Thickness ?? 0f;

    public readonly void Apply(Prop prop)
    {
        prop.DepthOffset = DepthOffset;
        prop.CustomDepth = CustomDepth;
        prop.CustomColor = CustomColor;
        prop.RenderOrder = RenderOrder;
        prop.Variation = Variation;
        prop.Seed = Seed;
        prop.RenderTime = RenderTime;
        prop.ApplyColor = ApplyColor;

        var rope = prop.Rope;
        if (rope is not null)
        {
            rope.ReleaseMode = ReleaseMode;
            rope.Thickness = RopeThickness;
            
            if (prop.IsAffine)
            {
                prop.Rect.Size.Y = PropHeight;
            }
        }
    }
}

class PropChangeRecord : IChangeRecord
{
    private readonly Prop[] targetProps;
    private readonly PropTransformExt[] oldTransforms;
    private readonly PropTransformExt[] newTransforms;
    private readonly PropSettings[] oldSettings;
    private readonly PropSettings[] newSettings;

    public PropChangeRecord(Prop[] targetProps, PropTransformExt[] oldTransforms, PropSettings[] oldSettings)
    {
        this.targetProps = targetProps;
        this.oldTransforms = oldTransforms;
        this.oldSettings = oldSettings;

        newTransforms = new PropTransformExt[targetProps.Length];
        for (int i = 0; i < targetProps.Length; i++)
        {
            newTransforms[i] = new PropTransformExt(targetProps[i]);
        }

        newSettings = new PropSettings[targetProps.Length];
        for (int i = 0; i < targetProps.Length; i++)
        {
            newSettings[i] = new PropSettings(targetProps[i]); 
        }
    }

    public void Apply(bool useNew)
    {
        RainEd.Instance.LevelView.EditMode = (int) EditModeEnum.Prop;
        var settings = useNew ? newSettings : oldSettings;

        for (int i = 0; i < targetProps.Length; i++)
        {
            var prop = targetProps[i];

            if (useNew)
            {
                newTransforms[i].Apply(prop);
            }
            else
            {
                oldTransforms[i].Apply(prop);
            }

            settings[i].Apply(targetProps[i]);
        }
    }
}

class PropListChangeRecord : IChangeRecord
{
    private readonly Prop[] oldProps;
    private readonly Prop[] newProps;

    public PropListChangeRecord(Prop[] oldProps, Prop[] newProps)
    {
        this.oldProps = oldProps;
        this.newProps = newProps;
    }

    public void Apply(bool useNew)
    {
        var propsList = RainEd.Instance.Level.Props;
        var targetArr = useNew ? newProps : oldProps;

        propsList.Clear();
        foreach (var prop in targetArr)
        {
            propsList.Add(prop);
        }
    }
}

class PropChangeRecorder
{
    private bool isTransformActive = false;

    private readonly List<PropTransformExt> oldTransforms = [];
    private readonly Dictionary<Prop, PropSettings> oldSettings = [];

    // for list changes
    private Prop[]? oldProps = null;

    public bool IsTransformActive { get => isTransformActive; }

    public PropChangeRecorder()
    {
        TakeSettingsSnapshot();
    }

    public void BeginTransform()
    {
        if (isTransformActive) throw new Exception("PropChangeRecorder.BeginTransform() was called twice");
        isTransformActive = true;

        var level = RainEd.Instance.Level;

        oldTransforms.Clear();
        foreach (var prop in level.Props)
        {
            oldTransforms.Add(new PropTransformExt(prop));
        }
    }

    private static bool TransformsEqual(Prop prop, PropTransformExt transform)
    {   
        if (!prop.Transform.Equals(transform.Transform)) return false;

        // check rope points/velocities
        if (prop.Rope?.Model is RopeModel model)
        {
            if (model.SegmentCount != transform.RopePoints.Length)
                return false;
            
            for (int i = 0; i < model.SegmentCount; i++)
                if (transform.RopePoints[i] != model.GetSegmentPos(i) || transform.RopeVelocities[i] != model.GetSegmentVel(i))
                    return false;
        }

        return true;
    }

    public void PushChanges()
    {
        isTransformActive = false;
        var level = RainEd.Instance.Level;

        if (level.Props.Count != oldTransforms.Count)
        {
            Log.Error("Props changed between BeginTransform and PushTransform of PropChangeRecorder");
            return;
        }

        List<Prop> changedProps = [];
        List<PropTransformExt> changedOldTransforms = [];
        List<PropSettings> changedOldSettings = [];

        // find props whose settings or transforms have changed
        for (int i = 0; i < level.Props.Count; i++)
        {
            if (
                !TransformsEqual(level.Props[i], oldTransforms[i]) ||
                !new PropSettings(level.Props[i]).Equals(oldSettings[level.Props[i]]))
            {
                changedProps.Add(level.Props[i]);
                changedOldTransforms.Add(oldTransforms[i]);
                changedOldSettings.Add(oldSettings[level.Props[i]]);
            }
        }

        if (changedProps.Count > 0)
        {
            Log.Debug("{ChangeCount} props changed", changedProps.Count);
            var changeRecord = new PropChangeRecord([..changedProps], [..changedOldTransforms], [..changedOldSettings]);
            RainEd.Instance.ChangeHistory.Push(changeRecord);
        }

        /*bool didChange = false;
        for (int i = 0; i < level.Props.Count; i++)
        {
            if (!level.Props[i].Transform.Equals(oldTransforms[i]))
            {
                didChange = true;
                break;
            }
        }*/

        TakeSettingsSnapshot();
    }

    public void BeginListChange()
    {
        oldProps = RainEd.Instance.Level.Props.ToArray();
    }

    public void PushListChange()
    {
        if (oldProps is null)
        {
            throw new Exception("PropChangeRecorder.PushListChange() called twice");
        }

        var level = RainEd.Instance.Level;
        var newList = level.Props.ToArray();

        // detect if any props had been added or removed
        bool didChange = false;
        if (newList.Length == oldProps.Length)
        {
            for (int i = 0; i < newList.Length; i++)
            {
                if (oldProps[i] != newList[i])
                {
                    didChange = true;
                    break;
                }
            }
        }
        else
        {
            didChange = true;
        }

        if (didChange)
        {
            var record = new PropListChangeRecord(oldProps, newList);
            RainEd.Instance.ChangeHistory.Push(record);
        }

        oldProps = null;

        TakeSettingsSnapshot();
    }

    public void TakeSettingsSnapshot()
    {
        var level = RainEd.Instance.Level;

        oldSettings.Clear();
        foreach (var prop in level.Props)
        {
            oldSettings.Add(prop, new PropSettings(prop));
        }
    }

    public void PushSettingsChanges()
    {
        if (!IsTransformActive)
        {
            BeginTransform();
            PushChanges();
        }

        /*var targetProps = new List<Prop>();
        var oldList = new List<PropSettings>();

        for (int i = 0; i < recordedProps.Count; i++)
        {
            var prop = recordedProps[i];
            var old = oldSettings[i];

            var newSettings = new PropSettings(prop);

            // if the settings had changed?
            if (!old.Equals(newSettings))
            {
                targetProps.Add(prop);
                oldList.Add(old);
            }
        }

        if (targetProps.Count > 0)
        {
            var changeRecord = new PropSettingsChangeRecord([..targetProps], [..oldList]);
            RainEd.Instance.ChangeHistory.Push(changeRecord);
        }*/
    }
}