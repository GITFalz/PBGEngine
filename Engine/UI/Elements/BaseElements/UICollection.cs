using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;
using PBG.UI.Creator;

using static PBG.UI.Styles;

namespace PBG.UI
{
    public interface IUICollection
    {
        int MaskIndex { get; set; }
        bool GrowFromChildren { get; set; }
        bool WasVisible { get; set; }
        void SetSpacing(float spacing);
        void SetBorder(Vector4 border);
        void SetBorderX(float x);
        void SetBorderY(float y);
        void SetBorderZ(float z);
        void SetBorderW(float w);
        void SetIgnoreInvisibleElements(bool ignore);
        void SetAllowScrollingToTop(bool allow);
        void SetScrollingSpeed(float speed);
        void SetGrowFromChildren(bool grow);
        void SetForceToggleVisible(bool force);
        void SetMaskChildren(bool mask);
        bool ContainsHoveringScrollView();

        void UpdateMaskIndices();
        void ForeachChildren(Action<UIElementBase> action);
    }
    public class UICol : UICol<UICol>
    {
        private UICol(string name, Class classes, UIElementBase[] subs, params Event<UICol>[] events) : base(classes.Styles, events)
        {
            Name = name;
            Tag = UIElementTag.UICollection;
            if (subs != null && subs.Length > 0)
                AddElements(subs);
        }


        // ----- ORIGINAL 11 PUBLIC CONSTRUCTORS (unchanged signatures) -----

        public UICol(params UIStyleData[] classes) : this("UICollection", new Class(classes), [], []) { }
        public UICol(string name, params UIStyleData[] classes) : this(name, new Class(classes), [], []) { }
        
        public UICol(Class classes) : this("UICollection", classes, [], [] ) { }
        public UICol(Class classes, Event<UICol> e1) : this("UICollection", classes, [], e1) { }
        public UICol(Class classes, Event<UICol> e1, Event<UICol> e2) : this("UICollection", classes, [], e1, e2) { }
        public UICol(Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3) : this("UICollection", classes, [], e1, e2, e3) { }
        public UICol(Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3, Event<UICol> e4) : this("UICollection", classes, [], e1, e2, e3, e4) { }
        
        public UICol(string name, Class classes) : this(name, classes, [], [] ) { }
        public UICol(string name, Class classes, Event<UICol> e1) : this(name, classes, [], e1) { }
        public UICol(string name, Class classes, Event<UICol> e1, Event<UICol> e2) : this(name, classes, [], e1, e2) { }
        public UICol(string name, Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3) : this(name, classes, [], e1, e2, e3) { }
        public UICol(string name, Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3, Event<UICol> e4) : this(name, classes, [], e1, e2, e3, e4) { }
        
        public UICol(Class classes, UIElementBase[] subs) : this("UICollection", classes, subs, []) { }
        public UICol(Class classes, Event<UICol> e1, UIElementBase[] subs) : this("UICollection", classes, subs, e1) { }
        public UICol(Class classes, Event<UICol> e1, Event<UICol> e2, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2) { }
        public UICol(Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3) { }
        public UICol(Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3, Event<UICol> e4, UIElementBase[] subs) : this("UICollection", classes, subs, e1, e2, e3, e4) { }

        public UICol(string name, Class classes, UIElementBase[] subs) : this(name, classes, subs, []) { }
        public UICol(string name, Class classes, Event<UICol> e1, UIElementBase[] subs) : this(name, classes, subs, e1) { }
        public UICol(string name, Class classes, Event<UICol> e1, Event<UICol> e2, UIElementBase[] subs) : this(name, classes, subs, e1, e2) { }
        public UICol(string name, Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3) { }
        public UICol(string name, Class classes, Event<UICol> e1, Event<UICol> e2, Event<UICol> e3, Event<UICol> e4, UIElementBase[] subs) : this(name, classes, subs, e1, e2, e3, e4) { }
    }

    public class UICol<TSelf> : UIPanel<TSelf>, IUICollection where TSelf : UICol<TSelf>
    {
        public float Spacing = 0;
        public Vector4 Border = (0, 0, 0, 0);
        public float Xborder { get => Border.X; set => Border.X = value; }
        public float Yborder { get => Border.Y; set => Border.Y = value; }
        public float Zborder { get => Border.Z; set => Border.Z = value; }
        public float Wborder { get => Border.W; set => Border.W = value; }

        public bool IgnoreInvisibleElements = false;
        public bool AllowScrollingToTop = false; // Used for scroll collections
        public float ScrollingSpeed = 5f;  // Used for scroll collections
        public bool GrowFromChildren { get; set; } = false;
        public bool MaskChildren = false;
        public bool ForceToggleVisible = true;
        public bool WasVisible { get; set; } = true;

        public List<UIElementBase> ChildElements = [];

        public UICol(UIStyleData[] classes, IEvent[] events) : base(classes, events) { Tag = UIElementTag.UICollection; }

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

        public override void FirstPass()
        {
            CollectionFirstPass();
            base.FirstPass();
        }

        public virtual void CollectionFirstPass()
        {
            float offsetX(UIElementBase child) => child.IsLeftAligned() ? Border.X : (child.IsRightAligned() ? Border.Z : 0);
            float offsetY(UIElementBase child) => child.IsTopAligned() ? Border.Y : (child.IsBottomAligned() ? Border.W : 0);

            float maxWidth = 0;
            float maxHeight = 0;

            HashSet<UIElementBase> percentWidthChildren = [];
            HashSet<UIElementBase> percentHeightChildren = [];

            if (!GrowFromChildren || !Width.IsNone())
            {
                CalculateWidth();
            }

            if (!GrowFromChildren || !Height.IsNone())
            {
                CalculateHeight();
            }

            ForeachChildren(child =>
            {
                child.FirstPass();
                if (!child.Visible && IgnoreInvisibleElements)
                    return;

                float xOffset = offsetX(child);
                float yOffset = offsetY(child);

                child.CollectionOffset = (xOffset, yOffset);

                if (GrowFromChildren)
                {
                    if (child.Width.IsPercent())
                    {
                        percentWidthChildren.Add(child);
                    }
                    else
                    {
                        maxWidth = Mathf.Max(maxWidth, Border.X + child.BaseOffset.X + child.Size.X + Border.Z);
                    }

                    if (child.Height.IsPercent())
                    {
                        percentHeightChildren.Add(child);
                    }
                    else
                    {
                        maxHeight = Mathf.Max(maxHeight, Border.Y + child.BaseOffset.Y + child.Size.Y + Border.W);
                    }
                }
            });
            if (GrowFromChildren)
            {
                if (!Width.IsPercent())
                    Width = UISize.Pixels(maxWidth);
                if (!Height.IsPercent())
                    Height = UISize.Pixels(maxHeight);
                CalculateWidth();
                CalculateHeight();
                ForeachChildren(percentWidthChildren, child =>
                {
                    child.Width.AddedOffset = -(Border.X + Border.Z);
                    child.CalculateWidth();
                });
                ForeachChildren(percentHeightChildren, child =>
                {
                    child.Height.AddedOffset = -(Border.Y + Border.W);
                    child.CalculateHeight();
                    child.CalculateHeight();
                });
            }
        }

        public override void SecondPass()
        {
            CalculateHeight();
            CalculateWidth();
            base.SecondPass();
            if (MaskChildren)
            {
                (Vector2 topLeft, Vector2 bottomRight) = GetMaskCorners();
                ControllerCheck().MaskData.AddElement(this, topLeft, bottomRight);
            }
            ForeachChildren(child =>
            {
                child.MaskIndex = MaskChildren ? MaskIndex : (child.ParentElement?.MaskIndex ?? -1);
                child.SecondPass();
            });
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
            ForeachChildren(child =>
            {
                child.UpdateChildMaskIndex(index);
            });
        }

        public override bool GetMaskPanel([NotNullWhen(true)] out Rendering.Mask.UIMaskStruct? mask) => ControllerCheck().MaskData.GetMask(MaskIndex, out mask);
        public override UIElementBase UpdateTransform()
        {
            (Vector2 topLeft, Vector2 bottomRight) = GetMaskCorners();
            UIController?.MaskData.UpdateTransform(this, topLeft, bottomRight);
            base.UpdateTransform();
            ForeachChildren(child => child.UpdateTransform());
            return this;
        }
        public override UIElementBase UpdateScale()
        {
            (Vector2 topLeft, Vector2 bottomRight) = GetMaskCorners();
            UIController?.MaskData.UpdateScale(this, topLeft, bottomRight);
            base.UpdateScale();
            ForeachChildren(child => child.UpdateScale());
            return this;
        }

        public override UIElementBase UpdateAnimationTranslation()
        {
            base.UpdateAnimationTranslation();
            ForeachChildren(child =>
            {
                child.AnimationTranslation = AnimationTranslation;
                child.UpdateAnimationTranslation();
            });
            return this;
        }

        public override UIElementBase UpdateAnimationScale()
        {
            base.UpdateAnimationScale();
            ForeachChildren(child =>
            {
                child.AnimationScale = AnimationScale;
                child.UpdateAnimationScale();
            });
            return this;
        }

        public override UIElementBase UpdateAnimationRotation()
        {
            base.UpdateAnimationRotation();
            ForeachChildren(child =>
            {
                child.AnimationRotation = AnimationRotation;
                child.UpdateAnimationRotation();
            });
            return this;
        }

        public override UIElementBase SetVisible(bool visible)
        {
            SetVisibleBefore = true;
            if (!visible)
            {
                WasVisible = Visible;
            }

            //if (Name == "test") Console.WriteLine(visible + " " + !ForceToggleVisible + " " + !WasVisible + " " + (ParentElement?.SetVisibleBefore ?? false));

            if (visible && !ForceToggleVisible && !WasVisible && (ParentElement?.SetVisibleBefore ?? false))
                return this;
            
            base.SetVisible(visible);
            ForeachChildren(child => child.SetVisible(visible));
            SetVisibleBefore = false;
            return this;
        }

        public bool ContainsHoveringScrollView()
        {
            for (int i = 0; i < ChildElements.Count; i++)
            {
                var child = ChildElements[i];
                if (child is IUICollection col)
                {
                    if (col is UIHScroll && child.Hovering)
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
            ForeachChildren(child =>
            {
                if (child is T typed)
                {
                    element = typed;
                    return false;
                }
                return true;
            });
            return element;
        }

        public override T? GetElementAt<T>(int number) where T : class
        {
            int count = 0;
            T? element = null;
            ForeachChildren(child =>
            {
                if (child is T typed)
                {
                    if (count == number || number <= 0)
                    {
                        element = typed;
                        return false;
                    }
                    count++;
                }
                return true;
            });
            return element;
        }

        public override UIElementBase? GetElement(UIElementTag tag)
        {
            UIElementBase? element = null;
            ForeachChildren(child =>
            {
                if (child.Tag == tag)
                {
                    element = child;
                    return false;
                }
                return true;
            });
            return element;
        }

        public override UIElementBase? GetElementAt(UIElementTag tag, int number)
        {
            int count = 0;
            UIElementBase? element = null;
            ForeachChildren(child =>
            {
                if (child.Tag == tag)
                {
                    if (count == number || number <= 0)
                    {
                        element = child;
                        return false;
                    }
                    count++;
                }
                return true;
            });
            return element;
        }

        public override UIElementBase? GetElement(string name)
        {
            UIElementBase? element = null;
            ForeachChildren(child =>
            {
                if (child.Name == name)
                {
                    element = child;
                    return false;
                }
                return true;
            });
            return element;
        }

        public override T? GetElement<T>(string name) where T : class
        {
            T? element = null;
            ForeachChildren(child =>
            {
                if (child.Name == name && child is T t)
                {
                    element = t;
                    return false;
                }
                return true;
            });
            return element;
        }

        public override UIElementBase? GetElementAt(string name, int number)
        {
            int count = 1;
            UIElementBase? element = null;
            ForeachChildren(child =>
            {
                if (child.Name == name)
                {
                    if (count == number || number <= 0)
                    {
                        element = child;
                        return false;
                    }
                    count++;
                }
                return true;
            });
            return element;
        }

        public override T? QueryElement<T>() where T : class
        {
            if (this is T typed)
                return typed;

            T? element = null;
            ForeachChildren(child =>
            {
                var e = child.QueryElement<T>();
                if (e != null)
                {
                    element = e;
                    return false;
                }
                return true;
            });
            return element;
        }

        public override List<T> QueryElements<T>() where T : class
        {
            List<T> elements = [];
            if (this is T typed)
                elements.Add(typed);
                
            ForeachChildren(child => elements.AddRange(child.QueryElements<T>()));
            return elements;
        }

        public override UIElementBase? QueryElement(string name)
        {
            UIElementBase? element = null;
            ForeachChildren(child =>
            {
                if (child.Name == name)
                {
                    element = child;
                    return false;
                }
                var e = child.QueryElement(name);
                if (e != null)
                {
                    element = e;
                    return false;
                }
                return true;
            });
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

        public UIElementBase AddElements(IEnumerable<UIElementBase> elements)
        {
            foreach (var element in elements)
            {
                AddElement(element);
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
            ForeachChildren(child => child.Delete());
        }

        public void ForeachChildren(Action<UIElementBase> action)
        {
            List<UIElementBase> copy = [.. ChildElements];
            for (int i = 0; i < copy.Count; i++)
            {
                action(copy[i]);
            }
        }

        public void ForeachChildren(Func<UIElementBase, bool> action)
        {
            List<UIElementBase> copy = [.. ChildElements];
            for (int i = 0; i < copy.Count; i++)
            {
                if (!action(copy[i]))
                    return;
            }
        }

        public void ForeachChildren(Action<UIElementBase, int> action)
        {
            List<UIElementBase> copy = [.. ChildElements];
            for (int i = 0; i < copy.Count; i++)
            {
                action(copy[i], i);
            }
        }

        public static void ForeachChildren(List<UIElementBase> children, Action<UIElementBase> action)
        {
            List<UIElementBase> copy = [.. children];
            for (int i = 0; i < copy.Count; i++)
            {
                action(copy[i]);
            }
        }
        
        public static void ForeachChildren(HashSet<UIElementBase> children, Action<UIElementBase> action)
        {
            List<UIElementBase> copy = [..children];
            for (int i = 0; i < copy.Count; i++)
            {
                action(copy[i]);
            }
        }
    }
}