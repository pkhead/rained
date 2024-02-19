namespace RainEd.ChangeHistory;

struct EnvironmentData
{
    public int TileSeed;
    public bool HasSunlight;
    public bool HasWater;
    public bool IsWaterInFront;
    public bool DefaultMedium;
    public int WaterLevel;
}

class EnvironmentChangeRecord : IChangeRecord
{
    private EnvironmentData oldData;
    private EnvironmentData newData;
    
    public EnvironmentChangeRecord(EnvironmentData oldData, EnvironmentData newData)
    {
        this.oldData = oldData;
        this.newData = newData;
    }

    public void Apply(bool useNew)
    {
        EnvironmentData data = useNew ? newData : oldData;

        var level = RainEd.Instance.Level;
        level.TileSeed = data.TileSeed;
        level.HasSunlight = data.HasSunlight;
        level.HasWater = data.HasWater;
        level.DefaultMedium = data.DefaultMedium;
        level.IsWaterInFront = data.IsWaterInFront;
        level.WaterLevel = data.WaterLevel;
    }
}

class EnvironmentChangeRecorder
{
    private EnvironmentData snapshot;
    private bool isRecording = false;

    private static EnvironmentData CreateSnapshot()
    {
        var level = RainEd.Instance.Level;
        return new() {
            TileSeed = level.TileSeed,
            HasSunlight = level.HasSunlight,
            HasWater = level.HasWater,
            DefaultMedium = level.DefaultMedium,
            IsWaterInFront = level.IsWaterInFront,
            WaterLevel = level.WaterLevel
        };
    }

    public void BeginChange()
    {
        if (isRecording)
            throw new Exception("MiscChangeRecorder.BeginChange() called twice");
        isRecording = true;
        var level = RainEd.Instance.Level;

        snapshot = CreateSnapshot();
    }

    public void PushChange()
    {
        if (!isRecording)
            throw new Exception("MiscChangeRecorder.PushChange() called twice");
        isRecording = false;

        var newSnapshot = CreateSnapshot();
        if (!newSnapshot.Equals(snapshot))
        {
            RainEd.Instance.ChangeHistory.Push(new EnvironmentChangeRecord(snapshot, newSnapshot));
        }
    }

    public void TryPushChange()
    {
        if (!isRecording) return;
        PushChange();
    }
}