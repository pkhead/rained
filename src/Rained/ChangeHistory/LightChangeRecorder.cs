using Raylib_cs;
using Rained.Assets;
using Rained.LevelData;
using Rained.EditorGui.Editors;
using System.Numerics;
using System.Diagnostics;
namespace Rained.ChangeHistory;

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

class LightmapWarpChangeRecord : IChangeRecord, IDisposable
{
    public LightChangeRecorder recorder;
    public required RlManaged.Image oldImage; // state of lightmap without brush modifications
    public required RlManaged.Image newImage; // state of the lightmap pre-warp
    public Vector2 quadA;
    public Vector2 quadB;
    public Vector2 quadC;
    public Vector2 quadD;

    public void Dispose()
    {
        oldImage.Dispose();
        newImage.Dispose();
    }

    public LightmapWarpChangeRecord(LightChangeRecorder recorder, ReadOnlySpan<Vector2> quad)
    {
        this.recorder = recorder;
        quadA = quad[0];
        quadB = quad[1];
        quadC = quad[2];
        quadD = quad[3];
    }

    public void Apply(bool useNew)
    {
        if (useNew)
        {
            ReadOnlySpan<Vector2> quadArr = [quadA, quadB, quadC, quadD];
            recorder.UpdateImage(newImage, null);
            recorder.ApplyWarp(quadArr);
            recorder.UpdateImage(null, RainEd.Instance.Level.LightMap.GetImage());
        }
        else
        {
            recorder.UpdateImage(newImage, oldImage);
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
        RainEd.Instance.LevelView.EditMode = (int) EditModeEnum.Light;
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

    private readonly RlManaged.Texture2D? origLightmap;
    private readonly RlManaged.Image? origLightmapImg;

    private RlManaged.RenderTexture2D? tmpFramebuffer;
    
    private float oldAngle, oldDist;

    public LightChangeRecorder(RlManaged.Image? lightMapImg)
    {
        if (lightMapImg is not null)
        {
            origLightmapImg = RlManaged.Image.Copy(lightMapImg);
            origLightmap = RlManaged.Texture2D.LoadFromImage(lightMapImg);
        }
    }

    public void Dispose()
    {
        origLightmap?.Dispose();
        origLightmapImg?.Dispose();
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

    public void PushWarpChange(RlManaged.Image preWarpedImage, ReadOnlySpan<Vector2> quads)
    {
        Debug.Assert(quads.Length == 4);

        var lightMap = RainEd.Instance.Level.LightMap;
        Debug.Assert(lightMap.RenderTexture is not null);
        Debug.Assert(origLightmapImg is not null);

        var flippedClone = RlManaged.Image.Copy(preWarpedImage);
        Raylib.ImageFlipVertical(flippedClone);

        var change = new LightmapWarpChangeRecord(this, quads)
        {
            oldImage = RlManaged.Image.Copy(origLightmapImg),
            newImage = flippedClone
        };
        Raylib.ImageDraw(
            origLightmapImg, preWarpedImage,
            new Rectangle(0, 0, preWarpedImage.Width, preWarpedImage.Height),
            new Rectangle(0, 0, preWarpedImage.Width, preWarpedImage.Height),
            Color.White
        );
        Raylib.UpdateTexture(origLightmap!, lightMap.GetImage());
        lastStroke = null;

        RainEd.Instance.ChangeHistory.Push(change);
    }

    public void UpdateImage(RlManaged.Image? newImage, RlManaged.Image? cleanImage)
    {
        var lightMap = RainEd.Instance.Level.LightMap;
        if (lightMap.RenderTexture is null) return;

        if (newImage is not null)
            Raylib.UpdateTexture((Texture2D)lightMap.Texture!, newImage);

        if (origLightmap is not null && cleanImage is not null)
        {
            Raylib.ImageDraw(
                origLightmapImg!, cleanImage,
                new Rectangle(0, 0, cleanImage.Width, cleanImage.Height),
                new Rectangle(0, 0, cleanImage.Width, cleanImage.Height),
                Color.White
            );
            Raylib.UpdateTexture(origLightmap, cleanImage);
        }
    }

    public void ApplyWarp(ReadOnlySpan<Vector2> quads)
    {
        var lightMap = RainEd.Instance.Level.LightMap;
        if (lightMap.RenderTexture is null) return;

        if (tmpFramebuffer is null || tmpFramebuffer.Texture.Width != lightMap.Width || tmpFramebuffer.Texture.Height != lightMap.Height)
        {
            tmpFramebuffer?.Dispose();
            tmpFramebuffer = RlManaged.RenderTexture2D.Load(lightMap.Width, lightMap.Height);
        }

        var rctx = RainEd.RenderContext;

        // render warped image to temporary framebuffer
        rctx.PushTransform();
        rctx.TransformMatrix = Matrix4x4.Identity;
        rctx.PushFramebuffer(((RenderTexture2D)tmpFramebuffer).ID!);

        Raylib.ClearBackground(Color.White);
        Raylib.BeginShaderMode(Shaders.LightStretchShader);
        LightMap.UpdateWarpShaderUniforms(quads);
        RlExt.DrawRenderTexture(lightMap.RenderTexture!, 0, 0, Color.White);
        Raylib.EndShaderMode();

        rctx.PopFramebuffer();

        // render temporary framebuffer to real light map
        rctx.PushFramebuffer(((RenderTexture2D)lightMap.RenderTexture).ID!);
        Raylib.ClearBackground(Color.White);
        RlExt.DrawRenderTexture(tmpFramebuffer, 0, 0, Color.White);

        rctx.PopFramebuffer();
        rctx.PopTransform();
    }

    public void Retrace()
    {
        var lightMap = RainEd.Instance.Level.LightMap;
        if (lightMap.RenderTexture is null || origLightmap is null) return;

        RainEd.Instance.LevelView.EditMode = (int) EditModeEnum.Light;

        lightMap.RaylibBeginTextureMode();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(origLightmap!, 0, 0, Color.White);
        Raylib.BeginShaderMode(Shaders.LevelLightShader);
        recurse(lastStroke);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        static void recurse(LightMapChangeRecord? thisStroke)
        {
            if (thisStroke is null) return;
            recurse(thisStroke.previous);

            foreach (var atom in thisStroke.atoms)
            {
                LightMap.DrawAtom(atom);
            }
        }
    }
}