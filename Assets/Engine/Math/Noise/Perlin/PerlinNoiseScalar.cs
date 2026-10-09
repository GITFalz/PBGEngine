using System.Runtime.CompilerServices;
using PBG.MathLibrary;

namespace PBG.Noise;

public static class PerlinNoiseScalar
{
    public static float Noise(float x, float y)
    {
        // Integer lattice coordinates
        float floorX = (float)Math.Floor(x);
        float floorY = (float)Math.Floor(y);

        int X = (int)floorX;
        int Y = (int)floorY;

        // Position inside the lattice cell
        x -= floorX;
        y -= floorY;

        float u = Fade(x);
        float v = Fade(y);

        // Hash the four lattice corners
        int h00 = Hash2D(X, Y);

        int h10 = Hash2D(X + 1, Y);
        int h01 = Hash2D(X, Y + 1);
        
        int h11 = Hash2D(X + 1,  Y + 1);

        // Gradient dot products
        float n00 = Grad(h00, x, y);

        float n10 = Grad(h10, x - 1, y);
        float n01 = Grad(h01, x, y - 1);

        float n11 = Grad(h11, x - 1, y - 1);

        // Interpolate
        float nx0 = Lerp(u, n00, n10);
        float nx1 = Lerp(u, n01, n11);

        return Lerp(v, nx0, nx1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float Lerp(float t, float a, float b) => a + (t * (b - a));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float Grad(int hash, float x, float y)
    {
        int sx = hash & 1;
        int sy = (hash >> 1) & 1;

        float gx = (sx == 0) ? x : -x;
        float gy = (sy == 0) ? y : -y;

        return gx + gy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Hash(int x)
    {
        x ^= (int)((uint)x >> 16);
        x *= unchecked((int)0x7FEB352D);
        x ^= (int)((uint)x >> 15);
        x *= unchecked((int)0x846CA68B);
        x ^= (int)((uint)x >> 16);
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Hash2D(int x, int y)
    {
        int h = x * 374761393 + y * 668265263;
        return Hash(h);
    }
}