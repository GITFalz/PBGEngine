using PBG.Graphics;
using PBG.UI;
using PBG.MathLibrary;
using PBG.UI.Creator;
using static PBG.UI.Styles;
using PBG.Data;
using PBG.Core;
using PBG;

public class ColorPicker : ScriptingNode
{
    /*
    private static ShaderProgram _pickerShader = new ShaderProgram("Painting/Rectangle.vert", "Painting/Picker.frag");
    private static ShaderProgram _pickerBarShader = new ShaderProgram("Painting/Rectangle.vert", "Painting/PickerBar.frag");

    private static VAO _colorPickerVao = new VAO();

    private static int _barModelLocation = _pickerBarShader.GetLocation("model");
    private static int _barProjectionLocation = _pickerBarShader.GetLocation("projection");
    private static int _barSizeLocation = _pickerBarShader.GetLocation("size");

    private static int _pickerModelLocation = _pickerShader.GetLocation("model");  
    private static int _pickerProjectionLocation = _pickerShader.GetLocation("projection");
    private static int _pickerSizeLocation = _pickerShader.GetLocation("size");
    private static int _pickerColorLocation = _pickerShader.GetLocation("color");
    */


    public int Width;
    public int Height;

    public bool Hovering = false;

    public Vector2i ColorPickerPosition = new Vector2i(94, 80);
    

    public float ColorPickerSize
    {
        get => 1 / _colorPickerSize;
        set
        {
            _colorPickerSize = 1 / value;
            _colorPickerScale = new Vector2(Width, Height) / _colorPickerSize;
        }
    }

    public Vector3 BaseColor = new Vector3(1, 0, 0);
    public Vector4 Color = new Vector4(1, 0, 0, 1f);
    public float Saturation = 0f;
    public float Brightness = 1f;

    private UIField RedField = null!;
    private UIField GreenField = null!;
    private UIField BlueField = null!;

    private Vector2i _colorPickerPosition = new Vector2i(100, 100);
    private Vector2 _colorPickerScale = new Vector2(1, 1);

    private float _colorPickerSize = 2f;

    public UI Ui;
    public UIController ColorPickerController;

    public Action<Vector4> SetColorAction = _ => { };

    public ColorPicker(int width, int height, Vector2i position)
    {
        Width = 300;
        Height = 200;

        _colorPickerPosition = position;

        Vector2i newPosition = ((int)_colorPickerPosition.X, (int)-_colorPickerPosition.Y + (Game.Height - Height));
        ColorPickerPosition = newPosition;
        ColorPickerSize = 1f;
    }

    void Start()
    {
        ColorPickerController = Transform.GetComponent<UIController>();
        Ui = new(this, _colorPickerPosition);
        ColorPickerController.AddElement(Ui);
        Transform.Disabled = true;
    }

    public bool IsHovering() => Hovering && !Transform.Disabled;
    
    public class UI(ColorPicker Picker, Vector2i Position) : UIScript { public UIImg ColorBarSlider = null!; public UIImg ColorPickSlider = null!; public override UIElementBase Script() =>
    new UICol(Class(left_[Position.X - 5], top_[Position.Y - 30], w_[Picker.Width + 10], h_[Picker.Height + 35]),
    OnHover<UICol>(_ => Picker.Hovering = true),
    Sub(
        new UICol(Class(w_full, h_[25], blank_full_g_[30]), OnHold<UICol>(_ =>
        {
            Vector2 mouseDelta = Input.GetMousePosition();
            if (mouseDelta == Vector2.Zero) return;
            Picker.UpdateColorPickerPosition();
        }),
        Sub(
            new UIImg(Class(w_[20], h_[20], icon_[15], middle_right, right_[2], bg_white, hover_scale_[1.2f], hover_scale_duration_[0.25f], hover_scale_easeout), OnClick<UIImg>(_ =>
            {
                Picker.Hovering = true;
                Picker.Transform.Disabled = true;
                Picker.SetColorAction = _ => { };
            }))
        )),
        new UIImg(Class(w_full, h_full_minus_[25], bottom_center, blank_full_g_[20], border_ui_[2, 2, 2, 2], border_color_g_[30])),
        new UICol(Class(w_[Mathf.Min(Picker.Width, Picker.Height - 20)], h_[Mathf.Min(Picker.Width, Picker.Height - 20)], bottom_left, left_[5], bottom_[25]), 
        OnHold<UICol>(_ => SetSliderColor(Input.GetMousePosition().X - Picker.ColorPickerPosition.X, Input.GetMousePosition().Y - (Game.Height - Picker.ColorPickerPosition.Y) + Picker.Height)), Sub(
            newImg(Class(w_[10], h_[10], right_[5], bottom_[5], depth_[10], blank_full_g_[100], border_ui_[3, 3, 3, 3], border_color_g_[40]), ref ColorPickSlider)
        )),
        new UIVCol(Class(w_[115], h_[Mathf.Min(Picker.Width, Picker.Height - 20)], top_right, top_[30], right_[5], grow_children, spacing_[5]), Sub(
            new UICol(Class(w_[115], h_[30]), [
                new UIText("R", Class(middle_left, left_[5])),
                ..Run(() =>
                {
                    Picker.RedField = new UIField("0", Class(mc_[3], fs_[1], middle_left));
                    return [
                    new UICol(Class(w_full_minus_[60], h_[30], blank_full_g_[10], left_[20], border_[5, 0, 0, 0]), Sub(
                        Picker.RedField
                    )),
                    new UICol(Class(w_[20], h_full, top_right, right_[20], blank_full, hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.3f, 0.3f, 0.3f, 1f)], hover_color_duration_[0.2f], hover_color_easeout), 
                    OnClickCol(c => SetRGB(Picker.RedField.GetByte() - 1, null, null)), [
                        new UIImg(Class(w_[20], h_[20], icon_[2], bg_white, middle_center))
                    ]),
                    new UICol(Class(w_[20], h_full, top_right, blank_full, hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.3f, 0.3f, 0.3f, 1f)], hover_color_duration_[0.2f], hover_color_easeout), 
                    OnClickCol(c => SetRGB(Picker.RedField.GetByte() + 1, null, null)), [
                        new UIImg(Class(w_[20], h_[20], icon_[0], bg_white, middle_center))
                    ])];
                })
            ]),
            new UICol(Class(w_[115], h_[30]), [
                new UIText("G", Class(middle_left, left_[5])),
                ..Run(() =>
                {
                    Picker.GreenField = new UIField("0", Class(mc_[3], fs_[1], middle_left));
                    return [
                    new UICol(Class(w_full_minus_[60], h_[30], blank_full_g_[10], left_[20], border_[5, 0, 0, 0]), Sub(
                        Picker.GreenField
                    )),
                    new UICol(Class(w_[20], h_full, top_right, right_[20], blank_full, hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.3f, 0.3f, 0.3f, 1f)], hover_color_duration_[0.2f], hover_color_easeout), 
                    OnClickCol(c => SetRGB(null, Picker.GreenField.GetByte() - 1, null)), [
                        new UIImg(Class(w_[20], h_[20], icon_[2], bg_white, middle_center))
                    ]),
                    new UICol(Class(w_[20], h_full, top_right, blank_full, hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.3f, 0.3f, 0.3f, 1f)], hover_color_duration_[0.2f], hover_color_easeout), 
                    OnClickCol(c => SetRGB(null, Picker.GreenField.GetByte() + 1, null)), [
                        new UIImg(Class(w_[20], h_[20], icon_[0], bg_white, middle_center))
                    ])];
                })
            ]),
            new UICol(Class(w_[115], h_[30]), [
                new UIText("B", Class(middle_left, left_[5])),
                ..Run(() =>
                {
                    Picker.BlueField = new UIField("0", Class(mc_[3], fs_[1], middle_left));
                    return [
                    new UICol(Class(w_full_minus_[60], h_[30], blank_full_g_[10], left_[20], border_[5, 0, 0, 0]), Sub(
                        Picker.BlueField
                    )),
                    new UICol(Class(w_[20], h_full, top_right, right_[20], blank_full, hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.3f, 0.3f, 0.3f, 1f)], hover_color_duration_[0.2f], hover_color_easeout), 
                    OnClickCol(c => SetRGB(null, null, Picker.BlueField.GetByte() - 1)), [
                        new UIImg(Class(w_[20], h_[20], icon_[2], bg_white, middle_center))
                    ]),
                    new UICol(Class(w_[20], h_full, top_right, blank_full, hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.3f, 0.3f, 0.3f, 1f)], hover_color_duration_[0.2f], hover_color_easeout), 
                    OnClickCol(c => SetRGB(null, null, Picker.BlueField.GetByte() + 1)), [
                        new UIImg(Class(w_[20], h_[20], icon_[0], bg_white, middle_center))
                    ])];
                })
            ])
        )),
        new UICol(Class(w_[Picker.Width], h_[20], bottom_[5], bottom_center),
        OnHold<UICol>(_ => SetBarColor(Input.GetMousePosition().X - Picker.ColorPickerPosition.X)),
        Sub(
            newImg(Class(w_[10], h_[16], right_[6], bottom_left, top_[4], depth_[10], blank_full, rgb_[1, 0, 0], border_ui_[3, 3, 3, 3], border_color_g_[40]), ref ColorBarSlider)
        )) 
    ));

    public void SetRGB(Vector3i rgb, bool updatePicker = true) => SetRGB(rgb.X, rgb.Y, rgb.Z, updatePicker);
    public void SetRGB(int? r, int? g, int? b, bool updatePicker = true)
    {
        byte Set(UIField field, int? c)  
        {
            if (c == null) return field.GetByte();
            byte C = (byte)Mathf.Clampy(c.Value, 0, 255);
            field.UpdateText($"{c}");
            return C;
        }

        var R = Set(Picker.RedField, r);
        var G = Set(Picker.GreenField, g);
        var B = Set(Picker.BlueField, b);

        if (updatePicker)
            Picker.SetColor(R, G, B, true);
    }

    public void SetBarColor(float x)
    {
        x = Mathf.Clampy(x, 0, Picker.Width);

        ColorBarSlider.BaseOffset.X = x - 4;
        ColorBarSlider.ApplyChanges(UIChange.Transform);

        float rX = x / Picker.Width;
        float h = Mathf.Clampy(rX * 360, 0, 360);

        Vector3 color;

        if (h < 60) color = (1, h / 60f, 0);
        else if (h < 120) color = (1 - (h - 60) / 60f, 1, 0);
        else if (h < 180) color = (0, 1, (h - 120) / 60f);
        else if (h < 240) color = (0, 1 - (h - 180) / 60f, 1);
        else if (h < 300) color = ((h - 240) / 60f, 0, 1);
        else color = (1, 0, 1 - (h - 300) / 60f);

        Picker.BaseColor = Mathf.Round(color * 255f) / 255f;

        Picker.Color = new Vector4(
            Mathf.Lerp(1, Picker.BaseColor.X, Picker.Saturation),
            Mathf.Lerp(1, Picker.BaseColor.Y, Picker.Saturation),
            Mathf.Lerp(1, Picker.BaseColor.Z, Picker.Saturation),
            1f
        );

        Picker.Color *= Picker.Brightness;
        Picker.Color.W = 1f;
        Picker.Color = Mathf.Round(Picker.Color * 255f) / 255f;

        SetRGB(Mathf.RoundToInt(Picker.Color.Xyz * 255), false);

        ColorPickSlider.UpdateColor(Picker.Color);
        ColorBarSlider.UpdateColor(new Vector4(Picker.BaseColor, 1f));

        Picker.SetColorAction(Picker.Color);

        Picker.Hovering = true;
    }
    public void SetSliderColor(float x, float y)
    {
        x = Mathf.Clampy(x, 0, Picker.Height - 20);
        y = Mathf.Clampy(y, 0, Picker.Height - 20);

        ColorPickSlider.BaseOffset = (x - 5, y - 5);
        ColorPickSlider.ApplyChanges(UIChange.Transform);

        float rX = x / (Picker.Height - 20);
        float rY = y / (Picker.Height - 20);

        Picker.Saturation = rX;
        Picker.Brightness = 1 - rY;

        Picker.Color = new Vector4(
            Mathf.Lerp(1, Picker.BaseColor.X, Picker.Saturation),
            Mathf.Lerp(1, Picker.BaseColor.Y, Picker.Saturation),
            Mathf.Lerp(1, Picker.BaseColor.Z, Picker.Saturation),
            1f
        );

        Picker.Color *= Picker.Brightness;
        Picker.Color.W = 1f;
        Picker.Color = Mathf.Round(Picker.Color * 255f) / 255f;

        SetRGB(Mathf.RoundToInt(Picker.Color.Xyz * 255), false);

        ColorPickSlider.UpdateColor(Picker.Color);

        Picker.SetColorAction(Picker.Color);

        Picker.Hovering = true;
    }}

    public void SetColor(byte r, byte g, byte b, bool setRGB)
    {
        RgbToHsv(r, g, b, out var h, out var s, out var v);
        Saturation = s;
        Brightness = v;
        Ui.SetBarColor((h / 360f) * Width);
        Ui.SetSliderColor(s * (Height - 20), (1 - v) * (Height - 20));     
        if (setRGB)
            Ui.SetRGB(r, g, b, false);
    }

    public static void RgbToHsv(byte r, byte g, byte b, out float h, out float s, out float v)
    {
        float R = r / 255f;
        float G = g / 255f;
        float B = b / 255f;

        float max = MathF.Max(R, MathF.Max(G, B));
        float min = MathF.Min(R, MathF.Min(G, B));
        float delta = max - min;

        // Brightness / Value
        v = max;

        // Saturation
        if (max < 0.00001f)
        {
            s = 0f;
            h = 0f;  // undefined hue
            return;
        }
        s = delta / max;

        // Hue
        if (delta < 0.00001f)
        {
            h = 0f; // undefined hue
            return;
        }

        if (max == R)
            h = 60f * (((G - B) / delta) % 6f);
        else if (max == G)
            h = 60f * (((B - R) / delta) + 2f);
        else
            h = 60f * (((R - G) / delta) + 4f);

        if (h < 0f)
            h += 360f;
    }

    public void UpdateColorPickerPosition()
    {
        _colorPickerPosition += Mathf.FloorToInt(Input.GetMouseDelta()); 
        Vector2i newPosition = ((int)_colorPickerPosition.X, (int)-_colorPickerPosition.Y + (Game.Height - Height));
        ColorPickerPosition = newPosition;

        Ui.Element.BaseOffset = (_colorPickerPosition.X - 5, _colorPickerPosition.Y - 30);
        Ui.Element.ApplyChanges(UIChange.Transform);
    }

    public void Resize()
    {
        Vector2i newPosition = ((int)_colorPickerPosition.X, (int)-_colorPickerPosition.Y + (Game.Height - Height));
        ColorPickerPosition = newPosition;
        //ColorPickerController.Resize();
    }

    public void Render()
    {
        /*
        GL.Viewport(0, 0, Game.Width, Game.Height);

        UIController.BindFramebuffer();

        GL.Enable(EnableCap.DepthTest);
        GL.Viewport(ColorPickerPosition.X, ColorPickerPosition.Y, Width, Height);

        float minSize = Mathf.Min(_colorPickerScale.X, _colorPickerScale.Y - 20);

        _pickerBarShader.Bind();

        Matrix4 model = Matrix4.CreateTranslation(0, _colorPickerScale.Y - 20, UIController.CumulativeDepth - 0.00004f);
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, -2, 2);

        GL.UniformMatrix4(_barModelLocation, true, ref model);
        GL.UniformMatrix4(_barProjectionLocation, true, ref projection);
        GL.Uniform2(_barSizeLocation, (_colorPickerScale.X, 20));

        _colorPickerVao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _colorPickerVao.Unbind();

        _pickerBarShader.Unbind();


        _pickerShader.Bind();

        model = Matrix4.CreateTranslation(0, 0, UIController.CumulativeDepth - 0.00004f);

        GL.UniformMatrix4(_pickerModelLocation, true, ref model);
        GL.UniformMatrix4(_pickerProjectionLocation, true, ref projection);
        GL.Uniform2(_pickerSizeLocation, (minSize, minSize));
        GL.Uniform3(_pickerColorLocation, BaseColor.X, BaseColor.Y, BaseColor.Z);

        _colorPickerVao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _colorPickerVao.Unbind();

        _pickerShader.Unbind();

        GL.Disable(EnableCap.Blend);

        GL.Viewport(0, 0, Game.Width, Game.Height);

        UIController.UnbindFramebuffer();
        */
    }

    public void Dispose()
    {
        //_colorPickerVao.DeleteBuffer();
        //ColorPickerController.Delete();
    }
}