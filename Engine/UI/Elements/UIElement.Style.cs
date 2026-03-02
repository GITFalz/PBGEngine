using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.UI
{
    public abstract partial class UIElementBase
    {
        private static readonly Dictionary<string, Action<UIElementBase, object>> _valueSetters = new Dictionary<string, Action<UIElementBase, object>>()
        {
            // Dataset
            {
                "Data", (element, obj) =>
                {
                    if (obj is Dictionary<string, object> list)
                    {
                        foreach (var (key, value) in list)
                        {
                            element.Dataset[key] = value;
                        }
                        list = [];
                    }
                }
            },

            // Basic Properties
            {
                "Depth", (element, obj) =>
                {
                    if (obj is float value)
                        element.Depth = value;
                }
            },
            {
                "Visible", (element, obj) =>
                {
                    if (obj is bool value)
                    {
                        element.Visible = value;
                        if (element is IUICollection col)
                            col.WasVisible = value;
                    }
                }
            },

            // Width Properties
            {
                "MinWidth", (element, obj) =>
                {
                    if (obj is UISize value)
                        element.MinWidth = value;
                }
            },
            {
                "Width", (element, obj) =>
                {
                    if (obj is UISize value)
                        element.Width = value;
                }
            },
            {
                "MaxWidth", (element, obj) =>
                {
                    if (obj is UISize value)
                        element.MaxWidth = value;
                }
            },

            // Height Properties
            {
                "MinHeight", (element, obj) =>
                {
                    if (obj is UISize value)
                        element.MinHeight = value;
                }
            },
            {
                "Height", (element, obj) =>
                {
                    if (obj is UISize value)
                        element.Height = value;
                }
            },
            {
                "MaxHeight", (element, obj) =>
                {
                    if (obj is UISize value)
                        element.MaxHeight = value;
                }
            },

            // Offset Properties
            {
                "BaseOffset", (element, obj) =>
                {
                    if (obj is Vector2 value)
                        element.BaseOffset = value;
                }
            },
            {
                "BaseOffsetX", (element, obj) =>
                {
                    if (obj is float value)
                        element.BaseOffset.X = value;
                }
            },
            {
                "BaseOffsetY", (element, obj) =>
                {
                    if (obj is float value)
                        element.BaseOffset.Y = value;
                }
            },

            // Alignment
            {
                "Alignement", (element, obj) =>
                {
                    if (obj is UIAlign value)
                        element.Alignement = value;
                }
            },

            // Panel-Specific Properties
            {
                "Color", (element, obj) =>
                {
                    if (obj is Vector4 value)
                    {
                        if (element is IUIPanel panel)
                            panel.Color = value;
                        else if (element is IUIText text)
                            text.Color = value;
                    }
                }
            },
            {
                "TextureID", (element, obj) =>
                {
                    if (obj is int value)
                    {
                        if (element is IUIPanel panel)
                            panel.TextureID = value;
                    }
                }
            },
            {
                "Slice", (element, obj) =>
                {
                    if (obj is Vector2 value)
                    {
                        if (element is IUIPanel panel)
                            panel.Slice = value;
                    }
                }
            },
            {
                "BorderUI", (element, obj) =>
                {
                    if (obj is Vector4 value && element is IUIPanel panel)
                        panel.BorderUI = value;
                }
            },
            {
                "BorderColor", (element, obj) =>
                {
                    if (obj is Vector4 value && element is IUIPanel panel)
                        panel.BorderColor = value;
                }
            },

            // Text-Specific Properties
            {
                "Text", (element, obj) =>
                {
                    if (obj is string value && element is IUIText text)
                        text.SetText(value);
                }
            },
            {
                "MaxCharCount", (element, obj) =>
                {
                    if (obj is int value && element is IUIText text)
                        text.MaxCharCount = value;
                }
            },
            {
                "FontSize", (element, obj) =>
                {
                    if (obj is float value && element is IUIText text)
                        text.FontSize = value;
                }
            },
            {
                "TextAlign", (element, obj) =>
                {
                    if (obj is TextAlign align)
                    {
                        if (element is UIText text)
                            text.TextAlign = align;
                        else if (element is UIField field)
                            field.TextAlign = align;
                    }
                }
            },

            // Collection-Specific Properties
            {
                "Spacing", (element, obj) =>
                {
                    if (obj is float value && element is IUICollection collection)
                        collection.SetSpacing(value);
                }
            },
            {
                "Border", (element, obj) =>
                {
                    if (obj is Vector4 value && element is IUICollection collection)
                        collection.SetBorder(value);
                }
            },
            {
                "BorderX", (element, obj) =>
                {
                    if (obj is float value && element is IUICollection collection)
                        collection.SetBorderX(value);
                }
            },
            {
                "BorderY", (element, obj) =>
                {
                    if (obj is float value && element is IUICollection collection)
                        collection.SetBorderY(value);
                }
            },
            {
                "BorderZ", (element, obj) =>
                {
                    if (obj is float value && element is IUICollection collection)
                        collection.SetBorderZ(value);
                }
            },
            {
                "BorderW", (element, obj) =>
                {
                    if (obj is float value && element is IUICollection collection)
                        collection.SetBorderW(value);
                }
            },
            {
                "IgnoreInvisibleElements", (element, obj) =>
                {
                    if (obj is bool value && element is IUICollection collection)
                        collection.SetIgnoreInvisibleElements(value);
                }
            },
            {
                "AllowScrollingToTop", (element, obj) =>
                {
                    if (obj is bool value && element is IUICollection collection)
                        collection.SetAllowScrollingToTop(value);
                }
            },
            {
                "ScrollingSpeed", (element, obj) =>
                {
                    if (obj is float value && element is IUICollection collection)
                        collection.SetScrollingSpeed(value);
                }
            },
            {
                "GrowFromChildren", (element, obj) =>
                {
                    if (obj is bool value && element is IUICollection collection)
                        collection.SetGrowFromChildren(value);
                }
            },
            {
                "ForceToggleVisible", (element, obj) =>
                {
                    if (obj is bool value && element is IUICollection collection)
                        collection.SetForceToggleVisible(value);
                }
            },
            {
                "MaskChildren", (element, obj) =>
                {
                    if (obj is bool value && element is IUICollection collection)
                        collection.SetMaskChildren(value);
                }
            },

            // --- Animation Properties: Scale (existing) + Rotation + Translation ----------------
            {
                "AnimationHoverScale", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetScale(value);
                }
            },
            {
                "AnimationHoverScaleDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetScaleDurationIn(value);
                }
            },
            {
                "AnimationHoverScaleDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetScaleDurationOut(value);
                }
            },
            {
                "AnimationHoverScaleEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetScaleEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationHoverScaleEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetScaleEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },

            // ---------------- Rotation ----------------
            {
                "AnimationHoverRotation", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetRotation(value);
                }
            },
            {
                "AnimationHoverRotationDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetRotationDurationIn(value);
                }
            },
            {
                "AnimationHoverRotationDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetRotationDurationOut(value);
                }
            },
            {
                "AnimationHoverRotationEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetRotationEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationHoverRotationEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetRotationEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },

            // ---------------- Translation ----------------
            {
                "AnimationHoverTranslation", (element, obj) =>
                {
                    if (obj is Vector2 v2) (element.AnimationHover ??= new()).SetTranslation(v2);
                }
            },
            {
                "AnimationHoverTranslationDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetTranslationDurationIn(value);
                }
            },
            {
                "AnimationHoverTranslationDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetTranslationDurationOut(value);
                }
            },
            {
                "AnimationHoverTranslationEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetTranslationEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationHoverTranslationEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetTranslationEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },

            {
                "AnimationHoverBaseColor", (element, obj) =>
                {
                    if (obj is Vector4 value) (element.AnimationHover ??= new()).SetBaseColor(value);
                }
            },
            {
                "AnimationHoverEndColor", (element, obj) =>
                {
                    if (obj is Vector4 value) (element.AnimationHover ??= new()).SetEndColor(value);
                }
            },
            {
                "AnimationHoverColorDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetColorDurationIn(value);
                }
            },
            {
                "AnimationHoverColorDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationHover ??= new()).SetColorDurationOut(value);
                }
            },
            {
                "AnimationHoverColorEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetColorEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationHoverColorEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationHover ??= new()).SetColorEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationHoverColorIgnoreWhenSelected", (element, obj) =>
                {
                    (element.AnimationHover ??= new()).IgnoreWhenSelected = true;
                }
            },
            // ------------------------------------------------------------------------------------
            

            // --- Animation Click Properties: Scale + Rotation + Translation ----------------
            {
                "AnimationClickScale", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetScale(value);
                }
            },
            {
                "AnimationClickScaleDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetScaleDurationIn(value);
                }
            },
            {
                "AnimationClickScaleDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetScaleDurationOut(value);
                }
            },
            {
                "AnimationClickScaleEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationClick ??= new()).SetScaleEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationClickScaleEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationClick ??= new()).SetScaleEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },

            // ---------------- Rotation ----------------
            {
                "AnimationClickRotation", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetRotation(value);
                }
            },
            {
                "AnimationClickRotationDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetRotationDurationIn(value);
                }
            },
            {
                "AnimationClickRotationDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetRotationDurationOut(value);
                }
            },
            {
                "AnimationClickRotationEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationClick ??= new()).SetRotationEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationClickRotationEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationClick ??= new()).SetRotationEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },

            // ---------------- Translation ----------------
            {
                "AnimationClickTranslation", (element, obj) =>
                {
                    if (obj is Vector2 v2) (element.AnimationClick ??= new()).SetTranslation(v2);
                }
            },
            {
                "AnimationClickTranslationDurationIn", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetTranslationDurationIn(value);
                }
            },
            {
                "AnimationClickTranslationDurationOut", (element, obj) =>
                {
                    if (obj is float value) (element.AnimationClick ??= new()).SetTranslationDurationOut(value);
                }
            },
            {
                "AnimationClickTranslationEaseInEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationClick ??= new()).SetTranslationEaseIn(EaseEffect.GetEaseEffect(rtype));
                }
            },
            {
                "AnimationClickTranslationEaseOutEffect", (element, obj) =>
                {
                    if (obj is Rendering.EasingType rtype) (element.AnimationClick ??= new()).SetTranslationEaseOut(EaseEffect.GetEaseEffect(rtype));
                }
            },
            // ------------------------------------------------------------------------------------
            {
                "AllowPassingMouse", (element, obj) =>
                {
                    if (obj is bool state) element.AllowPassingMouse = state;
                }
            }
        };
    }
}