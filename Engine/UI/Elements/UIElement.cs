using System.Data;
using PBG.Data;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI.Creator;

namespace PBG.UI
{
    public abstract class UIElement<TSelf> : UIElementBase where TSelf : UIElement<TSelf>
    {
        private Action<TSelf>? _onHoverEnter { get; set; } = null;
        private Action<TSelf>? _onHover { get; set; } = null;
        private Action<TSelf>? _onClick { get; set; } = null;
        private Action<TSelf>? _onHold { get; set; } = null;
        private Action<TSelf>? _onRelease { get; set; } = null;
        private Action<TSelf>? _onHoverExit { get; set; } = null;

        public UIElement(Vector4 defaultColor) : base(defaultColor) {}

        public virtual void OnHoverEnterAction() => _onHoverEnter?.Invoke((TSelf)this);
        public virtual void OnHoverAction() => _onHover?.Invoke((TSelf)this);
        public virtual void OnClickAction() => _onClick?.Invoke((TSelf)this);
        public virtual void OnHoldAction() => _onHold?.Invoke((TSelf)this);
        public virtual void OnReleaseAction() => _onRelease?.Invoke((TSelf)this);
        public virtual void OnHoverExitAction() => _onHoverExit?.Invoke((TSelf)this);

        public override bool Test()
        {
            var mouseOver = IsMouseOver();
            TestButtons(mouseOver);
            return mouseOver;
        }

        public override bool IsMouseOver()
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

        public override void CalculateBoundaries(Vector2 offset)
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

        public virtual UIElementBase SetOnHoverEnter(Action<TSelf>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHoverEnter = action;
            return this;
        }

        public virtual UIElementBase SetOnHover(Action<TSelf>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHover = action;
            return this;
        }

        public virtual UIElementBase SetOnClick(Action<TSelf>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onClick = action;
            return this;
        }

        public virtual UIElementBase SetOnHold(Action<TSelf>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHold = action;
            return this;
        }

        public virtual UIElementBase SetOnRelease(Action<TSelf>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onRelease = action;
            return this;
        }

        public virtual UIElementBase SetOnHoverExit(Action<TSelf>? action)
        {
            UIController?.SetAsInteractable(this, action != null);
            _onHoverExit = action;
            return this;
        }

        public override bool IsInteractable() =>
            _onHoverEnter != null ||
            _onHover != null ||
            _onClick != null ||
            _onHold != null ||
            _onRelease != null ||
            _onHoverExit != null ||
            AnimationHover != null ||
            AnimationClick != null;

        public override void Delete()
        {
            Visible = false;
            OnHoverExitAction();
            OnReleaseAction();
            base.Delete();
        }
    }

    public enum UIElementTag
    {
        Any,
        UIText,
        UIInputfield,
        UIImage,
        UIButton,
        UICollection,
        UIHorizontalCollection,
        UIHorizontalScrollView,
        UIVerticalCollection,
        UIVerticalScrollView,
    }

    public enum UIChange
    {
        None = 0,
        Transform = 1 << 0,
        Scale = 1 << 1,
        Color = 1 << 2,
        Characters = 1 << 3,
        Border = 1 << 4,
        BorderColor = 1 << 5,
    }
}