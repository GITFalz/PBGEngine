
using PBG.MathLibrary;

namespace PBG.Nodes;

public class GLSLFormatter : INodeCodeFormatter
{
    public string TypeToString(Type type)
    {
        return type switch
        {
            _ when type == typeof(bool)    => "bool",
            _ when type == typeof(float)   => "float",
            _ when type == typeof(int)     => "int",
            _ when type == typeof(Vector2) => "vec2",
            _ when type == typeof(Vector2i)=> "ivec2",
            _ when type == typeof(Vector3) => "vec3",
            _ when type == typeof(Vector3i)=> "ivec3",
            _ when type == typeof(Vector4) => "vec4",
            _ when type == typeof(Vector4i)=> "ivec4",
            _ => throw new Exception($"Cannot convert type '{type}' to GLSL type"),
        };
    }
    public string GetConstructor(Type type) => $"{TypeToString(type)}(";
    public string GetConstructor(object value) => GetConstructor(value.GetType());

    public string Str(float value) => Parse.Float.Str(value);
    public string Str(int value) => value.ToString();
    public string Str(bool value) => value ? "true" : "false";

    public string CastIntStart() => "int(";
    public string CastIntEnd() => ")";
    public string CastFloatStart() => "float(";
    public string CastFloatEnd() => ")";

    public string GetVectorFieldName(int index)
    {
        return index switch
        {
            0 => "X",
            1 => "Y",
            2 => "Z",
            3 => "W",
            _ => throw new Exception($"{index} does not have a vector field")
        };
    }

    public string ObjectToString(object value)
    {
        return value switch
        {
            bool b     => Str(b),
            float v    => Str(v),
            int v      => Str(v),
            Vector2 v  =>  $"vec2({Str(v.X)}, {Str(v.Y)})",
            Vector2i v => $"ivec2({Str(v.X)}, {Str(v.Y)})",
            Vector3 v  =>  $"vec3({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)})",
            Vector3i v => $"ivec3({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)})",
            Vector4 v  =>  $"vec4({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)}, {Str(v.W)})",
            Vector4i v => $"ivec4({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)}, {Str(v.W)})",
            _          => throw new Exception($"Unsupported type: {value.GetType()}")
        };
    }
}