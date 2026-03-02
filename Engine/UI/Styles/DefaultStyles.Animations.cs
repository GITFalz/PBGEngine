using PBG.MathLibrary;

namespace PBG.UI
{
    public static partial class Styles
    {
        // --- Scale factories
        public static UIStyleFactory<float> hover_scale_ = new UIStyleFactory<float>("hover-scale-", (scale, style) => style.animationHoverScale(scale));
        public static UIStyleFactory<float> hover_scale_in_duration_ = new UIStyleFactory<float>("hover-duration-in-", (scale, style) => style.animationHoverScaleDurationIn(scale));
        public static UIStyleFactory<float> hover_scale_out_duration_ = new UIStyleFactory<float>("hover-duration-out-", (scale, style) => style.animationHoverScaleDurationOut(scale));
        public static UIStyleFactory<float> hover_scale_duration_ = new UIStyleFactory<float>("hover-duration-", (scale, style) => style.animationHoverScaleDurationIn(scale).animationHoverScaleDurationOut(scale));

        public static UIStyleData hover_scale_in_linear => new UIStyleData("hover-scale-in-linear").animationHoverScaleEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_scale_in_easein => new UIStyleData("hover-scale-in-easein").animationHoverScaleEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_scale_in_easeout => new UIStyleData("hover-scale-in-easeout").animationHoverScaleEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_scale_in_easeinout => new UIStyleData("hover-scale-in-easeinout").animationHoverScaleEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_scale_out_linear => new UIStyleData("hover-scale-out-linear").animationHoverScaleEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_scale_out_easein => new UIStyleData("hover-scale-out-easein").animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_scale_out_easeout => new UIStyleData("hover-scale-out-easeout").animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_scale_out_easeinout => new UIStyleData("hover-scale-out-easeinout").animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_scale_linear => new UIStyleData("hover-scale-linear").animationHoverScaleEaseInEffect(Rendering.EasingType.Linear).animationHoverScaleEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_scale_easein => new UIStyleData("hover-scale-easein").animationHoverScaleEaseInEffect(Rendering.EasingType.EaseIn).animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_scale_easeout => new UIStyleData("hover-scale-easeout").animationHoverScaleEaseInEffect(Rendering.EasingType.EaseOut).animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_scale_easeinout => new UIStyleData("hover-scale-easeinout").animationHoverScaleEaseInEffect(Rendering.EasingType.EaseInOut).animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleFactory<float, float> hover_scale_linear_ = new UIStyleFactory<float, float>("hover-scale-linear-", (scale, duration, style) => style.animationHoverScale(scale).animationHoverScaleDurationIn(duration).animationHoverScaleDurationOut(duration).animationHoverScaleEaseInEffect(Rendering.EasingType.Linear).animationHoverScaleEaseOutEffect(Rendering.EasingType.Linear));
        public static UIStyleFactory<float, float> hover_scale_easein_ = new UIStyleFactory<float, float>("hover-scale-linear-", (scale, duration, style) => style.animationHoverScale(scale).animationHoverScaleDurationIn(duration).animationHoverScaleDurationOut(duration).animationHoverScaleEaseInEffect(Rendering.EasingType.EaseIn).animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseIn));
        public static UIStyleFactory<float, float> hover_scale_easeout_ = new UIStyleFactory<float, float>("hover-scale-linear-", (scale, duration, style) => style.animationHoverScale(scale).animationHoverScaleDurationIn(duration).animationHoverScaleDurationOut(duration).animationHoverScaleEaseInEffect(Rendering.EasingType.EaseOut).animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseOut));
        public static UIStyleFactory<float, float> hover_scale_easeinout_ = new UIStyleFactory<float, float>("hover-scale-linear-", (scale, duration, style) => style.animationHoverScale(scale).animationHoverScaleDurationIn(duration).animationHoverScaleDurationOut(duration).animationHoverScaleEaseInEffect(Rendering.EasingType.EaseInOut).animationHoverScaleEaseOutEffect(Rendering.EasingType.EaseInOut));


        // --- Rotation factories
        public static UIStyleFactory<float> hover_rotation_ = new UIStyleFactory<float>("hover-rotation-", (r, style) => style.animationHoverRotation(r));
        public static UIStyleFactory<float> hover_rotation_in_duration_ = new UIStyleFactory<float>("hover-rotation-duration-in-", (d, style) => style.animationHoverRotationDurationIn(d));
        public static UIStyleFactory<float> hover_rotation_out_duration_ = new UIStyleFactory<float>("hover-rotation-duration-out-", (d, style) => style.animationHoverRotationDurationOut(d));
        public static UIStyleFactory<float> hover_rotation_duration_ = new UIStyleFactory<float>("hover-rotation-duration-", (d, style) => style.animationHoverRotationDurationIn(d).animationHoverRotationDurationOut(d));
        
        public static UIStyleData hover_rotation_in_linear => new UIStyleData("hover-rotation-in-linear").animationHoverRotationEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_rotation_in_easein => new UIStyleData("hover-rotation-in-easein").animationHoverRotationEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_rotation_in_easeout => new UIStyleData("hover-rotation-in-easeout").animationHoverRotationEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_rotation_in_easeinout => new UIStyleData("hover-rotation-in-easeinout").animationHoverRotationEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_rotation_out_linear => new UIStyleData("hover-rotation-out-linear").animationHoverRotationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_rotation_out_easein => new UIStyleData("hover-rotation-out-easein").animationHoverRotationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_rotation_out_easeout => new UIStyleData("hover-rotation-out-easeout").animationHoverRotationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_rotation_out_easeinout => new UIStyleData("hover-rotation-out-easeinout").animationHoverRotationEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_rotation_linear => new UIStyleData("hover-rotation-linear").animationHoverRotationEaseInEffect(Rendering.EasingType.Linear).animationHoverRotationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_rotation_easein => new UIStyleData("hover-rotation-easein").animationHoverRotationEaseInEffect(Rendering.EasingType.EaseIn).animationHoverRotationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_rotation_easeout => new UIStyleData("hover-rotation-easeout").animationHoverRotationEaseInEffect(Rendering.EasingType.EaseOut).animationHoverRotationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_rotation_easeinout => new UIStyleData("hover-rotation-easeinout").animationHoverRotationEaseInEffect(Rendering.EasingType.EaseInOut).animationHoverRotationEaseOutEffect(Rendering.EasingType.EaseInOut);

        // --- Translation factories
        public static UIStyleFactory<Vector2> hover_translation_ = new UIStyleFactory<Vector2>("hover-translation-", (t, style) => style.animationHoverTranslation(t));
        public static UIStyleFactory<float> hover_translation_in_duration_ = new UIStyleFactory<float>("hover-translation-duration-in-", (d, style) => style.animationHoverTranslationDurationIn(d));
        public static UIStyleFactory<float> hover_translation_out_duration_ = new UIStyleFactory<float>("hover-translation-duration-out-", (d, style) => style.animationHoverTranslationDurationOut(d));
        public static UIStyleFactory<float> hover_translation_duration_ = new UIStyleFactory<float>("hover-translation-duration-", (d, style) => style.animationHoverTranslationDurationIn(d).animationHoverTranslationDurationOut(d));
        
        public static UIStyleData hover_translation_in_linear => new UIStyleData("hover-translation-in-linear").animationHoverTranslationEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_translation_in_easein => new UIStyleData("hover-translation-in-easein").animationHoverTranslationEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_translation_in_easeout => new UIStyleData("hover-translation-in-easeout").animationHoverTranslationEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_translation_in_easeinout => new UIStyleData("hover-translation-in-easeinout").animationHoverTranslationEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_translation_out_linear => new UIStyleData("hover-translation-out-linear").animationHoverTranslationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_translation_out_easein => new UIStyleData("hover-translation-out-easein").animationHoverTranslationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_translation_out_easeout => new UIStyleData("hover-translation-out-easeout").animationHoverTranslationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_translation_out_easeinout => new UIStyleData("hover-translation-out-easeinout").animationHoverTranslationEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_translation_linear => new UIStyleData("hover-translation-linear").animationHoverTranslationEaseInEffect(Rendering.EasingType.Linear).animationHoverTranslationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_translation_easein => new UIStyleData("hover-translation-easein").animationHoverTranslationEaseInEffect(Rendering.EasingType.EaseIn).animationHoverTranslationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_translation_easeout => new UIStyleData("hover-translation-easeout").animationHoverTranslationEaseInEffect(Rendering.EasingType.EaseOut).animationHoverTranslationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_translation_easeinout => new UIStyleData("hover-translation-easeinout").animationHoverTranslationEaseInEffect(Rendering.EasingType.EaseInOut).animationHoverTranslationEaseOutEffect(Rendering.EasingType.EaseInOut);


        // --- Color factories
        public static UIStyleFactory<Vector4, Vector4> hover_color_ = new UIStyleFactory<Vector4, Vector4>("hover-color-", (base_color, end, style) => style.animationHoverColor(base_color, end));
        public static UIStyleFactory<int, int> hover_color_g_ = new UIStyleFactory<int, int>("hover-color-g-", (base_color, end, style) => style.animationHoverColor(new Vector4(new Vector3((float)base_color / 100f), 1f), new Vector4(new Vector3((float)end / 100f), 1f)));
        public static UIStyleFactory<float> hover_color_in_duration_ = new UIStyleFactory<float>("hover-color-duration-in-", (d, style) => style.animationHoverColorDurationIn(d));
        public static UIStyleFactory<float> hover_color_out_duration_ = new UIStyleFactory<float>("hover-color-duration-out-", (d, style) => style.animationHoverColorDurationOut(d));
        public static UIStyleFactory<float> hover_color_duration_ = new UIStyleFactory<float>("hover-color-duration-", (d, style) => style.animationHoverColorDurationIn(d).animationHoverColorDurationOut(d));
        public static UIStyleData hover_color_ignore_when_selected => new UIStyleData("hover-color-ignore-when-selected").hoverColorIgnoreWhenSelected();
        
        public static UIStyleData hover_color_in_linear => new UIStyleData("hover-color-in-linear").animationHoverColorEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_color_in_easein => new UIStyleData("hover-color-in-easein").animationHoverColorEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_color_in_easeout => new UIStyleData("hover-color-in-easeout").animationHoverColorEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_color_in_easeinout => new UIStyleData("hover-color-in-easeinout").animationHoverColorEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_color_out_linear => new UIStyleData("hover-color-out-linear").animationHoverColorEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_color_out_easein => new UIStyleData("hover-color-out-easein").animationHoverColorEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_color_out_easeout => new UIStyleData("hover-color-out-easeout").animationHoverColorEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_color_out_easeinout => new UIStyleData("hover-color-out-easeinout").animationHoverColorEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData hover_color_linear => new UIStyleData("hover-color-linear").animationHoverColorEaseInEffect(Rendering.EasingType.Linear).animationHoverColorEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData hover_color_easein => new UIStyleData("hover-color-easein").animationHoverColorEaseInEffect(Rendering.EasingType.EaseIn).animationHoverColorEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData hover_color_easeout => new UIStyleData("hover-color-easeout").animationHoverColorEaseInEffect(Rendering.EasingType.EaseOut).animationHoverColorEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData hover_color_easeinout => new UIStyleData("hover-color-easeinout").animationHoverColorEaseInEffect(Rendering.EasingType.EaseInOut).animationHoverColorEaseOutEffect(Rendering.EasingType.EaseInOut);


        
        // --- Scale factories
        public static UIStyleFactory<float> click_scale_ = new UIStyleFactory<float>("click-scale-", (scale, style) => style.animationClickScale(scale));
        public static UIStyleFactory<float> click_scale_in_duration_ = new UIStyleFactory<float>("click-duration-in-", (scale, style) => style.animationClickScaleDurationIn(scale));
        public static UIStyleFactory<float> click_scale_out_duration_ = new UIStyleFactory<float>("click-duration-out-", (scale, style) => style.animationClickScaleDurationOut(scale));
        public static UIStyleFactory<float> click_scale_duration_ = new UIStyleFactory<float>("click-duration-", (scale, style) => style.animationClickScaleDurationIn(scale).animationClickScaleDurationOut(scale));

        public static UIStyleData click_scale_in_linear => new UIStyleData("click-scale-in-linear").animationClickScaleEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_scale_in_easein => new UIStyleData("click-scale-in-easein").animationClickScaleEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_scale_in_easeout => new UIStyleData("click-scale-in-easeout").animationClickScaleEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_scale_in_easeinout => new UIStyleData("click-scale-in-easeinout").animationClickScaleEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData click_scale_out_linear => new UIStyleData("click-scale-out-linear").animationClickScaleEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_scale_out_easein => new UIStyleData("click-scale-out-easein").animationClickScaleEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_scale_out_easeout => new UIStyleData("click-scale-out-easeout").animationClickScaleEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_scale_out_easeinout => new UIStyleData("click-scale-out-easeinout").animationClickScaleEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData click_scale_linear => new UIStyleData("click-scale-linear").animationClickScaleEaseInEffect(Rendering.EasingType.Linear).animationClickScaleEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_scale_easein => new UIStyleData("click-scale-easein").animationClickScaleEaseInEffect(Rendering.EasingType.EaseIn).animationClickScaleEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_scale_easeout => new UIStyleData("click-scale-easeout").animationClickScaleEaseInEffect(Rendering.EasingType.EaseOut).animationClickScaleEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_scale_easeinout => new UIStyleData("click-scale-easeinout").animationClickScaleEaseInEffect(Rendering.EasingType.EaseInOut).animationClickScaleEaseOutEffect(Rendering.EasingType.EaseInOut);

        // --- Rotation factories
        public static UIStyleFactory<float> click_rotation_ = new UIStyleFactory<float>("click-rotation-", (r, style) => style.animationClickRotation(r));
        public static UIStyleFactory<float> click_rotation_in_duration_ = new UIStyleFactory<float>("click-rotation-duration-in-", (d, style) => style.animationClickRotationDurationIn(d));
        public static UIStyleFactory<float> click_rotation_out_duration_ = new UIStyleFactory<float>("click-rotation-duration-out-", (d, style) => style.animationClickRotationDurationOut(d));
        public static UIStyleFactory<float> click_rotation_duration_ = new UIStyleFactory<float>("click-rotation-duration-", (d, style) => style.animationClickRotationDurationIn(d).animationClickRotationDurationOut(d));
        
        public static UIStyleData click_rotation_in_linear => new UIStyleData("click-rotation-in-linear").animationClickRotationEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_rotation_in_easein => new UIStyleData("click-rotation-in-easein").animationClickRotationEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_rotation_in_easeout => new UIStyleData("click-rotation-in-easeout").animationClickRotationEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_rotation_in_easeinout => new UIStyleData("click-rotation-in-easeinout").animationClickRotationEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData click_rotation_out_linear => new UIStyleData("click-rotation-out-linear").animationClickRotationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_rotation_out_easein => new UIStyleData("click-rotation-out-easein").animationClickRotationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_rotation_out_easeout => new UIStyleData("click-rotation-out-easeout").animationClickRotationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_rotation_out_easeinout => new UIStyleData("click-rotation-out-easeinout").animationClickRotationEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData click_rotation_linear => new UIStyleData("click-rotation-linear").animationClickRotationEaseInEffect(Rendering.EasingType.Linear).animationClickRotationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_rotation_easein => new UIStyleData("click-rotation-easein").animationClickRotationEaseInEffect(Rendering.EasingType.EaseIn).animationClickRotationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_rotation_easeout => new UIStyleData("click-rotation-easeout").animationClickRotationEaseInEffect(Rendering.EasingType.EaseOut).animationClickRotationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_rotation_easeinout => new UIStyleData("click-rotation-easeinout").animationClickRotationEaseInEffect(Rendering.EasingType.EaseInOut).animationClickRotationEaseOutEffect(Rendering.EasingType.EaseInOut);

        // --- Translation factories
        public static UIStyleFactory<Vector2> click_translation_ = new UIStyleFactory<Vector2>("click-translation-", (t, style) => style.animationClickTranslation(t));
        public static UIStyleFactory<float> click_translation_in_duration_ = new UIStyleFactory<float>("click-translation-duration-in-", (d, style) => style.animationClickTranslationDurationIn(d));
        public static UIStyleFactory<float> click_translation_out_duration_ = new UIStyleFactory<float>("click-translation-duration-out-", (d, style) => style.animationClickTranslationDurationOut(d));
        public static UIStyleFactory<float> click_translation_duration_ = new UIStyleFactory<float>("click-translation-duration-", (d, style) => style.animationClickTranslationDurationIn(d).animationClickTranslationDurationOut(d));
        
        public static UIStyleData click_translation_in_linear => new UIStyleData("click-translation-in-linear").animationClickTranslationEaseInEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_translation_in_easein => new UIStyleData("click-translation-in-easein").animationClickTranslationEaseInEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_translation_in_easeout => new UIStyleData("click-translation-in-easeout").animationClickTranslationEaseInEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_translation_in_easeinout => new UIStyleData("click-translation-in-easeinout").animationClickTranslationEaseInEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData click_translation_out_linear => new UIStyleData("click-translation-out-linear").animationClickTranslationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_translation_out_easein => new UIStyleData("click-translation-out-easein").animationClickTranslationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_translation_out_easeout => new UIStyleData("click-translation-out-easeout").animationClickTranslationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_translation_out_easeinout => new UIStyleData("click-translation-out-easeinout").animationClickTranslationEaseOutEffect(Rendering.EasingType.EaseInOut);

        public static UIStyleData click_translation_linear => new UIStyleData("click-translation-linear").animationClickTranslationEaseInEffect(Rendering.EasingType.Linear).animationClickTranslationEaseOutEffect(Rendering.EasingType.Linear);
        public static UIStyleData click_translation_easein => new UIStyleData("click-translation-easein").animationClickTranslationEaseInEffect(Rendering.EasingType.EaseIn).animationClickTranslationEaseOutEffect(Rendering.EasingType.EaseIn);
        public static UIStyleData click_translation_easeout => new UIStyleData("click-translation-easeout").animationClickTranslationEaseInEffect(Rendering.EasingType.EaseOut).animationClickTranslationEaseOutEffect(Rendering.EasingType.EaseOut);
        public static UIStyleData click_translation_easeinout => new UIStyleData("click-translation-easeinout").animationClickTranslationEaseInEffect(Rendering.EasingType.EaseInOut).animationClickTranslationEaseOutEffect(Rendering.EasingType.EaseInOut);

    }
}