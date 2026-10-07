[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ShaderAttribute : Attribute 
{
    public string VertexShaderPath;
    public string? FragmentShaderPath;

    public ShaderAttribute(string vertexShaderPath, string? fragmentShaderPath = null)
    {
        VertexShaderPath = vertexShaderPath;
        FragmentShaderPath = fragmentShaderPath;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UniformGroupAttribute : Attribute 
{
    public string Group;
    
    public UniformGroupAttribute(string group)
    {
        Group = group;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class UniformAttribute : Attribute 
{
    public string Uniform;
    
    public UniformAttribute(string uniform)
    {
        Uniform = uniform;
    }
}