using System.Runtime.InteropServices;
using PBG;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;
using Silk.NET.Input;
using static PBG.UI.Styles;

public class CurveWindow
{
    //private static ShaderProgram _curvePanelShader = new ShaderProgram("Painting/Rectangle.vert", "Painting/Curve.frag");

    //private static VAO _curvePanelVao = new VAO();

    private static int _modelLocation = -1;
    private static int _projectionLocation = -1;
    private static int _sizeLocation = -1;
    private static int _pointCountLocation = -1;

    static CurveWindow()
    {
        /*
        _modelLocation = _curvePanelShader.GetLocation("model");
        _projectionLocation = _curvePanelShader.GetLocation("projection");
        _sizeLocation = _curvePanelShader.GetLocation("size");
        _pointCountLocation = _curvePanelShader.GetLocation("pointCount");
        */
    }

    private SSBO<Vector2> _pointSSBO = new(new Vector2[]{(0, 0), (1,0.2f)});
    public List<Vector2> Points = [(0, 0), (1, 0.2f)];
    public List<UIButton> Buttons = [];
    public Dictionary<UIButton, int> ButtonIndex = new();

    public Vector2 Position { 
        get => _position;
        set
        {
            _position = value;
            ModelMatrix = Matrix4.CreateTranslation(value.X, value.Y, 0.1f);
        }
    }
    private Vector2 _position;
    public Vector2 _oldMouseButtonPosition = new Vector2(0, 0);
    public Vector2 Size;

    public Matrix4 ModelMatrix = Matrix4.Identity;
    public Matrix4 ProjectionMatrix;

    public UIController Controller = null!;

    public UICol Collection = null!;
    public UICol ButtonCollection = null!;
    public UICol InfoCollection = null!;
    public UIField XPositionField = null!;
    public UIField YPositionField = null!;
    private float _oldXPosition = 1f;
    private float _oldYPosition = 0f;

    public UIButton X0Button = null!;
    public UIButton X1Button = null!;

    public UIButton? SelectedButton = null;
    public UIButton DataSelectedButton = null!;

    private bool _isHoveringOver = false;

    public CurveWindow(UIController controller, Vector2 position, Vector4 offset, Vector2 size) 
    {
        /*
        Controller = controller;
        Position = position + (0, 98);
        size -= (14, 14);
        Size = size;

        ProjectionMatrix = UIController.OrthographicProjection;

        Collection = new UICollection("CurveCollection", controller, AnchorType.TopLeft, PositionType.Absolute, (0, 0, 0), size + (14, 112), offset, 0);

        UIImage background = new UIImage("Background", controller, AnchorType.ScaleFull, PositionType.Relative, (0.6f, 0.6f, 0.6f, 1f), (0, 0, 0), size, (0, 98, 0, 0), 0, 11, (10, 0.05f));
        background.SetOnHover(() => { _isHoveringOver = true; });

        InfoCollection = new UICollection("InfoCollection", controller, AnchorType.TopLeft, PositionType.Relative, (0, 0, 0), (size.X + 14, 72), (0, 17, 0, 0), 0)
        {
            Depth = 10
        };

        UICollection buttonXPosition = new UICollection("ButtonXPosition", controller, AnchorType.TopLeft, PositionType.Relative, (0, 0, 0), (size.X + 14, 30), (0, 6, 0, 0), 0);

        UIText xPositionText = new UIText("XPositionText", controller, AnchorType.MiddleLeft, PositionType.Relative, Vector4.One, (0, 0, 0), (size.X + 14 - 10, 20), (8, 0, 0, 0), 0);
        xPositionText.SetText("X Position", 1.2f).SetTextType(TextType.Alphabetic);

        UIImage xPositionFieldBackground = new UIImage("XPositionFieldBackground", controller, AnchorType.MiddleRight, PositionType.Relative, (0.5f, 0.5f, 0.5f, 1f), (0, 0, 0), (size.X + 14 - 10, 24), (-4, 0, 0, 0), 0, 11, (10, 0.05f));
        XPositionField = new UIInputField("XPositionField", controller, AnchorType.MiddleRight, PositionType.Relative, Vector4.One, (0, 0, 0), (size.X + 14 - 10, 24), (-12, 0, 0, 0), 0, 11, (10, 0.05f));
        XPositionField.SetMaxCharCount(10).SetText("0.0", 1.2f).SetTextType(TextType.Decimal);
        xPositionFieldBackground.SetScale(XPositionField.Scale + (16, 16));
        XPositionField.SetOnTextChange(() =>
        {
            float newXPosition = Float.Parse(XPositionField.Text);
            if (newXPosition != _oldXPosition && DataSelectedButton != null)
            {
                if (DataSelectedButton.Name == "X0Button" || DataSelectedButton.Name == "X1Button")
                    return;
                
                float deltaX = newXPosition - _oldXPosition;
                DataSelectedButton.Offset = new Vector4((newXPosition * Size.X) - 10, DataSelectedButton.Offset.Y, 0, 0);
                DataSelectedButton.Align();
                DataSelectedButton.UpdateTransformation();
                CheckButtonSwap(DataSelectedButton, deltaX > 0);
                _oldXPosition = newXPosition;
                UpdatePoints();
            }
        });

        buttonXPosition.AddElements(xPositionText, xPositionFieldBackground, XPositionField);

        UICollection buttonYPosition = new UICollection("ButtonYPosition", controller, AnchorType.TopLeft, PositionType.Relative, (0, 0, 0), (size.X + 14, 30), (0, 40, 0, 0), 0);

        UIText yPositionText = new UIText("YPositionText", controller, AnchorType.MiddleLeft, PositionType.Relative, Vector4.One, (0, 0, 0), (size.X + 14 - 10, 20), (8, 0, 0, 0), 0);
        yPositionText.SetText("Y Position", 1.2f).SetTextType(TextType.Alphabetic);

        UIImage yPositionFieldBackground = new UIImage("YPositionFieldBackground", controller, AnchorType.MiddleRight, PositionType.Relative, (0.5f, 0.5f, 0.5f, 1f), (0, 0, 0), (size.X + 14 - 10, 24), (-4, 0, 0, 0), 0, 11, (10, 0.05f));
        YPositionField = new UIInputField("YPositionField", controller, AnchorType.MiddleRight, PositionType.Relative, Vector4.One, (0, 0, 0), (size.X + 14 - 10, 24), (-12, 0, 0, 0), 0, 11, (10, 0.05f));
        YPositionField.SetMaxCharCount(10).SetText("0.0", 1.2f).SetTextType(TextType.Decimal);
        yPositionFieldBackground.SetScale(YPositionField.Scale + (16, 16));
        YPositionField.SetOnTextChange(() =>
        {
            float newYPosition = Float.Parse(YPositionField.Text);
            if (newYPosition != _oldYPosition && DataSelectedButton != null)
            {
                float deltaY = newYPosition - _oldYPosition;
                DataSelectedButton.Offset = new Vector4(DataSelectedButton.Offset.X, ((1 - newYPosition) * Size.Y) - 10, 0, 0);
                DataSelectedButton.Align();
                DataSelectedButton.UpdateTransformation();
                CheckButtonSwap(DataSelectedButton, deltaY > 0);
                _oldYPosition = newYPosition;
                UpdatePoints();
            }
        });

        buttonYPosition.AddElements(yPositionText, yPositionFieldBackground, YPositionField);


        InfoCollection.AddElements(buttonXPosition, buttonYPosition);
        
        ButtonCollection = new UICollection("ButtonCollection", controller, AnchorType.BottomCenter, PositionType.Relative, (0, 0, 0), size, (0, -10, 0, 0), 0)
        {
            Depth = 10
        };

        X0Button = new UIButton("X0Button", controller, AnchorType.TopLeft, PositionType.Relative, (0.7f, 0.7f, 0.7f, 1f), (0, 0, 0), (20, 20), (-10, size.Y - 10, 0, 0), 0, 10, (10, 0.05f), UIState.Interactable);
        X1Button = new UIButton("X1Button", controller, AnchorType.TopLeft, PositionType.Relative, (0.7f, 0.7f, 0.7f, 1f), (0, 0, 0), (20, 20), (size.X - 10, -10, 0, 0), 0, 10, (10, 0.05f), UIState.Interactable);

        Buttons.AddRange(X0Button, X1Button);
        ButtonIndex.Add(X0Button, 0);
        ButtonIndex.Add(X1Button, 1);

        X0Button.SetOnClick(() =>
        {
            Game.SetCursorState(CursorState.Grabbed);
            SetDataSelectedButton(X0Button);
        });
        X0Button.SetOnHold(() => 
        {
            Vector2 mouseDelta = Input.GetMouseDelta();
            if (mouseDelta != Vector2.Zero)
            {
                Vector4 offset = X0Button.Offset + (mouseDelta.X, mouseDelta.Y, 0, 0);
                offset = Mathf.Clamp((-10, -10, 0, 0), (-10, size.Y - 10, 0, 0), offset);
                X0Button.Offset = offset;
                X0Button.Align();
                X0Button.UpdateTransformation();
                SetPositionText(X0Button);
                UpdatePoints();
            }
        });
        X0Button.SetOnRelease(() => Game.SetCursorState(CursorState.Normal));

        X1Button.SetOnClick(() =>
        {
            Game.SetCursorState(CursorState.Grabbed);
            SetDataSelectedButton(X1Button);
        });
        X1Button.SetOnHold(() => 
        {
            Vector2 mouseDelta = Input.GetMouseDelta();
            if (mouseDelta != Vector2.Zero)
            {
                Vector4 offset = X1Button.Offset + (mouseDelta.X, mouseDelta.Y, 0, 0);
                offset = Mathf.Clamp((size.X - 10, -10, 0, 0), (size.X - 10, size.Y - 10, 0, 0), offset);
                X1Button.Offset = offset;
                X1Button.Align();
                X1Button.UpdateTransformation();
                SetPositionText(X1Button);
                UpdatePoints();
            }
        });
        X1Button.SetOnRelease(() => Game.SetCursorState(CursorState.Normal));

        ButtonCollection.AddElements(X0Button, X1Button);

        Collection.AddElements(background, InfoCollection, ButtonCollection);

        DataSelectedButton = X0Button;
        SetDataSelectedButton(X0Button);

        UpdatePoints();
        */
    }

    public void SetPositionText(UIButton button)
    {
        //float xPosition = (button.Offset.X + 10) / Size.X;
        //float yPosition = 1f - ((button.Offset.Y + 10) / Size.Y);

        //XPositionField.SetText(Float.Str(xPosition)).UpdateCharacters();
        //YPositionField.SetText(Float.Str(yPosition)).UpdateCharacters();
    }

    public void SetDataSelectedButton(UIButton button)
    {
        //XPositionField.SetText(Float.Str((button.Offset.X + 10) / Size.X)).UpdateCharacters();
        //YPositionField.SetText(Float.Str(1f - ((button.Offset.Y + 10) / Size.Y))).UpdateCharacters();
        DataSelectedButton = button;
    }

    public void MoveNode(Vector2 delta)
    {
        Position += delta;
    }

    public void UpdateButton(UIButton button)
    {
        Vector2 mouseDelta = Input.GetMouseDelta();
        UpdateButton(button, mouseDelta);
    }

    public void UpdateButton(UIButton button, Vector2 mouseDelta)
    {
        bool updatePoints;
        if (mouseDelta != Vector2.Zero)
        {
            //Vector4 offset = button.Offset + (mouseDelta.X, mouseDelta.Y, 0, 0);
            //offset = Mathf.Clamp((-10, -10, 0, 0), (Size.X - 10, Size.Y - 10, 0, 0), offset);
            //button.Offset = offset;
            //button.Align();
            //button.UpdateTransformation();
            updatePoints = true;
        }
        else
        {
            return;
        }

        if (CheckButtonSwap(button, mouseDelta.X > 0) || updatePoints)
            UpdatePoints();
    }

    public bool CheckButtonSwap(UIButton button, bool supX)
    {
        bool updatePoints = false;
        int index = ButtonIndex[button];
        if (index == 0 || index == Buttons.Count - 1) 
            return false;

        if (supX)
        {
            int nextIndex = index + 1;
            UIButton swapButton = Buttons[index + 1];
            int swapIndex = index + 1;
            bool swap = false;
            while (nextIndex != Buttons.Count - 1) // ignore if it is the last button
            {
                UIButton nextButton = Buttons[nextIndex];
                //if (button.Offset.X > nextButton.Offset.X)
                //{
                //    swapButton = nextButton;
                //    swapIndex = nextIndex;
                //    swap = true;
                //}
                //else
                //{
                //    break;
                //}
                nextIndex++;
            }
            if (swap)
            {
                SwapButton(index, swapIndex, button, swapButton);
                updatePoints = true;
            }

        }
        else
        {
            int prevIndex = index - 1;
            UIButton swapButton = Buttons[index - 1];
            int swapIndex = index - 1;
            bool swap = false;
            while (prevIndex != 0) // ignore if it is the first button
            {
                UIButton prevButton = Buttons[prevIndex];
                //if (button.Offset.X < prevButton.Offset.X)
                //{
                //    swapButton = prevButton;
                //    swapIndex = prevIndex;
                //    swap = true;
                //}
                //else
                //{
                //    break;
                //}
                prevIndex--;
            }
            if (swap)
            {
                SwapButton(index, swapIndex, button, swapButton);
                updatePoints = true;
            }
        }

        return updatePoints;
    }

    public void SwapButton(int index1, int index2, UIButton button1, UIButton button2)
    {
        Buttons.Remove(button1);
        Buttons.Insert(index2, button1);
        ButtonIndex[button1] = index2;
        ButtonIndex[button2] = index1;
    }

    public void UpdatePoints()
    {
        Points.Clear();
        foreach (UIButton button in Buttons)
        {
            //Vector2 point = new Vector2((button.Offset.X + 10) / Size.X, 1f - ((button.Offset.Y + 10) / Size.Y));
            //Points.Add(point);
        }
        //_pointSSBO.Update(Points.ToArray(), 0);
        //CurveNode?.UpdateCurve(this);
    }

    public UIButton AddButton()
    {
        return AddButton((Size.X / 2 - 10, Size.Y / 2 - 10, 0, 0));
    }

    public UIButton AddButton(Vector4 offset)
    {
        UIButton button = new UIButton(light_sharp_g_[70]);
        int index = 1;
        //while (Buttons.Count - 1 > index && Buttons[index].Offset.X < button.Offset.X)
        //{
        //    index++;
        //}
        Buttons.Insert(index, button);
        ButtonIndex.Add(button, index);
        button.SetOnClick(_ =>
        {
            Game.SetCursorState(CursorMode.Disabled);
            SelectedButton = button;
            SetDataSelectedButton(button);
        });
        button.SetOnHold(_ => { UpdateButton(button); SetPositionText(button); });
        button.SetOnRelease(_ => Game.SetCursorState(CursorMode.Normal));
        ButtonCollection.AddElement(button);
        return button;
    }

    public void UpdateButtons()
    {
        GenerateButtons();
        UpdatePoints();
    }

    public void RemoveButton(UIButton button)
    {
        int index = ButtonIndex[button];
        Buttons.Remove(button);
        ButtonIndex.Remove(button);
        for (int i = 0; i < Buttons.Count; i++)
        {
            ButtonIndex[Buttons[i]] = i;
        }
        button.Delete();
        ButtonCollection.RemoveElement(button);
        Controller.RemoveElement(button);
        GenerateButtons();
    }

    public void GenerateButtons()
    {
        Points.Clear();
        foreach (UIButton button in Buttons)
        {
            //Vector2 point = new Vector2((button.Offset.X + 10) / Size.X, 1f - ((button.Offset.Y + 10) / Size.Y));
            //Points.Add(point);
        }
       // _pointSSBO.Renew(Points);
    }
    
    public void Update()
    {
        if (!_isHoveringOver)
            return;

        if (Input.IsKeyPressed(Key.A))
        {
            UIButton button = AddButton();
            Controller.AddElement(button);
            SelectedButton = button;
            SetDataSelectedButton(button);
            UpdateButtons();
        }

        if (Input.IsKeyPressed(Key.D) && SelectedButton != null)
        {
            RemoveButton(SelectedButton);
            SelectedButton = null;
        }

        _isHoveringOver = false;
    }

    public void Render(Matrix4 modelMatrix, Matrix4 projectionMatrix)
    {
        /*
        modelMatrix = ModelMatrix * modelMatrix;
 
        _curvePanelShader.Bind();

        Matrix4 model = modelMatrix;
        Matrix4 projection = projectionMatrix;

        GL.Enable(EnableCap.DebugOutput);

        GL.UniformMatrix4(_modelLocation, true, ref model);
        GL.UniformMatrix4(_projectionLocation, true, ref projection);
        GL.Uniform2(_sizeLocation, Size);
        GL.Uniform1(_pointCountLocation, Points.Count);
        
        _curvePanelVao.Bind();
        _pointSSBO.Bind(0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        //Shader.Error("Error while drawing curve panel: ");

        _pointSSBO.Unbind();
        _curvePanelVao.Unbind();

        _curvePanelShader.Unbind();
        */
    }

    public void Destroy()
    {
        //_pointSSBO.DeleteBuffer();
        Points = [];
        Buttons = [];
        ButtonIndex = [];
    }
}