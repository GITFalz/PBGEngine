using PBG.MathLibrary;

namespace PBG.Noise
{
    public static class VoronoiLib
    {
        private static Vector2i[] offsets = [(1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1)];

        private static uint HashUInt(Vector2i p)
        {
            // Simple, fast, avalanche-style mixing
            uint h = (uint)p.X * 0x7feb352dU + (uint)p.Y * 0x846ca68bU;
            h ^= h >> 16;
            h *= 0x85ebca6bU;
            h ^= h >> 13;
            h *= 0xc2b2ae35U;
            h ^= h >> 16;
            return h;
        }

        private static float Hash(Vector2i p)
        {
            return (HashUInt(p) >> 8) * (1f / 16777215f);  // use top 24 bits → [0,1)
        }

        private static Vector2 Hash2(Vector2i p)
        {
            uint h = HashUInt(p);
            uint h2 = HashUInt(p + new Vector2i(19, 131));  // offset to decorrelate

            // Split into two roughly independent 16-bit values
            return new Vector2(
                (h & 0xFFFFU) / 65535f,
                (h2 & 0xFFFFU) / 65535f
            );
        }

        private static Vector3 Hash3(Vector2i p)
        {
            uint h = HashUInt(p);
            uint h2 = HashUInt(p + new Vector2i(19, 131));
            uint h3 = HashUInt(p + new Vector2i(113, 7));   // different offsets for decorrelation

            return new Vector3(
                (h  & 0xFFFFFFU) / 16777215f,   // 24 bits
                (h2 & 0xFFFFFFU) / 16777215f,
                (h3 & 0xFFFFFFU) / 16777215f
            );
        }

        private static Vector2i[] GetP(Vector2i p)
        {
            return [p, p + offsets[0], p + offsets[1], p + offsets[2], p + offsets[3], p + offsets[4], p + offsets[5], p + offsets[6], p + offsets[7]];
        }

        private static Vector2[] GetG(Vector2i[] p)
        {
            return [Hash2(p[0]), Hash2(p[1]) + offsets[0], Hash2(p[2]) + offsets[1], Hash2(p[3]) + offsets[2], Hash2(p[4]) + offsets[3], Hash2(p[5]) + offsets[4], Hash2(p[6]) + offsets[5], Hash2(p[7]) + offsets[6], Hash2(p[8]) + offsets[7]];
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
    }
}