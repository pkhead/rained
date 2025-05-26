using Rained.LevelData;
namespace Rained.ChangeHistory;

record class UniversalPropChangeRecord : IChangeRecord
{
    public record PropRecord(Prop Prop)
    {
        public Prop prop = Prop;
        public PropSettings? settingsStore;
        public PropTransformExt? transformStore;
    }

    public Prop[]? listStore;
    public readonly PropRecord[] propChanges;

    public UniversalPropChangeRecord(Prop[]? oldList, PropRecord[] propChanges)
    {
        this.listStore = oldList;
        this.propChanges = propChanges;
    }

    public void Apply(bool useNew)
    {
        var level = RainEd.Instance.Level;
        
        if (listStore is not null)
        {
            // swap current prop list with list store
            Prop[] newList = [..level.Props];

            level.Props.Clear();
            level.Props.AddRange(listStore);
            listStore = newList;
        }

        foreach (var record in propChanges)
        {
            if (record.settingsStore is not null)
            {
                // swap current prop settings with store
                var temp = record.settingsStore;
                record.settingsStore = new PropSettings(record.prop);
                temp.Apply(record.prop);
            }

            if (record.transformStore is not null)
            {
                // swap current matrix with store
                var temp = record.transformStore;
                record.transformStore = new PropTransformExt(record.prop);
                temp.Apply(record.prop);
            }
        }
    }
}

/// <summary>
/// A prop change recorder for UniversalChangeRecorder.
/// </summary>
class UniversalPropChangeRecorder : ChangeRecorder
{
    record class PropChangeData
    {
        public PropTransformExt? transform;
        public PropSettings? settings;
    };

    private readonly Dictionary<Prop, PropChangeData> propChanges = [];
    private Prop[]? listSnapshot = null;
    private bool active = false;

    public override bool Active => active;

    public void BeginChange()
    {
        var level = RainEd.Instance.Level;
        listSnapshot = [..level.Props];
        propChanges.Clear();
        active = true;
    }

    public override IChangeRecord? EndChange()
    {
        if (!active || listSnapshot is null) return null;
        active = false;
        
        var level = RainEd.Instance.Level;
        bool listChanged = false;

        // detect if props list or any props in it had changed
        {
            if (listSnapshot.Length != level.Props.Count)
            {
                listChanged = true;
                goto hadChanged;
            }

            for (int i = 0; i < level.Props.Count; i++)
            {
                if (level.Props[i] != listSnapshot[i])
                {
                    listChanged = true;
                    goto hadChanged;
                }
            }

            for (int i = 0; i < level.Props.Count; i++)
            {
                if (propChanges.ContainsKey(level.Props[i]))
                    goto hadChanged;
            }
        }

        return null; // no change was detected, return null
        hadChanged:;

        // change was detected...
        var records = new List<UniversalPropChangeRecord.PropRecord>(propChanges.Count);
        foreach (var (prop, change) in propChanges)
        {
            if (listSnapshot.Contains(prop) || RainEd.Instance.Level.Props.Contains(prop))
            {
                records.Add(new UniversalPropChangeRecord.PropRecord(prop)
                {
                    settingsStore = change.settings,
                    transformStore = change.transform
                });
            }
        }

        return new UniversalPropChangeRecord(listChanged ? listSnapshot : null, [..records]);
    }

    /// <summary>
    /// Called before a script-invocated prop settings change.
    /// </summary>
    /// <param name="prop"></param>
    public void ChangeSettings(Prop prop)
    {
        if (!active) return;

        PropChangeData? changeData;
        if (!propChanges.TryGetValue(prop, out changeData))
        {
            changeData = new PropChangeData();
            propChanges.Add(prop, changeData);
        }

        changeData.settings ??= new PropSettings(prop);
    }

    /// <summary>
    /// Called before a script-invocated prop transformation change.
    /// </summary>
    /// <param name="prop"></param>
    public void ChangeTransform(Prop prop)
    {
        if (!active) return;

        PropChangeData? changeData;
        if (!propChanges.TryGetValue(prop, out changeData))
        {
            changeData = new PropChangeData();
            propChanges.Add(prop, changeData);
        }

        changeData.transform ??= new PropTransformExt(prop);
    }
}