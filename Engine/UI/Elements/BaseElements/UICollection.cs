using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;

namespace PBG.UI
{
    public class UICol : UIPanel
    {
        public float Spacing = 0;
        public Vector4 Border = (0, 0, 0, 0);

        public bool IgnoreInvisibleElements = false;
        public bool AllowScrollingToTop = false; // Used for scroll collections
        public float ScrollingSpeed = 5f;  // Used for scroll collections
        public bool GrowFromChildren { get; set; } = false;
        public bool MaskChildren = false;
        public bool ForceToggleVisible = true;
        public bool WasVisible { get; set; } = true;
        public bool FitChildren { get; set; } = false;

        public List<UIElementBase> ChildElements = [];

        public UICol() : this("UICol") {}
        public UICol(string name) : base() 
        { 
            Name = name;
            Tag = UIElementTag.UICollection;
        }

        public UICol(params IStyleData[] styles) : this() 
        { 
            Class(styles);
        }

        public UICol(string name, params IStyleData[] styles) : this(name) 
        { 
            Class(styles);
        }

        public UICol Ref(ref UICol text)
        {
            text = this;
            return text;
        }

        public UICol Out(out UICol text)
        {
            text = this;
            return text;
        }

        public UICol Class(params IStyleData[] styles) => Style(this, styles);

        public UICol OnHoverEnter(Action<UICol>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UICol OnHover(Action<UICol>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UICol OnClick(Action<UICol>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UICol OnHold(Action<UICol>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UICol OnRelease(Action<UICol>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UICol OnHoverExit(Action<UICol>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }

        public UICol this[params IUIChild[] subElements]
        {
            get { AddElements(subElements); return this; }
        }

        public UICol this[IEnumerable<IUIChild> subElements]
        {
            get { AddElements(subElements); return this; }
        }

        public bool Has(UIElementBase element) => ChildElements.Contains(element);

        public void SetSpacing(float spacing) => Spacing = spacing;
        public void SetBorder(Vector4 border) => Border = border;
        public void SetBorderX(float x) => Border.X = x;
        public void SetBorderY(float y) => Border.Y = y;
        public void SetBorderZ(float z) => Border.Z = z;
        public void SetBorderW(float w) => Border.W = w;
        public void SetIgnoreInvisibleElements(bool ignore) => IgnoreInvisibleElements = ignore;
        public void SetAllowScrollingToTop(bool allow) => AllowScrollingToTop = allow;
        public void SetScrollingSpeed(float speed) => ScrollingSpeed = speed;
        public void SetGrowFromChildren(bool grow) => GrowFromChildren = grow;
        public void SetForceToggleVisible(bool force) => ForceToggleVisible = force;
        public void SetMaskChildren(bool mask) => MaskChildren = mask;
        public List<UIElementBase> GetChildren() => ChildElements;

        public override void FirstPass()
        {
            CollectionFirstPass();
        }

        protected float OffsetX(UIElementBase child) => child.IsLeftAligned() ? Border.X + child.Padding.X : (child.IsRightAligned() ? Border.Z + child.Padding.Z : 0);
        protected float OffsetY(UIElementBase child) => child.IsTopAligned() ? Border.Y + child.Padding.Y : (child.IsBottomAligned() ? Border.W + child.Padding.W : 0);

        public virtual void CollectionFirstPass()
        {
            bool notGrowOrFit = !GrowFromChildren || FitChildren;
            if (notGrowOrFit || !Width.IsNone())
                CalculateWidth();

            if (notGrowOrFit || !Height.IsNone())
                CalculateHeight();
            
            if (GrowFromChildren && !FitChildren)
                HandleGrowFromChildren();
            else
                HandleBasicFirstPass();
        }

        private void HandleBasicFirstPass()
        {
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.PercentAlignement = PercentAlignementType.None;
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float xOffset = OffsetX(child);
                float yOffset = OffsetY(child);

                child.CollectionOffset = (xOffset, yOffset);
            }
        }

        private void HandleGrowFromChildren()
        {
            float maxWidth = 0;
            float maxHeight = 0;

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.PercentAlignement = PercentAlignementType.None;
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    continue;

                float xOffset = OffsetX(child);
                float yOffset = OffsetY(child);

                child.CollectionOffset = (xOffset, yOffset);

                if (child.Width.IsPercent())
                    child.PercentAlignement |= PercentAlignementType.Horizontal;
                else
                    maxWidth = Mathf.Max(maxWidth, Border.X + child.BaseOffset.X + child.Size.X + Border.Z);

                if (child.Height.IsPercent())
                    child.PercentAlignement |= PercentAlignementType.Vertical;
                else
                    maxHeight = Mathf.Max(maxHeight, Border.Y + child.BaseOffset.Y + child.Size.Y + Border.W);
            }
            
            if (!Width.IsPercent())
                Width = UISize.Pixels(maxWidth);
            if (!Height.IsPercent())
                Height = UISize.Pixels(maxHeight);

            CalculateWidth();
            CalculateHeight();

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.PercentAlignement.HasFlag(PercentAlignementType.Horizontal))
                {
                    child.Width.AddedOffset = -(Border.X + Border.Z);
                    child.CalculateWidth();
                }

                if (child.PercentAlignement.HasFlag(PercentAlignementType.Vertical))
                {
                    child.Height.AddedOffset = -(Border.Y + Border.W);
                    child.CalculateHeight();
                }
            }
        }

        public override void SecondPass()
        {
            base.SecondPass();

            if (MaskChildren)
            {
                (Vector2 topLeft, Vector2 bottomRight) = GetMaskCorners();
                ControllerCheck().MaskData.AddElement(this, topLeft, bottomRight);
            }

            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.MaskIndex = MaskChildren ? MaskIndex : (child.ParentElement?.MaskIndex ?? -1);
                child.SecondPass();
            }
        }

        public (Vector2 topLeft, Vector2 bottomRight) GetMaskCorners()
        {
            Vector2 topLeft = Origin;
            Vector2 bottomRight = topLeft + Size;
            if (ParentElement != null && ParentElement.MaskIndex != -1 && ControllerCheck().MaskData.GetMask(ParentElement.MaskIndex, out var mask))
            {
                topLeft = Mathf.Max(mask.Value.TopLeft, topLeft);
                bottomRight = Mathf.Min(mask.Value.BottomRight, bottomRight);
            }
            return (topLeft, bottomRight);
        }

        public void UpdateMaskIndices() => UpdateChildMaskIndex(MaskIndex);
        public override void UpdateChildMaskIndex(int index)
        {
            base.UpdateChildMaskIndex(index);
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.UpdateChildMaskIndex(index);
            }
        }

        public override bool GetMaskPanel([NotNullWhen(true)] out Rendering.Mask.UIMaskStruct? mask) => ControllerCheck().MaskData.GetMask(MaskIndex, out mask);
        public override UIElementBase UpdateTransform()
        {
            (Vector2 topLeft, Vector2 bottomRight) = GetMaskCorners();
            UIController?.MaskData.UpdateTransform(this, topLeft, bottomRight);
            base.UpdateTransform();
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.UpdateTransform();
            }
            return this;
        }
        public override UIElementBase UpdateScale()
        {
            (Vector2 topLeft, Vector2 bottomRight) = GetMaskCorners();
            UIController?.MaskData.UpdateScale(this, topLeft, bottomRight);
            base.UpdateScale();
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.UpdateScale();
            }
            return this;
        }

        public override UIElementBase UpdateAnimationTranslation()
        {
            base.UpdateAnimationTranslation();
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.AnimationTranslation = AnimationTranslation;
                child.UpdateAnimationTranslation();
            }
            return this;
        }

        public override UIElementBase UpdateAnimationScale()
        {
            base.UpdateAnimationScale();
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.AnimationScale = AnimationScale;
                child.UpdateAnimationScale();
            }
            return this;
        }

        public override UIElementBase UpdateAnimationRotation()
        {
            base.UpdateAnimationRotation();
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.AnimationRotation = AnimationRotation;
                child.UpdateAnimationRotation();
            }
            return this;
        }

        public override UIElementBase SetVisible(bool visible)
        {
            SetVisibleBefore = true;
            if (!visible)
            {
                WasVisible = Visible;
            }

            if (visible && !ForceToggleVisible && !WasVisible && (ParentElement?.SetVisibleBefore ?? false))
                return this;
            
            base.SetVisible(visible);
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                child.SetVisible(visible);
            }
            SetVisibleBefore = false;
            return this;
        }

        public bool ContainsHoveringScrollView()
        {
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child is UICol col)
                {
                    if (child.Hovering)
                        return true;
                    if (col is UIVScroll && child.Hovering)
                        return true;
                    if (col.ContainsHoveringScrollView())
                        return true;
                }
            }
            return false;
        }

        public override T? GetElement<T>() where T : class
        {
            T? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child is T typed)
                {
                    element = typed;
                    break;
                }
            }
            return element;
        }

        public override T? GetElementAt<T>(int number) where T : class
        {
            int count = 0;
            T? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child is T typed)
                {
                    if (count == number || number <= 0)
                    {
                        element = typed;
                        break;
                    }
                    count++;
                }
            }
            return element;
        }

        public override UIElementBase? GetElement(UIElementTag tag)
        {
            UIElementBase? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Tag == tag)
                {
                    element = child;
                    break;
                }
            }
            return element;
        }

        public override UIElementBase? GetElementAt(UIElementTag tag, int number)
        {
            int count = 0;
            UIElementBase? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Tag == tag)
                {
                    if (count == number || number <= 0)
                    {
                        element = child;
                        break;
                    }
                    count++;
                }
            }
            return element;
        }

        public override UIElementBase? GetElement(string name)
        {
            UIElementBase? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Name == name)
                {
                    element = child;
                    break;
                }
            }
            return element;
        }

        public override T? GetElement<T>(string name) where T : class
        {
            T? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Name == name && child is T t)
                {
                    element = t;
                    break;
                }
            }
            return element;
        }

        public override UIElementBase? GetElementAt(string name, int number)
        {
            int count = 1;
            UIElementBase? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Name == name)
                {
                    if (count == number || number <= 0)
                    {
                        element = child;
                        break;
                    }
                    count++;
                }
            }
            return element;
        }

        public override T? QueryElement<T>() where T : class
        {
            if (this is T typed)
                return typed;

            T? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                var e = child.QueryElement<T>();
                if (e != null)
                {
                    element = e;
                    break;
                }
            }
            return element;
        }

        public override List<T> QueryElements<T>() where T : class
        {
            List<T> elements = [];
            if (this is T typed)
                elements.Add(typed);
                
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                elements.AddRange(child.QueryElements<T>());
            }
            return elements;
        }

        public override UIElementBase? QueryElement(string name)
        {
            UIElementBase? element = null;
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child.Name == name)
                {
                    element = child;
                    break;
                }
                var e = child.QueryElement(name);
                if (e != null)
                {
                    element = e;
                    break;
                }
            }
            return element;
        }

        public UIElementBase AddElement(UIElementBase element)
        {
            if (IsParent(element))
                throw new System.Exception("Cannot add parent as child element.");

            element.ParentElement = this;
            if (!Visible)
                element.Visible = Visible;
            element.MaskIndex = MaskIndex;
            ChildElements.Add(element);
            return this;
        }

        public UIElementBase Insert(int index, UIElementBase element)
        {
            if (IsParent(element))
                throw new System.Exception("Cannot add parent as child element.");

            element.ParentElement = this;
            if (!Visible)
                element.Visible = Visible;
            element.MaskIndex = MaskIndex;
            ChildElements.Insert(index, element);
            return this;
        }

        public UIElementBase AddElements(IEnumerable<IUIChild> elements)
        {
            foreach (var element in elements)
            {
                element.AddTo(this);
            }
            return this;
        }

        public UIElementBase AddElements(params UIElementBase[] elements)
        {
            foreach (var element in elements)
            {
                AddElement(element);
            }
            return this;
        }

        public override bool RemoveElement(UIElementBase element)
        {
            if (!ChildElements.Remove(element))
                return false;

            element.ParentElement = null;
            return true;
            
        }

        public override void Destroy()
        {
            ControllerCheck().MaskData.RemoveElement(this);
            base.Destroy();
        }

        public virtual void DeleteChildren()
        {
            UIElementBase[] copy = [..ChildElements];
            for (int i = 0; i < copy.Length; i++)
            {
                var child = copy[i];
                child.Delete();
            }
        }
    }
}