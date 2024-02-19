using Raylib_cs;
using RainEd.Light;
using System.Numerics;
namespace RainEd.ChangeHistory;

class LightChangeRecord : IChangeRecord
{
    public LightChangeRecorder recorder;

    public BrushAtom[] atoms;
    public LightChangeRecord? previous = null;

    public LightChangeRecord(BrushAtom[] atoms, LightChangeRecorder recorder)
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

class LightChangeRecorder : IDisposable
{
    private readonly List<BrushAtom> currentStrokeData = new();
    public LightChangeRecord? lastStroke = null;
    private readonly RlManaged.Texture2D origLightmap;

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

    /*public void ReloadLevel(RlManaged.RenderTexture2D lightmapRt, RlManaged.Texture2D origLightmap)
    {
        lastStroke = null;
        this.lightmapRt = lightmapRt;
        this.origLightmap = origLightmap;
    }*/

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

        var stroke = new LightChangeRecord(currentStrokeData.ToArray(), this);
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

        static void recurse(LightChangeRecord? thisStroke)
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