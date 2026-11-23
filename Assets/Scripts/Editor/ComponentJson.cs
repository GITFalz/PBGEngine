using System.ComponentModel;
using PBG;
using PBG.Editor;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Pipes;
using PBG.UI;

public class JsonComponentsGraph
{
    public List<JsonComponent>? Components { get; set; } = null;

    public void GenerateNodes(UIVScroll hierarchy)
    {
        if (Components == null)
            return;

        var node = new UICol(w_full, h_[45])[
            new UIImg(left_[8], top_[7], w_[30], h_[30], bg_white, icon_[50]),
            new UIText(EditorMenu.CurrentNodePath.Split('/')[^1], fs_[1.2f], left_[50], top_[10]),
            new UIText($"Node - {Components.Count} components", fs_[0.9f], gray_[70], left_[50], top_[25])
        ];

        hierarchy += node;

        foreach (var component in Components)
        {
            var sub = component.GenerateFields(hierarchy);
            
            if (sub != null)
            {
                hierarchy += EditorMenu.LightHsep;
                hierarchy += sub;
                hierarchy += EditorMenu.BlackHsep;
            }
        }
    }
}

public class JsonComponent
{
    public string? Name { get; set; } = null;
    public List<JsonValue>? Properties { get; set; } = null;
    public List<JsonValue>? Fields { get; set; } = null;

    public UIElementBase? GenerateFields(UIVScroll scroll)
    {
        if (Name == null)
            return null;

        var fieldCol = new UIVCol(grow_children, w_full, spacing_[2], invisible, padding_bottom_[5]);
        var icon = new UIButton(gray_[70], left_[5], icon_[7], middle_left, h_[20], w_[20]);

        UICol subCol = new UIVCol(grow_children, w_full, spacing_[2], ignore_invisible)[
            new UICol(h_[30], w_full, blank_full_g_[EditorMenu.HeaderGray]).OnClick(_ =>
            {
                bool visible = !fieldCol.Visible;
                fieldCol.SetVisible(!fieldCol.Visible);
                icon.UpdateIconIndex(visible ? 5 : 7);

                scroll.ApplyChanges(UIChange.Scale);
            })[
                icon,
                new UIText(Name.Split(".")[^1], fs_[1.2f], bg_white, left_[30], middle_left)
                
            ],
            fieldCol
        ];

        if (Properties != null)
        foreach (var property in Properties)
        {
            var sub = property.GenerateField(value => property.SetValue(EditorMenu.CurrentNodePath, Name, value));
            fieldCol += sub;
        }

        if (Fields != null)
        foreach (var field in Fields)
        {
            var sub = field.GenerateField(value => field.SetValue(EditorMenu.CurrentNodePath, Name, value));
            fieldCol += sub;
        }

        return subCol;
    }
}

public class JsonValue
{
    public string? Name { get; set; } = null;
    public string? Type { get; set; } = null;
    public string? Expandable { get; set; } = null;
    public string? Count { get; set; } = null;
    public string? Value { get; set; } = null;

    UIElementBase NameSection(string name, UIElementBase? sub = null)
    {
        return new UICol(h_[20], w_full)[
            new UIText(name.Length > 15 ? $"{name[..12]}..." : name, gray_[70], left_[5], mc_[name.Length.Min(15)], middle_left, fs_[0.9f]),
            sub
        ];
    }

    UIElementBase BaseValue(string name, UIElementBase? sub = null)
    {
        return NameSection(name, 
            new UICol(w_full_minus_[115], grow_children, middle_right, right_[5])[
                sub
            ]
        );
    }

    UIElementBase CreateField(string name, object value, IStyleData text_type, out UIField field) => 
    CreateField(name, value, text_type, null, out field);

    UIElementBase CreateField(string name, object value, IStyleData text_type, Action<UIField> onTextChange) => 
    CreateField(name, value, text_type, onTextChange, out _);

    UIElementBase CreateField(string name, object value, IStyleData text_type, Action<UIField>? onTextChange, out UIField field)
    {
        return BaseValue(name, 
            new UICol(middle_left, w_full, h_[20], blank_full_g_[10])[
                new UIField(value+"", middle_left, text_type, left_[5], mc_[20], fs_[0.9f]).OnTextChange(onTextChange).Out(out field)
            ]  
        );
    } 

    public UIElementBase GenerateField(Action<string> setValue)
    {
        string name = Name ?? "N/A";
        var col = new UIVCol(grow_children, w_full, spacing_[2]);

        if (Value != null && Type != null)
        {
            var value = JsonHelper.Deserialize(Value, Type);
            if (value is bool b)
            {
                col += BaseValue(name, 
                    new UICol(middle_left, w_[16], h_[16], blank_full_g_[10], b ? selected : empty).OnClick(col => {
                        col.IsSelected = !col.IsSelected;
                        var img = col.GetElement<UIImg>();
                        img?.UpdateColor(col.IsSelected ? "#3d7a4d" : "#1a1a1a");
                        setValue.Invoke(col.IsSelected.ToString());
                    })[
                        new UIImg(middle_center, w_[12], h_[12], blank_full, hex_[b ? "#3d7a4d" : "#1a1a1a"])
                    ]
                );
            }
            else if (value is int i)
            {
                col += CreateField(name, i, text_type_numeric, field => setValue.Invoke(field.GetInt().ToString()));
            }
            else if (value is float f)
            {
                col += CreateField(name, f, text_type_decimal, field => setValue.Invoke(field.GetFloat().ToString()));
            }
            else if (value is IVector vector)
            {
                IStyleData text_type = vector is IVector<float> ? text_type_decimal : text_type_numeric;

                col += NameSection(name);

                col += CreateField("X", vector.GetElementAt(0), text_type, out var vx);
                col += CreateField("Y", vector.GetElementAt(1), text_type, out var vy);

                Action<UIField>? onTextChange = null;

                if (vector.ElementCount == 2)
                {
                    if (value is Vector2)
                        onTextChange = _ => setValue.Invoke(new Vector2(vx.GetFloat(), vy.GetFloat()).Serialize());
                    else if (value is Vector2i)
                        onTextChange = _ => setValue.Invoke(new Vector2i(vx.GetInt(), vy.GetInt()).Serialize());
                    else if (value is Vector2u)
                        onTextChange = _ => setValue.Invoke(new Vector2u(vx.GetUInt(), vy.GetUInt()).Serialize());
                }
                else if (vector.ElementCount == 3)
                {
                    col += CreateField("Z", vector.GetElementAt(2), text_type, out var vz);

                    if (value is Vector3)
                        onTextChange = _ => setValue.Invoke(new Vector3(vx.GetFloat(), vy.GetFloat(), vz.GetFloat()).Serialize());
                    else if (value is Vector3i)
                        onTextChange = _ => setValue.Invoke(new Vector3i(vx.GetInt(), vy.GetInt(), vz.GetInt()).Serialize());
                    else if (value is Vector3u)
                        onTextChange = _ => setValue.Invoke(new Vector3u(vx.GetUInt(), vy.GetUInt(), vz.GetUInt()).Serialize());

                    vz.SetOnTextChange(onTextChange);
                }
                else if (vector.ElementCount == 4)
                {
                    col += CreateField("Z", vector.GetElementAt(2), text_type, out var vz);
                    col += CreateField("W", vector.GetElementAt(3), text_type, out var vw);

                    if (value is Vector4)
                        onTextChange = _ => setValue.Invoke(new Vector4(vx.GetFloat(), vy.GetFloat(), vz.GetFloat(), vw.GetFloat()).Serialize());
                    else if (value is Vector4i)
                        onTextChange = _ => setValue.Invoke(new Vector4i(vx.GetInt(), vy.GetInt(), vz.GetInt(), vw.GetInt()).Serialize());
                    else if (value is Vector4u)
                        onTextChange = _ => setValue.Invoke(new Vector4u(vx.GetUInt(), vy.GetUInt(), vz.GetUInt(), vw.GetUInt()).Serialize());

                    vz.SetOnTextChange(onTextChange);
                    vw.SetOnTextChange(onTextChange);
                }

                vx.SetOnTextChange(onTextChange);
                vy.SetOnTextChange(onTextChange);
            }
        }
            
        return col;
    }

    public void SetValue(string? path, string className, string value)
    {
        JsonHelper json = new();

        if (path != null) 
            json.Serialize("Path", path);

        json.Serialize("Class", className);
        json.Serialize("Name", Name);
        json.Serialize("Type", Type);
        json.Serialize("Value", value);
        GameWindow.Send(CommandType.SetField, json.ToString());
    }
}