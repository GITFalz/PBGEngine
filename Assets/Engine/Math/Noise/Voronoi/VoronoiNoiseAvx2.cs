namespace PBG.Noise;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;

using Vec2 = MathLibrary.Avx2Vector2;
using Vec2i = MathLibrary.Avx2Vector2i;
using Vec3 = MathLibrary.Avx2Vector3;
using Vec3i = MathLibrary.Avx2Vector3i;
using Vec4 = MathLibrary.Avx2Vector4;
using Vec4i = MathLibrary.Avx2Vector4i;

public static class VoronoiNoiseAvx2
{
    private static Vec2i[] offsets = [
        new(V256I.New(1),  V256I.New(1)),
        new(V256I.New(1),  V256I.New(0)),
        new(V256I.New(1),  V256I.New(-1)),
        new(V256I.New(0),  V256I.New(-1)),
        new(V256I.New(-1), V256I.New(-1)),
        new(V256I.New(-1), V256I.New(0)), 
        new(V256I.New(-1), V256I.New(1)),
        new(V256I.New(0),  V256I.New(1))
    ];

    private static Vec2[] floatOffsets = [
        new(V256F.New(1),  V256F.New(1)),
        new(V256F.New(1),  V256F.New(0)),
        new(V256F.New(1),  V256F.New(-1)),
        new(V256F.New(0),  V256F.New(-1)),
        new(V256F.New(-1), V256F.New(-1)),
        new(V256F.New(-1), V256F.New(0)), 
        new(V256F.New(-1), V256F.New(1)),
        new(V256F.New(0),  V256F.New(1))
    ];

    private static V256u _hashUInt_1 = V256U.New(0x7feb352dU);
    private static V256u _hashUInt_2 = V256U.New(0x846ca68bU);
    private static V256u _hashUInt_3 = V256U.New(0x85ebca6bU);
    private static V256u _hashUInt_4 = V256U.New(0xc2b2ae35U);
    private static V256u HashUInt(Vec2i p)
    {
        // Simple, fast, avalanche-style mixing
        V256u h = p.X.AsUInt32() * _hashUInt_1 + p.Y.AsUInt32() * _hashUInt_2;
        h ^= h >> 16;
        h *= _hashUInt_3;
        h ^= h >> 13;
        h *= _hashUInt_4;
        h ^= h >> 16;
        return h;
    }

    private static V256f _hash_1 = V256F.New(1f / 16777215f);

    private static V256f Hash(Vec2i p)
    {
        return (HashUInt(p) >> 8).ToFloat() * _hash_1;
    }

    private static Vec2i _hash2_1 = new(V256I.New(19), V256I.New(131));
    private static V256u _hash2_3 = V256U.New(0xFFFFU);
    private static V256f _hash2_4 = V256F.New(65535f);

    private static Vec2 Hash2(Vec2i p)
    {
        V256u h = HashUInt(p);
        V256u h2 = HashUInt(p + _hash2_1);  // offset to decorrelate

        // Split into two roughly independent 16-bit values
        return new(
            (h & _hash2_3).ToFloat() / _hash2_4, 
            (h2 & _hash2_3).ToFloat() / _hash2_4
        );
    }

    private static Vec2i _hash3_1 = new(V256I.New(19), V256I.New(131));
    private static Vec2i _hash3_3 = new(V256I.New(113), V256I.New(7));

    private static V256u _hash3_5 = V256U.New(0xFFFFFFU);
    private static V256f _hash3_6 = V256F.New(16777215f);

    private static Vec3 Hash3(Vec2i p)
    {
        V256u h = HashUInt(p);
        V256u h2 = HashUInt(p + _hash3_1);
        V256u h3 = HashUInt(p + _hash3_3);   // different offsets for decorrelation

        return new(
            (h  & _hash3_5).ToFloat() / _hash3_6,   // 24 bits
            (h2 & _hash3_5).ToFloat() / _hash3_6,
            (h3 & _hash3_5).ToFloat() / _hash3_6
        );
    }

    private static Vec2i GetP(Vec2i p, int index) => p + offsets[index];
    private static Vec2 GetG(Vec2i p, int index) => Hash2(p) + floatOffsets[index];

    public static V256f Voronoi(Vec2 p, out Vec2 g)
    {
        var fP = Avx2Math.FloorToInt(p);
        Vec2 besG = Avx2Math.Floor(fP) + Hash2(fP);
        var d = V256F.DistanceSquared(p, besG);
        var c = Hash(fP);

        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gi = Avx2Math.Floor(np) + Hash2(np);
            var d1 = V256F.DistanceSquared(p, gi);
 
            var dcomp = d1.CompareL(d);
            d.SetIf(dcomp, d1);
            besG.SetIf(dcomp, gi);
            c.SetIf(dcomp, Hash(np));
        }

        g = besG;

        return c;
    }

    // ----------------------------------------------------------------------
    // Edge Voronoi (F2 - F1)
    // ----------------------------------------------------------------------
    public static V256f EdgeVoronoi(Vec2 p) => EdgeVoronoi(p, out _);
    public static V256f EdgeVoronoi(Vec2 p, out Vec2 g)
    {
        var fP = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);
        g = Hash2(fP);
        var d = V256F.DistanceSquared(p, g);
        var d2 = V256F.New(999.0f);

        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gs = Hash2(np) + o;
            var dist = V256F.DistanceSquared(p, gs);

            var lessThanD = dist.CompareL(d);
            var lessThanD2 = dist.CompareL(d2);
            var elseLessThanD2 = lessThanD2.AndNot(lessThanD);
            var updatedG = lessThanD.Or(elseLessThanD2);

            d2.SetIf(lessThanD, d);           // d2 = d (old) where dist < d
            d2.SetIf(elseLessThanD2, dist);   // d2 = dist where NOT(dist<d) && dist<d2

            d.SetIf(lessThanD, dist);         // d = dist where dist < d

            g.SetIf(updatedG, gs);
        }

        return Avx.Sqrt(d2) - Avx.Sqrt(d);
    }


    // ----------------------------------------------------------------------
    // Color Voronoi
    // ----------------------------------------------------------------------
    public static Vec3 Voronoi3(Vec2 p, out Vec2 g)
    {
        var fP = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);
        g = Hash2(fP);
        var d = V256F.DistanceSquared(p, g);
        var c = Hash3(fP);

        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gs = Hash2(np) + o;
            var d1 = V256F.DistanceSquared(p, gs);

            var lessThanD = d1.CompareL(d);
            var hnp = Hash3(np);

            d.SetIf(lessThanD, d1);
            c.SetIf(lessThanD, hnp);
            g.SetIf(lessThanD, gs);
        }

        return c;
    }


    // ----------------------------------------------------------------------
    // Distance to voronoi cell
    // ----------------------------------------------------------------------
    public static V256f DistanceVoronoi(Vec2 p) => DistanceVoronoi(p, out _);
    public static V256f DistanceVoronoi(Vec2 p, out Vec2 g)
    {
        var fP = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);
        g = Hash2(fP);
        var d = V256F.DistanceSquared(p, g);

        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gs = Hash2(np) + o;
            var d1 = V256F.DistanceSquared(p, gs);

            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            g.SetIf(lessThanD, gs);
        }

        return Avx.Sqrt(d);
    }


    // ----------------------------------------------------------------------
    // Checkerboard voronoi
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK)
    // ----------------------------------------------------------------------
    public static V256f VoronoiChecker(Vec2 p, out Vec2 g)
    {
        var pi = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);
        g = Hash2(pi);
        var d = V256F.DistanceSquared(p, g);
        var c = Hash(pi);

        for (int i = 0; i < 8; i++)
        {
            var ps = GetP(pi, i);
            var gs = GetG(ps, i);
            var d1 = V256F.DistanceSquared(p, gs);

            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            c.SetIf(lessThanD, Hash(ps));
            g.SetIf(lessThanD, gs);
        }

        return (c * 10.0f).Floor().Mod(V256F.Two);
    }


    // ----------------------------------------------------------------------
    // Worley Flow voronoi
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK)
    // ----------------------------------------------------------------------
    public static V256f VoronoiWF(Vec2 p, out Vec2 g)
    {
        var pi = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);
        g = Hash2(pi);
        var d = V256F.DistanceSquared(p, g);

        for (int i = 0; i < 8; i++)
        {
            var ps = GetP(pi, i);
            var gs = GetG(ps, i);
            var d1 = V256F.DistanceSquared(p, gs);

            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            g.SetIf(lessThanD, gs);
        }

        var toPixel = p - g;
        var norm = V256F.Normalize(toPixel);
        var dir = (norm.X + V256F.One) / V256F.Two;

        return dir;
    }


    // ----------------------------------------------------------------------
    // Angle Voronoi
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK)
    // ----------------------------------------------------------------------
    public static V256f VoronoiAngle(Vec2 p, out Vec2 g)
    {
        //Vector2i[] ps = GetP(Mathf.FloorToInt(p)); // the floored position and its neighbors
        var pi = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);
        g = Hash2(pi);
        var d = V256F.DistanceSquared(p, g);

        for (int i = 0; i < 8; i++)
        {
            var ps = GetP(pi, i);
            var gs = GetG(ps, i);
            var d1 = V256F.DistanceSquared(p, gs);

            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            g.SetIf(lessThanD, gs);
        }

        var norm = V256F.Normalize(p - g);
        var angle = V256F.Atan2(norm.Y, norm.X);
        return (angle + V256F.PI) / (V256F.Two * V256F.PI);
    }


    // ----------------------------------------------------------------------
    // Voronoi feature point
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK.
    //  Note: per-lane "bestIndex" selection doesn't translate directly to SIMD -
    //  you'll likely need to track a running best gx/gy pair instead of an index,
    //  like the other functions above do.)
    // ----------------------------------------------------------------------
    public static Vec2 VoronoiPoint(Vec2 p)
    {
        var pi = Avx2Math.FloorToInt(p);
        p = Avx2Math.Fraction(p);

        var bestG = Hash2(pi);
        var minDist = V256F.DistanceSquared(p, bestG);

        for (int i = 0; i < 8; i++)
        {
            var gs = Hash2(pi + offsets[i]) + floatOffsets[i];
            var d = V256F.DistanceSquared(p, gs);

            var lessThanMin = d.CompareL(minDist);
            minDist.SetIf(lessThanMin, d);
            bestG.SetIf(lessThanMin, gs);
        }

        return (Vec2)pi + bestG;
    }
}
