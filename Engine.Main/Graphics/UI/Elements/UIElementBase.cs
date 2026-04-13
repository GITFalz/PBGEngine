using System.Diagnostics.CodeAnalysis;
using PBG.Data;
using PBG.Mathematics;
using PBG.UI.Animation;
using PBG.UI.Exception;



namespace PBG.UI
{
    public abstract partial class UIElementBase : IUIChild
    {
        public UIController? UIController = null;
        public UIElementBase? ParentElement = null;
        public PercentAlignementType PercentAlignement = PercentAlignementType.None;

        public UIElementTag Tag = UIElementTag.Any;

        public string Name = "UIElement";

        public UISize? MinWidth = null;
        public UISize Width = UISize.None(100);
        public UISize? MaxWidth = null;

        public UISize? MinHeight = null;
        public UISize Height = UISize.None(100);
        public UISize? MaxHeight = null;

        public Vector2 Size { get; set; } = (0, 0);
        public float SizeX { get => Size.X; set => Size = (value, Size.Y); }
        public float SizeY { get => Size.Y; set => Size = (Size.X, value); }
        public Vector2 BaseOffset = (0, 0);
        public Vector4 Color { get; set; }
        public float Xoffset
        {
            get => BaseOffset.X;
            set => BaseOffset.X = value;
        }
        public float Yoffset
        {
            get => BaseOffset.Y;
            set => BaseOffset.Y = value;
        }
        public Vector2 CollectionOffset = (0, 0);
        public Vector2 AddedOffset = (0, 0);
        public Vector4 Transform { get; set; } = (0, 0, 0, 0);
        public float Depth = 0;
        public int MaskIndex { get; set; } = -1;
        public bool Masked => MaskIndex >= 0;

        // Animation
        public Vector2 AnimationTranslation { get; set; } = (0, 0);
        public float AnimationScale { get; set; } = 1;
        public float AnimationRotation { get; set; } = 0;

        public UIAnimation? AnimationHover = null;
        public UIAnimation? AnimationClick = null;
        public UIAnimation? Animation = null;

        public bool IsAnimating = false;

        public Action DeleteHoverAnimationAction = () => { };
        public Action DeleteClickAnimationAction = () => {};
        public Action DeleteAnimationAction = () => {};

        public UIAlign Alignement = UIAlign.TopLeft;
        public Dataset Dataset = [];
        public bool Visible { get; set; } = true;
        public bool SetVisibleBefore = false;

        public Vector2 Origin
        {
            get => Transform.Xy;
            set => Transform = (value.X, value.Y, Transform.Z, Transform.W);
        }
        public Vector2 Center => Origin + (Size / 2);
        public Vector2 HoverFactor = Vector2.Zero;
        public Vector2 Point1 = Vector2.Zero;
        public Vector2 Point2 = Vector2.Zero;
        public Vector2 MaskPoint1 = Vector2.Zero;
        public Vector2 MaskPoint2 = Vector2.Zero;

        public Vector2 TopLeft => Origin;
        public Vector2 TopRight => Origin + (Size.X, 0);
        public Vector2 BottomLeft => Origin + (0, Size.Y);
        public Vector2 BottomRight => Origin + Size;

        public bool Hovering = false;
        public bool Clicked = false;
        public bool IsSelected = false;
        public bool AllowPassingMouse = false;

        public UIQueueAction? UpdateAction = null;

        public Action? OnCreated = null;

        public UIElementBase() { }
        public UIElementBase(Vector4 defaultColor)
        {
            Color = defaultColor;
        }
        public UIElementBase(string name) { Name = name; }
        public UIElementBase(UIAlign alignement) { Alignement = alignement; }
        public UIElementBase(string name, UIAlign alignement) { Name = name; Alignement = alignement; }

        private Action<UIElementBase>? _onHoverEnter { get; set; } = null;
        private Action<UIElementBase>? _onHover { get; set; } = null;
        private Action<UIElementBase>? _onClick { get; set; } = null;
        private Action<UIElementBase>? _onHold { get; set; } = null;
        private Action<UIElementBase>? _onRelease { get; set; } = null;
        private Action<UIElementBase>? _onHoverExit { get; set; } = null;

        public virtual void OnHoverEnterAction() => _onHoverEnter?.Invoke(this);
        public virtual void OnHoverAction() => _onHover?.Invoke(this);
        public virtual void OnClickAction() => _onClick?.Invoke(this);
        public virtual void OnHoldAction() => _onHold?.Invoke(this);
        public virtual void OnReleaseAction() => _onRelease?.Invoke(this);
        public virtual void OnHoverExitAction() => _onHoverExit?.Invoke(this);

        public virtual bool Test()
        {
            var mouseOver = IsMouseOver();
            TestButtons(mouseOver);
            return mouseOver;
        }

        public bool IsMouseOver()
        {
            Vector2 pos = Input.GetMousePosition();
            return MouseOver(pos);
        }

        private void TestButtons(bool mouseOver)
        {
            if (mouseOver)
            {
                if (!Hovering)
                {
                    if (UIController != null) AnimationHover?.Enter(UIController, this, ref DeleteHoverAnimationAction);
                    OnHoverEnterAction();
                    Hovering = true;
                }

                OnHoverAction();

                if (Input.IsMousePressed(MouseButton.Left) && !Clicked)
                {
                    if (UIController != null) AnimationClick?.Enter(UIController, this, ref DeleteClickAnimationAction);
                    OnClickAction();
                    Clicked = true;
                }
            }
            else if (Hovering)
            {
                if (UIController != null) AnimationHover?.Exit(UIController, this, ref DeleteHoverAnimationAction);
                OnHoverExitAction();
                Hovering = false;
            }

            if (Clicked)
            {
                OnHoldAction();
            }

            if (Input.IsMouseReleased(MouseButton.Left))
            {
                if (Clicked)
                {
                    if (UIController != null) AnimationClick?.Exit(UIController, this, ref DeleteClickAnimationAction);
                    OnReleaseAction();
                    Clicked = false;
                }
            }
        }

        public bool MouseOver(Vector2 pos)
        {
            Vector2 point1 = Point1;
            Vector2 point2 = Point2;

            if (Masked)
            {
                point1 = Mathf.Max(Point1, MaskPoint1);
                point2 = Mathf.Min(Point2, MaskPoint2);
            }

            bool inside = pos.X >= point1.X && pos.X <= point2.X && pos.Y >= point1.Y && pos.Y <= point2.Y;
            if (inside)
            {
                HoverFactor = new Vector2(
                    (pos.X - Point1.X) / (Point2.X - Point1.X),
                    (pos.Y - Point1.Y) / (Point2.Y - Point1.Y)
                );
            }

            return inside;
        }

        public void CalculateBoundaries(Vector2 offset)
        {
            Matrix4 model = UIController?.ModelMatrix ?? Matrix4.Identity;
            if (Masked && GetMaskPanel(out Rendering.Mask.UIMaskStruct? mask))
            {
                GetBoundaries(offset, mask.Value.TopLeft, mask.Value.Size, model, out MaskPoint1, out MaskPoint2);
            }

            GetBoundaries(offset, Origin, Size, model, out Point1, out Point2);
        }
        
        public void GetBoundaries(Vector2 offset, Vector2 origin, Vector2 size, Matrix4 model, out Vector2 point1, out Vector2 point2)
        {
            point1 = Vector3.TransformPosition((origin.X, origin.Y, 0), model).Xy + offset;
            point2 = Vector3.TransformPosition((origin.X + size.X, origin.Y + size.Y, 0), model).Xy + offset;
        }

        public UIElementBase StopTesting()
        {
            UIController?.SetAsInteractable(this, false);
            return this;
        }

        public UIElementBase ResumeTesting()
        {
            UIController?.SetAsInteractable(this, true);
            return this;
        }

        public virtual UIElementBase SetOnHoverEnter(Action<UIElementBase>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHoverEnter = action;
            return this;
        }

        public virtual UIElementBase SetOnHover(Action<UIElementBase>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHover = action;
            return this;
        }

        public virtual UIElementBase SetOnClick(Action<UIElementBase>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onClick = action;
            return this;
        }

        public virtual UIElementBase SetOnHold(Action<UIElementBase>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHold = action;
            return this;
        }

        public virtual UIElementBase SetOnRelease(Action<UIElementBase>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onRelease = action;
            return this;
        }

        public virtual UIElementBase SetOnHoverExit(Action<UIElementBase>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHoverExit = action;
            return this;
        }

        public virtual bool IsInteractable() =>
            _onHoverEnter != null ||
            _onHover != null ||
            _onClick != null ||
            _onHold != null ||
            _onRelease != null ||
            _onHoverExit != null ||
            AnimationHover != null ||
            AnimationClick != null;

        public void Delete()
        {
            Visible = false;
            OnHoverExitAction();
            OnReleaseAction();
            ParentElement?.RemoveElement(this);
            ParentElement = null;
            UIController?.RemoveElement(this);
        }

        public void AddTo(UICol parent) => parent.AddElement(this);

        public void Created()
        {
            OnCreated?.Invoke();
            OnCreated = null;
        }

        public bool IsParent(UIElementBase element)
        {
            if (ParentElement == null)
                return false;
            
            if (ParentElement == element)
                return true;

            return ParentElement.IsParent(element);
        }
        
        public UIElementBase Align()
        {
            FirstPass();
            SecondPass();
            return this;
        }

        public virtual void FirstPass()
        {
            CalculateWidth();
            CalculateHeight();
        }

        public void CalculateWidth()
        {
            float width = ParentElement?.Size.X ?? VoxelEngine.Width;
            float sizeX = Width.Compute(width);

            if (MinWidth != null)
                sizeX = Mathf.Max(sizeX, MinWidth.Compute(width));

            if (MaxWidth != null)
                sizeX = Mathf.Min(sizeX, MaxWidth.Compute(width));

            SizeX = sizeX;
            UIController?.CalculateBoundaries();
        }

        public void CalculateHeight()
        {
            float height = ParentElement?.Size.Y ?? VoxelEngine.Height;
            float sizeY = Height.Compute(height);

            if (MinHeight != null)
                sizeY = Mathf.Max(sizeY, MinHeight.Compute(height));

            if (MaxHeight != null)
                sizeY = Mathf.Min(sizeY, MaxHeight.Compute(height));

            SizeY = sizeY;
            UIController?.CalculateBoundaries();
        }

        public virtual void SecondPass()
        {
            float width = ParentElement?.Size.X ?? VoxelEngine.Width;
            float height = ParentElement?.Size.Y ?? VoxelEngine.Height;
            float depth = (ParentElement?.Transform.Z + 0.00001f ?? 0) + (Depth * 0.00001f);
            Transform = (Transform.X, Transform.Y, depth, Transform.W);
    
            Vector2 offset = BaseOffset + CollectionOffset + AddedOffset + (ParentElement?.Origin ?? Vector2.Zero);
            Origin = _computeBaseOrigin[Alignement](width, height, Size.X, Size.Y) + offset;

            if (UIController != null)
            {
                UIController.MaxDepth = Mathf.Max(UIController.MaxDepth, depth);
                UIController?.CalculateBoundaries();
            }
        }

        public virtual void Generate() { }
        public abstract void UpdateChildMaskIndex(int index);
        public virtual void UpdateTextureIndex(int textureIndex) { }
        public virtual void UpdateIconIndex(int textureIndex) {}
        public virtual void UpdateItem(string name) {}
        public abstract bool GetMaskPanel([NotNullWhen(true)] out PBG.Rendering.Mask.UIMaskStruct? mask);
        public void ApplyChanges(UIChange changes = UIChange.None)
        {
            bool run(UIChange flag) => (changes & flag) != 0;
            if (run(UIChange.Scale))
            {
                Align();
                UpdateScale();
                UpdateTransform();
            }
            if (run(UIChange.Transform))
            {
                SecondPass();
                UpdateTransform();
            }
            if (run(UIChange.Color)) UpdateColor();
            if (run(UIChange.Characters)) UpdateCharacters();
            if (run(UIChange.Border)) UpdateBorderUI();
            if (run(UIChange.BorderColor)) UpdateBorderColor();
        }
        public abstract UIElementBase UpdateTransform();
        public abstract UIElementBase UpdateScale();
        public abstract UIElementBase UpdateColor();
        public abstract UIElementBase UpdateBorderUI();
        public abstract UIElementBase UpdateBorderColor();
        public abstract UIElementBase UpdateBorderColor(Vector4 color);

        public UIElementBase UpdateColor(Vector4 color)
        {
            Color = color;
            UpdateColor();
            return this;
        }

        public UIElementBase UpdateColor(Vector3 color)
        {
            Color = new Vector4(color, 1f);
            UpdateColor();
            return this;
        }

        public UIElementBase UpdateColor(float gray) => UpdateColor(new Vector3(gray));
        
        public abstract UIElementBase UpdateAnimationTranslation();
        public abstract UIElementBase UpdateAnimationScale();
        public abstract UIElementBase UpdateAnimationRotation();

        public void UpdateAnimationTranslation(Vector2 value) { AnimationTranslation = value; UpdateAnimationTranslation();}
        public void UpdateAnimationScale(float value) { AnimationScale = value; UpdateAnimationScale();}
        public void UpdateAnimationRotation(float value) { AnimationRotation = value; UpdateAnimationRotation(); }

        public void HoverEnter() { if (UIController != null) AnimationHover?.Enter(UIController, this, ref DeleteHoverAnimationAction); }
        public void HoverExit() { if (UIController != null) AnimationHover?.Exit(UIController, this, ref DeleteHoverAnimationAction); }
        public void ClickEnter() { if (UIController != null) AnimationClick?.Enter(UIController, this, ref DeleteClickAnimationAction); } 
        public void ClickExit() { if (UIController != null) AnimationClick?.Exit(UIController, this, ref DeleteClickAnimationAction); }
        public void AnimationEnter() { if (UIController != null) Animation?.Enter(UIController, this, ref DeleteAnimationAction); }
        public void AnimationExit() { if (UIController != null) Animation?.Exit(UIController, this, ref DeleteAnimationAction); }
        
        public virtual UIElementBase UpdateCharacters() { return this; }
        public virtual UIElementBase SetVisible(bool visible)
        {
            if (!visible && Visible)
                HoverExit();
                
            Visible = visible;
            Clicked = false;
            Hovering = false;
            return this;
        }

        public void InitQueue(UIQueueEntry entry)
        {
            if (UpdateAction == null)
            {
                UpdateAction = new(this);
                UIController?.QueueAction(this);   
            }
            UpdateAction.Actions |= entry;
        }
        public void QueueAlign()
        {
            InitQueue(UIQueueEntry.Align);
        }
        public void QueueUpdateTransformation()
        {
            InitQueue(UIQueueEntry.Transform);
        }
        public void QueueUpdateScaling()
        {
            InitQueue(UIQueueEntry.Scale);
        }
        public void QueueUpdateVisibility(bool visible)
        {
            InitQueue(UIQueueEntry.Visibility);
            UpdateAction!.QueuedVisibility = visible;
        }
        public void QueueDisableAnimating()
        {
            InitQueue(UIQueueEntry.DisableAnimation);
        }

        [Flags]
        public enum UIQueueEntry : short
        {
            Align = 1,
            Transform = 2,
            Scale = 4,
            Visibility = 8,
            DisableAnimation = 16
        }

        public virtual T? GetElement<T>() where T : UIElementBase => this is T element ? element : null;
        public virtual T? GetElementAt<T>(int number) where T : UIElementBase => this is T element ? element : null;
        public virtual UIElementBase? GetElement(UIElementTag tag) => Tag == tag ? this : null;
        public virtual UIElementBase? GetElementAt(UIElementTag tag, int number) => Tag == tag ? this : null;
        public virtual UIElementBase? GetElement(string name) => Name == name ? this : null;
        public virtual T? GetElement<T>(string name) where T : UIElementBase => Name == name && this is T t ? t : null;
        public virtual UIElementBase? GetElementAt(string name, int number) => Name == name ? this : null;
        public virtual T? QueryElement<T>() where T : UIElementBase => GetElement<T>();
        public virtual List<T> QueryElements<T>() where T : UIElementBase => this is T t ? [t] : [];
        public virtual UIElementBase? QueryElement(string name) => GetElement(name);

        public UIElementBase? GetHighestMaskedParent()
        {
            if (ParentElement == null)
            {
                return MaskIndex != -1 ? this : null;
            }
            if (MaskIndex != -1)
            {
                if (ParentElement.MaskIndex != -1)
                {
                    return ParentElement.GetHighestMaskedParent();
                }
                return this;
            }
            return null;
        }

        public Vector2 GetMaskedSize()
        {
            (Vector2 point1, Vector2 point2) = GetRecursiveMaskedSize();
            Vector2 size = Mathf.Max(Vector2.Zero, point2 - point1);
            return Mathf.Min(size, Size);
        }

        protected (Vector2, Vector2) GetRecursiveMaskedSize()
        {
            Vector2 point1 = Mathf.Max(Point1, MaskPoint1);
            Vector2 point2 = Mathf.Min(Point2, MaskPoint2);

            if (ParentElement != null && ParentElement.Masked)
            {
                (Vector2 parentPoint1, Vector2 parentPoint2) = ParentElement.GetRecursiveMaskedSize();
                point1 = Mathf.Max(parentPoint1, point1);
                point2 = Mathf.Min(parentPoint2, point2);
            }

            return (point1, point2);
        }

        public virtual bool RemoveElement(UIElementBase element) => false;
        public bool RemoveFromParent() => ParentElement?.RemoveElement(this) ?? false;
        public abstract void Destroy();

        #region Helper functions
        public bool IsLeftAligned() => IsAlignedTo(UIAlignMasks.Left);
        public bool IsCenterAligned() => IsAlignedTo(UIAlignMasks.Center);
        public bool IsRightAligned() => IsAlignedTo(UIAlignMasks.Right);
        public bool IsTopAligned() => IsAlignedTo(UIAlignMasks.Top);
        public bool IsLMiddleAligned() => IsAlignedTo(UIAlignMasks.Middle);
        public bool IsBottomAligned() => IsAlignedTo(UIAlignMasks.Bottom);
        public bool IsAlignedTo(UIAlignMasks mask) => ((uint)Alignement & (uint)mask) != 0;
        protected UIController ControllerCheck()
        {
            if (UIController == null)
                throw new UIControllerNotFoundException(Name);
            return UIController;
        }
        #endregion

        private static readonly Dictionary<UIAlign, Func<float, float, float, float, Vector2>> _computeBaseOrigin = new Dictionary<UIAlign, Func<float, float, float, float, Vector2>>()
        {
            { UIAlign.TopLeft, (pWidth, pHeight, width, height) =>      (0,                  0                   ) },
            { UIAlign.TopCenter, (pWidth, pHeight, width, height) =>    (pWidth/2 - width/2, 0                   ) },
            { UIAlign.TopRight, (pWidth, pHeight, width, height) =>     (pWidth - width,     0                   ) },

            { UIAlign.MiddleLeft, (pWidth, pHeight, width, height) =>   (0,                  pHeight/2 - height/2) },
            { UIAlign.MiddleCenter, (pWidth, pHeight, width, height) => (pWidth/2 - width/2, pHeight/2 - height/2) },
            { UIAlign.MiddleRight, (pWidth, pHeight, width, height) =>  (pWidth - width,     pHeight/2 - height/2) },

            { UIAlign.BottomLeft, (pWidth, pHeight, width, height) =>   (0,                  pHeight - height    ) },
            { UIAlign.BottomCenter, (pWidth, pHeight, width, height) => (pWidth/2 - width/2, pHeight - height    ) },
            { UIAlign.BottomRight, (pWidth, pHeight, width, height) =>  (pWidth - width,     pHeight - height    ) },
        };

        public T InternalClass<T>(T element, params IStyleData[] styles)
        {
            for (int i = 0; i < styles.Length; i++)
                styles[i].Set(this);
            return element;
        }

        public override string ToString()
        {
            return $"UIElement(Name: {Name}, Tag: {Tag}, Position: {Origin}, Size: {Size})";
        }
    }

    [Flags]
    public enum PercentAlignementType
    {
        None,
        Vertical,
        Horizontal
    }
}