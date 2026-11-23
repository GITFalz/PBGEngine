
using PBG.MathLibrary;

namespace PBG.Nodes;

public class Avx2Formatter : INodeCodeFormatter
{
    public string TypeToString(Type type)
    {
        return type switch
        {
            _ when type == typeof(bool)    => "bool",
            _ when type == typeof(float)   => "Vector256<float>",
            _ when type == typeof(int)     => "Vector256<int>",
            _ when type == typeof(Vector2) => "Avx2Vector2",
            _ when type == typeof(Vector2i)=> "Avx2Vector2i",
            _ when type == typeof(Vector3) => "Avx2Vector3",
            _ when type == typeof(Vector3i)=> "Avx2Vector3i",
            _ when type == typeof(Vector4) => "Avx2Vector4",
            _ when type == typeof(Vector4i)=> "Avx2Vector4i",
            _ => throw new Exception($"Cannot convert type '{type}' to GLSL type"),
        };
    }
    public string GetConstructor(Type type) => $"new {TypeToString(type)}(";
    public string GetConstructor(object value) => GetConstructor(value.GetType());

    public string Str(float value) => $"Vector256.Create({Parse.Float.Str(value)})";
    public string Str(int value) => $"Vector256.Create({value})";
    public string Str(bool value) => value.ToString();

    public string CastIntStart() => "(";
    public string CastIntEnd() => ").ToInt()";
    public string CastFloatStart() => "(";
    public string CastFloatEnd() => ").ToFloat()";

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
            Vector2 v  =>  $"new Avx2Vector2({Str(v.X)}, {Str(v.Y)})",
            Vector2i v => $"new Avx2Vector2i({Str(v.X)}, {Str(v.Y)})",
            Vector3 v  =>  $"new Avx2Vector3({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)})",
            Vector3i v => $"new Avx2Vector3i({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)})",
            Vector4 v  =>  $"new Avx2Vector4({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)}, {Str(v.W)})",
            Vector4i v => $"new Avx2Vector4i({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)}, {Str(v.W)})",
            _          => throw new Exception($"Unsupported type: {value.GetType()}")
        };
    }
}