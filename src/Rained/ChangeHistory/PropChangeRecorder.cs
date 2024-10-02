using RainEd.LevelData;
using RainEd.EditorGui.Editors;
using System.Numerics;

namespace RainEd.ChangeHistory;

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

class PropTransformChangeRecord : IChangeRecord
{
    private readonly Prop[] targetProps;
    private readonly PropTransformExt[] oldTransforms;
    private readonly PropTransformExt[] newTransforms;

    public PropTransformChangeRecord(Prop[] targetProps, PropTransformExt[] oldTransforms)
    {
        this.targetProps = targetProps;
        this.oldTransforms = oldTransforms;

        newTransforms = new PropTransformExt[targetProps.Length];
        for (int i = 0; i < targetProps.Length; i++)
        {
            newTransforms[i] = new PropTransformExt(targetProps[i]);
        }
    }

    public void Apply(bool useNew)
    {
        RainEd.Instance.LevelView.EditMode = (int) EditModeEnum.Prop;

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

class PropSettingsChangeRecord : IChangeRecord
{
    private readonly Prop[] targetProps;
    private readonly PropSettings[] oldSettings;
    private readonly PropSettings[] newSettings;

    public PropSettingsChangeRecord(Prop[] targetProps, PropSettings[] oldSettings)
    {
        this.targetProps = targetProps;
        this.oldSettings = oldSettings;

        newSettings = new PropSettings[targetProps.Length];
        for (int i = 0; i < targetProps.Length; i++)
        {
            newSettings[i] = new PropSettings(targetProps[i]); 
        }
    }

    public void Apply(bool useNew)
    {
        var settings = useNew ? newSettings : oldSettings;

        for (int i = 0; i < targetProps.Length; i++)
        {
            settings[i].Apply(targetProps[i]);
        }
    }
}

class PropChangeRecorder
{
    private bool isTransformActive = false;
    private readonly List<PropTransformExt> oldTransforms = [];
    private Prop[]? oldProps = null;

    private readonly List<PropSettings> oldSettings = [];
    private readonly List<Prop> recordedProps = [];

    public bool IsTransformActive { get => isTransformActive; }

    public PropChangeRecorder()
    {
        TakeSettingsSnapshot();
    }

    public void BeginTransform()
    {
        var level = RainEd.Instance.Level;

        oldTransforms.Clear();
        foreach (var prop in level.Props)
        {
            oldTransforms.Add(new PropTransformExt(prop));
            recordedProps.Add(prop);
        }
    }

    public void PushTransform()
    {
        if (isTransformActive) throw new Exception("PropChangeRecorder.PushTransform() was called twice");
        isTransformActive = false;
        var level = RainEd.Instance.Level;

        if (level.Props.Count != oldTransforms.Count)
        {
            Log.Error("Props changed between BeginTransform and PushTransform of PropChangeRecorder");
            return;
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

        var changeRecord = new PropTransformChangeRecord(level.Props.ToArray(), oldTransforms.ToArray());
        RainEd.Instance.ChangeHistory.Push(changeRecord);

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
        recordedProps.Clear();

        foreach (var prop in level.Props)
        {
            recordedProps.Add(prop);
            oldSettings.Add(new PropSettings(prop));
        }
    }

    public void PushSettingsChanges()
    {
        var targetProps = new List<Prop>();
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
        }

        TakeSettingsSnapshot();
    }
}