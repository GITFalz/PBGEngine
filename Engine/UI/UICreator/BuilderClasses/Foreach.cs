namespace PBG.UI;

public class UIList(List<IUIChild> children) : IUIChild
{
    public void AddTo(UICol parent)
    {
        for (int i = 0; i < children.Count; i++)
            children[i].AddTo(parent);
    }
}

public class UIArray(IUIChild[] children) : IUIChild
{
    public void AddTo(UICol parent)
    {
        for (int i = 0; i < children.Length; i++)
            children[i].AddTo(parent);
    }
}

public class Run : IUIChild
{
    private readonly IUIChild? _element = null;

    public Run(Func<IUIChild[]?> action)
    {
        var elements = action.Invoke();
        _element = elements == null ? null : new UIArray(elements);
    }

    public Run(Func<IUIChild?> action)
    {
        _element = action.Invoke();
    }

    public Run(Action action)
    {
        action.Invoke();
        _element = null;
    }

    public void AddTo(UICol parent) => _element?.AddTo(parent);
}

public class Foreach<T> : IUIChild
{
    private List<IUIChild> _elements = [];

    public Foreach(IEnumerable<T> data, Func<T, IUIChild?> action)
    {
        foreach (var item in data)
        {
            var element = action(item);
            if (element != null)
                _elements.Add(element);
        }
    }

    public Foreach(IEnumerable<T> data, Func<int, T, IUIChild?> action)
    {
        int i = 0;
        foreach (var item in data)
        {
            var element = action(i, item);
            if (element != null)
                _elements.Add(element);
            i++;
        }
    }

    public void AddTo(UICol parent)
    {
        for (int i = 0; i < _elements.Count; i++)
            _elements[i].AddTo(parent);
    }
}

public class Foreach<TKey, TValue> : IUIChild
{
    private List<IUIChild> _elements = [];

    public Foreach(IEnumerable<KeyValuePair<TKey, TValue>> data, Func<TKey, TValue, IUIChild?> action)
    {
        foreach (var kvp in data)
        {
            var element = action(kvp.Key, kvp.Value);
            if (element != null)
                _elements.Add(element);
        }
    }

    public void AddTo(UICol parent)
    {
        for (int i = 0; i < _elements.Count; i++)
            _elements[i].AddTo(parent);
    }
}

public class Forloop : IUIChild
{
    private List<IUIChild> _elements = [];

    public Forloop(uint start, uint count, Func<IUIChild?> action)
    {
        for (uint i = start; i < count; i++)
        {
            var element = action();
            if (element != null)
                _elements.Add(element);
        }
    }

    public Forloop(int start, int count, Func<IUIChild?> action)
    {
        for (int i = start; i < count; i++)
        {
            var element = action();
            if (element != null)
                _elements.Add(element);
        }
    }

    public Forloop(uint start, uint count, Func<uint, IUIChild?> action)
    {
        for (uint i = start; i < count; i++)
        {
            var element = action(i);
            if (element != null)
                _elements.Add(element);
        }
    }

    public Forloop(int start, int count, Func<int, IUIChild?> action)
    {
        for (int i = start; i < count; i++)
        {
            var element = action(i);
            if (element != null)
                _elements.Add(element);
        }
    }

    public void AddTo(UICol parent)
    {
        for (int i = 0; i < _elements.Count; i++)
            _elements[i].AddTo(parent);
    }
}

public class If : IUIChild
{
    private IUIChild? _child = null;

    public If(bool condition, Func<IUIChild> action)
    {
        if (condition) _child = action();
    }

    public If(bool condition, Func<IUIChild[]> action)
    {
        if (condition) _child = new UIArray(action());
    }

    public void AddTo(UICol parent) => _child?.AddTo(parent);
}
