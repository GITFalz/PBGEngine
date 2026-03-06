using PBG.MathLibrary;

namespace PBG.Hash
{
    public static class Hash
    {
        // Constants for better hash distribution
        private const uint PRIME1 = 2654435761U;
        private const uint PRIME2 = 2246822519U;
        private const uint PRIME3 = 3266489917U;
        private const uint PRIME4 = 668265263U;
        private const uint PRIME5 = 374761393U;

        // Default seed (can be changed globally or per-call)
        public static uint GlobalSeed { get; set; } = 3;

        /// <summary>
        /// Hash a float to a value between 0 and 1 with optional seed
        /// </summary>
        public static float HashFloat(float value, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint bits = BitConverter.SingleToUInt32Bits(value);

            // Mix in the seed
            bits ^= seed * PRIME5;

            // Hash the combined value
            bits ^= bits >> 16;
            bits *= PRIME1;
            bits ^= bits >> 13;
            bits *= PRIME2;
            bits ^= bits >> 16;

            return (bits & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Hash a Vector2 to a value between 0 and 1 with optional seed
        /// </summary>
        public static float HashVector2(Vector2 vec, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint x = BitConverter.SingleToUInt32Bits(vec.X);
            uint y = BitConverter.SingleToUInt32Bits(vec.Y);

            // Mix in the seed first
            uint hash = seed * PRIME5;
            hash = RotateLeft(hash, 13);

            // Combine with vector components
            hash += x * PRIME1;
            hash = RotateLeft(hash, 13);
            hash += y * PRIME2;
            hash = RotateLeft(hash, 13);
            hash *= PRIME3;

            // Final mixing
            hash ^= hash >> 16;
            hash *= PRIME4;
            hash ^= hash >> 13;
            hash *= PRIME1;
            hash ^= hash >> 16;

            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Hash a Vector2i to a value between 0 and 1 with optional seed
        /// </summary>
        public static float HashVector2i(Vector2i vec, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint x = (uint)vec.X;
            uint y = (uint)vec.Y;

            // Mix in the seed first
            uint hash = seed * PRIME5;
            hash = RotateLeft(hash, 15);

            // Wang hash variation for integers
            hash += x * PRIME1;
            hash = RotateLeft(hash, 15);
            hash += y * PRIME2;
            hash = RotateLeft(hash, 13);
            hash *= PRIME3;

            hash ^= hash >> 16;
            hash *= PRIME4;
            hash ^= hash >> 13;
            hash *= PRIME1;
            hash ^= hash >> 16;

            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Hash a Vector3 to a value between 0 and 1 with optional seed
        /// </summary>
        public static float HashVector3(Vector3 vec, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint x = BitConverter.SingleToUInt32Bits(vec.X);
            uint y = BitConverter.SingleToUInt32Bits(vec.Y);
            uint z = BitConverter.SingleToUInt32Bits(vec.Z);

            // Start with seed
            uint hash = seed * PRIME5;
            hash = RotateLeft(hash, 13);

            hash += x * PRIME1;
            hash = RotateLeft(hash, 13);
            hash += y * PRIME2;
            hash = RotateLeft(hash, 13);
            hash += z * PRIME3;
            hash = RotateLeft(hash, 13);
            hash *= PRIME4;

            hash ^= hash >> 16;
            hash *= PRIME1;
            hash ^= hash >> 13;
            hash *= PRIME2;
            hash ^= hash >> 16;

            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Hash a Vector3i to a value between 0 and 1 with optional seed
        /// </summary>
        public static float HashVector3i(Vector3i vec, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint x = (uint)vec.X;
            uint y = (uint)vec.Y;
            uint z = (uint)vec.Z;

            // Start with seed
            uint hash = seed * PRIME5;
            hash = RotateLeft(hash, 15);

            hash += x * PRIME1;
            hash = RotateLeft(hash, 15);
            hash += y * PRIME2;
            hash = RotateLeft(hash, 13);
            hash += z * PRIME3;
            hash = RotateLeft(hash, 15);
            hash *= PRIME4;

            hash ^= hash >> 16;
            hash *= PRIME1;
            hash ^= hash >> 13;
            hash *= PRIME2;
            hash ^= hash >> 16;

            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Hash a Vector4 to a value between 0 and 1 with optional seed
        /// </summary>
        public static float HashVector4(Vector4 vec, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint x = BitConverter.SingleToUInt32Bits(vec.X);
            uint y = BitConverter.SingleToUInt32Bits(vec.Y);
            uint z = BitConverter.SingleToUInt32Bits(vec.Z);
            uint w = BitConverter.SingleToUInt32Bits(vec.W);

            // Start with seed
            uint hash = seed * PRIME5;
            hash = RotateLeft(hash, 13);

            hash += x * PRIME1;
            hash = RotateLeft(hash, 13);
            hash += y * PRIME2;
            hash = RotateLeft(hash, 13);
            hash += z * PRIME3;
            hash = RotateLeft(hash, 13);
            hash += w * PRIME4;
            hash = RotateLeft(hash, 13);
            hash *= PRIME1;

            hash ^= hash >> 16;
            hash *= PRIME2;
            hash ^= hash >> 13;
            hash *= PRIME3;
            hash ^= hash >> 16;

            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Alternative hash function with different distribution characteristics
        /// Good for when you need different random patterns
        /// </summary>
        public static float HashFloat2(float value, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            uint bits = BitConverter.SingleToUInt32Bits(value);

            // Mix in seed
            bits ^= seed * PRIME5;

            // Alternative mixing function
            bits += ~(bits << 15);
            bits ^= bits >> 10;
            bits += bits << 3;
            bits ^= bits >> 6;
            bits += ~(bits << 11);
            bits ^= bits >> 16;

            return (bits & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>
        /// Fractional hash - extracts fractional part after scaling
        /// Useful for noise-like applications
        /// </summary>
        public static float FractionalHash(Vector2 vec, uint seed = 0)
        {
            if (seed == 0) seed = GlobalSeed;

            // Mix seed into the dot product calculation
            float seedFloat = (float)seed / uint.MaxValue; // Normalize seed to 0-1
            Vector2 seedVec = new Vector2(12.9898f + seedFloat, 78.233f + seedFloat * 0.5f);

            float dot = Vector2.Dot(vec, seedVec);
            return MathF.Abs(MathF.Sin(dot) * 43758.5453f) % 1.0f;
        }

        /// <summary>
        /// Convert a string to a seed value
        /// Useful for world names or other text-based seeds
        /// </summary>
        public static uint StringToSeed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            uint hash = 2166136261U; // FNV-1a hash
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619U;
            }
            return hash;
        }

        /// <summary>
        /// Generate multiple hash values from the same input with different internal seeds
        /// Useful when you need multiple uncorrelated random values from the same position
        /// </summary>
        public static float HashVector2Multi(Vector2 vec, int index, uint seed = 0)
        {
            return HashVector2(vec, seed + (uint)(index * PRIME5));
        }

        public static float HashVector2iMulti(Vector2i vec, int index, uint seed = 0)
        {
            return HashVector2i(vec, seed + (uint)(index * PRIME5));
        }

        // Helper function for bit rotation
        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
    }
}