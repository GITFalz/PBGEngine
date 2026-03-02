using PBG.MathLibrary;

namespace PBG.MathLibrary
{
    public static partial class Mathf
    {
        #region MIN
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Min(params float[] values)
        {
            float min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }
        
        public static double Min(double a, double b) => a < b ? a : b;
        public static double Min(params double[] values)
        {
            double min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }

        public static int Min(int a, int b) => a < b ? a : b;
        public static int Min(params int[] values)
        {
            int min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }

        public static uint Min(uint a, uint b) => a < b ? a : b;
        public static uint Min(params uint[] values)
        {
            uint min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }
        
        public static Vector2 Min(Vector2 a, Vector2 b) => (Min(a.X, b.X), Min(a.Y, b.Y));
        public static Vector2 Min(params Vector2[] values)
        {
            Vector2 min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }

        public static Vector2i Min(Vector2i a, Vector2i b) => (Min(a.X, b.X), Min(a.Y, b.Y));
        public static Vector2i Min(params Vector2i[] values)
        {
            Vector2i min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }
        
        public static Vector3 Min(Vector3 a, Vector3 b) => (Min(a.X, b.X), Min(a.Y, b.Y), Min(a.Z, b.Z));
        public static Vector3 Min(params Vector3[] values)
        {
            Vector3 min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }

        public static Vector3i Min(Vector3i a, Vector3i b) => (Min(a.X, b.X), Min(a.Y, b.Y), Min(a.Z, b.Z));
        public static Vector3i Min(params Vector3i[] values)
        {
            Vector3i min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Min(min, values[i]);
            return min;
        }

        public static float Min(this float a, params float[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static double Min(this double a, params double[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static int Min(this int a, params int[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static uint Min(this uint a, params uint[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static Vector2 Min(this Vector2 a, params Vector2[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static Vector2i Min(this Vector2i a, params Vector2i[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static Vector3 Min(this Vector3 a, params Vector3[] others) => others.Length == 0 ? a : Min(a, Min(others));
        public static Vector3i Min(this Vector3i a, params Vector3i[] others) => others.Length == 0 ? a : Min(a, Min(others));

        public static void MinSet(this ref float a, params float[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref double a, params double[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref int a, params int[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref uint a, params uint[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref Vector2 a, params Vector2[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref Vector2i a, params Vector2i[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref Vector3 a, params Vector3[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        public static void MinSet(this ref Vector3i a, params Vector3i[] others) => a = others.Length == 0 ? a : Min(a, Min(others));
        #endregion



        #region Max
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Max(params float[] values)
        {
            float max = values[0];
            for (int i = 1; i < values.Length; i++)
                max = Max(max, values[i]);
            return max;
        }

        public static double Max(double a, double b) => a > b ? a : b;
        public static double Max(params double[] values)
        {
            double max = values[0];
            for (int i = 1; i < values.Length; i++)
                max = Max(max, values[i]);
            return max;
        }

        public static int Max(int a, int b) => a > b ? a : b;
        public static int Max(params int[] values)
        {
            int max = values[0];
            for (int i = 1; i < values.Length; i++)
                max = Max(max, values[i]);
            return max;
        }

        public static uint Max(uint a, uint b) => a > b ? a : b;
        public static uint Max(params uint[] values)
        {
            uint max = values[0];
            for (int i = 1; i < values.Length; i++)
                max = Max(max, values[i]);
            return max;
        }

        public static Vector2 Max(Vector2 a, Vector2 b) => (Max(a.X, b.X), Max(a.Y, b.Y));
        public static Vector2 Max(params Vector2[] values)
        {
            Vector2 min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Max(min, values[i]);
            return min;
        }

        public static Vector2i Max(Vector2i a, Vector2i b) => (Max(a.X, b.X), Max(a.Y, b.Y));
        public static Vector2i Max(params Vector2i[] values)
        {
            Vector2i min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Max(min, values[i]);
            return min;
        }

        public static Vector3 Max(Vector3 a, Vector3 b) => (Max(a.X, b.X), Max(a.Y, b.Y), Max(a.Z, b.Z));
        public static Vector3 Max(params Vector3[] values)
        {
            Vector3 min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Max(min, values[i]);
            return min;
        }

        public static Vector3i Max(Vector3i a, Vector3i b) => (Max(a.X, b.X), Max(a.Y, b.Y), Max(a.Z, b.Z));
        public static Vector3i Max(params Vector3i[] values)
        {
            Vector3i min = values[0];
            for (int i = 1; i < values.Length; i++)
                min = Max(min, values[i]);
            return min;
        }

        public static float Max(this float a, params float[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static double Max(this double a, params double[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static int Max(this int a, params int[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static uint Max(this uint a, params uint[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static Vector2 Max(this Vector2 a, params Vector2[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static Vector2i Max(this Vector2i a, params Vector2i[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static Vector3 Max(this Vector3 a, params Vector3[] others) => others.Length == 0 ? a : Max(a, Max(others));
        public static Vector3i Max(this Vector3i a, params Vector3i[] others) => others.Length == 0 ? a : Max(a, Max(others));

        public static void MaxSet(this ref float a, params float[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref double a, params double[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref int a, params int[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref uint a, params uint[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref Vector2 a, params Vector2[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref Vector2i a, params Vector2i[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref Vector3 a, params Vector3[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        public static void MaxSet(this ref Vector3i a, params Vector3i[] others) => a = others.Length == 0 ? a : Max(a, Max(others));
        #endregion
    }
}