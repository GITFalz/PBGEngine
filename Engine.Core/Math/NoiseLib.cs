using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using PBG.MathLibrary;

namespace PBG.Noise
{
    public class NoiseLib
    {
        #region Noise functions

        public static float Noise(float x)
        {
            var X = Mathf.FloorToInt(x) & 0xff;
            x -= Mathf.Floor(x);
            var u = Fade(x);
            return Lerp(u, Grad(perm[X], x), Grad(perm[X + 1], x - 1)) * 2;
        }

        public static float Noise(float x, float y)
        {
            var X = Mathf.FloorToInt(x) & 0xff;
            var Y = Mathf.FloorToInt(y) & 0xff;
            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);
            var u = Fade(x);
            var v = Fade(y);
            var A = (perm[X] + Y) & 0xff;
            var B = (perm[X + 1] + Y) & 0xff;
            return Lerp(v, Lerp(u, Grad(perm[A], x, y), Grad(perm[B], x - 1, y)),
            Lerp(u, Grad(perm[A + 1], x, y - 1), Grad(perm[B + 1], x - 1, y - 1)));
        }

        public static float Noise(int octaves, float x, float y)
        {
            var result = 0.0f;
            var amp = 1.0f;
            var freq = 1.0f;
            var max = 0.0f;
            for (var i = 0; i < octaves; i++)
            {
                result += Noise(x * freq, y * freq) * amp;
                max += amp;
                amp *= 0.5f;
                freq *= 2.0f;
            }
            return result / max;
        }

        public static float Noise(Vector2 coord)
        {
            return Noise(coord.X, coord.Y);
        }

        public static float Noise(float x, float y, float z)
        {
            var X = Mathf.FloorToInt(x) & 0xff;
            var Y = Mathf.FloorToInt(y) & 0xff;
            var Z = Mathf.FloorToInt(z) & 0xff;
            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);
            z -= Mathf.Floor(z);
            var u = Fade(x);
            var v = Fade(y);
            var w = Fade(z);
            var A = (perm[X] + Y) & 0xff;
            var B = (perm[X + 1] + Y) & 0xff;
            var AA = (perm[A] + Z) & 0xff;
            var BA = (perm[B] + Z) & 0xff;
            var AB = (perm[A + 1] + Z) & 0xff;
            var BB = (perm[B + 1] + Z) & 0xff;
            return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA], x, y, z), Grad(perm[BA], x - 1, y, z)),
                                Lerp(u, Grad(perm[AB], x, y - 1, z), Grad(perm[BB], x - 1, y - 1, z))),
                        Lerp(v, Lerp(u, Grad(perm[AA + 1], x, y, z - 1), Grad(perm[BA + 1], x - 1, y, z - 1)),
                                Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1), Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
        }

        public static float Noise(Vector3 coord)
        {
            return Noise(coord.X, coord.Y, coord.Z);
        }
        public static float AngleNoise(float x, float y)
        {
            float angle = Noise(x, y) * 3.14159265359f;
            angle = (float)Math.Atan2(Math.Sin(angle), Math.Cos(angle));
            return (angle / 3.14159265359f + 1.0f) * 0.5f;
        }
        public static float AngleNoise2(float x, float y)
        {
            float noiseX = Noise(x, y);
            float noiseY = Noise(x + 100.0f, y + 100.0f);
            float angle = (float)Math.Atan2(noiseY, noiseX);
            return (angle / 3.14159265359f + 1.0f) * 0.5f;
        }
        public static float AngleNoise(float x, float y, int octaves)
        {
            float noiseX = Fbm(x, y, octaves);
            float noiseY = Fbm(x + 100.0f, y + 100.0f, octaves);
            float angle = (float)Math.Atan2(noiseY, noiseX);
            return (angle / 3.14159265359f + 1.0f) * 0.5f;
        }
        public static float AngleNoise(Vector2 coord) { return AngleNoise(coord.X, coord.Y); }
        public static float AngleNoise(Vector2 coord, int octaves) { return AngleNoise(coord.X, coord.Y, octaves); }
        public static Vector2 AngleToDirection(float angleNoise)
        {
            float angle = (angleNoise * 2.0f - 1.0f) * 3.14159265359f;
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        #endregion

        #region fBm functions

        public static float Fbm(float x, int octave)
        {
            var f = 0.0f;
            var w = 0.5f;
            for (var i = 0; i < octave; i++)
            {
                f += w * Noise(x);
                x *= 2.0f;
                w *= 0.5f;
            }
            return f;
        }

        public static float Fbm(Vector2 coord, int octave)
        {
            var f = 0.0f;
            var w = 0.5f;
            for (var i = 0; i < octave; i++)
            {
                f += w * Noise(coord);
                coord *= 2.0f;
                w *= 0.5f;
            }
            return f;
        }

        public static float Fbm(float x, float y, int octave)
        {
            return Fbm(new Vector2(x, y), octave);
        }

        public static float Fbm(Vector3 coord, int octave)
        {
            var f = 0.0f;
            var w = 0.5f;
            for (var i = 0; i < octave; i++)
            {
                f += w * Noise(coord);
                coord *= 2.0f;
                w *= 0.5f;
            }
            return f;
        }

        public static float Fbm(float x, float y, float z, int octave)
        {
            return Fbm(new Vector3(x, y, z), octave);
        }

        #endregion

        #region Private functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Hash(uint x)
        {
            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;

            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Hash1(int x, uint seed)
        {
            uint h = seed;

            h ^= (uint)x * 0x9E3779B9u;

            return Hash(h);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Hash2(int x, int y, uint seed)
        {
            uint h = seed;

            h ^= (uint)x * 0x9E3779B9u;
            h ^= (uint)y * 0x85EBCA6Bu;

            return Hash(h);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Hash3(int x, int y, int z, uint seed)
        {
            uint h = seed;

            h ^= (uint)x * 0x9E3779B9u;
            h ^= (uint)y * 0x85EBCA6Bu;
            h ^= (uint)z * 0xC2B2AE35u;

            return Hash(h);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Grad(int hash, float x)
        {
            return (hash & 1) == 0 ? x : -x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Grad(int hash, float x, float y)
        {
            return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Grad(int hash, float x, float y, float z)
        {
            var h = hash & 15;
            var u = h < 8 ? x : y;
            var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        static int[] perm = {
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
            151
        };

        #endregion
    }

    [InternalSystemInit(InitPriority.Data)]
    public static unsafe class NoiseLibSimd
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
}