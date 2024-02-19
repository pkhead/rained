using Raylib_cs;
using RainEd.Light;
using System.Numerics;
namespace RainEd.ChangeHistory;

class LightMapChangeRecord : IChangeRecord
{
    public LightChangeRecorder recorder;

    public BrushAtom[] atoms;
    public LightMapChangeRecord? previous = null;

    public LightMapChangeRecord(BrushAtom[] atoms, LightChangeRecorder recorder)
    {
        this.recorder = recorder;
        this.atoms = atoms;
        previous = recorder.lastStroke;
    }
    
    public void Apply(bool useNew)
    {
        // redo call
        if (useNew)
        {
            recorder.lastStroke = this;
            recorder.Retrace();
        }

        // undo call
        else
        {
            recorder.lastStroke = previous;
            recorder.Retrace();
        }
    }
}

class LightParametersChangeRecord : IChangeRecord
{
    private float oldAngle, oldDist;
    private float newAngle, newDist;

    public LightParametersChangeRecord(
        float oldAngle, float oldDist,
        float newAngle, float newDist
    )
    {
        this.oldAngle = oldAngle;
        this.newAngle = newAngle;
        this.oldDist = oldDist;
        this.newDist = newDist;
    }

    public void Apply(bool useNew)
    {
        RainEd.Instance.Window.EditMode = (int) EditModeEnum.Light;
        var level = RainEd.Instance.Level;

        if (useNew)
        {
            level.LightAngle = newAngle;
            level.LightDistance = newDist;
        }
        else
        {
            level.LightAngle = oldAngle;
            level.LightDistance = oldDist;
        }
    }
}

class LightChangeRecorder : IDisposable
{
    private readonly List<BrushAtom> currentStrokeData = new();
    public LightMapChangeRecord? lastStroke = null;
    private readonly RlManaged.Texture2D origLightmap;
    
    private float oldAngle, oldDist;

    public LightChangeRecorder()
    {
        var lightMapImg = RainEd.Instance.Level.LightMap.GetImage();
        origLightmap = RlManaged.Texture2D.LoadFromImage(lightMapImg);
        lightMapImg.Dispose();
    }

    public void Dispose()
    {
        origLightmap.Dispose();
        lastStroke = null;
    }
    
    public void UpdateParametersSnapshot()
    {
        var level = RainEd.Instance.Level;

        oldAngle = level.LightAngle;
        oldDist = level.LightDistance;
    }

    public void PushParameterChanges()
    {
        var level = RainEd.Instance.Level;

        var newAngle = level.LightAngle;
        var newDist = level.LightDistance;

        if (newAngle != oldAngle || newDist != oldDist)
        {
            RainEd.Instance.ChangeHistory.Push(new LightParametersChangeRecord(
                oldAngle, oldDist,
                newAngle, newDist
            ));

            UpdateParametersSnapshot();
        }
    }

    // record brush atom if last atom is different
    public void RecordAtom(BrushAtom atom)
    {
        if (currentStrokeData.Count == 0 || !atom.Equals(currentStrokeData[^1]))
            currentStrokeData.Add(atom);
    }

    public void ClearStrokeData()
    {
        currentStrokeData.Clear();
    }

    public void EndStroke()
    {
        if (currentStrokeData.Count == 0) return;

        var stroke = new LightMapChangeRecord(currentStrokeData.ToArray(), this);
        currentStrokeData.Clear();

        RainEd.Instance.ChangeHistory.Push(stroke);
        lastStroke = stroke;
    }

    public void Retrace()
    {
        RainEd.Instance.Window.EditMode = (int) EditModeEnum.Light;
        var lightMap = RainEd.Instance.Level.LightMap;

        lightMap.RaylibBeginTextureMode();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(origLightmap, 0, 0, Color.White);
        Raylib.BeginShaderMode(RainEd.Instance.LightBrushDatabase.Shader);
        recurse(lastStroke);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        static void recurse(LightMapChangeRecord? thisStroke)
        {
            if (thisStroke is null) return;
            recurse(thisStroke.previous);

            foreach (var atom in thisStroke.atoms)
            {
                var tex = RainEd.Instance.LightBrushDatabase.Brushes[atom.brush].Texture;
                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    atom.rect,
                    new Vector2(atom.rect.Width, atom.rect.Height) / 2f,
                    atom.rotation,
                    atom.mode ? Color.Black : Color.White
                );
            }
        }
    }
}