using System.Numerics;
using Rained.LevelData;
using Rained.EditorGui.Editors;
namespace Rained.ChangeHistory;

struct CameraData
{
    public Vector2 Position;
    public float[] CornerOffsets = new float[4];
    public float[] CornerAngles = new float[4];

    public CameraData(Camera camera)
    {
        Position = camera.Position;
        camera.CornerOffsets.CopyTo(CornerOffsets, 0);
        camera.CornerAngles.CopyTo(CornerAngles, 0);
    }

    public readonly void Apply(Camera camera)
    {
        camera.Position = Position;
        CornerOffsets.CopyTo(camera.CornerOffsets, 0);
        CornerAngles.CopyTo(camera.CornerAngles, 0);
    }

    public readonly bool IsEqual(Camera other)
    {
        if (Position != other.Position) return false;

        for (int i = 0; i < 4; i++)
            if (CornerOffsets[i] != other.CornerOffsets[i]) return false;
        
        return true;
    }
}

class CameraChangeRecord : IChangeRecord
{
    public CameraData[] OldData;
    public CameraData[] NewData;
    public bool UserCreated = false;

    public CameraChangeRecord(CameraData[] oldData, CameraData[] newData)
    {
        OldData = oldData;
        NewData = newData;
    }

    public void Apply(bool useNew)
    {
        var level = RainEd.Instance.Level;
        if (UserCreated) RainEd.Instance.LevelView.EditMode = (int) EditModeEnum.Camera;

        var data = useNew ? NewData : OldData;
        if (level.Cameras.Count > data.Length) level.Cameras.RemoveRange(data.Length-1, level.Cameras.Count - data.Length);
        
        for (int i = 0; i < data.Length; i++)
        {
            if (i < level.Cameras.Count)
                data[i].Apply(level.Cameras[i]);
            else
            {
                var cam = new Camera();
                data[i].Apply(cam);
                level.Cameras.Add(cam);
            }
        }
    }
}

class CameraChangeRecorder : ChangeRecorder
{
    private readonly List<CameraData> snapshot;
    private bool isRecording = false;

    public override bool Active => isRecording;
    private bool userCreated;

    public CameraChangeRecorder()
    {
        snapshot = new List<CameraData>();
    }

    public void BeginChange(bool userCreated = true)
    {
        if (isRecording)
        {
            ValidationError("CameraChangeRecorder.BeginChange() is already active");
            return;
        }
        
        isRecording = true;
        this.userCreated = userCreated;

        var level = RainEd.Instance.Level;

        snapshot.Clear();
        for (int i = 0; i < level.Cameras.Count; i++)
        {
            snapshot.Add(new CameraData(level.Cameras[i]));
        }
    }

    public override IChangeRecord? EndChange()
    {
        if (!isRecording) return null;
        IChangeRecord? ret = null;
        var level = RainEd.Instance.Level;

        bool camerasChanged = snapshot.Count != level.Cameras.Count;
        if (!camerasChanged)
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (!snapshot[i].IsEqual(level.Cameras[i]))
                {
                    camerasChanged = true;
                    break;
                }
            }
        
        if (camerasChanged)
        {
            var newCameraData = new CameraData[level.Cameras.Count];
            for (int i = 0; i < level.Cameras.Count; i++)
                newCameraData[i] = new CameraData(level.Cameras[i]);
            
            ret = new CameraChangeRecord([..snapshot], newCameraData)
            {
                UserCreated = userCreated
            };
        }

        isRecording = false;
        return ret;
    }

    public void PushChange()
    {
        if (!isRecording)
        {
            ValidationError("CameraChangeRecorder.PushChange() called, but recorder is not active");
            return;
        }
        
        TryPushChange();
    }
}