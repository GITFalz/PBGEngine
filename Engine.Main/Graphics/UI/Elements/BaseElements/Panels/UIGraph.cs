using PBG.UI.Creator;


namespace PBG.UI
{
    public class UIGraph : UIPanel
    {
        public int PointCount = 10;
        public float[] Points = [];

        public UIGraph() : base() 
        { 
            Name = "UIGraph"; 
            Tag = UIElementTag.UIGraph;
        }

        public UIGraph(params IStyleData[] styles) : this()
        { 
            Class(styles);
        }
        
        public UIGraph Ref(ref UIGraph text)
        {
            text = this;
            return text;
        }

        public UIGraph Out(out UIGraph text)
        {
            text = this;
            return text;
        }

        public UIGraph Class(params IStyleData[] styles)
        {
            Styles.bg_white.Set(this);
            InternalClass(this, styles);
            Points = new float[PointCount];
            return this;
        } 

        public UIGraph OnHoverEnter(Action<UIGraph>? action)    { UIEventExtensions.OnHoverEnter(this, action); return this; }
        public UIGraph OnHover(Action<UIGraph>? action)         { UIEventExtensions.OnHover(this, action); return this; }
        public UIGraph OnClick(Action<UIGraph>? action)         { UIEventExtensions.OnClick(this, action); return this; }
        public UIGraph OnHold(Action<UIGraph>? action)          { UIEventExtensions.OnHold(this, action); return this; }
        public UIGraph OnRelease(Action<UIGraph>? action)       { UIEventExtensions.OnRelease(this, action); return this; }
        public UIGraph OnHoverExit(Action<UIGraph>? action)     { UIEventExtensions.OnHoverExit(this, action); return this; }

        public void AdvancePoint(float newPoint)
        {
            for (int i = 1; i < Points.Length; i++)
            {
                Points[i-1] = Points[i];
            }
            Points[^1] = newPoint;
            UIController?.UIMesh.UpdateGraph(this);
        }
    }
}