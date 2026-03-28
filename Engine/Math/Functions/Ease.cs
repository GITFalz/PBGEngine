namespace PBG.MathLibrary;

public static class Ease
{
    public static float Apply(EasingType type, float a, float b, float t, float multiplier = 2)
    {
        return Mathf.Lerp(a, b, (float)Apply(type, t, multiplier));
    }

    public static Vector2 Apply(EasingType type, Vector2 a, Vector2 b, float t, float multiplier = 2)
    {
        return Mathf.Lerp(a, b, (float)Apply(type, t, multiplier));
    }

    public static Vector3 Apply(EasingType type, Vector3 a, Vector3 b, float t, float multiplier = 2)
    {
        return Mathf.Lerp(a, b, (float)Apply(type, t, multiplier));
    }

    public static Vector4 Apply(EasingType type, Vector4 a, Vector4 b, float t, float multiplier = 2)
    {
        return Mathf.Lerp(a, b, (float)Apply(type, t, multiplier));
    }

    public static Quaternion Apply(EasingType type, Quaternion a, Quaternion b, float t, float multiplier = 2)
    {
        return Quaternion.Slerp(a, b, (float)Apply(type, t, multiplier));
    }

    public static double Apply(EasingType type, float t, float multiplier)
    {
        multiplier = Mathf.Clampy(multiplier, 0.1f, 4f);
        return type switch
        {
            EasingType.Linear => t,
            EasingType.EaseIn => EaseIn(t, multiplier),
            EasingType.EaseOut => EaseOut(t, multiplier),
            EasingType.EaseInOut => EaseInOut(t, multiplier),
            EasingType.BounceIn => Bounce(t, multiplier),
            EasingType.ElasticIn => Elastic(t, multiplier),
            _ => t,
        };
    }

    public static float EaseIn(float t, float multiplier)
    {
        return Mathf.Pow(t, multiplier);
    }

    public static float EaseOut(float t, float multiplier)
    {
        return 1f - Mathf.Pow(1f - t, multiplier);
    }

    public static float EaseInOut(float t, float multiplier)
    {
        if (t < 0.5f)
        {
            return EaseIn(t * 2f, multiplier) / 2f;
        }
        else
        {
            return EaseOut((t - 0.5f) * 2f, multiplier) / 2f + 0.5f;
        }
    }

    public static float Bounce(float t, float multiplier)
    {
        if (t < 0.363636f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 0.727272f)
        {
            t -= 0.545454f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 0.909090f)
        {
            t -= 0.818181f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 0.954545f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    public static double Elastic(float t, float multiplier)
    {
        if (t == 0 || t == 1)
            return t;

        float p = 0.3f / multiplier;      // period
        float s = p / 4f;                 // phase offset

        return Math.Pow(2, -10 * t) * Math.Sin((t - s) * (2 * Math.PI) / p) + 1;
    }

    public static EasingType GetEasingType(int type)
    {
        return type switch
        {
            0 => EasingType.Linear,
            1 => EasingType.EaseIn,
            2 => EasingType.EaseOut,
            3 => EasingType.EaseInOut,
            4 => EasingType.BounceIn,
            5 => EasingType.ElasticIn,
            _ => EasingType.Linear,
        };
    }
}

public enum EasingType
{
    Linear = 0,
    EaseIn = 1,
    EaseOut = 2,
    EaseInOut = 3,
    BounceIn = 4,
    ElasticIn = 5
}