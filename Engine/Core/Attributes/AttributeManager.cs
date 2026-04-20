using System.Reflection;

namespace PBG.Core;

public static class AttributeManager
{
    public static IEnumerable<(Type Type, T Attribute)> GetAttribute<T>() where T : Attribute => GetAttribute<T>(AppDomain.CurrentDomain.GetAssemblies());
    public static IEnumerable<(Type Type, T Attribute)> GetAttribute<T>(Assembly[] assemblies) where T : Attribute 
    {
        return assemblies
        .SelectMany(a => a.GetTypes())
        .Where(t => t.IsDefined(typeof(T), inherit: false))
        .Select(t => (Type: t, Attribute: t.GetCustomAttribute<T>()))
        .Where(x => x.Attribute != null)!;
    }

    public static IOrderedEnumerable<(Type Type, T1 Attribute)> GetOrderedAttribute<T1, T2>(Assembly[] assemblies, Func<(Type Type, T1 Attribute), T2> order) where T1 : Attribute
    {
        var attributes = GetAttribute<T1>(assemblies);
        return attributes.OrderBy(order);
    }

    public static IOrderedEnumerable<(Type Type, T1 Attribute)> GetOrderedAttribute<T1, T2>(Assembly assembly, Func<(Type Type, T1 Attribute), T2> order) where T1 : Attribute
    {
        var attributes = GetAttribute<T1>([assembly]);
        return attributes.OrderBy(order);
    }

    public static IOrderedEnumerable<(Type Type, T1 Attribute)> GetOrderedAttribute<T1, T2>(Func<(Type Type, T1 Attribute), T2> order) where T1 : Attribute
    {
        var attributes = GetAttribute<T1>();
        return attributes.OrderBy(order);
    }

    public static void InvokeAttributeMethod<T>(IEnumerable<(Type Type, T _)> attributes, string methodName, BindingFlags bindingFlags)
    {
        foreach (var (type, _) in attributes) 
        {
            #if DEBUG
            Console.WriteLine("=== Invoking " + methodName + " for type " + type.Name + " ===");
            #endif
            var method = type.GetMethod(methodName, bindingFlags);
            #if DEBUG
            if (method == null)
                Console.WriteLine("Method not found");
            #endif
            method?.Invoke(null, null);
        }
    }
}