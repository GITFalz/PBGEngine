using PBG.Graphics;
using PBG.Data;
using PBG.MathLibrary;
using TextCopy;
using PBG.Core;
using Silk.NET.Input;

namespace PBG.UI
{
    public class UIController : ScriptingNode
    {
        private static Shader? _uiPlaneShader;
        private static Descriptor _uiPlaneDescriptor;

        public static List<UIController> Controllers = [];
        public static Matrix4 OrthographicProjection = Matrix4.CreateOrthographicOffCenter(0, 1, 1, 0, -1, 1);

        public static PBG.UI.UIField? ActiveInputField = null;
        private static bool _clickedInputField = false;

        public static int CursorCharacter = 0;
        public static int SelectionSize = 0;

        public static float CumulativeDepth = 0f;

        public HashSet<UI.UIElementBase> AbsoluteElements = [];
        public HashSet<UI.UIElementBase> Elements = [];

        // Events
        public HashSet<UIElementBase> InteractableElementsSet = [];
        public List<UI.UIElementBase> InteractableElements = [];

        public Rendering.Meshes.UIMesh UIMesh;
        public Rendering.Meshes.TextMesh TextMesh;
        public Rendering.Mask.MaskData MaskData;

        public Vector3 Position = (0, 0, 0);
        public float Scale = 1;
        public float MaxDepth = 0f;
        public Matrix4 ModelMatrix = Matrix4.Identity;

        public bool RegenerateBuffers = true;
        public Queue<UIElementBase> ElementsToAdd = [];
        public Queue<UIElementBase> ElementsToRemove = [];
        public HashSet<UIElementBase> AddedElements = [];

        public Queue<UIElementBase> UpdateQueue = [];

        private bool _interactUpdate = false;
        public HashSet<UIElementBase> AddAsInteractableQueue = [];
        public HashSet<UIElementBase> RemoveAsInteractableQueue = [];

        public List<AnimationData> AnimationList = [];

        public UIData UIData;

        public bool UpdateBoundaries = true;
        public bool RemoveInputfieldOnEnter = true;

        public UIAlignment Alignment;

        public static FBO _fbo;

        private static List<UIController> _currentControllers = [];
        public bool DisableInputHandling = false;

        public UIController(string? name = null, TextureType textureType = TextureType.Nearest)
        {
            Name = name ?? "UIController";
            if (_uiPlaneShader == null)
            {
                _uiPlaneShader = new(new()
                {
                    VertexShaderPath = Path.Combine(Game.ShaderPath, "vulkan/fullScreen.vert"),
                    FragmentShaderPath = Path.Combine(Game.ShaderPath, "vulkan/fullScreen.frag")
                });
                _uiPlaneShader.Compile();

                /*
                _uiPlaneModelLocation = _uiPlaneShader.GetLocation("ubo.model");
                _uiPlaneProjectionLocation = _uiPlaneShader.GetLocation("ubo.projection");
                _uiPlaneSizeLocation = _uiPlaneShader.GetLocation("ubo.size");
                */
                
                _uiPlaneDescriptor = _uiPlaneShader.GetDescriptorSet();

                _fbo = new(Game.Width, Game.Height);

                _uiPlaneDescriptor.BindFramebuffer(_fbo, 0);
            }

            if (name != null)
                Name = name;

            UIData = textureType == TextureType.Linear ? UIData.LinearUI : UIData.PixelPerfectUI;
            Controllers.Add(this);
            MaskData = new(this);
            UIMesh = new(this);
            TextMesh = new(this);
            Alignment = new(this);
        }

        public UIController(UIAlignment alignment, string? name = null, TextureType textureType = TextureType.Nearest) : this(name, textureType)
        {
            Alignment = alignment;
        }

        public static void InitControllers(Scene scene)
        {
            var controllers = scene.RootNode.QueryComponents<UIController>();
            
            _currentControllers = [];
            HashSet<UIController> dupes = [];

            for (int i = 0; i < controllers.Count; i++)
            {
                var controller = controllers[i];
                if (dupes.Add(controller))
                    _currentControllers.Insert(0, controller);
            }
        }

        public static void HandleInputs(Scene scene)
        {
            for (int i = 0; i < _currentControllers.Count; i++)
            {
                var controller = _currentControllers[i];
                if (controller.HandleInputs())
                    return;
            }
        }

        public bool SetAsInteractable(UI.UIElementBase element, bool hasAction)
        {
            bool change = hasAction ? AddAsInteractableQueue.Add(element) : RemoveAsInteractableQueue.Add(element);
            if (change) _interactUpdate = true;
            return change;   
        } 

        public void QueueAction(UIElementBase element) => UpdateQueue.Enqueue(element);

        public void AddElement(UIElementBase element)
        {
            if (ElementsToAdd.Contains(element))
                return;

            ElementsToAdd.Enqueue(element);
            RegenerateBuffers = true;
        }

        public void AddElements(IEnumerable<UIElementBase> elements)
        {
            foreach (var element in elements)
            {
                AddElement(element);
            }
        }

        public void RemoveElement(UIElementBase element)
        {
            if (ElementsToRemove.Contains(element))
                return;

            ElementsToRemove.Enqueue(element);
            RegenerateBuffers = true;
        }

        public void Internal_AddElement(UIElementBase element)
        {
            element.UIController = this;
            if (element.ParentElement == null)
                AbsoluteElements.Add(element);
            else
                AbsoluteElements.Remove(element);

            Elements.Add(element);
            AddedElements.Add(element);

            if (element.IsInteractable() && !InteractableElementsSet.Contains(element))
            {
                InteractableElements.Add(element);
                InteractableElementsSet.Add(element);
            }

            if (element is IUICollection uiCollection)
            {
                uiCollection.ForeachChildren(Internal_AddElement);
            }
        }

        public void Internal_RemoveElement(UIElementBase element)
        {
            AbsoluteElements.Remove(element);
            Elements.Remove(element);

            if (InteractableElementsSet.Remove(element))
            {
                InteractableElements.Remove(element);
            }
            
            element.Destroy();

            if (element is IUICollection uiCollection)
            {
                uiCollection.ForeachChildren(Internal_RemoveElement);
            }

            element.UIController = null;
        }

        public static void InputField(Key key)
        {
            if (ActiveInputField == null)
                return;

            if (Input.IsKeyDown(Key.ControlLeft))
            {
                if (key == Key.V)
                {
                    if (SelectionSize != 0)
                    {
                        int startIndex = CursorCharacter;
                        int count = SelectionSize;

                        if (SelectionSize < 0)
                        {
                            startIndex = CursorCharacter + SelectionSize;
                            count = Mathf.Abs(SelectionSize);
                        }

                        SelectionSize = 0;
                        ActiveInputField.RemoveText(startIndex, count, false);     
                    }

                    var text = ClipboardService.GetText();
                    if (text != null)
                    {
                        ActiveInputField.AddText(text);
                    }
                    return;
                }
                if (key == Key.C)
                {
                    string copyText = "";
                    var text = ActiveInputField.GetText();

                    bool start = true;
                    var textLength = text.Length;
                    if (CursorCharacter >= textLength && textLength != 0)
                    {
                        start = false;
                    }

                    if (SelectionSize >= 0)
                    {
                        for (int i = 0; i < SelectionSize; i++)
                        {
                            int index = CursorCharacter + i;
                            if (index < 0 || index >= textLength)
                                continue;

                            copyText += text[index];
                        }
                    }
                    else
                    {
                        for (int i = SelectionSize; i <= (start ? -1 : 0); i++)
                        {
                            int index = CursorCharacter + i;
                            if (index < 0 || index >= textLength)
                                continue;

                            copyText += text[index];
                        }
                    }

                    ClipboardService.SetText(copyText);
                    return;
                }
            }

            if (key == Key.Backspace)
            {
                if (SelectionSize != 0)
                {
                    int startIndex = CursorCharacter;
                    int count = SelectionSize;

                    if (SelectionSize < 0)
                    {
                        startIndex = CursorCharacter + SelectionSize;
                        count = Mathf.Abs(SelectionSize);
                    }

                    SelectionSize = 0;
                    ActiveInputField.RemoveText(startIndex, count);     
                }
                else
                {
                    ActiveInputField.RemoveCharacter();
                }
                return;
            }

            if (key == Key.Enter && (ActiveInputField.UIController?.RemoveInputfieldOnEnter ?? true))
            {
                ActiveInputField.OnTextEnter?.Invoke(ActiveInputField);
                RemoveInputfield();
                return;
            }

            if (!Char.GetChar(out char c, key, Input.AreKeysDown(Key.ShiftLeft, Key.ShiftRight), Input.AreKeysDown(Key.AltLeft, Key.AltRight)))
                return;

            if (TextShaderHelper.CharExists(c))
            {
                ActiveInputField.AddCharacter(c);
            }
        }

        public void CalculateBoundaries() => UpdateBoundaries = true;

        public void SetPosition(float x, float y)
        {
            SetPosition(new Vector3(x, y, Position.Z));
        }

        public void SetPosition(Vector2 position)
        {
            SetPosition(new Vector3(position.X, position.Y, Position.Z));
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
            ModelMatrix = Matrix4.CreateTranslation(Position) * Matrix4.CreateScale(new Vector3(Scale, Scale, 1f));
            UpdateBoundaries = true;
        }

        public void SetScale(float scale, Vector3 windowOffset)
        {
            Vector3 mousePosition = Input.GetMousePosition3() - windowOffset;
            float scaleDelta = scale / Scale;
            Position = mousePosition + (Position - mousePosition) * scaleDelta;
            Scale = scale;
            ModelMatrix = Matrix4.CreateTranslation(Position) * Matrix4.CreateScale(new Vector3(Scale, Scale, 1f));
            UpdateBoundaries = true;
        }

        public Matrix4 GetProjection()
        {
            int width = Alignment.Width;
            int height = Alignment.Height;
            return Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -2, 2);
        }


        void Resize()
        {
            MaxDepth = 0f;

            foreach (var element in AbsoluteElements)
            {
                element.FirstPass();
                element.SecondPass();
            }

            _uiPlaneDescriptor.BindFramebuffer(_fbo, 0);

            UIMesh.Resize();
            TextMesh.Resize();
            UpdateBoundaries = true;
        }

        public bool HandleInputs()
        {
            if (Transform.Disabled || DisableInputHandling)
                return false;

            var over = false;
            for (int i = 0; i < InteractableElements.Count; i++)
            {
                var element = InteractableElements[i];
                if (element.Visible && element.Test() && !element.AllowPassingMouse)
                {
                    over = true;
                }
            }
            return over;
        }

        void Update()
        {
            HandleAnimations();
            GenerateBuffers();

            Vector2 offset = (Alignment.Left, Alignment.Top);
            
            if (_clickedInputField && Input.IsMouseDown(MouseButton.Left) && ActiveInputField != null && Input.MouseDelta.X != 0)
            {
                int charCount = ActiveInputField.GetText().Length;
                float maxWidth = ActiveInputField.Point2.X - ActiveInputField.Point1.X;
                float width = Mathf.Lerp(0, maxWidth, charCount / (float)(ActiveInputField.MaxCharCount ?? 20));
                float start = ActiveInputField.TextAlign switch
                {
                    TextAlign.Left => 0,
                    TextAlign.Center => (maxWidth - width) * 0.5f,
                    TextAlign.Right => maxWidth - width,
                    _ => 0
                };
                
                var character = Mathf.Max(0, Mathf.RoundToInt((ActiveInputField.HoverFactor.X - (start / maxWidth)) * (float)(ActiveInputField.MaxCharCount ?? 20)));
                if (charCount < character && ActiveInputField.TextAlign != TextAlign.Left)
                    character = 0;
                else
                    character = Mathf.Min(character, charCount);

                var result = character - CursorCharacter;
                if (result != SelectionSize)
                {
                    SelectionSize = result;
                    TextMesh.SetCursor(ActiveInputField);
                }
            }

            if (Input.IsMouseReleased(MouseButton.Left))
            {
                _clickedInputField = false;
            }

            UIMesh.Update();
            TextMesh.Update();
            MaskData.Update();

            if (_interactUpdate)
            {
                foreach (var element in AddAsInteractableQueue)
                {
                    if (InteractableElementsSet.Contains(element))
                        continue;

                    InteractableElements.Add(element);
                    InteractableElementsSet.Add(element);
                }
                foreach (var element in RemoveAsInteractableQueue)
                {
                    if (InteractableElementsSet.Remove(element))
                    {
                        InteractableElements.Remove(element);
                    }
                }
                AddAsInteractableQueue = [];
                RemoveAsInteractableQueue = [];
                _interactUpdate = false;
            }

            if (UpdateBoundaries)
            {
                foreach (var element in Elements)
                {
                    element.CalculateBoundaries(offset);
                }

                UpdateBoundaries = false;
            }
        }

        private void GenerateBuffers()
        {
            if (RegenerateBuffers)
            {
                MaxDepth = 0f;

                while (ElementsToRemove.Count > 0)
                {
                    UIElementBase element = ElementsToRemove.Dequeue();
                    Internal_RemoveElement(element);
                }

                while (ElementsToAdd.Count > 0)
                {
                    UIElementBase element = ElementsToAdd.Dequeue();
                    Internal_AddElement(element);
                }

                foreach (var element in AbsoluteElements)
                {
                    element.FirstPass();
                    element.SecondPass();
                }

                foreach (var element in AddedElements)
                {
                    element.Generate();
                }

                AddedElements = [];

                RegenerateBuffers = false;
            }

            while (UpdateQueue.Count > 0)
            {
                var element = UpdateQueue.Dequeue();
                if (Elements.Contains(element))
                {
                    element.UpdateAction?.Execute();
                    element.UpdateAction = null;
                }
            }
        }

        private void HandleAnimations()
        {
            for (int i = 0; i < AnimationList.Count; i++)
            {
                var animation = AnimationList[i];
                animation.Element.IsAnimating = true;

                animation.CurrentTime += GameTime.DeltaTime;

                if (animation.CurrentTime >= animation.Duration || animation.delete)
                {
                    animation.End();
                    AnimationList.RemoveAt(i);
                    animation.Element.QueueDisableAnimating();
                    i--;
                    continue;
                }

                animation.Ease();
            }
        }

        public static bool IsActiveInputfield(PBG.UI.UIField inputfield) => ActiveInputField == inputfield;
        public static void SetInputfield(PBG.UI.UIField inputfield)
        {
            ActiveInputField = inputfield;
            _clickedInputField = true;
            SelectionSize = 0;

            int charCount = inputfield.GetText().Length;
            float maxWidth = inputfield.Point2.X - inputfield.Point1.X;
            float width = Mathf.Lerp(0, maxWidth, charCount / (float)(inputfield.MaxCharCount ?? 20));
            float start = inputfield.TextAlign switch
            {
                TextAlign.Left => 0,
                TextAlign.Center => (maxWidth - width) * 0.5f,
                TextAlign.Right => maxWidth - width,
                _ => 0
            };

            CursorCharacter = Mathf.Max(0, Mathf.RoundToInt((inputfield.HoverFactor.X - (start / maxWidth)) * (float)(inputfield.MaxCharCount ?? 20)));
            if (charCount < CursorCharacter && inputfield.TextAlign != TextAlign.Left)
                CursorCharacter = 0;
            else
                CursorCharacter = Mathf.Min(CursorCharacter, charCount);

            inputfield.UIController?.TextMesh.SetCursor(inputfield);
        }

        public static void RemoveInputfield()
        {
            ActiveInputField?.UIController?.TextMesh.RemoveCursor();
            ActiveInputField = null;
        }

        public static void ClearFrameBuffer()
        {
            CumulativeDepth = 0f;
        }

        void Render()
        {
            var viewport = GFX.GetViewport();

            int width = Alignment.Width;
            int height = Alignment.Height;

            _fbo.Bind();

            GFX.Viewport(Alignment.Left, Alignment.Top, width, height);

            Matrix4 model = ModelMatrix * Matrix4.CreateTranslation((0, 0, CumulativeDepth));
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, width, 0, height, -2, 2);
            //projection.M22 *= -1;

            if (UIMesh.ElementCount > 0)
            {
                UIData.UiShader.Bind();

                UIMesh.Descriptor.UniformMatrix4(UIData.modelLoc, model);
                UIMesh.Descriptor.UniformMatrix4(UIData.projectionLoc, projection);

                UIMesh.Render();
            }

            if (TextMesh.LineCount > 0)
            {
                UIData.TextShader.Bind();

                TextMesh.Descriptor.UniformMatrix4(UIData.textModelLoc, model);
                TextMesh.Descriptor.UniformMatrix4(UIData.textProjectionLoc, projection);
                TextMesh.Descriptor.Uniform1(UIData.textTimeLoc, GameTime.TotalTime);

                TextMesh.Render();
            }

            CumulativeDepth += MaxDepth + 0.00001f;

            _fbo.Unbind();

            GFX.Viewport(viewport.x, viewport.y, viewport.width, viewport.height);
        }

        

        public static void GlobalRender()
        {
            if (_uiPlaneShader == null)
                return;

            _uiPlaneShader.Bind();
            _uiPlaneDescriptor.Bind();

            GFX.Draw(3, 1, 0, 0);
        }

        public static void BindFramebuffer()
        {
            _fbo.Bind();
        }

        public static void UnbindFramebuffer()
        {
            _fbo.Unbind();
        }

        void Dispose()
        {
            Controllers.Remove(this);
            UIMesh.Delete();
            TextMesh.Delete();
            MaskData.Delete();

            AbsoluteElements = [];
            Elements = [];
            InteractableElements = [];
            InteractableElementsSet = [];

            ElementsToAdd = [];
            ElementsToRemove = [];
            AddedElements = [];

            UpdateQueue = [];

            AddAsInteractableQueue = [];
            RemoveAsInteractableQueue = [];
            
            AnimationList = [];
        }
    }
}