using System.Reflection;
using PBG.Core;

namespace PBG.Graphics;

[InternalSystemInit(InitPriority.Shader)]
public static class ShaderRegistry
{
    private static Dictionary<Type, Shader> _shaders = [];


    public static Descriptor GetDescriptorSet<T>() where T : class
    {
        if (!_shaders.TryGetValue(typeof(T), out var shader))
            throw new Exception($"[ERROR] : Could not find shader of type '{typeof(T)}' in the registry");
        
        return shader.GetDescriptorSet();
    }

    static void ResolveUniforms(Type type, Shader shader, string group = "")
    {
        var fields = AttributeManager.GetFields<UniformAttribute>(type, BindingFlags.Public | BindingFlags.Static);

        foreach (var (attr, field) in fields)
        {
            field.SetValue(null, shader.GetLocation(group + attr.Uniform));
        }

        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var uniformGroup = nested.GetCustomAttribute<UniformGroupAttribute>();
            if (uniformGroup != null)
                ResolveUniforms(nested, shader, $"{group}.{uniformGroup.Group}.");
        }
    }

    private static void Init()
    {
        var shaderAttributes = AttributeManager.GetAttribute<ShaderAttribute>();
        foreach (var (type, shaderAttr) in shaderAttributes)
        {
            var vertPath = shaderAttr.VertexShaderPath;
            var fragPath = shaderAttr.FragmentShaderPath;

            var info = new ShaderInfo()
            {
                VertexShaderFile = vertPath,
            };

            if (fragPath != null)
                info.FragmentShaderFile = fragPath;

            var shader = new Shader(info);

            shader.Compile();

            ResolveUniforms(type, shader);

            var shaderField = type.GetField("Shader", BindingFlags.Public | BindingFlags.Static);
            shaderField?.SetValue(null, shader);
        }
    }
}