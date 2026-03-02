using System.Globalization;
using PBG.MathLibrary;

namespace PBG.Parse
{
    public static class Int
    {
        public static int Parse(string value, int replacement = 0)
        {
            if (value == null)
                return replacement;

            if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;

            return replacement;
        }

        public static int Parse(object value, int replacement = 0)
        {
            if (value == null)
                return replacement;

            switch (value)
            {
                case int i:
                    return i;
                case string s when int.TryParse(s, out var parsed):
                    return parsed;
                case IConvertible convertible:
                    try
                    {
                        return convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return replacement;
                    }
                default:
                    return replacement;
            }
        }
    }

    public static class Float
    {
        public static float Parse(string value, float replacement = 0f)
        {
            try
            {
                return float.Parse(value.Trim(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return replacement;
            }
        }

        public static float Parse(object value, float replacement = 0f)
        {
            if (value == null)
                return replacement;

            switch (value)
            {
                case float f:
                    return f;
                case double d:
                    return (float)d;
                case string s when float.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                                                    System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                    return parsed;
                case IConvertible convertible:
                    try
                    {
                        return convertible.ToSingle(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return replacement;
                    }
                default:
                    return replacement;
            }
        }

        public static bool TryParse(string value, out float result)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch (Exception)
            {
                result = 0f;
                return false;
            }
        }

        public static string Str(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    // ---------------------- VECTOR2 ----------------------
    public static class Vec2
    {
        public static Vector2 Parse(object value, Vector2 replacement = default)
        {
            if (value == null) return replacement;

            switch (value)
            {
                case Vector2 v: return v;
                case string s:
                    {
                        var parts = s.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2) return replacement;
                        if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                            return new Vector2(x, y);
                        return replacement;
                    }
                case float[] arr when arr.Length >= 2: return new Vector2(arr[0], arr[1]);
                case double[] arr when arr.Length >= 2: return new Vector2((float)arr[0], (float)arr[1]);
                default: return replacement;
            }
        }

        public static bool TryParse(string value, out Vector2 result)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        public static string Str(Vector2 value) => $"{value.X.ToString(CultureInfo.InvariantCulture)}, {value.Y.ToString(CultureInfo.InvariantCulture)}";
    }

    // ---------------------- VECTOR3 ----------------------
    public static class Vec3
    {
        public static Vector3 Parse(object value, Vector3 replacement = default)
        {
            if (value == null) return replacement;

            switch (value)
            {
                case Vector3 v: return v;
                case string s:
                    {
                        var parts = s.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3) return replacement;
                        if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                            return new Vector3(x, y, z);
                        return replacement;
                    }
                case float[] arr when arr.Length >= 3: return new Vector3(arr[0], arr[1], arr[2]);
                case double[] arr when arr.Length >= 3: return new Vector3((float)arr[0], (float)arr[1], (float)arr[2]);
                default: return replacement;
            }
        }

        public static bool TryParse(string value, out Vector3 result)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        public static string Str(Vector3 value)
            => $"{value.X.ToString(CultureInfo.InvariantCulture)}, {value.Y.ToString(CultureInfo.InvariantCulture)}, {value.Z.ToString(CultureInfo.InvariantCulture)}";
    }

    // ---------------------- VECTOR4 ----------------------
    public static class Vec4
    {
        public static Vector4 Parse(object value, Vector4 replacement = default)
        {
            if (value == null) return replacement;

            switch (value)
            {
                case Vector4 v: return v;
                case string s:
                    {
                        var parts = s.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 4) return replacement;
                        if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) &&
                            float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                            return new Vector4(x, y, z, w);
                        return replacement;
                    }
                case float[] arr when arr.Length >= 4: return new Vector4(arr[0], arr[1], arr[2], arr[3]);
                case double[] arr when arr.Length >= 4: return new Vector4((float)arr[0], (float)arr[1], (float)arr[2], (float)arr[3]);
                default: return replacement;
            }
        }

        public static bool TryParse(string value, out Vector4 result)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        public static string Str(Vector4 value)      
            => $"{value.X.ToString(CultureInfo.InvariantCulture)}, {value.Y.ToString(CultureInfo.InvariantCulture)}, {value.Z.ToString(CultureInfo.InvariantCulture)}, {value.W.ToString(CultureInfo.InvariantCulture)}";
    }

    public static class Bool
    {
        public static bool Parse(string value, bool replacement = false)
        {
            if (value == null)
                return replacement;

            if (bool.TryParse(value.Trim(), out var result))
                return result;

            return replacement;
        }

        public static bool Parse(object value, bool replacement = false)
        {
            if (value == null)
                return replacement;

            switch (value)
            {
                case bool b:
                    return b;
                case string s when bool.TryParse(s, out var parsed):
                    return parsed;
                case IConvertible convertible:
                    try
                    {
                        return convertible.ToBoolean(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return replacement;
                    }
                default:
                    return replacement;
            }
        }
    }
}