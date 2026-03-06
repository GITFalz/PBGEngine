public static class GLSLHelper
{
    public static string GLSLConvertTo(ValueType from, ValueType to, string variable)
    {
        if (from == to)
            return variable;

        return from switch
        {
            ValueType.Float => ConvertFloatTo(to, variable),
            ValueType.Int => ConvertIntTo(to, variable),
            ValueType.Vector2 => ConvertVector2To(to, variable),
            ValueType.Vector2i => ConvertVector2iTo(to, variable),
            ValueType.Vector3 => ConvertVector3To(to, variable),
            ValueType.Vector3i => ConvertVector3iTo(to, variable),
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, null),
        };
    }

    public static string ConvertFloatTo(ValueType to, string variable)
    {
        if (to == ValueType.Float)
            return variable;

        return to switch
        {
            ValueType.Int => $"int({variable})",
            ValueType.Vector2 => $"vec2({variable}, {variable})",
            ValueType.Vector2i => $"ivec2({variable}, {variable})",
            ValueType.Vector3 => $"vec3({variable}, {variable}, {variable})",
            ValueType.Vector3i => $"ivec3({variable}, {variable}, {variable})",
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
        };
    }

    public static string ConvertIntTo(ValueType to, string variable)
    {
        if (to == ValueType.Int)
            return variable;

        return to switch
        {
            ValueType.Float => $"float({variable})",
            ValueType.Vector2 => $"vec2({variable}, {variable})",
            ValueType.Vector2i => $"ivec2({variable}, {variable})",
            ValueType.Vector3 => $"vec3({variable}, {variable}, {variable})",
            ValueType.Vector3i => $"ivec3({variable}, {variable}, {variable})",
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
        };
    }

    public static string ConvertVector2To(ValueType to, string variable)
    {
        if (to == ValueType.Vector2)
            return variable;

        return to switch
        {
            ValueType.Float => $"{variable}.x",
            ValueType.Int => $"int({variable}.x)",
            ValueType.Vector2i => $"ivec2({variable})",
            ValueType.Vector3 => $"vec3({variable}, 0)",
            ValueType.Vector3i => $"ivec3({variable}, 0)",
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
        };
    }

    public static string ConvertVector2iTo(ValueType to, string variable)
    {
        if (to == ValueType.Vector2i)
            return variable;

        return to switch
        {
            ValueType.Float => $"float({variable}.x)",
            ValueType.Int => $"{variable}.x",
            ValueType.Vector2 => $"vec2({variable})",
            ValueType.Vector3 => $"vec3({variable}, 0)",
            ValueType.Vector3i => $"ivec3({variable}, 0)",
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
        };
    }

    public static string ConvertVector3To(ValueType to, string variable)
    {
        if (to == ValueType.Vector3)
            return variable;

        return to switch
        {
            ValueType.Float => $"{variable}.x",
            ValueType.Int => $"int({variable}.x)",
            ValueType.Vector2 => $"{variable}.xy",
            ValueType.Vector2i => $"ivec2({variable}.xy)",
            ValueType.Vector3i => $"ivec3({variable})",
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
        };
    }

    public static string ConvertVector3iTo(ValueType to, string variable)
    {
        if (to == ValueType.Vector3i)
            return variable;

        return to switch
        {
            ValueType.Float => $"float({variable}.x)",
            ValueType.Int => $"{variable}.x",
            ValueType.Vector2 => $"vec2({variable}.xy)",
            ValueType.Vector2i => $"{variable}.xy",
            ValueType.Vector3 => $"vec3({variable})",
            _ => throw new ArgumentOutOfRangeException(nameof(to), to, null)
        };
    }
}
