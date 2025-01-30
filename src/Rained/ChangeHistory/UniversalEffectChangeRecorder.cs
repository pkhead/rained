using Rained.LevelData;
using Rained.EditorGui.Editors;
namespace Rained.ChangeHistory;

record class UniversalEffectChangeRecord : IChangeRecord
{
    public record class EffectRecord(Effect effect)
    {
        public Effect effect = effect;
        public EffectConfigData? configStore;
        public float[,]? matrixStore;
    }

    public Effect[]? listStore;
    public readonly EffectRecord[] effectChanges;

    public UniversalEffectChangeRecord(Effect[]? oldList, EffectRecord[] effectChanges)
    {
        this.listStore = oldList;
        this.effectChanges = effectChanges;
    }

    public void Apply(bool useNew)
    {
        var level = RainEd.Instance.Level;
        
        if (listStore is not null)
        {
            // swap current effect list with list store
            Effect[] newList = [..level.Effects];

            level.Effects.Clear();
            level.Effects.AddRange(listStore);
            listStore = newList;
        }

        foreach (var record in effectChanges)
        {
            if (record.configStore is not null)
            {
                // swap current effect config with store
                var temp = record.configStore.Value;
                record.configStore = new EffectConfigData(record.effect);
                temp.Apply(record.effect);
            }

            if (record.matrixStore is not null)
            {
                // swap current matrix with store
                (record.matrixStore, record.effect.Matrix) = (
                    (float[,]) record.effect.Matrix.Clone(),
                    record.matrixStore
                );
            }
        }
    }
}

/// <summary>
/// An effect change recorder for UniversalChangeRecorder.
/// </summary>
class UniversalEffectChangeRecorder : ChangeRecorder
{
    record class EffectChangeData
    {
        public EffectConfigData? config;
        public float[,]? matrix;
    };

    private readonly Dictionary<Effect, EffectChangeData> effectChanges = [];
    private Effect[]? listSnapshot = null;
    private bool active = false;

    public override bool Active => active;

    public void BeginChange()
    {
        var level = RainEd.Instance.Level;
        listSnapshot = [..level.Effects];
        effectChanges.Clear();
        active = true;
    }

    public override IChangeRecord? EndChange()
    {
        if (!active || listSnapshot is null) return null;
        active = false;
        
        var level = RainEd.Instance.Level;
        bool listChanged = false;

        // detect if effects list or any effects in it had changed
        {
            if (listSnapshot.Length != level.Effects.Count)
            {
                listChanged = true;
                goto hadChanged;
            }

            for (int i = 0; i < level.Effects.Count; i++)
            {
                if (level.Effects[i] != listSnapshot[i])
                {
                    listChanged = true;
                    goto hadChanged;
                }
            }

            for (int i = 0; i < level.Effects.Count; i++)
            {
                if (effectChanges.ContainsKey(level.Effects[i]))
                    goto hadChanged;
            }
        }

        return null; // no change was detected, return null
        hadChanged:;

        // change was detected...
        var records = new UniversalEffectChangeRecord.EffectRecord[effectChanges.Count];
        int j = 0;
        foreach (var (effect, change) in effectChanges)
        {
            records[j++] = new UniversalEffectChangeRecord.EffectRecord(effect)
            {
                configStore = change.config,
                matrixStore = change.matrix
            };
        }

        return new UniversalEffectChangeRecord(listChanged ? listSnapshot : null, records);
    }

    /// <summary>
    /// Called before a script-invocated effect config change.
    /// </summary>
    /// <param name="effect"></param>
    public void ChangeConfig(Effect effect)
    {
        if (!active) return;

        EffectChangeData? changeData;
        if (!effectChanges.TryGetValue(effect, out changeData))
        {
            changeData = new EffectChangeData();
            effectChanges.Add(effect, changeData);
        }

        changeData.config ??= new EffectConfigData(effect);
    }

    /// <summary>
    /// Called before a script-invocated effect matrix change.
    /// </summary>
    /// <param name="effect"></param>
    public void ChangeMatrix(Effect effect)
    {
        if (!active) return;

        EffectChangeData? changeData;
        if (!effectChanges.TryGetValue(effect, out changeData))
        {
            changeData = new EffectChangeData();
            effectChanges.Add(effect, changeData);
        }

        changeData.matrix ??= (float[,]) effect.Matrix.Clone();
    }
}