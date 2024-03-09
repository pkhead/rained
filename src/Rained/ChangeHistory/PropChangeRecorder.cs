namespace RainEd.ChangeHistory;

class PropTransformChangeRecord : IChangeRecord
{
    private readonly Prop[] targetProps;
    private readonly PropTransform[] oldTransforms;
    private readonly PropTransform[] newTransforms;

    public PropTransformChangeRecord(Prop[] targetProps, PropTransform[] oldTransforms)
    {
        this.targetProps = targetProps;
        this.oldTransforms = oldTransforms;

        newTransforms = new PropTransform[targetProps.Length];
        for (int i = 0; i < targetProps.Length; i++)
        {
            newTransforms[i] = targetProps[i].Transform.Clone();
        }
    }

    public void Apply(bool useNew)
    {
        for (int i = 0; i < targetProps.Length; i++)
        {
            var prop = targetProps[i];

            if (useNew)
            {
                prop.Transform = newTransforms[i].Clone();
            }
            else
            {
                prop.Transform = oldTransforms[i].Clone();
            }
        }
    }
}

class PropChangeRecorder
{
    private bool isTransformActive = false;
    private readonly List<PropTransform> oldTransforms = [];

    public bool IsTransformActive { get => isTransformActive; }

    public void BeginTransform()
    {
        var level = RainEd.Instance.Level;

        oldTransforms.Clear();
        foreach (var prop in level.Props)
        {
            oldTransforms.Add(prop.Transform.Clone());
        }
    }

    public void PushTransform()
    {
        if (isTransformActive) throw new Exception("PropChangeRecorder.PushTransform() was called twice");
        isTransformActive = false;
        var level = RainEd.Instance.Level;

        if (level.Props.Count != oldTransforms.Count)
        {
            RainEd.Logger.Error("Props changed between BeginTransform and PushTransform of PropChangeRecorder");
            return;
        }

        bool didChange = false;
        for (int i = 0; i < level.Props.Count; i++)
        {
            if (!level.Props[i].Transform.Equals(oldTransforms[i]))
            {
                didChange = true;
                break;
            }
        }

        if (didChange)
        {
            var changeRecord = new PropTransformChangeRecord(level.Props.ToArray(), oldTransforms.ToArray());
            RainEd.Instance.ChangeHistory.Push(changeRecord);
        }
    }
}