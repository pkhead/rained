using System;
using Drizzle.Lingo.Runtime;

namespace Drizzle.Logic.Rendering;

public sealed partial class LevelRenderer
{
    private static readonly Vector2i[] FogDisplacements =
    {
        (-2, 0),
        (0, -2),
        (2, 0),
        (0, 2),
        (-1, -1),
        (1, -1),
        (1, 1),
        (-1, 1)
    };

    public void RenderColorsNewFrame()
    {
        var finalImage = _runtime.GetCastMember("finalImage")!.image!;
        var dpImage = _runtime.GetCastMember("dpImage")!.image!;
        var fogImage = _runtime.GetCastMember("fogImage")!.image!;
        var shadowImage = _runtime.GetCastMember("shadowImage")!.image!;
        var rainBowMask = _runtime.GetCastMember("rainBowMask")!.image!;
        var flattenedGradientA = _runtime.GetCastMember("flattenedGradientA")!.image!;
        var flattenedGradientB = _runtime.GetCastMember("flattenedGradientB")!.image!;
        var finalDecalImage = _runtime.GetCastMember("finalDecalImage")!.image!;

        var gAnyDecals = Movie.gAnyDecals > 0;
        var dptsL = (LingoList)Movie.dptsL;
        var fogDptsL = (LingoList)Movie.fogDptsL;
        var gDecalColors = (LingoList)Movie.gDecalColors;
        var grimeActive = Movie.grimeActive > 0;
        var grimeOnGradients = Movie.grimeOnGradients > 0;
        var bkgFix = Movie.bkgFix > 0;

        var c = (int)Movie.c - 1;
        for (var q = 0; q < 1400; q++)
        {
            // NOTE: q and c are shifted by one compared to the original Lingo code.
            // This is much more sane, but keep it in mind.

            var layer = 1;

            var getColor = finalImage.getpixel(q, c);

            if (getColor.GreenByte is > 7 and < 11)
            {
            }
            else if (getColor == new LingoColor(0, 11, 0))
            {
                finalImage.setpixel(q, c, new LingoColor(10, 0, 0));
            }
            else
            {
                if (getColor == LingoColor.White)
                    layer = 0;

                var lowResDepth = dptsL.getpos(dpImage.getpixel(q, c));
                var fgDp = fogDptsL.getpos(fogImage.getpixel(q, c));

                var fogFac = (255 - fogImage.getpixel(q, c).RedByte) / 255.0f;
                fogFac = (fogFac - 0.0275f) / 0.9411f;
                var rainBowFac = 0f;

                if (fogFac <= 0.2f)
                {
                    foreach (var (dpX, dpY) in FogDisplacements)
                    {
                        var dpQ = Math.Clamp(q + dpX, 0, 1339);
                        var dpC = Math.Clamp(c + dpY, 0, 799);
                        var otherFogFac = (255 - fogImage.getpixel(dpQ, dpC).RedByte) / 255.0f;
                        otherFogFac = (otherFogFac - 0.0275f) / 0.9411f;
                        if (Math.Abs(fogFac - otherFogFac) > 0.0333f)
                        {
                            rainBowFac += Math.Clamp(fogFac - otherFogFac, 0, 1) + 1;
                            if (rainBowFac > 5)
                                break;
                        }
                    }
                }

                LingoColor col = default;

                var palCol = 2;
                var effectColor = 0;
                var dark = 0;

                var getColPacked = getColor.BitPack;
                if (getColPacked == new LingoColor(255, 0, 0).BitPack)
                {
                    palCol = 1;
                }
                else if (getColPacked == new LingoColor(0, 255, 0).BitPack)
                {
                    palCol = 2;
                }
                else if (getColPacked == new LingoColor(0, 0, 255).BitPack)
                {
                    palCol = 3;
                }
                else if (getColPacked == new LingoColor(255, 0, 255).BitPack)
                {
                    palCol = 2;
                    effectColor = 1;
                }
                else if (getColPacked == new LingoColor(0, 255, 255).BitPack)
                {
                    palCol = 2;
                    effectColor = 2;
                }
                else if (getColPacked == new LingoColor(255, 150, 255).BitPack)
                {
                    palCol = 3;
                    effectColor = 1;
                }
                else if (getColPacked == new LingoColor(150, 255, 255).BitPack)
                {
                    palCol = 3;
                    effectColor = 2;
                }
                else if (getColPacked == new LingoColor(150, 0, 0).BitPack)
                {
                    palCol = 1;
                    dark = 1;
                }
                else if (getColPacked == new LingoColor(0, 150, 0).BitPack)
                {
                    palCol = 2;
                    dark = 1;
                }
                else if (getColPacked == new LingoColor(0, 0, 150).BitPack)
                {
                    palCol = 3;
                    dark = 1;
                }
                else if (getColPacked == new LingoColor(150, 0, 150).BitPack)
                {
                    palCol = 1;
                    effectColor = 1;
                }
                else if (getColPacked == new LingoColor(0, 150, 150).BitPack)
                {
                    palCol = 1;
                    effectColor = 2;
                }

                if (getColor.GreenByte == 255 && getColor.BlueByte == 150)
                {
                    palCol = 1;
                    effectColor = 3;
                }

                col.RedByte = (byte)(((palCol - 1) * 30) + fgDp);

                if (shadowImage.getpixel(q, c) != default)
                {
                    col.RedByte += 90;
                }

                var greenCol = effectColor;

                if(grimeActive
                    && (grimeOnGradients || greenCol is not 1 or 2 or 3))
                {
                    if (rainBowFac > 5f)
                    {
                        greenCol += 4;
                        RainbowifyPixel(new Vector2i(q, c));
                    }
                    else if (rainBowMask.getpixel(q, c).BitPack != LingoColor.PackWhite)
                    {
                        greenCol += 4;
                    }
                }

                if (effectColor > 0)
                {
                    if (effectColor == 3)
                    {
                        col.BlueByte = getColor.RedByte;
                    }
                    else
                    {
                        var gradient = effectColor == 1 ? flattenedGradientA : flattenedGradientB;
                        col.BlueByte = (byte)(255 - gradient.getpixel(q, c).RedByte);
                    }
                    if(col.BlueByte == 255 && bkgFix)
                    {
                        col.BlueByte = 254;
                    }
                }
                else
                {
                    if (gAnyDecals)
                    {
                        var dcGet = finalDecalImage.getpixel(q, c);
                        if (dcGet != LingoColor.White && dcGet != default)
                        {
                            if (dcGet == Movie.gPEcolors[1][2])
                            {
                                if (!DoesGreenValueMeanRainbow(greenCol))
                                    greenCol += 4;
                            }
                            else
                            {
                                var decalColor = (int) gDecalColors.getpos(dcGet);
                                if (decalColor == 0 && gDecalColors.count < 255)
                                {
                                    gDecalColors.add(dcGet);
                                    decalColor = (int) gDecalColors.count;
                                }

                                col.BlueByte = (byte)(256 - decalColor);
                                greenCol += 8;
                            }
                        }
                    }
                }

                col.GreenByte = (byte)(greenCol + dark * 16);

                finalImage.setpixel(q, c, layer == 0 ? LingoColor.White : col);
            }
        }

        Movie.c += 1;

        if (Movie.c > 800)
        {
            Movie.c += 1;
            Movie.keepLooping = (LingoNumber)0;
        }

        void RainbowifyPixel(Vector2i pxl)
        {
            if (pxl.X < 1 || pxl.Y < 1)
                return;

            if (!IsPixelInFinalImageRainbowed(pxl + (-1, 0)))
            {
                var currCol = finalImage.getpixel(pxl.X - 1, pxl.Y);
                MathHelper.SatAdd(ref currCol.GreenByte, 4);
                finalImage.setpixel(pxl.X - 1, pxl.Y, currCol);
            }

            if (!IsPixelInFinalImageRainbowed(pxl + (0, -1)))
            {
                var currCol = finalImage.getpixel(pxl.X, pxl.Y - 1);
                MathHelper.SatAdd(ref currCol.GreenByte, 4);
                finalImage.setpixel(pxl.X, pxl.Y - 1, currCol);
            }

            rainBowMask.setpixel(pxl.X+1, pxl.Y, default);
            rainBowMask.setpixel(pxl.X, pxl.Y+1, default);
        }

        bool IsPixelInFinalImageRainbowed(Vector2i pxl)
        {
            if (pxl.X < 0 || pxl.Y < 0)
                return false;

            var pxlColor = finalImage.getpixel(pxl.X, pxl.Y);
            if (pxlColor == LingoColor.White)
                return false;

            return DoesGreenValueMeanRainbow(pxlColor.GreenByte);
        }

        bool DoesGreenValueMeanRainbow(int grn)
        {
            return grn is (> 3 and < 8) or (> 11 and < 16);
        }
    }
}
