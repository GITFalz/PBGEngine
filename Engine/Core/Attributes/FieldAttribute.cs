using System.Reflection;

namespace PBG.Core;

public class FieldAttribute : Attribute
{
    public string? Name;

    public FieldAttribute(string? name = null)
    {
        Name = name;
    }
}