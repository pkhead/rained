namespace Rained.Rendering;
using Assets;
using LevelData;
using System.Numerics;
using Raylib_cs;

class PropRenderer(LevelEditRender renderInfo)
{
    private readonly LevelEditRender renderInfo = renderInfo;

    public void RenderLayer(int srcLayer, int alpha)
    {
        var viewTl = renderInfo.ViewTopLeft;
        var viewBr = renderInfo.ViewBottomRight;

        var level = RainEd.Instance.Level;

        int srcDepth = srcLayer * 10;

        var rctx = RainEd.RenderContext;
        rctx.CullMode = Glib.CullMode.None;

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

        Span<Vector2> transformQuads = stackalloc Vector2[4];
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
            var displayTexture = propTexture ?? RainEd.Instance.PlaceholderTexture;

            var variation = prop.Variation == -1 ? 0 : prop.Variation;
            var depthOffset = Math.Max(0, prop.DepthOffset - srcDepth);
        
            // draw missing texture if needed
            if (propTexture is null)
            {
                rctx.Shader = null;
                var srcRect = new Rectangle(Vector2.Zero, 2.0f * Vector2.One);

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
                SetupPropShader(prop, renderPalette, displayTexture);

                // draw each sublayer of the prop
                for (int depth = prop.PropInit.LayerCount - 1; depth >= 0; depth--)
                {
                    float startFade =
                        (prop.PropInit.Type is PropType.SimpleDecal or PropType.VariedDecal)
                        ? 0.364f : 0f;
                    
                    float whiteFade = Math.Clamp((1f - startFade) * ((depthOffset + depth / 2f) / 10f) + startFade, 0f, 1f);
                    var srcRect = prop.PropInit.GetPreviewRectangle(variation, depth);

                    if (renderPalette && rctx.Shader != Shaders.PropShader.GlibShader)
                    {
                        // R channel represents sublayer
                        // A channel is alpha, as usual
                        float sublayer = (float)depth / prop.PropInit.LayerCount * prop.PropInit.Depth + prop.DepthOffset;
                        rctx.DrawColor = new Glib.Color(Math.Clamp(sublayer / 29f, 0f, 1f), 0f, 0f, alpha / 255f);
                    }
                    else
                    {
                        rctx.DrawColor = new Glib.Color(alpha / 255f, whiteFade, 0f, 0f);
                    }
                    
                    using (var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, displayTexture.GlibTexture))
                    {
                        transformQuads[0] = quad[0] * Level.TileSize;
                        transformQuads[1] = quad[1] * Level.TileSize;
                        transformQuads[2] = quad[2] * Level.TileSize;
                        transformQuads[3] = quad[3] * Level.TileSize;

                        if (prop.IsAffine)
                        {
                            // top-left
                            batch.TexCoord(srcRect.X / displayTexture.Width, srcRect.Y / displayTexture.Height);
                            batch.Vertex(transformQuads[0]);

                            // bottom-left
                            batch.TexCoord(srcRect.X / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                            batch.Vertex(transformQuads[3]);

                            // bottom-right
                            batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                            batch.Vertex(transformQuads[2]);

                            // top right
                            batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, srcRect.Y / displayTexture.Height);
                            batch.Vertex(transformQuads[1]);
                        }
                        else
                        {
                            DrawDeformedMesh(batch, transformQuads, new Rectangle(
                                srcRect.X / displayTexture.Width,
                                srcRect.Y / displayTexture.Height,
                                srcRect.Width / displayTexture.Width,
                                srcRect.Height / displayTexture.Height)
                            );
                        };
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

                    for (int i = 0; i < rope.SegmentCount; i++)
                    {
                        var newPos = rope.GetSmoothSegmentPos(i);
                        var oldPos = rope.GetSmoothLastSegmentPos(i);
                        var lerpPos = (newPos - oldPos) * prop.Rope.SimulationTimeRemainder + oldPos;

                        Raylib.DrawCircleV(lerpPos * Level.TileSize, 2f, segColor);
                    }
                }
            }
        }
        
        rctx.Shader = null;
    }

    private void SetupPropShader(Prop prop, bool renderPalette, Texture2D displayTexture)
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
                rctx.Shader.SetUniform("v4_textureSize", new Vector4(displayTexture.Width, displayTexture.Height, 0f, 0f));
            
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

    private static void DrawDeformedMesh(Glib.BatchDrawHandle batch, ReadOnlySpan<Vector2> quad, Rectangle uvRect)
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
                batch.Vertex(vPos0);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(u, nextV));
                batch.Vertex(vPos1);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(nextU, nextV));
                batch.Vertex(vPos2);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(nextU, v));
                batch.Vertex(vPos3);
            }
        }
    }
}