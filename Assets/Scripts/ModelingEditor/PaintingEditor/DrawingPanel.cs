using PBG;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using Silk.NET.Input;

public class DrawingPanel
{
    public static int WindowWidth = 1000;
    public static int WindowHeight = 800;

    public static int Width;
    public static int Height;

    public static int TextureWidth = 100;
    public static int TextureHeight = 100;

    /*
    private static FBO _fbo = new FBO(TextureWidth, TextureHeight, FBOType.Color);
    private static ShaderProgram _paintingShader = new ShaderProgram("Painting/Painting.vert", "Painting/Painting.frag");
    private static ShaderProgram _textureShader = new ShaderProgram("Painting/Rectangle.vert", "Painting/Texture.frag");
    private static ShaderProgram _brushCircleShader = new ShaderProgram("Painting/Rectangle.vert", "Painting/CircleOutline.frag");
    private static ShaderProgram _alphaGridShader = new ShaderProgram("Painting/Rectangle.vert", "Painting/alphaGrid.frag");
    private static VAO _vao = new VAO();
    private static VAO _textureVao = new VAO();


    public static ShaderProgram selectionShader = new ShaderProgram("Selection/Selection.vert", "Selection/Selection.frag");
    public static int SelectionModelLocation = selectionShader.GetLocation("model");
    public static int SelectionProjectionLocation = selectionShader.GetLocation("projection");
    public static int SelectionSizeLocation = selectionShader.GetLocation("selectionSize");
    public static int SelectionColorLocation = selectionShader.GetLocation("color");

    public static VAO selectionVao = new();

    private struct PaintingLocations
    {
        public int Model = _paintingShader.GetLocation("model");
        public int Projection = _paintingShader.GetLocation("projection");
        public int Size = _paintingShader.GetLocation("size");
        public int Point1 = _paintingShader.GetLocation("point1");
        public int Point2 = _paintingShader.GetLocation("point");
        public int Radius = _paintingShader.GetLocation("radius");  
        public int Color = _paintingShader.GetLocation("color");
        public int Mode = _paintingShader.GetLocation("paintMode");
        public int Falloff = _paintingShader.GetLocation("falloff");
        public int BrushStrength = _paintingShader.GetLocation("brushStrength");
        public PaintingLocations() {}
    }
    private static PaintingLocations PaintingLocation = new();

    private struct AlphaGridLocations
    {
        public int Model = _alphaGridShader.GetLocation("model");
        public int Projection = _alphaGridShader.GetLocation("projection");
        public int Size = _alphaGridShader.GetLocation("size");
        public int Position = _alphaGridShader.GetLocation("position");
        public int Zoom = _alphaGridShader.GetLocation("zoom");
        public AlphaGridLocations() { }
    }
    private static AlphaGridLocations AlphaGridLocation = new();

    private struct BrushCircleLocations
    {
        public int Model = _brushCircleShader.GetLocation("model");
        public int Projection = _brushCircleShader.GetLocation("projection");
        public int TextureSize = _brushCircleShader.GetLocation("textureSize");
        public int PixelPos = _brushCircleShader.GetLocation("pixelPos");
        public int Size = _brushCircleShader.GetLocation("size");
        public int Radius = _brushCircleShader.GetLocation("radius");
        public int BrushSet = _brushCircleShader.GetLocation("brushSet");
        public int Falloff = _brushCircleShader.GetLocation("falloff");
        public int Mode = _brushCircleShader.GetLocation("brushMode");
        public int BrushStrength = _brushCircleShader.GetLocation("brushStrength");
        public BrushCircleLocations() { }
    }
    private static BrushCircleLocations BrushCircleLocation = new();
    */
    
    private static Matrix4 _projectionMatrix;
    private static Matrix4 _textureProjectionMatrix = Matrix4.Identity;

    public static Vector4 BrushColor = new Vector4(1, 1, 1, 1);

    public static bool IsDrawing = false;

    private static float _brushHalfSize = 50;
    private static float _brushSize = 100;
    public static float BrushSize 
    {
        get {
            return _brushSize;
        }
        set
        {
            _brushSize = value;
            _brushHalfSize = value / 2f;
        }
    }

    public static Vector2 DrawingCanvasPosition
    {
        get => _drawingCanvasPosition;
        set
        {
            _drawingCanvasPosition = value;
            _drawingCanvasOffset = _drawingCanvasPosition * _drawingCanvasSize;
        }
    }

    public static Vector2 CanvasPosition = new Vector2(0, 0);
    public static Vector2i WindowPosition = new Vector2i(200, 50);

    public static void SetDrawingCanvasPosition(float x, float y) { SetDrawingCanvasPosition((x, y));}
    public static void SetDrawingCanvasPosition(Vector2 position) { DrawingCanvasPosition = position; }

    public static float DrawingCanvasSize
    {
        get => 1 / _drawingCanvasSize;
        set
        {
            _drawingCanvasSize = 1 / value;
            CanvasScale = new Vector2(Width, Height) / _drawingCanvasSize;
            _drawingCanvasOffset = _drawingCanvasPosition * _drawingCanvasSize;
        }
    }

    public static void SetDrawingCanvasSize(float size) { DrawingCanvasSize = size; }


    public static void Zoom(float zoomFactor)
    {
        SetScale(Mathf.Clampy(DrawingCanvasSize + zoomFactor, 0.01f, 100));
    }

    public static void SetScale(float scale)
    {
        Vector2 mousePosition = Input.GetMousePosition() - (WindowPosition.X, 0);

        Vector2 offset = mousePosition - CanvasPosition;
        Vector2 position = offset / DrawingCanvasSize;

        Vector2 mPosition = position * scale;
        Vector2 mOffset = mPosition - mousePosition;
        Vector2 newPosition = mOffset * -1;

        CanvasPosition = newPosition;
        DrawingCanvasSize = scale;
        SetDrawingCanvasPosition(CanvasPosition.X + WindowPosition.X, CanvasPosition.Y + (Game.Height - WindowHeight));
    }

    public static void LoadTexture(string filePath)
    {
        /*
        _fbo.LoadFileFromPNG(filePath, out int width, out int height);
        Width = width;
        Height = height;

        _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

        DrawingCanvasSize = DrawingCanvasSize;
        DrawingCanvasPosition = DrawingCanvasPosition;
        */
    }

    public static void SaveTexture(string filePath)
    {
        //_fbo.SaveFramebufferToPNG(Width, Height, filePath);
    }


    private static Vector2 _drawingCanvasPosition = new Vector2(0, 0);
    private static Vector2 _drawingCanvasOffset = new Vector2(200, 50);
    public static Vector2 CanvasScale = new Vector2(100, 100);

    public static float _drawingCanvasSize = 2f;

    public static DrawingMode DrawingMode = DrawingMode.Brush;
    public static bool DisplayBrushCircle = false;
    public static void SetDrawingMode(DrawingMode mode) 
    { 
        DrawingMode = mode;
        DisplayBrushCircle = ((int)mode & 0b1110) != 0;
    }

    public static float Falloff = 2f;
    public static float BrushStrength = 1f;
    public static int RenderSet = 0;

    public static Action<byte, byte, byte> ColorPickAction = (r, g, b) => {};

    public DrawingPanel(int width, int height)
    {
        Width = width;
        Height = height;

        //_fbo.Renew(width, height, FBOType.Color);

        _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

        DrawingCanvasPosition = new Vector2(200, 50);
        DrawingCanvasSize = 1f;
    }

    public static void Renew(int width, int height)
    {
        Width = width;
        Height = height;

        //_fbo.Renew(width, height, FBOType.Color);

        _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

        DrawingCanvasSize = DrawingCanvasSize;
        DrawingCanvasPosition = DrawingCanvasPosition;
    }

    //public static void BindTexture(TextureUnit unit) => _fbo.BindTexture(unit);
    //public static void UnbindTexture() => _fbo.UnbindTexture(TextureUnit.Texture0);

    private static Vector2 _oldMousePosition = Vector2.Zero;
    private static Vector2 _oldUvPosition = Vector2.Zero;
    private static bool _clicked = false;

    // Selection
    private static Vector2 _selectionOrigin;
    private static Vector2i _startSelection;
    private static Vector2i _endSelection;
    private static byte[] _copiedPixels = [];
    private static int _copiedWidth;
    private static int _copiedHeight;
    private static Texture? _copiedTexture = null;

    public static void Update()
    {
        if (Input.IsMouseReleased(MouseButton.Left))
        {
            _clicked = false;
        }

        PickColor();
        Selection();
    }

    public static Vector2i GetPixelPosition() =>  Mathf.FloorToInt((Input.GetMousePosition() - (200, 0) - CanvasPosition) * _drawingCanvasSize);

    public static void PickColor()
    {
        if (DrawingMode == DrawingMode.Pick && Input.IsMousePressed(MouseButton.Left))
        {
            Vector2i pixelPos = GetPixelPosition();
            if (pixelPos.X >= 0 && pixelPos.X < Width && pixelPos.Y >= 0 && pixelPos.Y < Height)
            {
                //byte[] pixel = _fbo.GetPixels(pixelPos.X, pixelPos.Y, 1, 1);
                //ColorPickAction(pixel[0], pixel[1], pixel[2]);
            }
        }
    }

    public static void Selection()
    {
        if (DrawingMode == DrawingMode.Selection)
        {
            if (Input.IsMousePressed(MouseButton.Left))
            {
                if (_copiedTexture != null)
                {
                    //_fbo.SetPixels(_startSelection.X, _startSelection.Y, _copiedWidth, _copiedHeight, _copiedPixels);
                    _copiedTexture = null;
                }

                Vector2i pixelPos = GetPixelPosition();
                _startSelection = Mathf.Clampy(pixelPos, (0, 0), (Width-1, Height-1));
                _selectionOrigin = _startSelection;
            }

            if (Input.IsMouseReleased(MouseButton.Left))
            {
                if (_copiedTexture != null)
                    return;

                Vector2i pixelPos = GetPixelPosition();
                _endSelection = Mathf.Clampy(pixelPos, (0, 0), (Width-1, Height-1));
            }   

            if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.C))
            {
                if (_copiedTexture != null)
                    return;

                Vector2i start = Mathf.Min(_startSelection, _endSelection);
                Vector2i end = Mathf.Max(_startSelection, _endSelection);

                _copiedWidth = end.X - start.X;
                _copiedHeight = end.Y - start.Y;
                //_copiedPixels = _fbo.GetPixels(start.X, start.Y, _copiedWidth, _copiedHeight);
                
            }   

            if (Input.IsKeyDown(Key.G) && _copiedTexture != null)
            {
                _selectionOrigin += Input.MouseDelta * _drawingCanvasSize;
                _startSelection = Mathf.FloorToInt(_selectionOrigin);
                _endSelection = _startSelection + (_copiedWidth, _copiedHeight);
            }

            if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyPressed(Key.V))
            {
                _startSelection = GetPixelPosition();
                _endSelection = _startSelection + (_copiedWidth, _copiedHeight);
                _selectionOrigin = _startSelection;

                if (_copiedTexture == null)
                {
                   // _copiedTexture = new(_copiedWidth, _copiedHeight, _copiedPixels, TextureType.Nearest);
                }
                else
                {
                    //_copiedTexture.Renew(_copiedWidth, _copiedHeight, _copiedPixels, TextureType.Nearest);
                }
            }       
        }
        
    }
    
    public static void RenderFramebuffer()
    {
        Vector2 mousePos = Input.GetMousePosition();
        if (IsDrawing && Input.IsMouseDown(MouseButton.Left) && !(Input.GetMouseDelta() == Vector2.Zero && _oldMousePosition == mousePos))
        { 
            if (((int)DrawingMode & (int)DrawingModeSet.FrameBufferSet) != 0)
            {
                Vector2 point1 = _clicked ? _oldMousePosition : mousePos;

                var point2 = mousePos * _drawingCanvasSize - _drawingCanvasOffset;
                point1 = point1 * _drawingCanvasSize - _drawingCanvasOffset;

                RenderFrameBuffer(point1, point2);
                _clicked = true;
            }
            else if (DrawingMode == DrawingMode.Pencil)
            {
                RenderPencil(GetPixelPosition()); 
            }

            _oldMousePosition = mousePos;
        }
    }

    private static void RenderPencil(Vector2i pixelPos)
    {
        //_fbo.SetPixels(pixelPos.X, pixelPos.Y, 1, 1, [(byte)(BrushColor.X * 255),(byte)(BrushColor.Y * 255),(byte)(BrushColor.Z * 255),(byte)(BrushColor.W * 255)]);
    }

    public static void RenderFramebuffer(Vector2 oldUv, Vector2 uv)
    {
        Vector2 point1 = _clicked ? oldUv : uv;

        uv *= (Width, Height);
        point1 *= (Width, Height);

        if (((int)DrawingMode & (int)DrawingModeSet.FrameBufferSet) != 0)
            RenderFrameBuffer(point1, uv);
        else
            RenderPencil((Mathf.FloorToInt(uv.X), Mathf.FloorToInt(uv.Y)));

    }

    private static void RenderFrameBuffer(Vector2 point1, Vector2 point2)
    {
        /*
        GL.Viewport(0, 0, Width, Height);

        _fbo.Bind();

        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        if (DrawingMode == DrawingMode.Eraser)
        {
            GL.Disable(EnableCap.Blend); // Replace pixels
        }
        else
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        _paintingShader.Bind();

        Matrix4 model = Matrix4.Identity;

        GL.UniformMatrix4(PaintingLocation.Model, false, ref model);
        GL.UniformMatrix4(PaintingLocation.Projection, true, ref _projectionMatrix);
        GL.Uniform2(PaintingLocation.Size, new Vector2(Width, Height));
        GL.Uniform2(PaintingLocation.Point1, point1);
        GL.Uniform2(PaintingLocation.Point2, point2);
        GL.Uniform1(PaintingLocation.Radius, _drawingCanvasSize * _brushHalfSize * DrawingCanvasSize);
        GL.Uniform4(PaintingLocation.Color, BrushColor);
        GL.Uniform1(PaintingLocation.Mode, (int)DrawingMode);
        GL.Uniform1(PaintingLocation.Falloff, Falloff);
        GL.Uniform1(PaintingLocation.BrushStrength, BrushStrength);

        //Console.WriteLine(point1 + " " + point2);

        _fbo.BindTexture(TextureUnit.Texture0);
        _vao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _vao.Unbind();
        _fbo.UnbindTexture(TextureUnit.Texture0);

        _paintingShader.Unbind();
        _fbo.Unbind();

        GL.Disable(EnableCap.Blend);
        */
    }

    public static void RenderTexture()
    {
        /*
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        Matrix4 model = Matrix4.CreateTranslation(CanvasPosition.X, CanvasPosition.Y, 0.01f);
        _textureProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, WindowWidth, WindowHeight, 0, -1, 1);

        _alphaGridShader.Bind();

        GL.UniformMatrix4(AlphaGridLocation.Model, true, ref model);
        GL.UniformMatrix4(AlphaGridLocation.Projection, true, ref _textureProjectionMatrix);
        GL.Uniform2(AlphaGridLocation.Size, CanvasScale);
        GL.Uniform2(AlphaGridLocation.Position, CanvasPosition);
        GL.Uniform1(AlphaGridLocation.Zoom, DrawingCanvasSize);

        _textureVao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _textureVao.Unbind();

        _alphaGridShader.Unbind();

        _textureShader.Bind();

        int modelLocation = GL.GetUniformLocation(_textureShader.ID, "model");
        int projectionLocation = GL.GetUniformLocation(_textureShader.ID, "projection");
        int sizeLocation = GL.GetUniformLocation(_textureShader.ID, "size");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(projectionLocation, true, ref _textureProjectionMatrix);
        GL.Uniform2(sizeLocation, CanvasScale);

        _fbo.BindTexture(TextureUnit.Texture0);
        _textureVao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _textureVao.Unbind();
        _fbo.UnbindTexture(TextureUnit.Texture0);

        _textureShader.Unbind();
        
        GL.Enable(EnableCap.DepthTest);
        */
    }

    public static void RenderSelection()
    {
        /*
        if (DrawingMode != DrawingMode.Selection)
            return;

        GL.Disable(EnableCap.DepthTest);

        Vector2i end;
        if (Input.IsMouseDown(MouseButton.Left))
            end = GetPixelPosition();
        else
            end = _endSelection;

        Vector2 selectionSize = end - _startSelection;
        Vector3 color = new Vector3(1, 0.5f, 0.25f);

        Matrix4 model = Matrix4.CreateTranslation(CanvasPosition.X + _startSelection.X / _drawingCanvasSize, CanvasPosition.Y + _startSelection.Y / _drawingCanvasSize, 0);
        _textureProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, WindowWidth, WindowHeight, 0, -1, 1);

        if (_copiedTexture != null)
        {

            _textureShader.Bind();

            int modelLocation = GL.GetUniformLocation(_textureShader.ID, "model");
            int projectionLocation = GL.GetUniformLocation(_textureShader.ID, "projection");
            int sizeLocation = GL.GetUniformLocation(_textureShader.ID, "size");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(projectionLocation, true, ref _textureProjectionMatrix);
            GL.Uniform2(sizeLocation, new Vector2(_copiedTexture.Width, _copiedTexture.Height) / _drawingCanvasSize);

            _copiedTexture.Bind();
            _textureVao.Bind();

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            _textureVao.Unbind();
            _copiedTexture.Unbind();

            _textureShader.Unbind();
            
            GL.Enable(EnableCap.DepthTest);
        }

        selectionShader.Bind();

        GL.UniformMatrix4(SelectionModelLocation, true, ref model);
        GL.UniformMatrix4(SelectionProjectionLocation, true, ref _textureProjectionMatrix);
        GL.Uniform2(SelectionSizeLocation, selectionSize / _drawingCanvasSize);
        GL.Uniform3(SelectionColorLocation, color);

        selectionVao.Bind();

        GL.DrawArrays(PrimitiveType.Lines, 0, 8);

        selectionVao.Unbind();

        selectionShader.Unbind();
        */
    }

    /// <summary>
    /// Renders the brush circle at the current mouse position
    /// </summary>
    public static void RenderBrushCircle()
    {
        if ((int)DrawingMode < 2 || (int)DrawingMode > (int)DrawingMode.Blur)
            return;

        float brushSize;
        float halfBrushSize;
        Vector2 brushPos;

        if (DrawingMode != DrawingMode.Pencil)
        {
            brushSize = _brushSize * DrawingCanvasSize;
            halfBrushSize = _brushHalfSize * DrawingCanvasSize;
            brushPos = Input.GetMousePosition() - (200, 50) - (halfBrushSize, halfBrushSize);
        }
        else
        {
            Vector2 gridValue = CanvasScale / new Vector2(Width, Height);

            brushSize = 1f * DrawingCanvasSize;
            halfBrushSize = 0.5f * DrawingCanvasSize;
            brushPos = Mathf.Round((Input.GetMousePosition() - (200, 50) - CanvasPosition - (halfBrushSize, halfBrushSize)) / gridValue) * gridValue + CanvasPosition;
        }

        RenderBrushCircle(brushPos, brushSize, halfBrushSize);
    }

    /// <summary>
    /// Renders the brush circle at the given uv position
    /// </summary>
    /// <param name="uv"></param>
    public static void RenderBrushCircle(Vector2 uv)
    {
        if ((int)DrawingMode < 2 || (int)DrawingMode > (int)DrawingMode.Blur)
            return;

        float brushSize;
        float halfBrushSize;
        Vector2 brushUvPos;

        if (DrawingMode != DrawingMode.Pencil)
        {
            brushSize = _brushSize * DrawingCanvasSize;
            halfBrushSize = _brushHalfSize * DrawingCanvasSize;
            brushUvPos = uv * new Vector2(Width, Height) - (halfBrushSize, halfBrushSize) + CanvasPosition;
        }
        else
        {
            brushSize = 1f;
            halfBrushSize = 0.5f;
            brushUvPos = uv * new Vector2(Width, Height) - (halfBrushSize, halfBrushSize) + CanvasPosition;
        }

        RenderBrushCircle(brushUvPos, brushSize, halfBrushSize);
    }

    private static void RenderBrushCircle(Vector2 brushPos, float brushSize, float halfBrushSize)
    {
        /*
        _brushCircleShader.Bind();

        Matrix4 model = Matrix4.CreateTranslation(brushPos.X, brushPos.Y, 0.02f);
        _textureProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, WindowWidth, WindowHeight, 0, -1, 1);

        GL.UniformMatrix4(BrushCircleLocation.Model, true, ref model);
        GL.UniformMatrix4(BrushCircleLocation.Projection, true, ref _textureProjectionMatrix);
        GL.Uniform2(BrushCircleLocation.TextureSize, new Vector2i(Width, Height));
        GL.Uniform2(BrushCircleLocation.PixelPos, GetPixelPosition());
        GL.Uniform2(BrushCircleLocation.Size, new Vector2(brushSize, brushSize));
        GL.Uniform1(BrushCircleLocation.Radius, halfBrushSize);
        GL.Uniform1(BrushCircleLocation.BrushSet, RenderSet);
        GL.Uniform1(BrushCircleLocation.Falloff, Falloff);
        GL.Uniform1(BrushCircleLocation.Mode, (int)DrawingMode);
        GL.Uniform1(BrushCircleLocation.BrushStrength, BrushStrength);

        _vao.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6); 

        _vao.Unbind();

        _brushCircleShader.Unbind();

        RenderSet = 0;

        GL.Disable(EnableCap.Blend);
        */
    }
}

public enum DrawingMode
{
    None = 0,
    Move = 1,
    Eraser = 2,
    Brush = 4,
    Pencil = 8,
    Blur = 16,
    Pick = 32,
    Selection = 64
}

public enum DrawingModeSet
{
    FrameBufferSet = (int)DrawingMode.Eraser | (int)DrawingMode.Brush | (int)DrawingMode.Blur,
    BrushCircleSet = (int)DrawingMode.Brush | (int)DrawingMode.Pencil | (int)DrawingMode.Eraser | (int)DrawingMode.Blur
}