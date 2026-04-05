namespace PBG.UI;

public interface IStyleData
{
    public void Set(UIElementBase element);
}

public struct DataStyleData : IStyleData, IDisposable
{
    private List<DatasetValue> _values = [];
    public DataStyleData(string name, object value) { Add(name, value); }

    public void Add(string name, object value) => _values.Add(new() { Name = name, Value = value });
    public void Set(UIElementBase element)
    {
        for (int i = 0; i < _values.Count; i++) 
        {
            var set = _values[i];
            element.Dataset[set.Name] = set.Value;
        }
    }

    public void Dispose()
    {
        _values.Clear();
    }

    private struct DatasetValue
    {
        public string Name;
        public object Value;
    }
}

public struct ValueStyle(Action<UIElementBase> action) : IStyleData
{
    public void Set(UIElementBase element) => action.Invoke(element);
}

public struct UnaryStyle<T> : IStyleData
{
    private readonly T _v;
    private Action<T, UIElementBase> _action;

    public UnaryStyle(T v, Action<T, UIElementBase> action)
    {
        _action = action;
        _v = v;
    }

    public UnaryStyle(Action<T, UIElementBase> action)
    {
        _action = action;
    }

    public void Set(UIElementBase element) => _action.Invoke(_v, element);

    public readonly UnaryStyle<T> this[T v] => new(v, _action);
}

public struct BinaryStyle<T1, T2> : IStyleData
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private Action<T1, T2, UIElementBase> _action;

    public BinaryStyle(T1 v1, T2 v2, Action<T1, T2, UIElementBase> action)
    {
        _action = action;
        _v1 = v1;
        _v2 = v2;
    }

    public BinaryStyle(Action<T1, T2, UIElementBase> action)
    {
        _action = action;
    }

    public void Set(UIElementBase element) => _action.Invoke(_v1, _v2, element);

    public readonly BinaryStyle<T1, T2> this[T1 v1, T2 v2] => new(v1, v2, _action);
}

public struct TrinaryStyle<T1, T2, T3> : IStyleData
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private readonly T3 _v3;
    private Action<T1, T2, T3, UIElementBase> _action;

    public TrinaryStyle(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, UIElementBase> action)
    {
        _action = action;
        _v1 = v1;
        _v2 = v2;
        _v3 = v3;
    }

    public TrinaryStyle(Action<T1, T2, T3, UIElementBase> action)
    {
        _action = action;
    }

    public void Set(UIElementBase element) => _action.Invoke(_v1, _v2, _v3, element);

    public readonly TrinaryStyle<T1, T2, T3> this[T1 v1, T2 v2, T3 v3] => new(v1, v2, v3, _action);
}

public struct QuaternaryStyle<T1, T2, T3, T4> : IStyleData
{
    private readonly T1 _v1;
    private readonly T2 _v2;
    private readonly T3 _v3;
    private readonly T4 _v4;
    private Action<T1, T2, T3, T4, UIElementBase> _action;

    public QuaternaryStyle(T1 v1, T2 v2, T3 v3, T4 v4, Action<T1, T2, T3, T4, UIElementBase> action)
    {
        _action = action;
        _v1 = v1;
        _v2 = v2;
        _v3 = v3;
        _v4 = v4;
    }

    public QuaternaryStyle(Action<T1, T2, T3, T4, UIElementBase> action)
    {
        _action = action;
    }

    public void Set(UIElementBase element) => _action.Invoke(_v1, _v2, _v3, _v4, element);

    public readonly QuaternaryStyle<T1, T2, T3, T4> this[T1 v1, T2 v2, T3 v3, T4 v4] => new(v1, v2, v3, v4, _action);
}


public struct UnaryType<T>(
    Action<T, UIElementBase> styleFunc1
)
{
    private Action<T, UIElementBase> _styleFunc1 = styleFunc1;
    public readonly UnaryStyle<T> this[T value] => new(value, _styleFunc1);
}

public struct BinaryType<T1, T2>(
    Action<T1, UIElementBase> styleFunc1, 
    Action<T2, UIElementBase> styleFunc2
)
{
    private Action<T1, UIElementBase> _styleFunc1 = styleFunc1;
    private Action<T2, UIElementBase> _styleFunc2 = styleFunc2;
    public readonly UnaryStyle<T1> this[T1 value] => new(value, _styleFunc1);
    public readonly UnaryStyle<T2> this[T2 value] => new(value, _styleFunc2);
}

public struct TrinaryType<T1, T2, T3>(
    Action<T1, UIElementBase> styleFunc1, 
    Action<T2, UIElementBase> styleFunc2, 
    Action<T3, UIElementBase> styleFunc3
)
{
    private Action<T1, UIElementBase> _styleFunc1 = styleFunc1;
    private Action<T2, UIElementBase> _styleFunc2 = styleFunc2;
    private Action<T3, UIElementBase> _styleFunc3 = styleFunc3;
    public readonly UnaryStyle<T1> this[T1 value] => new(value, _styleFunc1);
    public readonly UnaryStyle<T2> this[T2 value] => new(value, _styleFunc2);
    public readonly UnaryStyle<T3> this[T3 value] => new(value, _styleFunc3);
}

public struct QuaternaryType<T1, T2, T3, T4>(
    Action<T1, UIElementBase> styleFunc1, 
    Action<T2, UIElementBase> styleFunc2, 
    Action<T3, UIElementBase> styleFunc3,
    Action<T4, UIElementBase> styleFunc4
) where T1 : struct where T2 : struct where T3 : struct where T4 : struct
{
    private Action<T1, UIElementBase> _styleFunc1 = styleFunc1;
    private Action<T2, UIElementBase> _styleFunc2 = styleFunc2;
    private Action<T3, UIElementBase> _styleFunc3 = styleFunc3;
    private Action<T4, UIElementBase> _styleFunc4 = styleFunc4;

    public readonly UnaryStyle<T1> this[T1 value] => new(value, _styleFunc1);
    public readonly UnaryStyle<T2> this[T2 value] => new(value, _styleFunc2);
    public readonly UnaryStyle<T3> this[T3 value] => new(value, _styleFunc3);
    public readonly UnaryStyle<T4> this[T4 value] => new(value, _styleFunc4);
}