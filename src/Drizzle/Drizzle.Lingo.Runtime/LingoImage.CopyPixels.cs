using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using SixLabors.ImageSharp.PixelFormats;

namespace Drizzle.Lingo.Runtime;

public sealed unsafe partial class LingoImage
{
    // Not used by the editor (but there is a similar lingo API)
    // Mostly just for unit tests right now.
    // Maybe optimizations for the editor later.
    public void fill(LingoColor color)
    {
        CopyIfShared();

        switch (Type)
        {
            case ImageType.B8G8R8A8:
                FillCore<PixelOpsBgra32, Bgra32>(this, color);
                break;
            case ImageType.B5G5R5A1:
                FillCore<PixelOpsBgra5551, Bgra5551>(this, color);
                break;
            case ImageType.Palette8:
                FillCore<PixelOpsPalette8, L8>(this, color);
                break;
            case ImageType.Palette1:
                FillCore<PixelOpsBit, int>(this, color);
                break;
            case ImageType.L8:
                FillCore<PixelOpsL8, L8>(this, color);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private static void FillCore<TWriter, TDstData>(LingoImage dst, LingoColor color)
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : struct
    {
        var dstSpan = MemoryMarshal.Cast<byte, TDstData>(dst.ImageBuffer);
        TWriter.Fill(dstSpan, color.BitPack);
    }


    public void copypixels(LingoImage source, LingoList? destQuad, LingoRect sourceRect, LingoPropertyList paramList)
    {
        if (destQuad == null)
            return;

        ParseCommonCopyPixelsParameters(paramList, out var parameters);

        var quad = new DestQuad
        {
            TopLeft = ((LingoPoint)destQuad.List[0]!).AsVector2,
            TopRight = ((LingoPoint)destQuad.List[1]!).AsVector2,
            BottomRight = ((LingoPoint)destQuad.List[2]!).AsVector2,
            BottomLeft = ((LingoPoint)destQuad.List[3]!).AsVector2,
        };

        CopyPixelsQuadImpl(source, this, quad, sourceRect, parameters);
    }

    public void copypixels(LingoImage source, LingoRect destRect, LingoRect sourceRect)
    {
        var parameters = new CopyPixelsParameters
        {
            Blend = 1,
            Ink = CopyPixelsInk.Copy
        };

        CopyPixelsImpl(source, this, destRect, sourceRect, parameters);
    }

    public void copypixels(LingoImage source, LingoRect destRect, LingoRect sourceRect, LingoPropertyList paramList)
    {
        ParseCommonCopyPixelsParameters(paramList, out var parameters);

        CopyPixelsImpl(source, this, destRect, sourceRect, parameters);
    }

    private static void ParseCommonCopyPixelsParameters(
        LingoPropertyList paramList,
        out CopyPixelsParameters parameters)
    {
        parameters = default;
        if (paramList.Dict.TryGetValue(new LingoSymbol("blend"), out var blendValObj))
        {
            var dec = (LingoNumber)blendValObj!;
            parameters.Blend = (float)(dec.DecimalValue / 100f);
        }
        else
        {
            parameters.Blend = 1;
        }

        if (paramList.Dict.TryGetValue(new LingoSymbol("color"), out var colorObj))
            parameters.ForeColor = (LingoColor)colorObj!;

        if (paramList.Dict.TryGetValue(new LingoSymbol("ink"), out var inkVal))
            parameters.Ink = (CopyPixelsInk)(int)inkVal!;

        if (paramList.Dict.TryGetValue(new LingoSymbol("mask"), out var mask)
            || paramList.Dict.TryGetValue(new LingoSymbol("maskImage"), out mask)
            || paramList.Dict.TryGetValue(new LingoSymbol("maskimage"), out mask))
            parameters.Mask = (LingoMask?)mask;
    }

    private static void CopyPixelsQuadImpl(
        LingoImage source,
        LingoImage dest,
        in DestQuad destQuad,
        LingoRect sourceRect,
        in CopyPixelsParameters parameters)
    {
        dest.CopyIfShared();
        var srcBox = CalcSrcBox(source, sourceRect);

        // Guard against edge case in InvBilinear when TopLeft equals TopRight
        if (Vector2.DistanceSquared(destQuad.TopLeft, destQuad.TopRight) < 0.1f)
        {
            DestQuad correctedQuad = destQuad;
            correctedQuad.TopRight += Vector2.Normalize(destQuad.BottomRight - destQuad.TopRight) * 0.25f;
            CopyPixelsQuadGenWriter(source, dest, correctedQuad, srcBox, parameters);
        }
        else
        {
            CopyPixelsQuadGenWriter(source, dest, destQuad, srcBox, parameters);
        }
    }

    private static void CopyPixelsQuadGenWriter(
        LingoImage src, LingoImage dst,
        in DestQuad destQuad,
        Vector4 srcBox,
        in CopyPixelsParameters parameters)
    {
        switch (dst.Type)
        {
            case ImageType.B8G8R8A8:
                CopyPixelsQuadGenSampler<Bgra32, PixelOpsBgra32>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.B5G5R5A1:
                CopyPixelsQuadGenSampler<Bgra5551, PixelOpsBgra5551>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.Palette8:
                CopyPixelsQuadGenSampler<L8, PixelOpsPalette8>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.Palette1:
                CopyPixelsQuadGenSampler<int, PixelOpsBit>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.L8:
                CopyPixelsQuadGenSampler<L8, PixelOpsL8>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private static void CopyPixelsQuadGenSampler<TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        in DestQuad destQuad,
        Vector4 srcBox,
        in CopyPixelsParameters parameters)
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        switch (src.Type)
        {
            case ImageType.B8G8R8A8:
                CopyPixelsQuadCore<Bgra32, PixelOpsBgra32, TDstData, TWriter>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.B5G5R5A1:
                CopyPixelsQuadCore<Bgra5551, PixelOpsBgra5551, TDstData, TWriter>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.Palette8:
                CopyPixelsQuadCore<L8, PixelOpsPalette8, TDstData, TWriter>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.Palette1:
                CopyPixelsQuadCore<int, PixelOpsBit, TDstData, TWriter>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            case ImageType.L8:
                CopyPixelsQuadCore<L8, PixelOpsL8, TDstData, TWriter>(
                    src,
                    dst,
                    destQuad,
                    srcBox,
                    parameters);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private static void CopyPixelsQuadCore<TSrcData, TSampler, TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        in DestQuad destQuad,
        Vector4 srcBox,
        in CopyPixelsParameters parameters)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        // Advanced copy features not implemented on AVX code path, they're rare so it's fine probably.
        var mustScalar = parameters.Ink == CopyPixelsInk.Darkest || parameters.Mask != null;
        if (Avx2.IsSupported && !mustScalar)
        {
            CopyPixelsQuadCoreAvx2<TSrcData, TSampler, TDstData, TWriter>(
                src, dst,
                destQuad, srcBox,
                parameters);
        }
        else
            CopyPixelsQuadCoreScalar<TSrcData, TSampler, TDstData, TWriter>(
                src, dst,
                destQuad, srcBox,
                parameters);
    }

    private static void CopyPixelsQuadCoreScalar<TSrcData, TSampler, TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        in DestQuad destQuad,
        Vector4 srcBox,
        in CopyPixelsParameters parameters)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        var srcImgW = src.Width;
        var srcImgH = src.Height;
        var dstImgW = dst.Width;
        var dstImgH = dst.Height;

        var boundsTL = Vector2.Min(
            destQuad.TopLeft,
            Vector2.Min(
                destQuad.TopRight,
                Vector2.Min(destQuad.BottomLeft, destQuad.BottomRight)));

        var boundsBR = Vector2.Max(
            destQuad.TopLeft,
            Vector2.Max(
                destQuad.TopRight,
                Vector2.Max(destQuad.BottomLeft, destQuad.BottomRight)));

        var boundL = Math.Clamp((int)boundsTL.X, 0, dstImgW);
        var boundT = Math.Clamp((int)boundsTL.Y, 0, dstImgH);
        var boundR = Math.Clamp((int)MathF.Ceiling(boundsBR.X), 0, dstImgW);
        var boundB = Math.Clamp((int)MathF.Ceiling(boundsBR.Y), 0, dstImgH);

        var doBackgroundTransparent = parameters.Ink == CopyPixelsInk.BackgroundTransparent;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var doBlend = parameters.Blend != 1;
        var fgc = parameters.ForeColor;

        ReadOnlySpan<TSrcData> srcSpan = MemoryMarshal.Cast<byte, TSrcData>(src.ImageBuffer);
        var dstSpan = MemoryMarshal.Cast<byte, TDstData>(dst.ImageBuffer);

        var a = destQuad.TopLeft;
        var b = destQuad.TopRight;
        var c = destQuad.BottomRight;
        var d = destQuad.BottomLeft;

        // Pull out parameters in InvBilinear that do not change for the quad.
        Vector2 k1 = c - d + a - b;
        float k2 = -4f * Cross2d(k1, d - a);
        float k3 = Cross2d(a - d, b - a);
        Vector2 k4 = b - a;
        Vector2 k5 = d - a;

        for (int y = boundT; y < boundB; y++)
        {
            for (int x = boundL; x < boundR; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);

                var st = InvBilinear(p, a, k1, k2, k3, k4, k5);

                if (MathF.Max(MathF.Abs(st.X - 0.5f), MathF.Abs(st.Y - 0.5f)) < 0.5f)
                {
                    st.X = Lerp(srcBox.X, srcBox.Z, st.X);
                    st.Y = Lerp(srcBox.Y, srcBox.W, st.Y);

                    var dstPos = dstImgW * y + x;

                    var imgRow = (int)(st.Y * srcImgH) * srcImgW;
                    var color = DoSample<TSrcData, TSampler>(st.X, st.Y, srcImgW, srcSpan, imgRow);

                    if (doBackgroundTransparent && color == LingoColor.PackWhite)
                        continue;

                    CopyPixelsCoreDoOutputScalar<TSrcData, TSampler, TDstData, TWriter>(
                        parameters, color, fgc, doBlend, dstSpan, dstPos);
                }
            }
        }

        static float Lerp(float x, float y, float a) => x + a * (y - x);

        // https://www.desmos.com/calculator/pkkdfmvfyw
        // https://people.csail.mit.edu/bkph/articles/Quadratics.pdf
        static Vector2 InvBilinear(Vector2 p, Vector2 a, Vector2 k1, float k2, float k3, Vector2 k4, Vector2 k5)
        {
            float b = k3 - Cross2d(k1, p - a);
            float c = Cross2d(p - a, k4);
            float rad = MathF.Sqrt(b * b + k2 * c);
            float v1 = -2f * c / (b + rad);
            Vector2 e1 = p - a - k5 * v1;
            float u1 = Vector2.Dot(e1, e1) / Vector2.Dot(k4 + k1 * v1, e1);
            float v2 = -2f * c / (b - rad);
            Vector2 e2 = p - a - k5 * v2;
            float u2 = Vector2.Dot(e2, e2) / Vector2.Dot(k4 + k1 * v2, e2);
            return MathF.Max(MathF.Abs(u1 - 0.5f), MathF.Abs(v1 - 0.5f)) <= 0.5f ? new Vector2(u1, v1) : new Vector2(u2, v2);
        }
    }

    private static float Cross2d(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static void CopyPixelsQuadCoreAvx2<TSrcData, TSampler, TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        in DestQuad destQuad,
        Vector4 srcBox,
        in CopyPixelsParameters parameters)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        var srcImgW = src.Width;
        var srcImgH = src.Height;
        var dstImgW = dst.Width;
        var dstImgH = dst.Height;

        var boundsTL = Vector2.Min(
            destQuad.TopLeft,
            Vector2.Min(
                destQuad.TopRight,
                Vector2.Min(destQuad.BottomLeft, destQuad.BottomRight)));

        var boundsBR = Vector2.Max(
            destQuad.TopLeft,
            Vector2.Max(
                destQuad.TopRight,
                Vector2.Max(destQuad.BottomLeft, destQuad.BottomRight)));

        var boundL = Math.Clamp((int)boundsTL.X, 0, dstImgW);
        var boundT = Math.Clamp((int)boundsTL.Y, 0, dstImgH);
        var boundR = Math.Clamp((int)MathF.Ceiling(boundsBR.X), 0, dstImgW);
        var boundB = Math.Clamp((int)MathF.Ceiling(boundsBR.Y), 0, dstImgH);

        var doBackgroundTransparent = parameters.Ink == CopyPixelsInk.BackgroundTransparent;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var doBlend = parameters.Blend != 1;
        var fgc = parameters.ForeColor;
        var fgVec = Vector256.Create(fgc.BitPack).AsByte();

        ReadOnlySpan<TSrcData> srcSpan = MemoryMarshal.Cast<byte, TSrcData>(src.ImageBuffer);
        var dstSpan = MemoryMarshal.Cast<byte, TDstData>(dst.ImageBuffer);

        var a = destQuad.TopLeft;
        var b = destQuad.TopRight;
        var c = destQuad.BottomRight;
        var d = destQuad.BottomLeft;

        // Pull out parameters in InvBilinear that do not change for the quad.
        Vector2 k1 = c - d + a - b;
        float k2 = -4f * Cross2d(k1, d - a);
        float k3 = Cross2d(a - d, b - a);
        Vector2 k4 = b - a;
        Vector2 k5 = d - a;

        var posMask = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
        var widthVec = Vector256.Create(boundR);

        for (var y = boundT; y < boundB; y++)
        {
            var yVec = Vector256.Create(y + 0.5f);

            for (var x = boundL; x < boundR; x += 8)
            {
                var pos = Avx2.Add(posMask, Vector256.Create(x));
                var writeMask = Avx2.CompareGreaterThan(widthVec, pos);

                var xVec = Avx.Add(Avx.ConvertToVector256Single(pos), Vector256.Create(0.5f));
                var (s, t) = QuadInvBilinearAvx2(xVec, yVec, a, k1, k2, k3, k4, k5);

                var vecHalf = Vector256.Create(0.5f);
                var maskNeg = Vector256.Create(-0.0f);

                // Pixels in mask = in quad.
                writeMask = Avx2.And(writeMask, Avx.And(
                    Avx.CompareLessThan(Avx.AndNot(maskNeg, Avx.Subtract(s, vecHalf)), vecHalf),
                    Avx.CompareLessThan(Avx.AndNot(maskNeg, Avx.Subtract(t, vecHalf)), vecHalf)
                ).AsInt32());

                if (writeMask.Equals(Vector256<int>.Zero))
                    continue;

                // apply srcBox via simple lerp.
                s = Lerp(Vector256.Create(srcBox.X), Vector256.Create(srcBox.Z), s);
                t = Lerp(Vector256.Create(srcBox.Y), Vector256.Create(srcBox.W), t);

                // The above transformation for srcBox can actually cause us to go outside the legal source box.
                // So we need another mask just for reading.
                var readMask = Avx2.And(
                    Avx.And(
                        Avx.CompareLessThan(Avx.AndNot(maskNeg, Avx.Subtract(s, vecHalf)), vecHalf),
                        Avx.CompareLessThan(Avx.AndNot(maskNeg, Avx.Subtract(t, vecHalf)), vecHalf)
                    ).AsInt32(), writeMask);

                var imgX = Avx.ConvertToVector256Int32WithTruncation(Avx.Multiply(s, Vector256.Create((float)srcImgW)));
                var imgY = Avx.ConvertToVector256Int32WithTruncation(Avx.Multiply(t, Vector256.Create((float)srcImgH)));

                var imgPos = Avx2.Add(Avx2.MultiplyLow(imgY, Vector256.Create(srcImgW)), imgX);

                var posL = imgPos.GetLower();
                var maskL = readMask.GetLower();
                var l0 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(0), maskL.GetElement(0), srcSpan);
                var l1 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(1), maskL.GetElement(1), srcSpan);
                var l2 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(2), maskL.GetElement(2), srcSpan);
                var l3 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(3), maskL.GetElement(3), srcSpan);
                var lowerSample = Vector128.Create(l0, l1, l2, l3);

                var posU = imgPos.GetUpper();
                var maskU = readMask.GetUpper();
                var u0 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(0), maskU.GetElement(0), srcSpan);
                var u1 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(1), maskU.GetElement(1), srcSpan);
                var u2 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(2), maskU.GetElement(2), srcSpan);
                var u3 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(3), maskU.GetElement(3), srcSpan);
                var upperSample = Vector128.Create(u0, u1, u2, u3);

                var color = Vector256.Create(lowerSample, upperSample);

                CopyPixelsCoreDoOutputAvx2<TDstData, TWriter>(
                    doBackgroundTransparent,
                    writeMask,
                    color,
                    fgVec,
                    dstImgW,
                    x, y,
                    doBlend,
                    parameters,
                    dstSpan);
            }
        }

        static Vector256<float> Lerp(Vector256<float> x, Vector256<float> y, Vector256<float> a) =>
            Avx.Add(x, Avx.Multiply(a, Avx.Subtract(y, x)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CoreAvx2DoMaskedSample<TSrcData, TSampler>(
        int pos,
        int mask,
        ReadOnlySpan<TSrcData> srcSpan)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
    {
        if (mask == 0)
            return LingoColor.PackWhite;

        return TSampler.Sample(srcSpan, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Cross2dAvx2(
        Vector256<float> aX, Vector256<float> aY, Vector256<float> bX, Vector256<float> bY) =>
        aX * bY - aY * bX;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> Dot2dAvx2(
        Vector256<float> aX, Vector256<float> aY, Vector256<float> bX, Vector256<float> bY) =>
        aX * bX + aY * bY;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (Vector256<float> s, Vector256<float> t) QuadInvBilinearAvx2(
        Vector256<float> pX,
        Vector256<float> pY,
        Vector2 a,
        Vector2 k1,
        float k2,
        float k3,
        Vector2 k4,
        Vector2 k5)
    {
        // Please refer to the scalar version up above to have the slightest of a clue what is going on here.
        var aX = Vector256.Create(a.X);
        var aY = Vector256.Create(a.Y);

        var k1x = Vector256.Create(k1.X);
        var k1y = Vector256.Create(k1.Y);

        var k2v = Vector256.Create(k2);

        var k3v = Vector256.Create(k3);

        var k4x = Vector256.Create(k4.X);
        var k4y = Vector256.Create(k4.Y);

        var k5x = Vector256.Create(k5.X);
        var k5y = Vector256.Create(k5.Y);

        var pmaX = Avx.Subtract(pX, aX);
        var pmaY = Avx.Subtract(pY, aY);

        var b = k3v - Cross2dAvx2(k1x, k1y, pmaX, pmaY);
        var c = Cross2dAvx2(pmaX, pmaY, k4x, k4y);
        var rad = Avx.Sqrt(b * b + k2v * c);

        var v1 = -2f * c / (b + rad); // Front facing result
        var e1X = pmaX - k5x * v1;
        var e1Y = pmaY - k5y * v1;
        var u1 = Dot2dAvx2(e1X, e1Y, e1X, e1Y) / Dot2dAvx2(k4x + k1x * v1, k4y + k1y * v1, e1X, e1Y);

        var oobMask = Avx.Or(
                Avx.Or(Avx.CompareLessThan(u1, Vector256<float>.Zero), Avx.CompareGreaterThan(u1, Vector256.Create(1f))),
                Avx.Or(Avx.CompareLessThan(v1, Vector256<float>.Zero), Avx.CompareGreaterThan(v1, Vector256.Create(1f))));

        if (oobMask == Vector256<float>.Zero)
            return (u1, v1);

        var v2 = -2f * c / (b - rad); // Back facing result
        var e2X = pmaX - k5x * v2;
        var e2Y = pmaY - k5y * v2;
        var u2 = Dot2dAvx2(e2X, e2Y, e2X, e2Y) / Dot2dAvx2(k4x + k1x * v2, k4y + k1y * v2, e2X, e2Y);
        return (Avx.BlendVariable(u1, u2, oobMask), Avx.BlendVariable(v1, v2, oobMask));
    }

    private static void CopyPixelsImpl(
        LingoImage source,
        LingoImage dest,
        LingoRect destRect,
        LingoRect sourceRect,
        in CopyPixelsParameters parameters)
    {
        dest.CopyIfShared();
        Debug.Assert(!dest.IsPxl);

        // Integer coordinates for the purpose of rasterization.
        var dstL = (int)destRect.left;
        var dstT = (int)destRect.top;
        var dstR = (int)destRect.right;
        var dstB = (int)destRect.bottom;

        if (dstL > dest.width || dstT > dest.height || dstR < 0 || dstB < 0)
        {
            //Log.Debug("copyPixels(): ignoring complete out-of-bounds write.");
            return;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        // todo: make sure to not apply this when mask is set.
        if (source.IsPxl && parameters.Blend == 1 &&
            parameters.Ink is CopyPixelsInk.Copy or CopyPixelsInk.BackgroundTransparent)
        {
            // CopiesPixelFast += 1;
            CopyPixelsPxlRectGenWriter(dest, (dstL, dstT, dstR, dstB), parameters);
            return;
        }

        var srcBox = CalcSrcBox(source, sourceRect);

        CopyPixelsRectGenWriter(source, dest, srcBox, (dstL, dstT, dstR, dstB), parameters);
    }

    private static Vector4 CalcSrcBox(LingoImage source, LingoRect sourceRect)
    {
        // Float coordinates for the purposes of sampling.
        var srcL = (float)(sourceRect.left / source.width.DecimalValue);
        var srcT = (float)(sourceRect.top / source.height.DecimalValue);
        var srcR = (float)(sourceRect.right / source.width.DecimalValue);
        var srcB = (float)(sourceRect.bottom / source.height.DecimalValue);

        // LTRB
        var srcBox = new Vector4(srcL, srcT, srcR, srcB);
        return srcBox;
    }

    private static void CopyPixelsRectGenWriter(
        LingoImage src, LingoImage dst,
        Vector4 srcBox,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
    {
        switch (dst.Type)
        {
            case ImageType.B8G8R8A8:
                CopyPixelsRectGenSampler<Bgra32, PixelOpsBgra32>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.B5G5R5A1:
                CopyPixelsRectGenSampler<Bgra5551, PixelOpsBgra5551>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.Palette8:
                CopyPixelsRectGenSampler<L8, PixelOpsPalette8>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.Palette1:
                CopyPixelsRectGenSampler<int, PixelOpsBit>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.L8:
                CopyPixelsRectGenSampler<L8, PixelOpsL8>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private static void CopyPixelsRectGenSampler<TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        Vector4 srcBox,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        switch (src.Type)
        {
            case ImageType.B8G8R8A8:
                CopyPixelsRectCoreCopy<Bgra32, PixelOpsBgra32, TDstData, TWriter>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.B5G5R5A1:
                CopyPixelsRectCoreCopy<Bgra5551, PixelOpsBgra5551, TDstData, TWriter>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.Palette8:
                CopyPixelsRectCoreCopy<L8, PixelOpsPalette8, TDstData, TWriter>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.Palette1:
                CopyPixelsRectCoreCopy<int, PixelOpsBit, TDstData, TWriter>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            case ImageType.L8:
                CopyPixelsRectCoreCopy<L8, PixelOpsL8, TDstData, TWriter>(
                    src,
                    dst,
                    srcBox,
                    dstBox,
                    parameters);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    // Struct generics for static dispatch.
    private static void CopyPixelsRectCoreCopy<TSrcData, TSampler, TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        Vector4 srcBox,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        // Advanced copy features not implemented on AVX code path, they're rare so it's fine probably.
        var mustScalar = parameters.Ink == CopyPixelsInk.Darkest || parameters.Mask != null;
        if (Avx2.IsSupported && !mustScalar)
            CopyPixelsRectCoreCopyAvx2<TSrcData, TSampler, TDstData, TWriter>(
                src, dst,
                srcBox, dstBox,
                parameters);
        else
            CopyPixelsRectCoreCopyScalar<TSrcData, TSampler, TDstData, TWriter>(
                src, dst,
                srcBox, dstBox,
                parameters);
    }

    private static void CopyPixelsRectCoreCopyScalar<TSrcData, TSampler, TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        Vector4 srcBox,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        var (dstL, dstT, dstR, dstB) = dstBox;

        var (initS, initT, incSrcS, incSrcT) =
            CopyPixelsRectCoreCopyCalcSampleCoords(srcBox, dstL, dstT, dstR, dstB);

        var srcImgW = src.Width;
        var srcImgH = src.Height;
        var dstImgW = dst.Width;
        var dstImgH = dst.Height;

        var doBackgroundTransparent = parameters.Ink == CopyPixelsInk.BackgroundTransparent;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var doBlend = parameters.Blend != 1;
        var fgc = parameters.ForeColor;

        CopyPixelsRectCoreCopyClampDst(ref dstL, ref dstR, ref initS, incSrcS, dstImgW);
        CopyPixelsRectCoreCopyClampDst(ref dstT, ref dstB, ref initT, incSrcT, dstImgH);

        ReadOnlySpan<TSrcData> srcSpan = MemoryMarshal.Cast<byte, TSrcData>(src.ImageBuffer);
        var dstSpan = MemoryMarshal.Cast<byte, TDstData>(dst.ImageBuffer);

        var t = initT;
        for (var y = dstT; y < dstB; y++, t += incSrcT)
        {
            var imgRow = (int)(t * srcImgH) * srcImgW;
            var s = initS;
            for (var x = dstL; x < dstR; x++, s += incSrcS)
            {
                var dstPos = dstImgW * y + x;

                if (parameters.Mask is { } mask)
                {
                    var maskX = (int)(s * srcImgW);
                    var maskY = (int)(t * srcImgH);

                    if (maskX < 0 || maskY < 0 || maskX >= mask.Width || maskY >= mask.Height)
                        continue;

                    if (DoBitRead(MemoryMarshal.Cast<byte, int>(mask.Data), mask.Width * maskY + maskX))
                        continue;
                }

                var color = DoSample<TSrcData, TSampler>(s, t, srcImgW, srcSpan, imgRow);

                if (doBackgroundTransparent && color == LingoColor.PackWhite)
                    continue;

                CopyPixelsCoreDoOutputScalar<TSrcData, TSampler, TDstData, TWriter>(
                    parameters, color, fgc, doBlend, dstSpan, dstPos);
            }
        }
    }

    private static void CopyPixelsCoreDoOutputScalar<TSrcData, TSampler, TDstData, TWriter>(
        in CopyPixelsParameters parameters,
        int color,
        LingoColor fgc,
        bool doBlend,
        Span<TDstData> dstSpan,
        int dstPos)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        var unpacked = LingoColor.BitUnpack(color);
        int r = unpacked.RedByte;
        int g = unpacked.GreenByte;
        int b = unpacked.BlueByte;

        r = Math.Min(0xFF, r + fgc.RedByte);
        g = Math.Min(0xFF, g + fgc.GreenByte);
        b = Math.Min(0xFF, b + fgc.BlueByte);

        if (doBlend)
        {
            var unpackedDst = LingoColor.BitUnpack(TWriter.Sample(dstSpan, dstPos));

            var blendSrc = new Vector4(r, g, b, 0);
            var blendDst = new Vector4(unpackedDst.RedByte, unpackedDst.GreenByte, unpackedDst.BlueByte, 0);

            var final = blendSrc * parameters.Blend + blendDst * (1 - parameters.Blend);

            r = (int)final.X;
            g = (int)final.Y;
            b = (int)final.Z;
        }
        else if (parameters.Ink == CopyPixelsInk.Darkest)
        {
            var existing = LingoColor.BitUnpack(TWriter.Sample(dstSpan, dstPos));

            r = Math.Min(r, existing.RedByte);
            g = Math.Min(g, existing.GreenByte);
            b = Math.Min(b, existing.BlueByte);
        }

        TWriter.Write(dstSpan, dstPos, new LingoColor(r, g, b).BitPack);
    }

    private static int DoSample<TSrcData, TSampler>(
        float s,
        float t,
        int srcImgW,
        ReadOnlySpan<TSrcData> srcSpan,
        int imgRow)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
    {
        if (s < 0 || s >= 1 || t < 0 || t >= 1)
            return LingoColor.PackWhite;

        var imgX = (int)(s * srcImgW);

        return TSampler.Sample(srcSpan, imgRow + imgX);
    }

    private static void CopyPixelsRectCoreCopyAvx2<TSrcData, TSampler, TDstData, TWriter>(
        LingoImage src, LingoImage dst,
        Vector4 srcBox,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
        where TSampler : struct, IPixelOps<TSrcData>
        where TSrcData : unmanaged
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        var (dstL, dstT, dstR, dstB) = dstBox;

        var (initS, initT, incSrcS, incSrcT) =
            CopyPixelsRectCoreCopyCalcSampleCoords(srcBox, dstL, dstT, dstR, dstB);

        ReadOnlySpan<TSrcData> srcSpan = MemoryMarshal.Cast<byte, TSrcData>(src.ImageBuffer);
        var dstSpan = MemoryMarshal.Cast<byte, TDstData>(dst.ImageBuffer);

        var srcImgW = src.Width;
        var srcImgH = src.Height;
        var dstImgW = dst.Width;
        var dstImgH = dst.Height;

        CopyPixelsRectCoreCopyClampDst(ref dstL, ref dstR, ref initS, incSrcS, dstImgW);
        CopyPixelsRectCoreCopyClampDst(ref dstT, ref dstB, ref initT, incSrcT, dstImgH);

        var doBackgroundTransparent = parameters.Ink == CopyPixelsInk.BackgroundTransparent;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var doBlend = parameters.Blend != 1;
        var fgc = parameters.ForeColor;
        var fgVec = Vector256.Create(fgc.BitPack).AsByte();

        var vecIncS = Avx.Multiply(Vector256.Create(0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f), Vector256.Create(incSrcS));

        var posMask = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
        var widthVec = Vector256.Create(dstR);

        var initVecS = Vector256.Create(initS);
        var incVecS = Vector256.Create(incSrcS * 8);

        var srcImgWVec = Vector256.Create(srcImgW);

        var t = initT;
        for (var y = dstT; y < dstB; y++, t += incSrcT)
        {
            var imgRow = (int)(t * srcImgH) * srcImgW;
            var imgRowVec = Vector256.Create(imgRow);
            var s = initVecS;
            for (var x = dstL; x < dstR; x += 8, s = Avx.Add(s, incVecS))
            {
                var pos = Avx2.Add(posMask, Vector256.Create(x));
                var writeMask = Avx2.CompareGreaterThan(widthVec, pos);

                // Color vectors on AVX contain BGRA32 data per lane.

                Vector256<int> color;
                if (t is < 0 or >= 1)
                    color = Vector256<int>.AllBitsSet;
                else
                {
                    var vecS = Avx.Add(s, vecIncS);
                    var coord = Avx.ConvertToVector256Int32WithTruncation(
                        Avx.Multiply(vecS, Vector256.Create((float)srcImgW)));

                    var readMask = Avx2.And(
                        Avx2.CompareGreaterThan(coord, Vector256<int>.AllBitsSet),
                        Avx2.CompareGreaterThan(srcImgWVec, coord));

                    var imgPos = Avx2.Add(imgRowVec, coord);

                    // @formatter:off
                    var posL = imgPos.GetLower();
                    var maskL = readMask.GetLower();
                    var l0 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(0), maskL.GetElement(0), srcSpan);
                    var l1 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(1), maskL.GetElement(1), srcSpan);
                    var l2 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(2), maskL.GetElement(2), srcSpan);
                    var l3 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posL.GetElement(3), maskL.GetElement(3), srcSpan);
                    var lowerSample = Vector128.Create(l0, l1, l2, l3);

                    var posU = imgPos.GetUpper();
                    var maskU = readMask.GetUpper();
                    var u0 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(0), maskU.GetElement(0), srcSpan);
                    var u1 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(1), maskU.GetElement(1), srcSpan);
                    var u2 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(2), maskU.GetElement(2), srcSpan);
                    var u3 = CoreAvx2DoMaskedSample<TSrcData, TSampler>(posU.GetElement(3), maskU.GetElement(3), srcSpan);
                    var upperSample = Vector128.Create(u0, u1, u2, u3);
                    // @formatter:on

                    color = Vector256.Create(lowerSample, upperSample);
                }

                CopyPixelsCoreDoOutputAvx2<TDstData, TWriter>(
                    doBackgroundTransparent,
                    writeMask,
                    color,
                    fgVec,
                    dstImgW,
                    x, y,
                    doBlend,
                    parameters,
                    dstSpan);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<byte> DoBlend8Avx2(Vector256<byte> src, Vector256<byte> dst, Vector256<float> blend)
    {
        var res = Vector256.Create(0xFF_00_00_00).AsByte();

        var blendInv = Avx.Subtract(Vector256.Create(1f), blend);

        var bMask = Vector256.Create(
            0, 255, 255, 255,
            4, 255, 255, 255,
            8, 255, 255, 255,
            12, 255, 255, 255,
            0, 255, 255, 255,
            4, 255, 255, 255,
            8, 255, 255, 255,
            12, 255, 255, 255);

        var srcBlue = Avx2.Shuffle(src, bMask);
        var dstBlue = Avx2.Shuffle(dst, bMask);

        var resBlue = DoSingleBlend(srcBlue, dstBlue, blend, blendInv);

        var bInvMask = Vector256.Create(
            0, 255, 255, 255,
            4, 255, 255, 255,
            8, 255, 255, 255,
            12, 255, 255, 255,
            0, 255, 255, 255,
            4, 255, 255, 255,
            8, 255, 255, 255,
            12, 255, 255, 255);

        res = Avx2.Or(res, Avx2.Shuffle(resBlue, bInvMask));

        var gMask = Vector256.Create(
            1, 255, 255, 255,
            5, 255, 255, 255,
            9, 255, 255, 255,
            13, 255, 255, 255,
            1, 255, 255, 255,
            5, 255, 255, 255,
            9, 255, 255, 255,
            13, 255, 255, 255);

        var srcGreen = Avx2.Shuffle(src, gMask);
        var dstGreen = Avx2.Shuffle(dst, gMask);

        var resGreen = DoSingleBlend(srcGreen, dstGreen, blend, blendInv);

        var gInvMask = Vector256.Create(
            255, 0, 255, 255,
            255, 4, 255, 255,
            255, 8, 255, 255,
            255, 12, 255, 255,
            255, 0, 255, 255,
            255, 4, 255, 255,
            255, 8, 255, 255,
            255, 12, 255, 255);

        res = Avx2.Or(res, Avx2.Shuffle(resGreen, gInvMask));

        var rMask = Vector256.Create(
            2, 255, 255, 255,
            6, 255, 255, 255,
            10, 255, 255, 255,
            14, 255, 255, 255,
            2, 255, 255, 255,
            6, 255, 255, 255,
            10, 255, 255, 255,
            14, 255, 255, 255);

        var scrRed = Avx2.Shuffle(src, rMask);
        var dstRed = Avx2.Shuffle(dst, rMask);

        var resRed = DoSingleBlend(scrRed, dstRed, blend, blendInv);

        var rInvMask = Vector256.Create(
            255, 255, 0, 255,
            255, 255, 4, 255,
            255, 255, 8, 255,
            255, 255, 12, 255,
            255, 255, 0, 255,
            255, 255, 4, 255,
            255, 255, 8, 255,
            255, 255, 12, 255);

        return Avx2.Or(res, Avx2.Shuffle(resRed, rInvMask));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<byte> DoSingleBlend(
            Vector256<byte> srcColor, Vector256<byte> dstColor,
            Vector256<float> blend, Vector256<float> blendInv)
        {
            var srcFloat = Avx.ConvertToVector256Single(srcColor.AsInt32());
            var dstFloat = Avx.ConvertToVector256Single(dstColor.AsInt32());

            var resFloat = Avx.Add(
                Avx.Multiply(srcFloat, blend),
                Avx.Multiply(dstFloat, blendInv)
            );

            var res = Avx.ConvertToVector256Int32WithTruncation(resFloat);
            return res.AsByte();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyPixelsCoreDoOutputAvx2<TDstData, TWriter>(
        bool doBackgroundTransparent,
        Vector256<int> writeMask,
        Vector256<int> color,
        Vector256<byte> fgVec,
        int dstImgW,
        int x, int y,
        bool doBlend,
        in CopyPixelsParameters parameters,
        Span<TDstData> dstSpan)
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        // Don't write if sampled color is white (== transparent)
        if (doBackgroundTransparent)
            writeMask = Avx2.AndNot(Avx2.CompareEqual(color, Vector256<int>.AllBitsSet), writeMask);

        // Add foreground color.
        color = Avx2.AddSaturate(color.AsByte(), fgVec).AsInt32();

        var dstPos = dstImgW * y + x;

        if (doBlend)
        {
            var blendVec = Vector256.Create(parameters.Blend);
            var dstColor = TWriter.Read8(dstSpan, dstPos, writeMask);

            var res = DoBlend8Avx2(color.AsByte(), dstColor.AsByte(), blendVec);

            color = res.AsInt32();
        }

        TWriter.Write8(dstSpan, dstPos, color, writeMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyPixelsRectCoreCopyClampDst(
        ref int dst0,
        ref int dst1,
        ref float initTex,
        float incSrcTex,
        int dstImg)
    {
        if (dst0 < 0)
        {
            initTex += -dst0 * incSrcTex;
            dst0 = 0;
        }

        dst1 = Math.Min(dst1, dstImg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (float initS, float initT, float incSrcS, float incSrcT) CopyPixelsRectCoreCopyCalcSampleCoords(
        Vector4 srcBox, int dstL, int dstT, int dstR, int dstB)
    {
        var srcW = srcBox.Z - srcBox.X;
        var srcH = srcBox.W - srcBox.Y;
        var dstW = dstR - dstL;
        var dstH = dstB - dstT;

        // Horizontal increment for sampling coordinates when the rasterizer iterates.
        var incSrcS = srcW / dstW;
        var incSrcT = srcH / dstH;

        // Half-texel offset so we sample the *center* of the pixels, not the edges.
        var initS = srcW / (dstW * 2) + srcBox.X;
        var initT = srcH / (dstH * 2) + srcBox.Y;

        return (initS, initT, incSrcS, incSrcT);
    }

    private static void CopyPixelsPxlRectGenWriter(
        LingoImage dst,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
    {
        switch (dst.Type)
        {
            case ImageType.B8G8R8A8:
                CopyPixelsPxlRectCore<Bgra32, PixelOpsBgra32>(
                    dst,
                    dstBox,
                    parameters);
                break;
            case ImageType.B5G5R5A1:
                CopyPixelsPxlRectCore<Bgra5551, PixelOpsBgra5551>(
                    dst,
                    dstBox,
                    parameters);
                break;
            case ImageType.Palette8:
                CopyPixelsPxlRectCore<L8, PixelOpsPalette8>(
                    dst,
                    dstBox,
                    parameters);
                break;
            case ImageType.Palette1:
                CopyPixelsPxlRectCore<int, PixelOpsBit>(
                    dst,
                    dstBox,
                    parameters);
                break;
            case ImageType.L8:
                CopyPixelsPxlRectCore<L8, PixelOpsPalette8>(
                    dst,
                    dstBox,
                    parameters);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private static void CopyPixelsPxlRectCore<TDstData, TWriter>(
        LingoImage dst,
        (int l, int t, int r, int b) dstBox,
        in CopyPixelsParameters parameters)
        where TWriter : struct, IPixelOps<TDstData>
        where TDstData : unmanaged
    {
        var dstSpan = MemoryMarshal.Cast<byte, TDstData>(dst.ImageBuffer);
        var (dstL, dstT, dstR, dstB) = dstBox;

        dstL = Math.Clamp(dstL, 0, dst.Width);
        dstT = Math.Clamp(dstT, 0, dst.Height);
        dstR = Math.Clamp(dstR, 0, dst.Width);
        dstB = Math.Clamp(dstB, 0, dst.Height);

        // todo: remove round trip to Vector4 here please.
        var fgc = parameters.ForeColor;
        var packed = fgc.BitPack;

        if (dstL == 0 && dstT == 0 && dstR == dst.width && dstB == dst.height)
        {
            // Writing to the whole image with pxl is commonly used as a fill operation.
            TWriter.Fill(dstSpan, packed);
            return;
        }

        var dstWidth = dst.Width;

        for (var y = dstT; y < dstB; y++)
        {
            for (var x = dstL; x < dstR; x++)
            {
                TWriter.Write(dstSpan, y * dstWidth + x, packed);
            }
        }
    }

    private struct CopyPixelsParameters
    {
        public CopyPixelsInk Ink;
        public float Blend;

        public LingoColor ForeColor;
        public LingoMask? Mask;
    }

    private enum CopyPixelsInk
    {
        Copy = 0,
        BackgroundTransparent = 36,
        Darkest = 39
    }

    internal interface IPixelOps<TPixel>
    {
        static abstract int Sample(ReadOnlySpan<TPixel> srcDat, int rowMajorPos);
        static abstract Vector256<int> Read8(ReadOnlySpan<TPixel> dstDat, int rowMajorPos0, Vector256<int> readMask);
        static abstract void Write(Span<TPixel> dstDat, int rowMajorPos, int value);

        static abstract void Write8(
            Span<TPixel> dstDat,
            int rowMajorPos0,
            Vector256<int> pixelData,
            Vector256<int> writeMask);

        static abstract void Fill(Span<TPixel> dstDat, int value);
    }

    internal struct PixelOpsBgra32 : IPixelOps<Bgra32>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sample(ReadOnlySpan<Bgra32> srcDat, int rowMajorPos)
        {
            ref readonly var px = ref srcDat[rowMajorPos];
            return Unsafe.As<Bgra32, int>(ref Unsafe.AsRef(px));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> Read8(ReadOnlySpan<Bgra32> dstDat, int rowMajorPos0, Vector256<int> readMask)
        {
            fixed (Bgra32* px = &dstDat[rowMajorPos0])
            {
                return Avx2.MaskLoad((int*)px, readMask);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<Bgra32> dstDat, int rowMajorPos, int value)
        {
            dstDat[rowMajorPos] = Unsafe.As<int, Bgra32>(ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8(Span<Bgra32> dstDat, int rowMajorPos0, Vector256<int> pixelData,
            Vector256<int> writeMask)
        {
            fixed (Bgra32* ptr = &dstDat[rowMajorPos0])
            {
                var iPtr = (int*)ptr;
                Avx2.MaskStore(iPtr, writeMask, pixelData);
            }
        }

        public static void Fill(Span<Bgra32> dstDat, int value)
        {
            dstDat.Fill(Unsafe.As<int, Bgra32>(ref value));
        }
    }

    internal struct PixelOpsBgra5551 : IPixelOps<Bgra5551>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sample(ReadOnlySpan<Bgra5551> srcDat, int rowMajorPos)
        {
            // TODO: Make this fast.
            var bgra5551 = srcDat[rowMajorPos];
            var bgra = new Bgra32();
            bgra.FromBgra5551(bgra5551);
            return (int)bgra.PackedValue;
        }

        public static Vector256<int> Read8(ReadOnlySpan<Bgra5551> dstDat, int rowMajorPos0, Vector256<int> readMask)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<Bgra5551> dstDat, int rowMajorPos, int value)
        {
            dstDat[rowMajorPos].FromBgra32(Unsafe.As<int, Bgra32>(ref value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8(
            Span<Bgra5551> dstDat,
            int rowMajorPos0,
            Vector256<int> pixelData,
            Vector256<int> writeMask)
        {
            // TODO: Make this fast.
            dstDat = dstDat[rowMajorPos0..];
            dstDat = dstDat[..Math.Min(8, dstDat.Length)];
            for (var i = 0; i < dstDat.Length; i++)
            {
                var elem = pixelData.GetElement(i);
                dstDat[i].FromBgra32(Unsafe.As<int, Bgra32>(ref elem));
            }
        }

        public static void Fill(Span<Bgra5551> dstDat, int value)
        {
            var px = new Bgra5551();
            px.FromBgra32(Unsafe.As<int, Bgra32>(ref value));
            dstDat.Fill(px);
        }
    }

    internal struct PixelOpsPalette8 : IPixelOps<L8>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sample(ReadOnlySpan<L8> srcDat, int rowMajorPos)
        {
            var px = srcDat[rowMajorPos].PackedValue;
            var lingoColor = (LingoColor)px;
            return lingoColor.BitPack;
        }

        public static Vector256<int> Read8(ReadOnlySpan<L8> dstDat, int rowMajorPos0, Vector256<int> readMask)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<L8> dstDat, int rowMajorPos, int value)
        {
            dstDat[rowMajorPos] = ToPalettized(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8(Span<L8> dstDat, int rowMajorPos0, Vector256<int> pixelData, Vector256<int> writeMask)
        {
            // todo: make this fast.

            dstDat = dstDat[rowMajorPos0..];
            dstDat = dstDat[..Math.Min(8, dstDat.Length)];

            for (var i = 0; i < dstDat.Length; i++)
            {
                if (writeMask.GetElement(i) == 0)
                    continue;

                dstDat[i] = ToPalettized(pixelData.GetElement(i));
            }
        }

        public static void Fill(Span<L8> dstDat, int value)
        {
            dstDat.Fill(ToPalettized(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static L8 ToPalettized(int color)
        {
            // Red.
            if (color == LingoColor.PackRed)
                return new L8(6);

            // Black.
            if (color == LingoColor.PackBlack)
                return new L8(255);

            // White.
            return new L8(0);
        }
    }

    internal struct PixelOpsL8 : IPixelOps<L8>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sample(ReadOnlySpan<L8> srcDat, int rowMajorPos)
        {
            var px = srcDat[rowMajorPos].PackedValue;
            return new LingoColor(px, px, px).BitPack;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> Read8(ReadOnlySpan<L8> dstDat, int rowMajorPos0, Vector256<int> readMask)
        {
            ref var baseData = ref Unsafe.As<L8, byte>(
                ref Unsafe.Add(ref MemoryMarshal.GetReference(dstDat), rowMajorPos0));

            // Safe to do because I pad the image buffers with +8 pixels lol.
            var eight = Unsafe.ReadUnaligned<ulong>(ref baseData);
            var vec = Vector256.CreateScalar(eight).AsByte();
            var select = Vector256.Create(
                (byte)
                0, 0, 0, 0,
                1, 1, 1, 0,
                2, 2, 2, 0,
                3, 3, 3, 0,
                4, 4, 4, 0,
                5, 5, 5, 0,
                6, 6, 6, 0,
                7, 7, 7, 0);

            var shuffled = Vector256.Shuffle(vec, select);

            var alphaMask = Vector256.Create(
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF);

            var alphas = Vector256.BitwiseOr(shuffled, alphaMask);

            return alphas.AsInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<L8> dstDat, int rowMajorPos, int value)
        {
            dstDat[rowMajorPos] = new L8((byte)(value & 0xFF));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8(Span<L8> dstDat, int rowMajorPos0, Vector256<int> pixelData, Vector256<int> writeMask)
        {
            ref var baseData = ref Unsafe.As<L8, byte>(
                ref Unsafe.Add(ref MemoryMarshal.GetReference(dstDat), rowMajorPos0));

            var shuffledVec = Avx2.Shuffle(pixelData.AsByte(), Vector256.Create(
                    0x00, 0x04, 0x08, 0x0C,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0x00, 0x04, 0x08, 0x0C,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF))
                .AsUInt64();

            var shuffled = shuffledVec.GetLower().ToScalar() | shuffledVec.GetUpper().ToScalar();

            var shuffledMaskVec = Avx2.Shuffle(writeMask.AsByte(), Vector256.Create(
                    0x00, 0x04, 0x08, 0x0C,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0x00, 0x04, 0x08, 0x0C,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF))
                .AsUInt64();

            var shuffledMask = shuffledMaskVec.GetLower().ToScalar() | shuffledMaskVec.GetUpper().ToScalar();

            var existingData = Unsafe.ReadUnaligned<ulong>(ref baseData);
            var newData = (existingData & ~shuffledMask) | (shuffled & shuffledMask);
            Unsafe.WriteUnaligned(ref baseData, newData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill(Span<L8> dstDat, int value)
        {
            dstDat.Fill(new L8((byte)(value & 0xFF)));
        }
    }

    internal struct PixelOpsBit : IPixelOps<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sample(ReadOnlySpan<int> srcDat, int rowMajorPos)
        {
            return DoBitRead(srcDat, rowMajorPos) ? LingoColor.PackWhite : LingoColor.PackBlack;
        }

        public static Vector256<int> Read8(ReadOnlySpan<int> dstDat, int rowMajorPos0, Vector256<int> readMask)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<int> dstDat, int rowMajorPos, int value)
        {
            DoBitWrite(dstDat, rowMajorPos, value == LingoColor.PackWhite);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8(Span<int> dstDat, int rowMajorPos0, Vector256<int> pixelData,
            Vector256<int> writeMask)
        {
            // todo: do these writes on int instead idk.
            var bytes = MemoryMarshal.Cast<int, byte>(dstDat);

            var isBlack = Avx2.CompareEqual(pixelData, Vector256.Create(LingoColor.PackWhite));
            var bits = Avx.MoveMask(isBlack.AsSingle());
            var writeMaskBit = Avx.MoveMask(writeMask.AsSingle());
            bits &= writeMaskBit;

            var pos = rowMajorPos0 >> 3;
            var posRem = rowMajorPos0 & 7;

            if (posRem == 0)
            {
                // Aligned yay.
                ref var dst = ref bytes[pos];

                dst &= (byte)~writeMaskBit;
                dst |= (byte)bits;
            }
            else
            {
                // 1010_1010
                //
                // 32       40
                // 4    <   5   >
                // 0000_0000 0000_0000
                ref var dstA = ref bytes[pos];

                dstA &= (byte)(~(writeMaskBit << posRem));
                dstA |= (byte)(bits << posRem);

                var secondMask = (byte)(~(writeMaskBit >> (8 - posRem)));
                // if secondMask is 0xFF no bits would be written, skip write to avoid OOB.
                if (secondMask == 0xFF)
                    return;

                ref var dstB = ref bytes[pos + 1];

                dstB &= secondMask;
                dstB |= (byte)(bits >> (8 - posRem));
            }
        }

        public static void Fill(Span<int> dstDat, int value)
        {
            dstDat.Fill(value != LingoColor.PackBlack ? -1 : 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoBitWrite(Span<int> buf, int rowMajorPos, bool white)
    {
        var bytePos = rowMajorPos >> 5;
        var mask = 1 << rowMajorPos;
        ref var val = ref buf[bytePos];

        if (white)
            val |= mask;
        else
            val &= ~mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DoBitRead(ReadOnlySpan<int> buf, int rowMajorPos)
    {
        var bytePos = rowMajorPos >> 5;
        var mask = 1 << rowMajorPos;

        ref readonly var val = ref buf[bytePos];

        return (val & mask) != 0;
    }

    private struct DestQuad
    {
        public Vector2 TopLeft;
        public Vector2 TopRight;
        public Vector2 BottomRight;
        public Vector2 BottomLeft;
    }
}
