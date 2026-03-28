using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Core;

public abstract class EditorWatcher
{
    public static List<EditorWatcher> EditorWatchers = [];

    public abstract void Update();

    public static void UpdateAll()
    {
        for (int i = 0; i < EditorWatchers.Count; i++)
            EditorWatchers[i].Update();
    }

    public static void Clear()
    {
        EditorWatchers = [];
    }

    public void SetValue(ref float value, float newValue, UIField field)
    {
        if (value != newValue && field.GetFloat() != newValue)
        {
            value = newValue;
            field.UpdateText(""+newValue);
        }
    }
}

public class FloatWatcher : EditorWatcher
{
    private Func<float> _action;
    private UIField _valueField;
    private float _value;

    public FloatWatcher(Func<float> action, UIField valueField)
    {
        var value = action.Invoke();
        _action = action;
        _valueField = valueField;
        _value = value; 
    }

    public override void Update()
    {
        var value = _action.Invoke();
        SetValue(ref _value, value, _valueField);
    }
}

public class Vector2Watcher : EditorWatcher
{
    private Func<Vector2> _action;
    private UIField _xField;
    private UIField _yField;
    private float _x;
    private float _y;

    public Vector2Watcher(Func<Vector2> action, UIField xField, UIField yField)
    {
        var vector = action.Invoke();
        _action = action;
        _xField = xField; _yField = yField;
        _x = vector.X; _y = vector.Y; 
    }

    public override void Update()
    {
        var vector = _action.Invoke();
        SetValue(ref _x, vector.X, _xField);
        SetValue(ref _y, vector.Y, _yField);
    }
}

public class Vector3Watcher : EditorWatcher
{
    private Func<Vector3> _action;
    private UIField _xField;
    private UIField _yField;
    private UIField _zField;
    private float _x;
    private float _y;
    private float _z;

    public Vector3Watcher(Func<Vector3> action, UIField xField, UIField yField, UIField zField)
    {
        var vector = action.Invoke();
        _action = action;
        _xField = xField; _yField = yField; _zField = zField;
        _x = vector.X; _y = vector.Y; _z = vector.Z;
    }

    public override void Update()
    {
        var vector = _action.Invoke();
        SetValue(ref _x, vector.X, _xField);
        SetValue(ref _y, vector.Y, _yField);
        SetValue(ref _z, vector.Z, _zField);
    }
}