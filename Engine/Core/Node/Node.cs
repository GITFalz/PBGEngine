using System.Diagnostics.CodeAnalysis;

namespace PBG.Core;

public abstract class Node
{
    public Scene Scene = null!;
    public List<TransformNode> Children = [];

    public TransformNode GetNode(string path)
    {
        string[] names = path.Split('/');
        if (names.Length == 0)
            throw new Exception("Path cannot be empty");

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.Name == names[0])
            {
                if (names.Length == 1)
                {
                    // Base case: last name in path
                    return child;
                }
                else
                {
                    // Recursive case: create the subpath for the child
                    string subPath = string.Join("/", names, 1, names.Length - 1);
                    return child.GetNode(subPath);
                }
            }
        }

        // If no child matches
        throw new Exception($"Node not found: {path}");
    }

    public bool GetNode(string path, [NotNullWhen(true)] out TransformNode? node)
    {
        node = null;
        string[] names = path.Split('/');
        if (names.Length == 0)
            return false;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.Name == names[0])
            {
                if (names.Length == 1)
                {
                    // Base case: last name in path
                    node = child;
                    return true;
                }
                else
                {
                    // Recursive case: create the subpath for the child
                    string subPath = string.Join("/", names, 1, names.Length - 1);
                    return child.GetNode(subPath, out node);
                }
            }
        }

        // If no child matches
        return false;
    }

    public virtual void InitAwake()
    {
        for (int i = 0; i < Children.Count; i++)
            Children[i].InitAwake();
    }

    internal virtual void InitLoop(Scene scene)
    {
        for (int i = 0; i < Children.Count; i++)
            Children[i].InitLoop(scene);
    }

    public T QueryComponent<T>() where T : ScriptingNode
    {
        if (QueryComponent<T>(out var c))
            return c;
        throw new Exception("No component found of type: " + typeof(T));
    }

    public List<T> QueryComponents<T>() where T : ScriptingNode
    {
        List<T> components = [];
        QueryComponents(components);
        return components;
    }

    public bool QueryComponent<T>([NotNullWhen(true)] out T? component) where T : ScriptingNode
    {
        component = null;
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.GetComponent(out component) || child.QueryComponent(out component))
            {
                return true;
            }
        }
        return false;
    }

    public string GetUniqueName() => GetUniqueName("Collection");
    public string GetUniqueName(string name)
    {
        HashSet<string> names = [.. Children.Select(c => c.Name)];
        int n = 1;
        string newName = name;
        while (names.Contains(newName))
        {
            newName = $"{name} {n}";
            n++;
        }
        return newName;
    }

    public void QueryComponents<T>(List<T> components) where T : ScriptingNode
    {
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.GetComponents(components);
            child.QueryComponents(components);
        }
    }

    public void RemoveChild(TransformNode node)
    {
        Children.Remove(node);
    }

    public virtual void Delete() {}
    public void DeleteChildren()
    {
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Dispose();
        }
        Children.Clear();
    }
}