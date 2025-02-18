namespace Rained.Rendering;
using Assets;
using LevelData;
using System.Numerics;
using Raylib_cs;

class PropRenderer(LevelEditRender renderInfo)
{
    private readonly LevelEditRender renderInfo = renderInfo;
    private readonly Vector2[] _transformQuads = new Vector2[4];

    public void RenderLayer(int srcLayer, int alpha)
    {
        var viewTl = renderInfo.ViewTopLeft;
        var viewBr = renderInfo.ViewBottomRight;

        var level = RainEd.Instance.Level;

        int srcDepth = srcLayer * 10;

        var rctx = RainEd.RenderContext;
        rctx.CullMode = Glib.CullMode.None;
        var oldRenderFlags = rctx.Flags;

        bool renderPalette;

        // palette rendering mode
        if (renderInfo.UsePalette)
        {
            renderPalette = true;
            renderInfo.Palette.UpdateTexture();
        }

        // normal rendering mode
        else
        {
            renderPalette = false;
        }

        var transformQuads = _transformQuads;
        foreach (var prop in level.Props)
        {
            // don't draw prop if outside of the specified layer
            if (prop.DepthOffset < srcDepth || prop.DepthOffset >= srcDepth + 10)
                continue;
            
            // cull prop if it is outside of the view bounds
            if (prop.Rope is null)
            {
                var aabb = prop.CalcAABB();
                var aabbMin = aabb.Position;
                var aabbMax = aabb.Position + aabb.Size;
                if (aabbMax.X < viewTl.X || aabbMax.Y < viewTl.Y || aabbMin.X > viewBr.X || aabbMin.Y > viewBr.Y)
                {
                    continue;
                }
            }
            
            var quad = prop.QuadPoints;

            // get prop texture, or the placeholder texture if it couldn't be loaded
            var propTexture = RainEd.Instance.AssetGraphics.GetPropTexture(prop.PropInit);

            var variation = prop.Variation == -1 ? 0 : prop.Variation;
            var depthOffset = Math.Max(0, prop.DepthOffset - srcDepth);
        
            // draw missing texture if needed
            if (propTexture is null)
            {
                rctx.Shader = null;
                rctx.ClearRenderFlags(Glib.RenderFlags.DepthTest);
                var srcRect = new Rectangle(Vector2.Zero, 2.0f * Vector2.One);

                var displayTexture = RainEd.Instance.PlaceholderTexture;

                using var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, displayTexture.GlibTexture);
                        
                // top-left
                batch.TexCoord(srcRect.X / displayTexture.Width, srcRect.Y / displayTexture.Height);
                batch.Vertex(quad[0] * Level.TileSize);

                // bottom-left
                batch.TexCoord(srcRect.X / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                batch.Vertex(quad[3] * Level.TileSize);

                // bottom-right
                batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                batch.Vertex(quad[2] * Level.TileSize);

                // top-right
                batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, srcRect.Y / displayTexture.Height);
                batch.Vertex(quad[1] * Level.TileSize);
            }
            else
            {
                SetupPropShader(prop, renderPalette, propTexture.Width, propTexture.Height);
                rctx.SetRenderFlags(Glib.RenderFlags.DepthTest);

                // draw each sublayer of the prop
                for (int depth = prop.PropInit.LayerCount - 1; depth >= 0; depth--)
                {
                    float startFade =
                        (prop.PropInit.Type is PropType.SimpleDecal or PropType.VariedDecal)
                        ? 0.364f : 0f;
                    
                    float whiteFade = Math.Clamp((1f - startFade) * ((depthOffset + depth / 2f) / 10f) + startFade, 0f, 1f);
                    var srcRect = prop.PropInit.GetPreviewRectangle(variation, depth);
                    int sublayer = prop.PropInit.LayerDepths[depth] + prop.DepthOffset;

                    if (renderPalette && rctx.Shader != Shaders.PropShader.GlibShader)
                    {
                        // R channel represents sublayer
                        // A channel is alpha, as usual
                        rctx.DrawColor = new Glib.Color(Math.Clamp(sublayer / 29f, 0f, 1f), 0f, 0f, alpha / 255f);
                    }
                    else
                    {
                        rctx.DrawColor = new Glib.Color(alpha / 255f, whiteFade, 0f, 0f);
                    }

                    var z = LevelEditRender.GetSublayerZCoord(sublayer);
                    transformQuads[0] = quad[0] * Level.TileSize;
                    transformQuads[1] = quad[1] * Level.TileSize;
                    transformQuads[2] = quad[2] * Level.TileSize;
                    transformQuads[3] = quad[3] * Level.TileSize;
                    var dx = transformQuads[1] - transformQuads[0];
                    var dy = transformQuads[3] - transformQuads[0];

                    if (prop.IsAffine)
                    {   
                        propTexture.DrawRectangle(srcRect, new Rectangle(0f, 0f, 1f, 1f), (tex, sr, dr) =>
                        {
                            using var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, tex);
                            Vector2 a, b, c, d;

                            // top-left
                            a = transformQuads[0] + dx * dr.Left + dy * dr.Top;
                            batch.TexCoord(sr.Left / tex.Width, sr.Top / tex.Height);
                            batch.Vertex(a.X, a.Y, z);

                            // bottom-left
                            b = transformQuads[0] + dx * dr.Left + dy * dr.Bottom;
                            batch.TexCoord(sr.Left / tex.Width, sr.Bottom / tex.Height);
                            batch.Vertex(b.X, b.Y, z);

                            // bottom-right
                            c = transformQuads[0] + dx * dr.Right + dy * dr.Bottom;
                            batch.TexCoord(sr.Right / tex.Width, sr.Bottom / tex.Height);
                            batch.Vertex(c.X, c.Y, z);

                            // top-right
                            d = transformQuads[0] + dx * dr.Right + dy * dr.Top;
                            batch.TexCoord(sr.Right / tex.Width, sr.Top / tex.Height);
                            batch.Vertex(d.X, d.Y, z);
                        });
                    }
                    else
                    {
                        var singleTex = propTexture.SingleTexture;
                        if (singleTex is not null)
                        {
                            using var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, singleTex);
                            DrawDeformedMesh(batch, transformQuads, z, new Rectangle(
                                srcRect.X / singleTex.Width,
                                srcRect.Y / singleTex.Height,
                                srcRect.Width / singleTex.Width,
                                srcRect.Height / singleTex.Height)
                            );
                        }
                        else
                        {
                            // quads is for LargeTexture debugging
                            // var dbgQuads = new List<(Vector2 a, Vector2 b, Vector2 c, Vector2 d)>();
                            propTexture.DrawRectangle(srcRect, new Rectangle(0f, 0f, 1f, 1f), (tex, sr, dr) =>
                            {
                                using var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, tex);
                                var quad = transformQuads;

                                var u = dr.Left;
                                var v = dr.Top;
                                var nextU = dr.Right;
                                var nextV = dr.Bottom;
                                
                                Vector2 uPos0 = Vector2.Lerp(quad[0], quad[1], u);
                                Vector2 uPos1 = Vector2.Lerp(quad[3], quad[2], u);
                                Vector2 nextUPos0 = Vector2.Lerp(quad[0], quad[1], nextU);
                                Vector2 nextUPos1 = Vector2.Lerp(quad[3], quad[2], nextU);

                                Vector2 vPos0 = Vector2.Lerp(uPos0, uPos1, v);
                                Vector2 vPos1 = Vector2.Lerp(uPos0, uPos1, nextV);
                                Vector2 vPos2 = Vector2.Lerp(nextUPos0, nextUPos1, nextV);
                                Vector2 vPos3 = Vector2.Lerp(nextUPos0, nextUPos1, v);

                                ReadOnlySpan<Vector2> q = [
                                    new Vector2(vPos0.X, vPos0.Y),
                                    new Vector2(vPos3.X, vPos3.Y),
                                    new Vector2(vPos2.X, vPos2.Y),
                                    new Vector2(vPos1.X, vPos1.Y),
                                ];

                                var imgSize = new Vector2(tex.Width, tex.Height);
                                DrawDeformedMesh(batch, q, z, new Rectangle(sr.Position / imgSize, sr.Size / imgSize));

                                // dbgQuads.Add((q[0], q[1], q[2], q[3]));
                            });

                            // var oldShader = rctx.Shader;
                            // rctx.Shader = null;
                            // foreach (var q in dbgQuads)
                            // {
                            //     rctx.UseGlLines = true;
                            //     rctx.DrawColor = Glib.Color.Black;
                            //     rctx.DrawLine(q.a, q.b);
                            //     rctx.DrawLine(q.b, q.c);
                            //     rctx.DrawLine(q.c, q.d);
                            //     rctx.DrawLine(q.d, q.a);
                            // }
                            // rctx.Shader = oldShader;
                        }
                    }
                }
            }

            rctx.Shader = null;
            rctx.CullMode = Glib.CullMode.None;

            // render segments of rope-type props
            if (prop.Rope is not null)
            {
                var rope = prop.Rope.Model;

                if (rope is not null)
                {
                    var segColor = prop.PropInit.Rope!.PreviewColor;
                    segColor.A = (byte)(segColor.A * alpha / 255f);

                    var depthTestEnabled = rctx.Flags.HasFlag(Glib.RenderFlags.DepthTest);
                    rctx.ClearRenderFlags(Glib.RenderFlags.DepthTest);

                    for (int i = 1; i < rope.SegmentCount; i++)
                    {
                        var newPos = rope.GetSmoothSegmentPos(i);
                        var oldPos = rope.GetSmoothLastSegmentPos(i);
                        var lerpPos = (newPos - oldPos) * prop.Rope.SimulationTimeRemainder + oldPos;
                        var prevNewPos = rope.GetSmoothSegmentPos(i - 1);
                        var prevOldPos = rope.GetSmoothLastSegmentPos(i - 1);
                        var prevLerpPos = (prevNewPos - prevOldPos) * prop.Rope.SimulationTimeRemainder + prevOldPos;

                        rctx.DrawColor = Raylib.ToGlibColor(segColor);
                        rctx.DrawColor.R *= 0.5f;
                        rctx.DrawColor.G *= 0.5f;
                        rctx.DrawColor.B *= 0.5f;
                        rctx.UseGlLines = false;
                        rctx.LineWidth = 3;
                        rctx.DrawLine(lerpPos * Level.TileSize, prevLerpPos * Level.TileSize);
                        rctx.UseGlLines = true;
                        rctx.LineWidth = 1;
                    }

                    for (int i = 0; i < rope.SegmentCount; i++)
                    {
                        var newPos = rope.GetSmoothSegmentPos(i);
                        var oldPos = rope.GetSmoothLastSegmentPos(i);
                        var lerpPos = (newPos - oldPos) * prop.Rope.SimulationTimeRemainder + oldPos;

                        rctx.DrawColor = Raylib.ToGlibColor(segColor);
                        rctx.DrawCircle(lerpPos * Level.TileSize, 2f, 16);
                    }
                    
                    if (depthTestEnabled)
                        rctx.SetRenderFlags(Glib.RenderFlags.DepthTest);
                }
            }
        }
        
        rctx.Shader = null;
        rctx.Flags = oldRenderFlags;
    }

    private void SetupPropShader(Prop prop, bool renderPalette, int texWidth, int texHeight)
    {
        var rctx = RainEd.RenderContext;
        var quad = prop.QuadPoints;

        var isStdProp = prop.PropInit.Type is PropType.Standard or PropType.VariedStandard;

        if (renderPalette)
        {
            // select and configure shader based on prop's color treatment
            if (isStdProp && prop.PropInit.ColorTreatment == PropColorTreatment.Standard)
            {
                rctx.Shader = Shaders.PaletteShader.GlibShader;
            }
            else if (isStdProp && prop.PropInit.ColorTreatment == PropColorTreatment.Bevel)
            {
                rctx.Shader = Shaders.BevelTreatmentShader.GlibShader;
            }
            else if (prop.PropInit.SoftPropRender is not null)
            {
                rctx.Shader = Shaders.SoftPropShader.GlibShader;

                var softProp = prop.PropInit.SoftPropRender.Value;

                // i don't really know how these options work...
                float highlightThreshold = 0.666f;
                float shadowThreshold = 0.333f;
                rctx.Shader.SetUniform("v4_softPropShadeInfo", new Vector4(
                    softProp.ContourExponent,
                    highlightThreshold,
                    shadowThreshold,
                    prop.CustomDepth
                ));
            }
            else
            {
                rctx.Shader = Shaders.PropShader.GlibShader;
            }
        }
        else
        {
            rctx.Shader = Shaders.PropShader.GlibShader;
        }

        // setup shader uniforms
        if (rctx.Shader != Shaders.PropShader.GlibShader)
        {
            if (rctx.Shader.HasUniform("u_paletteTex"))
                rctx.Shader.SetUniform("u_paletteTex", renderInfo.Palette.Texture);
            if (rctx.Shader.HasUniform("v4_textureSize"))
                rctx.Shader.SetUniform("v4_textureSize", new Vector4(texWidth, texHeight, 0f, 0f));
            
            if (rctx.Shader.HasUniform("v4_bevelData"))
            {
                rctx.Shader.SetUniform("v4_bevelData", new Vector4(prop.PropInit.Bevel, 0f, 0f, 0f));
            }
            
            if (rctx.Shader.HasUniform("v4_lightDirection"))
            {
                var level = RainEd.Instance.Level;
                var correctedAngle = level.LightAngle + MathF.PI / 2f;
                var lightDist = 1f - level.LightDistance / 10f;
                var lightZ = lightDist * (3.0f - 0.5f) + 0.5f; // an approximation
                rctx.Shader.SetUniform("v4_lightDirection", new Vector4(MathF.Cos(correctedAngle), MathF.Sin(correctedAngle), lightZ, 0f));
            }
            
            if (rctx.Shader.HasUniform("v4_propRotation"))
            {
                var right = Vector2.Normalize(quad[1] - quad[0]);
                var up = Vector2.Normalize(quad[3] - quad[0]);
                rctx.Shader.SetUniform("v4_propRotation", new Vector4(right.X, right.Y, up.X, up.Y));
            }

            rctx.DrawBatch(); // force flush batch, as uniform changes aren't detected
        }
    }

    private static void DrawDeformedMesh(Glib.BatchDrawHandle batch, ReadOnlySpan<Vector2> quad, float z, Rectangle uvRect)
    {
        const float uStep = 1.0f / 6.0f;
        const float vStep = 1.0f / 6.0f;
        float nextU;
        float nextV;

        for (float u = 0f; u < 1f; u += uStep)
        {
            nextU = Math.Min(u + uStep, 1f);

            Vector2 uPos0 = Vector2.Lerp(quad[0], quad[1], u);
            Vector2 uPos1 = Vector2.Lerp(quad[3], quad[2], u);
            Vector2 nextUPos0 = Vector2.Lerp(quad[0], quad[1], nextU);
            Vector2 nextUPos1 = Vector2.Lerp(quad[3], quad[2], nextU);

            for (float v = 0f; v < 1f; v += vStep)
            {
                nextV = Math.Min(v + vStep, 1f);

                Vector2 vPos0 = Vector2.Lerp(uPos0, uPos1, v);
                Vector2 vPos1 = Vector2.Lerp(uPos0, uPos1, nextV);
                Vector2 vPos2 = Vector2.Lerp(nextUPos0, nextUPos1, nextV);
                Vector2 vPos3 = Vector2.Lerp(nextUPos0, nextUPos1, v);

                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(u, v));
                batch.Vertex(vPos0.X, vPos0.Y, z);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(u, nextV));
                batch.Vertex(vPos1.X, vPos1.Y, z);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(nextU, nextV));
                batch.Vertex(vPos2.X, vPos2.Y, z);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(nextU, v));
                batch.Vertex(vPos3.X, vPos3.Y, z);
            }
        }
    }
}