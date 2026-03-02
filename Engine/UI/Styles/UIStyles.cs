using PBG.MathLibrary;

namespace PBG.UI
{
    public struct UIStyleData
    {
        public string Name { get; set; }
        public Dictionary<string, object> Styles;

        public UIStyleData(string name)
        {
            Name = name;
            Styles = [];
        }

        public UIStyleData minWidth(UISize size) { Styles["MinWidth"] = size; return this; }
        public UIStyleData width(UISize size) { Styles["Width"] = size; return this; }
        public UIStyleData maxWidth(UISize size) { Styles["MaxWidth"] = size; return this; }

        public UIStyleData minHeight(UISize size) { Styles["MinHeight"] = size; return this; }
        public UIStyleData height(UISize size) { Styles["Height"] = size; return this; }
        public UIStyleData maxHeight(UISize size) { Styles["MaxHeight"] = size; return this; }

        public UIStyleData offset(Vector2 offset) { Styles["BaseOffset"] = offset; return this; }
        public UIStyleData offsetX(float offsetX) { Styles["BaseOffsetX"] = offsetX; return this; }
        public UIStyleData offsetY(float offsetX) { Styles["BaseOffsetY"] = offsetX; return this; }

        public UIStyleData leftOffset(float offset) { Styles["BaseOffsetX"] = offset; return this; }
        public UIStyleData rightOffset(float offset) { Styles["BaseOffsetX"] = -offset; return this; }
        public UIStyleData topOffset(float offset) { Styles["BaseOffsetY"] = offset; return this; }
        public UIStyleData bottomOffset(float offset) { Styles["BaseOffsetY"] = -offset; return this; }

        public UIStyleData align(UIAlign align) { Styles["Alignement"] = align; return this; }
        public UIStyleData align(int a) => align((UIAlign)a);

        public UIStyleData visible(bool visible) { Styles["Visible"] = visible; return this; }
        public UIStyleData forceToggleVisible(bool force) { Styles["ForceToggleVisible"] = force; return this; }

        public UIStyleData color(Vector4 color) { Styles["Color"] = color; return this; }
        public UIStyleData backgroundColor(Vector4 color) { Styles["BackgroundColor"] = color; return this; }
        public UIStyleData texture(int id) { Styles["TextureID"] = id; return this; }
        public UIStyleData slice(Vector2 slice) { Styles["Slice"] = slice; return this; }

        // ===== Scale =====
        public UIStyleData animationHoverScale(float scale) { Styles["AnimationHoverScale"] = scale; return this; }
        public UIStyleData animationHoverScaleDurationIn(float value) { Styles["AnimationHoverScaleDurationIn"] = value; return this; }
        public UIStyleData animationHoverScaleDurationOut(float value) { Styles["AnimationHoverScaleDurationOut"] = value; return this; }
        public UIStyleData animationHoverScaleEaseInEffect(Rendering.EasingType type) { Styles["AnimationHoverScaleEaseInEffect"] = type; return this; }
        public UIStyleData animationHoverScaleEaseOutEffect(Rendering.EasingType type) { Styles["AnimationHoverScaleEaseOutEffect"] = type; return this; }

        // ===== Rotation =====
        public UIStyleData animationHoverRotation(float rotation) { Styles["AnimationHoverRotation"] = rotation; return this; }
        public UIStyleData animationHoverRotationDurationIn(float value) { Styles["AnimationHoverRotationDurationIn"] = value; return this; }
        public UIStyleData animationHoverRotationDurationOut(float value) { Styles["AnimationHoverRotationDurationOut"] = value; return this; }
        public UIStyleData animationHoverRotationEaseInEffect(Rendering.EasingType type) { Styles["AnimationHoverRotationEaseInEffect"] = type; return this; }
        public UIStyleData animationHoverRotationEaseOutEffect(Rendering.EasingType type) { Styles["AnimationHoverRotationEaseOutEffect"] = type; return this; }

        // ===== Translation =====
        public UIStyleData animationHoverTranslation(Vector2 translation) { Styles["AnimationHoverTranslation"] = translation; return this; }
        public UIStyleData animationHoverTranslationDurationIn(float value) { Styles["AnimationHoverTranslationDurationIn"] = value; return this; }
        public UIStyleData animationHoverTranslationDurationOut(float value) { Styles["AnimationHoverTranslationDurationOut"] = value; return this; }
        public UIStyleData animationHoverTranslationEaseInEffect(Rendering.EasingType type) { Styles["AnimationHoverTranslationEaseInEffect"] = type; return this; }
        public UIStyleData animationHoverTranslationEaseOutEffect(Rendering.EasingType type) { Styles["AnimationHoverTranslationEaseOutEffect"] = type; return this; }

        // ===== Color =====
        public UIStyleData animationHoverColor(Vector4 base_color, Vector4 end_color) { Styles["AnimationHoverBaseColor"] = base_color; Styles["AnimationHoverEndColor"] = end_color; return this; }
        public UIStyleData animationHoverColorDurationIn(float value) { Styles["AnimationHoverColorDurationIn"] = value; return this; }
        public UIStyleData animationHoverColorDurationOut(float value) { Styles["AnimationHoverColorDurationOut"] = value; return this; }
        public UIStyleData animationHoverColorEaseInEffect(Rendering.EasingType type) { Styles["AnimationHoverColorEaseInEffect"] = type; return this; }
        public UIStyleData animationHoverColorEaseOutEffect(Rendering.EasingType type) { Styles["AnimationHoverColorEaseOutEffect"] = type; return this; }
        public UIStyleData hoverColorIgnoreWhenSelected() { Styles["AnimationHoverColorIgnoreWhenSelected"] = true; return this; }

        // ===== Scale =====
        public UIStyleData animationClickScale(float scale) { Styles["AnimationClickScale"] = scale; return this; }
        public UIStyleData animationClickScaleDurationIn(float value) { Styles["AnimationClickScaleDurationIn"] = value; return this; }
        public UIStyleData animationClickScaleDurationOut(float value) { Styles["AnimationClickScaleDurationOut"] = value; return this; }
        public UIStyleData animationClickScaleEaseInEffect(Rendering.EasingType type) { Styles["AnimationClickScaleEaseInEffect"] = type; return this; }
        public UIStyleData animationClickScaleEaseOutEffect(Rendering.EasingType type) { Styles["AnimationClickScaleEaseOutEffect"] = type; return this; }

        // ===== Rotation =====
        public UIStyleData animationClickRotation(float rotation) { Styles["AnimationClickRotation"] = rotation; return this; }
        public UIStyleData animationClickRotationDurationIn(float value) { Styles["AnimationClickRotationDurationIn"] = value; return this; }
        public UIStyleData animationClickRotationDurationOut(float value) { Styles["AnimationClickRotationDurationOut"] = value; return this; }
        public UIStyleData animationClickRotationEaseInEffect(Rendering.EasingType type) { Styles["AnimationClickRotationEaseInEffect"] = type; return this; }
        public UIStyleData animationClickRotationEaseOutEffect(Rendering.EasingType type) { Styles["AnimationClickRotationEaseOutEffect"] = type; return this; }

        // ===== Translation =====
        public UIStyleData animationClickTranslation(Vector2 translation) { Styles["AnimationClickTranslation"] = translation; return this; }
        public UIStyleData animationClickTranslationDurationIn(float value) { Styles["AnimationClickTranslationDurationIn"] = value; return this; }
        public UIStyleData animationClickTranslationDurationOut(float value) { Styles["AnimationClickTranslationDurationOut"] = value; return this; }
        public UIStyleData animationClickTranslationEaseInEffect(Rendering.EasingType type) { Styles["AnimationClickTranslationEaseInEffect"] = type; return this; }
        public UIStyleData animationClickTranslationEaseOutEffect(Rendering.EasingType type) { Styles["AnimationClickTranslationEaseOutEffect"] = type; return this; }
        

        public UIStyleData borderUI(float x, float y, float z, float w) { Styles["BorderUI"] = new Vector4(x, y, z, w); return this; }
        public UIStyleData borderColor(float r, float g, float b, float a) { Styles["BorderColor"] = new Vector4(r, g, b, a); return this; }
        public UIStyleData borderColor(Vector4 color) { Styles["BorderColor"] = color; return this; }

        public UIStyleData text(string text) { Styles["Text"] = text; return this; }
        public UIStyleData maxChars(int chars) { Styles["MaxCharCount"] = chars; return this; }
        public UIStyleData fontSize(float fontSize) { Styles["FontSize"] = fontSize; return this; }
        public UIStyleData textAlign(TextAlign align) { Styles["TextAlign"] = align;  return this; }

        public UIStyleData spacing(float spacing) { Styles["Spacing"] = spacing; return this; }
        public UIStyleData border(Vector4 border) { Styles["Border"] = border; return this; }

        public UIStyleData data(string key, object value)
        {
            if (!Styles.TryGetValue("Data", out var dataList))
                dataList = new Dictionary<string, object>();

            if (dataList is not Dictionary<string, object> list)
                return this;

            list.Add(key, value);
            Styles["Data"] = list;
            return this;
        }
        public UIStyleData depth(float depth) { Styles["Depth"] = depth; return this; }

        public UIStyleData leftBorder(float border) { Styles["BorderX"] = border; return this; }
        public UIStyleData topBorder(float border) { Styles["BorderY"] = border; return this; }
        public UIStyleData rightBorder(float border) { Styles["BorderZ"] = border; return this; }
        public UIStyleData bottomBorder(float border) { Styles["BorderW"] = border; return this; }

        public UIStyleData ignoreInvisibleElements(bool ignore) { Styles["IgnoreInvisibleElements"] = ignore; return this; }
        public UIStyleData allowScrollingToTop(bool allow) { Styles["AllowScrollingToTop"] = allow; return this; }
        public UIStyleData scrollingSpeed(float speed) { Styles["ScrollingSpeed"] = speed; return this; }
        public UIStyleData growFromChildren(bool grow) { Styles["GrowFromChildren"] = grow; return this; }
        public UIStyleData maskChildren(bool mask) { Styles["MaskChildren"] = mask; return this; }

        public UIStyleData allowPassingMouse(bool allow) { Styles["AllowPassingMouse"] = allow; return this; }

        public void Apply(UIElementBase element)
        {
            if (Styles == null)
                return;

            foreach (var (name, value) in Styles)
            {
                element.SetValue(name, value);
            }
        }

        public void Clear()
        {
            if (Styles == null)
                return;
                
            if (Styles.TryGetValue("Data", out var list) && list is Dictionary<string, object> dataList)
            {
                dataList.Clear();
                Styles.Remove("Data");
            }
            Styles = [];
        }

        public override string ToString()
        {
            if (Styles == null)
                return "";
                
            string line = "Style: ";
            foreach (var style in Styles)
            {
                line += $",{style.Key}: {style.Value} ";
            }
            return line;
        }
    }

    public struct UIStyleFactory<T>(string name, Func<T, UIStyleData, UIStyleData> styleFunc)
    {
        private Func<T, UIStyleData, UIStyleData> _styleFunc = styleFunc;
        public readonly UIStyleData this[T value] => _styleFunc(value, new UIStyleData(name));
    }

    public struct UIStyleFactory<T1, T2>(string name, Func<T1, T2, UIStyleData, UIStyleData> styleFunc)
    {
        private Func<T1, T2, UIStyleData, UIStyleData> _styleFunc = styleFunc;
        public readonly UIStyleData this[T1 value1, T2 value2] => _styleFunc(value1, value2, new UIStyleData(name));
    }

    public struct UIStyleFactory<T1, T2, T3>(string name, Func<T1, T2, T3, UIStyleData, UIStyleData> styleFunc)
    {
        private Func<T1, T2, T3, UIStyleData, UIStyleData> _styleFunc = styleFunc;
        public readonly UIStyleData this[T1 value1, T2 value2, T3 value3] => _styleFunc(value1, value2, value3, new UIStyleData(name));
    }

    public struct UIStyleFactory<T1, T2, T3, T4>(string name, Func<T1, T2, T3, T4, UIStyleData, UIStyleData> styleFunc)
    {
        private Func<T1, T2, T3, T4, UIStyleData, UIStyleData> _styleFunc = styleFunc;
        public readonly UIStyleData this[T1 value1, T2 value2, T3 value3, T4 value4] => _styleFunc(value1, value2, value3, value4, new UIStyleData(name));
    }

    public struct UIStyleDoubleFactory<T1, T2>(string name, Func<T1, UIStyleData, UIStyleData> styleFunc1, Func<T2, UIStyleData, UIStyleData> styleFunc2)
    {
        private Func<T1, UIStyleData, UIStyleData> _styleFunc1 = styleFunc1;
        private Func<T2, UIStyleData, UIStyleData> _styleFunc2 = styleFunc2;
        public readonly UIStyleData this[T1 value1] => _styleFunc1(value1, new UIStyleData(name));
        public readonly UIStyleData this[T2 value2] => _styleFunc2(value2, new UIStyleData(name));
    }

    public struct UIStyleTripleFactory<T1, T2, T3>(string name, Func<T1, UIStyleData, UIStyleData> styleFunc1, Func<T2, UIStyleData, UIStyleData> styleFunc2, Func<T3, UIStyleData, UIStyleData> styleFunc3)
    {
        private Func<T1, UIStyleData, UIStyleData> _styleFunc1 = styleFunc1;
        private Func<T2, UIStyleData, UIStyleData> _styleFunc2 = styleFunc2;
        private Func<T3, UIStyleData, UIStyleData> _styleFunc3 = styleFunc3;
        public readonly UIStyleData this[T1 value1] => _styleFunc1(value1, new UIStyleData(name));
        public readonly UIStyleData this[T2 value2] => _styleFunc2(value2, new UIStyleData(name));
        public readonly UIStyleData this[T3 value3] => _styleFunc3(value3, new UIStyleData(name));
    }
}