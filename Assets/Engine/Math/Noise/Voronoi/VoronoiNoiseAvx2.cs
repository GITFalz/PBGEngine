namespace PBG.Noise;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;

public static class VoronoiNoiseAvx2
{
    private static (V256i, V256i)[] offsets = [
        (V256I.New(1),  V256I.New(1)),
        (V256I.New(1),  V256I.New(0)),
        (V256I.New(1),  V256I.New(-1)),
        (V256I.New(0),  V256I.New(-1)),
        (V256I.New(-1), V256I.New(-1)),
        (V256I.New(-1), V256I.New(0)), 
        (V256I.New(-1), V256I.New(1)),
        (V256I.New(0),  V256I.New(1))
    ];

    private static V256u _hashUInt_1 = V256U.New(0x7feb352dU);
    private static V256u _hashUInt_2 = V256U.New(0x846ca68bU);
    private static V256u _hashUInt_3 = V256U.New(0x85ebca6bU);
    private static V256u _hashUInt_4 = V256U.New(0xc2b2ae35U);
    private static V256u HashUInt(V256i px, V256i py)
    {
        // Simple, fast, avalanche-style mixing
        V256u h = px.AsUInt32() * _hashUInt_1 + py.AsUInt32() * _hashUInt_2;
        h ^= h >> 16;
        h *= _hashUInt_3;
        h ^= h >> 13;
        h *= _hashUInt_4;
        h ^= h >> 16;
        return h;
    }

    private static V256f _hash_1 = V256F.New(1f / 16777215f);
    private static V256f Hash(V256i px, V256i py)
    {
        return (HashUInt(px, py) >> 8).ToFloat() * _hash_1;
    }

    private static V256i _hash2_1 = V256I.New(19);
    private static V256i _hash2_2 = V256I.New(131);
    private static V256u _hash2_3 = V256U.New(0xFFFFU);
    private static V256f _hash2_4 = V256F.New(65535f);
    private static (V256f, V256f) Hash2(V256i px, V256i py)
    {
        V256u h = HashUInt(px, py);
        V256u h2 = HashUInt(px + _hash2_1, py + _hash2_2);  // offset to decorrelate

        // Split into two roughly independent 16-bit values
        return (
            (h & _hash2_3).ToFloat() / _hash2_4, 
            (h2 & _hash2_3).ToFloat() / _hash2_4
        );
    }

    private static V256i _hash3_1 = V256I.New(19);
    private static V256i _hash3_2 = V256I.New(131);
    private static V256i _hash3_3 = V256I.New(113);
    private static V256i _hash3_4 = V256I.New(7);
    private static V256u _hash3_5 = V256U.New(0xFFFFFFU);
    private static V256f _hash3_6 = V256F.New(16777215f);
    private static (V256f, V256f, V256f) Hash3(V256i px, V256i py)
    {
        V256u h = HashUInt(px, py);
        V256u h2 = HashUInt(px + _hash3_1, py + _hash3_2);
        V256u h3 = HashUInt(px + _hash3_3, py + _hash3_4);   // different offsets for decorrelation

        return (
            (h  & _hash3_5).ToFloat() / _hash3_6,   // 24 bits
            (h2 & _hash3_5).ToFloat() / _hash3_6,
            (h3 & _hash3_5).ToFloat() / _hash3_6
        );
    }

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (V256i, V256i) GetOffset(int index)
    {
        int ib0 = ~index & 1;
        int b1 = (index >> 1) & 1;
        int ib1 = ~b1 & 1;
        int b2 = 1 - ((index >> 1) & 2);
        return (V256i.New((ib0 | ib1) * b2), (ib0 * ib1 | -b1) * b2);
    }
    */
    
    /*
    private static Vector2i[] GetP(Vector2i p)
    {
        return [p, p + offsets[0], p + offsets[1], p + offsets[2], p + offsets[3], p + offsets[4], p + offsets[5], p + offsets[6], p + offsets[7]];
    }

    private static Vector2[] GetG(Vector2i[] p)
    {
        return [Hash2(p[0]), Hash2(p[1]) + offsets[0], Hash2(p[2]) + offsets[1], Hash2(p[3]) + offsets[2], Hash2(p[4]) + offsets[3], Hash2(p[5]) + offsets[4], Hash2(p[6]) + offsets[5], Hash2(p[7]) + offsets[6], Hash2(p[8]) + offsets[7]];
    }
    */

    private static (V256i, V256i) GetP(V256i px, V256i py, int index)
    {
        var (ox, oy) = offsets[index];
        return (px + ox, py + oy);
    }

    private static (V256f, V256f) GetG(V256i px, V256i py, int index)
    {
        var (ox, oy) = offsets[index];
        var (hx, hy) = Hash2(px, py);
        return (hx + ox.ToFloat(), hy + oy.ToFloat());
    }

    /*
    private static (V256i, V256i) GetOffset(int index)
    {
        int ib0 = ~index & 1;
        int b1 = (index >> 1) & 1;
        int ib1 = ~b1 & 1;
        int b2 = 1 - ((index >> 1) & 2);
        return (V256i.New((ib0 | ib1) * b2), (ib0 * ib1 | -b1) * b2);
    }

    

    public static Vector2 VoronoiOrigin(Vector2 p)
    {
        Vector2i f = Mathf.FloorToInt(p);
        Vector2 pn = Hash2(f);
        return f + pn;
    }

    // Mathf.Single color voronoi
    public static float Voronoi(Vector2 p, out Vector2 g)
    {
        var fP = Mathf.FloorToInt(p);
        Vector2 besG = Mathf.Floor(fP) + Hash2(fP); // world-space seed 
        float d = Vector2.Distance(p, besG);
        float c = Hash(fP);
        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            Vector2 gi = Mathf.Floor(np) + Hash2(np); // world-space seed
            float d1 = Vector2.Distance(p, gi);
            if (d1 < d)
            {
                d = d1;
                besG = gi;
                c = Hash(np);
            }
        }
        g = besG;
        return c;
    }
    */

    public static V256f Voronoi(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        //var fP = Mathf.FloorToInt(p);
        var fPx = px.FloorToInt();
        var fPy = py.FloorToInt();

        //Vector2 besG = Mathf.Floor(fP) + Hash2(fP);
        var (hash2x, hash2y) = Hash2(fPx, fPy);
        var besGx = fPx.ToFloat() + hash2x;
        var besGy = fPy.ToFloat() + hash2y;

        //float d = Vector2.Distance(p, besG);
        var d = V256F.DistanceSquared((px, py), (besGx, besGy));

        //float c = Hash(fP);
        var c = Hash(fPx, fPy);

        for (int i = 1; i < 9; i++)
        {
            //var o = offsets[i - 1];
            var (ox, oy) = offsets[i - 1];

            //var np = fP + o;  
            var npx = fPx + ox;
            var npy = fPy + oy;

            //Vector2 gi = Mathf.Floor(np) + Hash2(np); // world-space seed
            var (nphx, nphy) = Hash2(npx, npy);
            var gix = npx.ToFloat() + nphx;
            var giy = npy.ToFloat() + nphy;

            //float d1 = Vector2.Distance(p, gi);
            var d1 = V256F.DistanceSquared((px, py), (gix, giy));
            /*
            if (d1 < d)
            {
                d = d1;
                besG = gi;
                c = Hash(np);
            }
            */
            var dcomp = d1.CompareL(d);
            d.SetIf(dcomp, d1);
            besGx.SetIf(dcomp, gix);
            besGy.SetIf(dcomp, giy);
            c.SetIf(dcomp, Hash(npx, npy));
        }

        //g = besG;
        gx = besGx;
        gy = besGy;

        return c;
    }

    
    /*
    // Edge voronoi
    public static float VoronoiF2(Vector2 p, out Vector2 g)
    {
        var fP = Mathf.FloorToInt(p);
        p = Mathf.Fraction(p);
        g = Hash2(fP);
        float d = Vector2.Distance(p, g);
        float d2 = 999.0f;
        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gs = Hash2(np) + o;
            float dist = Vector2.Distance(p, gs);
            if (dist < d)
            {
                d2 = d; d = dist;
                g = gs;
            }
            else if (dist < d2)
            {
                d2 = dist;
                g = gs;
            }
        }
        return d2 - d;
    }
    */

    /*
    // Color voronoi
    public static Vector3 Voronoi3(Vector2 p, out Vector2 g)
    {
        var fP = Mathf.FloorToInt(p);
        p = Mathf.Fraction(p);
        g = Hash2(fP);
        float d = Vector2.Distance(p, g);
        Vector3 c = Hash3(fP);
        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gs = Hash2(np) + o;
            float d1 = Vector2.Distance(p, gs);
            if (d1 < d)
            {
                d = d1; c = Hash3(np);
                g = gs;
            }
        }
        return c;
    }
    */

    /*
    // Distance to voronoi cell
    public static float VoronoiDistance(Vector2 p, out Vector2 g)
    {
        var fP = Mathf.FloorToInt(p);
        p = Mathf.Fraction(p);
        g = Hash2(fP);
        float d = Vector2.Distance(p, g);
        for (int i = 1; i < 9; i++)
        {
            var o = offsets[i - 1];
            var np = fP + o;
            var gs = Hash2(np) + o;
            float d1 = Vector2.Distance(p, gs);
            if (d1 < d)
            {
                d = d1;
                g = gs;
            }
        }
        return d;
    }
    */

    /*
    // Checkerboard voronoi
    public static float VoronoiChecker(Vector2 p, out Vector2 g)
    {
        Vector2i[] ps = GetP(Mathf.FloorToInt(p)); // the floored position and its neighbors
        Vector2[] gs = GetG(ps); // the random positions of the neighbors
        p = Mathf.Fraction(p);
        g = gs[0];
        float d = Vector2.Distance(p, gs[0]);
        float c = Hash(ps[0]);
        for (int i = 1; i < 9; i++)
        {
            float d1 = Vector2.Distance(p, gs[i]);
            if (d1 < d)
            {
                d = d1; c = Hash(ps[i]);
                g = gs[i];
            }
        }
        return Mathf.Mod(Mathf.Floor(c * 10.0f), 2.0f);
    }
    */

    /*
    // Worley Flow voronoi
    public static float VoronoiWF(Vector2 p, out Vector2 g)
    {
        Vector2i[] ps = GetP(Mathf.FloorToInt(p)); // the floored position and its neighbors
        Vector2[] gs = GetG(ps); // the random positions of the neighbors
        p = Mathf.Fraction(p);
        g = gs[0];
        float d = Vector2.Distance(p, gs[0]);
        for (int i = 1; i < 9; i++)
        {
            float d1 = Vector2.Distance(p, gs[i]);
            if (d1 < d)
            {
                d = d1;
                g = gs[i];
            }
        }
        float dir = (Vector2.Dot((p - g).Normalized(), new Vector2(1, 0)) + 1) / 2.0f; // flow from feature to pixel
        return dir;
    }
    */

    /*
    public static float VoronoiAngle(Vector2 p, out Vector2 g)
    {
        Vector2i[] ps = GetP(Mathf.FloorToInt(p)); // the floored position and its neighbors
        Vector2[] gs = GetG(ps); // the random positions of the neighbors
        p = Mathf.Fraction(p);
        g = gs[0];
        float d = Vector2.Distance(p, gs[0]);
        for (int i = 1; i < 9; i++)
        {
            float d1 = Vector2.Distance(p, gs[i]);
            if (d1 < d)
            {
                d = d1;
                g = gs[i];
            }
        }
        Vector2 dir = (p - g).Normalized(); // flow from feature to pixel
        return (float)((Math.Atan2(dir.Y, dir.X) + Math.PI) / (2.0f * Math.PI));
    }
    */

    /*
    public static Vector2 VoronoiPoint(Vector2 p)
    {
        Vector2i[] ps = GetP(Mathf.FloorToInt(p));
        Vector2[] gs = GetG(ps);

        p = Mathf.Fraction(p);

        int bestIndex = 0;
        float minDist = Vector2.Distance(p, gs[0]);

        for (int i = 1; i < 9; i++)
        {
            float d = Vector2.Distance(p, gs[i]);
            if (d < minDist)
            {
                minDist = d;
                bestIndex = i;
            }
        }

        return ps[bestIndex] + gs[bestIndex];
    }
    */

    // ----------------------------------------------------------------------
    // Edge Voronoi (F2 - F1)
    // ----------------------------------------------------------------------
    public static V256f EdgeVoronoi(V256f px, V256f py) => EdgeVoronoi(px, py, out _, out _);
    public static V256f EdgeVoronoi(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        //var fP = Mathf.FloorToInt(p);
        var fPx = px.FloorToInt();
        var fPy = py.FloorToInt();

        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //g = Hash2(fP);
        var (hgx, hgy) = Hash2(fPx, fPy);
        gx = hgx;
        gy = hgy;

        //float d = Vector2.Distance(p, g);
        var d = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        //float d2 = 999.0f;
        var d2 = V256F.New(999.0f);

        for (int i = 1; i < 9; i++)
        {
            //var o = offsets[i - 1];
            var (ox, oy) = offsets[i - 1];

            //var np = fP + o;
            var npx = fPx + ox;
            var npy = fPy + oy;

            //var gs = Hash2(np) + o;
            var (hnpx, hnpy) = Hash2(npx, npy);
            var gsx = hnpx + ox.ToFloat();
            var gsy = hnpy + oy.ToFloat();

            //float dist = Vector2.Distance(p, gs);
            var dist = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (dist < d)
            {
                d2 = d; d = dist;
                g = gs;
            }
            else if (dist < d2)
            {
                d2 = dist;
                g = gs;
            }
            */
            var lessThanD = dist.CompareL(d);
            var lessThanD2 = dist.CompareL(d2);
            // "else if" -> only where NOT lessThanD but lessThanD2 (verify this API)
            var elseLessThanD2 = lessThanD2.AndNot(lessThanD);
            var updatedG = lessThanD.Or(elseLessThanD2);

            // order matters: d2 must read the OLD d before d gets overwritten
            d2.SetIf(lessThanD, d);           // d2 = d (old) where dist < d
            d2.SetIf(elseLessThanD2, dist);   // d2 = dist where NOT(dist<d) && dist<d2

            d.SetIf(lessThanD, dist);         // d = dist where dist < d

            gx.SetIf(updatedG, gsx);
            gy.SetIf(updatedG, gsy);
        }

        //return d2 - d;
        return Avx.Sqrt(d2) - Avx.Sqrt(d);
    }


    // ----------------------------------------------------------------------
    // Color Voronoi
    // ----------------------------------------------------------------------
    public static (V256f, V256f, V256f) Voronoi3(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        //var fP = Mathf.FloorToInt(p);
        var fPx = px.FloorToInt();
        var fPy = py.FloorToInt();

        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //g = Hash2(fP);
        var (hgx, hgy) = Hash2(fPx, fPy);
        gx = hgx;
        gy = hgy;

        //float d = Vector2.Distance(p, g);
        var d = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        //Vector3 c = Hash3(fP);
        var (cr, cg, cb) = Hash3(fPx, fPy);

        for (int i = 1; i < 9; i++)
        {
            //var o = offsets[i - 1];
            var (ox, oy) = offsets[i - 1];

            //var np = fP + o;
            var npx = fPx + ox;
            var npy = fPy + oy;

            //var gs = Hash2(np) + o;
            var (hnpx, hnpy) = Hash2(npx, npy);
            var gsx = hnpx + ox.ToFloat();
            var gsy = hnpy + oy.ToFloat();

            //float d1 = Vector2.Distance(p, gs);
            var d1 = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (d1 < d)
            {
                d = d1; c = Hash3(np);
                g = gs;
            }
            */
            var lessThanD = d1.CompareL(d);
            var (hnpr, hnpg, hnpb) = Hash3(npx, npy);

            d.SetIf(lessThanD, d1);
            cr.SetIf(lessThanD, hnpr);
            cg.SetIf(lessThanD, hnpg);
            cb.SetIf(lessThanD, hnpb);
            gx.SetIf(lessThanD, gsx);
            gy.SetIf(lessThanD, gsy);
        }

        //return c;
        return (cr, cg, cb);
    }


    // ----------------------------------------------------------------------
    // Distance to voronoi cell
    // ----------------------------------------------------------------------
    public static V256f DistanceVoronoi(V256f px, V256f py) => DistanceVoronoi(px, py, out _, out _);
    public static V256f DistanceVoronoi(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        //var fP = Mathf.FloorToInt(p);
        var fPx = px.FloorToInt();
        var fPy = py.FloorToInt();

        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //g = Hash2(fP);
        var (hgx, hgy) = Hash2(fPx, fPy);
        gx = hgx;
        gy = hgy;

        //float d = Vector2.Distance(p, g);
        var d = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        for (int i = 0; i < 8; i++)
        {
            //var o = offsets[i - 1];
            var (ox, oy) = offsets[i];

            //var np = fP + o;
            var npx = fPx + ox;
            var npy = fPy + oy;

            //var gs = Hash2(np) + o;
            var (hnpx, hnpy) = Hash2(npx, npy);
            var gsx = hnpx + ox.ToFloat();
            var gsy = hnpy + oy.ToFloat();

            //float d1 = Vector2.Distance(p, gs);
            var d1 = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (d1 < d)
            {
                d = d1;
                g = gs;
            }
            */
            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            gx.SetIf(lessThanD, gsx);
            gy.SetIf(lessThanD, gsy);
        }

        //return d;
        return Avx.Sqrt(d);
    }


    // ----------------------------------------------------------------------
    // Checkerboard voronoi
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK)
    // ----------------------------------------------------------------------
    public static V256f VoronoiChecker(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        // Mathf.FloorToInt(p)
        var pix = px.FloorToInt();
        var piy = py.FloorToInt();

        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //g = gs[0];
        (gx, gy) = Hash2(pix, piy);

        //float d = Vector2.Distance(p, gs[0]);
        var d = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        //float c = Hash(ps[0]);
        var c = Hash(pix, piy);

        for (int i = 0; i < 8; i++)
        {
            //float d1 = Vector2.Distance(p, gs[i]);
            var (psx, psy) = GetP(pix, piy, i);
            var (gsx, gsy) = GetG(psx, psy, i);
            var d1 = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (d1 < d)
            {
                d = d1; c = Hash(ps[i]);
                g = gs[i];
            }
            */
            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            c.SetIf(lessThanD, Hash(psx, psy));
            gx.SetIf(lessThanD, gsx);
            gy.SetIf(lessThanD, gsy);
        }

        //return Mathf.Mod(Mathf.Floor(c * 10.0f), 2.0f);
        return (c * 10.0f).Floor().Mod(V256F.Two);
    }


    // ----------------------------------------------------------------------
    // Worley Flow voronoi
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK)
    // ----------------------------------------------------------------------
    public static V256f VoronoiWF(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        //Vector2i[] ps = GetP(Mathf.FloorToInt(p)); // the floored position and its neighbors
        var pix = px.FloorToInt();
        var piy = py.FloorToInt();

        //Vector2[] gs = GetG(ps); // the random positions of the neighbors
        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //g = gs[0];
        (gx, gy) = Hash2(pix, piy);

        //float d = Vector2.Distance(p, gs[0]);
        var d = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        for (int i = 0; i < 8; i++)
        {
            //float d1 = Vector2.Distance(p, gs[i]);
            var (psx, psy) = GetP(pix, piy, i);
            var (gsx, gsy) = GetG(psx, psy, i);
            var d1 = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (d1 < d)
            {
                d = d1;
                g = gs[i];
            }
            */
            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            gx.SetIf(lessThanD, gsx);
            gy.SetIf(lessThanD, gsy);
        }

        //float dir = (Vector2.Dot((p - g).Normalized(), new Vector2(1, 0)) + 1) / 2.0f; // flow from feature to pixel
        var toPixelX = fracPx - gx;
        var toPixelY = fracPy - gy;
        var (normX, _) = V256F.Normalize(toPixelX, toPixelY); // TODO: SIMD Normalize
        var dot = normX; // dot with (1,0) is just the x component
        var dir = (dot + V256F.One) / V256F.Two;

        //return dir;
        return dir;
    }


    // ----------------------------------------------------------------------
    // Angle Voronoi
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK)
    // ----------------------------------------------------------------------
    public static V256f VoronoiAngle(V256f px, V256f py, out V256f gx, out V256f gy)
    {
        //Vector2i[] ps = GetP(Mathf.FloorToInt(p)); // the floored position and its neighbors
        var pix = px.FloorToInt();
        var piy = py.FloorToInt();

        //Vector2[] gs = GetG(ps); // the random positions of the neighbors
        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //g = gs[0];
        (gx, gy) = Hash2(pix, piy);

        //float d = Vector2.Distance(p, gs[0]);
        var d = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        for (int i = 0; i < 8; i++)
        {
            //float d1 = Vector2.Distance(p, gs[i]);
            var (psx, psy) = GetP(pix, piy, i);
            var (gsx, gsy) = GetG(psx, psy, i);
            var d1 = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (d1 < d)
            {
                d = d1;
                g = gs[i];
            }
            */
            var lessThanD = d1.CompareL(d);
            d.SetIf(lessThanD, d1);
            gx.SetIf(lessThanD, gsx);
            gy.SetIf(lessThanD, gsy);
        }

        //Vector2 dir = (p - g).Normalized(); // flow from feature to pixel
        var dirX = fracPx - gx;
        var dirY = fracPy - gy;
        var (normX, normY) = V256F.Normalize(dirX, dirY); // TODO: SIMD Normalize

        //return (float)((Math.Atan2(dir.Y, dir.X) + Math.PI) / (2.0f * Math.PI));
        var angle = V256F.Atan2(normY, normX); // TODO: SIMD Atan2
        return (angle + V256F.PI) / (V256F.Two * V256F.PI);
    }


    // ----------------------------------------------------------------------
    // Voronoi feature point
    // (GetP/GetG based - logic kept as-is with comments, non-functional OK.
    //  Note: per-lane "bestIndex" selection doesn't translate directly to SIMD -
    //  you'll likely need to track a running best gx/gy pair instead of an index,
    //  like the other functions above do.)
    // ----------------------------------------------------------------------
    public static (V256f, V256f) VoronoiPoint(V256f px, V256f py)
    {
        //Vector2i[] ps = GetP(Mathf.FloorToInt(p));
        var pix = px.FloorToInt();
        var piy = py.FloorToInt();

        //Vector2[] gs = GetG(ps);
        var (gx, gy) = Hash2(pix, piy);

        //p = Mathf.Fraction(p);
        var fracPx = px.Fraction();
        var fracPy = py.Fraction();

        //int bestIndex = 0;
        //float minDist = Vector2.Distance(p, gs[0]);
        var minDist = V256F.DistanceSquared((fracPx, fracPy), (gx, gy));

        // best result tracked as world-space point instead of an index, since
        // "bestIndex" is per-scalar and doesn't work per-lane in SIMD
        var bestX = pix.ToFloat() + gx;
        var bestY = piy.ToFloat() + gy;

        for (int i = 0; i < 8; i++)
        {
            //float d = Vector2.Distance(p, gs[i]);
            var (psx, psy) = GetP(pix, piy, i);
            var (gsx, gsy) = GetG(psx, psy, i);
            var d = V256F.DistanceSquared((fracPx, fracPy), (gsx, gsy));

            /*
            if (d < minDist)
            {
                minDist = d;
                bestIndex = i;
            }
            */
            var lessThanMin = d.CompareL(minDist);
            minDist.SetIf(lessThanMin, d);

            var candX = psx.ToFloat() + gsx;
            var candY = psy.ToFloat() + gsy;
            bestX.SetIf(lessThanMin, candX);
            bestY.SetIf(lessThanMin, candY);
        }

        //return ps[bestIndex] + gs[bestIndex];
        return (bestX, bestY);
    }
}
