using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using PBG.MathLibrary;

namespace PBG
{
    public readonly struct PString
    {
        public readonly string Value;
        public PString(string value) => Value = value;

        public static implicit operator PString(string s) => new PString(s);
        public static implicit operator string(PString s) => s.Value;

        public static PString operator /(PString a, PString b) => Path.Combine(a.Value, b.Value);
        public static PString operator /(string a, PString b) => Path.Combine(a, b.Value);
        public static PString operator /(PString a, string b) => Path.Combine(a.Value, b);
        public static PString operator /(PString a, string[] b) => a.Value.P(b);

        public string P(string pn) => Path.Combine(this, pn);
        public string P(params string[] pn)
        {
            string v = Value;
            for (int i = 0; i < pn.Length; i++)
                v = Path.Combine(v, pn[i]);
            return v;
        }
    }

    public static class String
    {
        public static string P(this PString pa, string pn) => Path.Combine(pa, pn);
        public static string P(this PString pa, params string[] pn)
        {
            for (int i = 0; i < pn.Length; i++)
                pa = Path.Combine(pa, pn[i]);
            return pa;
        }

        public static string P(this string pa, string pn) => Path.Combine(pa, pn);
        public static string P(this string pa, params string[] pn)
        {
            for (int i = 0; i < pn.Length; i++)
                pa = Path.Combine(pa, pn[i]);
            return pa;
        }

        public static PString P(this string p) => p;

        public static string Repeat(string value, int count)
        {
            return new StringBuilder(value.Length * count).Insert(0, value, count).ToString();
        }

        public static class Print
        {
            public static string Vec4(Vector4 vec)
            {
                return $"({Float(vec.X)}, {Float(vec.Y)}, {Float(vec.Z)}, {Float(vec.W)})";
            }

            public static string Vec3(Vector3 vec)
            {
                return $"({Float(vec.X)}, {Float(vec.Y)}, {Float(vec.Z)})";
            }

            public static string Vec2(Vector2 vec)
            {
                return $"({Float(vec.X)}, {Float(vec.Y)})";
            }

            public static string Float(float value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static class Parse
        {
            public static Vector4 Vec4(string str)
            {
                str = str.Trim(['(', ')', ' ']);
                string[] parts = str.Split([','], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4)
                    return Vector4.Zero;

                float x = Float(parts[0].Trim());
                float y = Float(parts[1].Trim());
                float z = Float(parts[2].Trim());
                float w = Float(parts[3].Trim());

                return new Vector4(x, y, z, w);
            }

            public static Vector3 Vec3(string str)
            {
                str = str.Trim(['(', ')']);
                string[] parts = str.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                    return Vector3.Zero;

                float x = Float(parts[0].Trim());
                float y = Float(parts[1].Trim());
                float z = Float(parts[2].Trim());

                return new Vector3(x, y, z);
            }

            public static Vector2 Vec2(string str)
            {
                str = str.Trim(['(', ')']);
                string[] parts = str.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    return Vector2.Zero;

                float x = Float(parts[0].Trim());
                float y = Float(parts[1].Trim());

                return new Vector2(x, y);
            }

            public static float Float(string str)
            {
                return float.Parse(str, CultureInfo.InvariantCulture);
            }

            public static float Float(string str, float replace)
            {
                return float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : replace;
            }
        }
    }
}