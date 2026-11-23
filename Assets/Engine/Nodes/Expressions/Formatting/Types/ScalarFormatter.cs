using PBG.MathLibrary;

namespace PBG.Nodes;

public class ScalarFormatter : INodeCodeFormatter
{
    public string TypeToString(Type type) => type.Name;
    public string GetConstructor(Type type) => $"new {TypeToString(type)}(";
    public string GetConstructor(object value) => GetConstructor(value.GetType());

    public string Str(float value) => Parse.Float.Str(value);
    public string Str(int value) => value.ToString();
    public string Str(bool value) => value.ToString();

    public string CastIntStart() => "(int)(";
    public string CastIntEnd() => ")";
    public string CastFloatStart() => "(float)(";
    public string CastFloatEnd() => ")";

    public string GetVectorFieldName(int index)
    {
        return index switch
        {
            0 => "x",
            1 => "y",
            2 => "z",
            3 => "w",
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
            Vector2 v  =>  $"new Vector2({Str(v.X)}, {Str(v.Y)})",
            Vector2i v => $"new Vector2i({Str(v.X)}, {Str(v.Y)})",
            Vector3 v  =>  $"new Vector3({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)})",
            Vector3i v => $"new Vector3i({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)})",
            Vector4 v  =>  $"new Vector4({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)}, {Str(v.W)})",
            Vector4i v => $"new Vector4i({Str(v.X)}, {Str(v.Y)}, {Str(v.Z)}, {Str(v.W)})",
            _          => throw new Exception($"Unsupported type: {value.GetType()}")
        };
    }
}