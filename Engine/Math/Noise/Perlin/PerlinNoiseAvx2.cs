using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace PBG.Noise;

public static class PerlinNoiseAvx2
{
    public static V256f Noise(V256f x, V256f y)
    {
        // Integer lattice coordinates
        V256f floorX = Avx.RoundToNegativeInfinity(x);
        V256f floorY = Avx.RoundToNegativeInfinity(y);

        V256i X = V256I.FloorToInt(floorX);
        V256i Y = V256I.FloorToInt(floorY);

        // Position inside the lattice cell
        x = Avx.Subtract(x, floorX);
        y = Avx.Subtract(y, floorY);

        V256f u = Fade(x);
        V256f v = Fade(y);

        // Hash the four lattice corners
        V256i h00 = Hash2D(X, Y);
        V256i h10 = Hash2D(Avx2.Add(X, V256I.One), Y);

        V256i h01 = Hash2D(X, Avx2.Add(Y, V256I.One));

        V256i h11 = Hash2D(Avx2.Add(X, V256I.One),  Avx2.Add(Y, V256I.One));

        // Gradient dot products
        V256f n00 = Grad(h00, x, y);
        V256f n10 = Grad(h10, Avx.Subtract(x, V256F.One), y);

        V256f n01 = Grad(h01, x, Avx.Subtract(y, V256F.One));

        V256f n11 = Grad(h11, Avx.Subtract(x, V256F.One), Avx.Subtract(y, V256F.One));

        // Interpolate
        V256f nx0 = Lerp(u, n00, n10);
        V256f nx1 = Lerp(u, n01, n11);

        return Lerp(v, nx0, nx1);
    }
    
    static readonly V256f Fifteen = V256F.New(15f);
    static readonly V256f NegativeZero = V256F.New(-0.0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256f Fade(V256f t)
    {
        // t*t*t*(t*(t*6-15)+10)
        var t2 = V256F.Multiply(t, t);
        var t3 = V256F.Multiply(t2, t);
        var t6 = V256F.Multiply(t, V256F.Six);
        var inner = V256F.Add(V256F.Multiply(V256F.Subtract(t6, Fifteen), t), V256F.Ten);
        return V256F.Multiply(t3, inner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256f Lerp(V256f t, V256f a, V256f b) => V256F.Add(a, V256F.Multiply(t, V256F.Subtract(b, a)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256f Grad(V256i hash, V256f x, V256f y)
    {
        V256i sx = Avx2.And(hash, V256I.One);
        V256i sy = Avx2.And(Avx2.ShiftRightLogical(hash, 1), V256I.One);

        V256f negX = Avx.Xor(x, NegativeZero);
        V256f negY = Avx.Xor(y, NegativeZero);

        V256f maskX = Avx.ConvertToVector256Single(Avx2.CompareEqual(sx, V256I.Zero));

        V256f maskY = Avx.ConvertToVector256Single(Avx2.CompareEqual(sy, V256I.Zero));

        V256f gx = Avx.BlendVariable(negX, x, maskX);
        V256f gy = Avx.BlendVariable(negY, y, maskY);

        return Avx.Add(gx, gy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256i Hash(V256i x)
    {
        x = Avx2.Xor(x, Avx2.ShiftRightLogical(x, 16));
        x = Avx2.MultiplyLow(x, V256I.New(unchecked((int)0x7FEB352D)));
        x = Avx2.Xor(x, Avx2.ShiftRightLogical(x, 15));
        x = Avx2.MultiplyLow(x, V256I.New(unchecked((int)0x846CA68B)));
        x = Avx2.Xor(x, Avx2.ShiftRightLogical(x, 16));
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static V256i Hash2D(V256i x, V256i y)
    {
        V256i h = Avx2.Add(Avx2.MultiplyLow(x, V256I.New(374761393)), Avx2.MultiplyLow(y, V256I.New(668265263)));
        return Hash(h);
    }
}