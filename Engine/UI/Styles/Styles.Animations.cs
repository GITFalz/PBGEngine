using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.Animation;

namespace PBG.UI2;

public static partial class Styles
{
    private static UIAnimation AnimationHover(UIElementBase e) => e.AnimationHover ??= new();
    private static UIAnimation AnimationClick(UIElementBase e) => e.AnimationClick ??= new();

    // --- Scale factories
    public static UnaryType<float> hover_scale_ = new((scale, e) => e.AnimationScale = scale);
    public static UnaryType<float> hover_scale_in_duration_ = new((scale, e) => AnimationHover(e).SetScaleDurationIn(scale));
    public static UnaryType<float> hover_scale_out_duration_ = new((scale, e) => AnimationHover(e).SetScaleDurationOut(scale));
    public static UnaryType<float> hover_scale_duration_ = new((scale, e) => AnimationHover(e).SetScaleDurationIn(scale).SetScaleDurationOut(scale));

    public static ValueStyle hover_scale_in_linear => new(e => AnimationHover(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_scale_in_easein => new(e => AnimationHover(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_scale_in_easeout => new(e => AnimationHover(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_scale_in_easeinout => new(e => AnimationHover(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_scale_out_linear => new(e => AnimationHover(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_scale_out_easein => new(e => AnimationHover(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_scale_out_easeout => new(e => AnimationHover(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_scale_out_easeinout => new(e => AnimationHover(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_scale_linear => new(e => AnimationHover(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_scale_easein => new(e => AnimationHover(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_scale_easeout => new(e => AnimationHover(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_scale_easeinout => new(e => AnimationHover(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static BinaryStyle<float, float> hover_scale_linear_ = new((scale, duration, e) => AnimationHover(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static BinaryStyle<float, float> hover_scale_easein_ = new((scale, duration, e) => AnimationHover(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static BinaryStyle<float, float> hover_scale_easeout_ = new((scale, duration, e) => AnimationHover(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static BinaryStyle<float, float> hover_scale_easeinout_ = new((scale, duration, e) => AnimationHover(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));


    // --- Rotation factories
    public static UnaryType<float> hover_rotation_ = new((rotation, e) => e.AnimationRotation = rotation);
    public static UnaryType<float> hover_rotation_in_duration_ = new((rotation, e) => AnimationHover(e).SetRotationDurationIn(rotation));
    public static UnaryType<float> hover_rotation_out_duration_ = new((rotation, e) => AnimationHover(e).SetRotationDurationOut(rotation));
    public static UnaryType<float> hover_rotation_duration_ = new((rotation, e) => AnimationHover(e).SetRotationDurationIn(rotation).SetRotationDurationOut(rotation));

    public static ValueStyle hover_rotation_in_linear => new(e => AnimationHover(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_rotation_in_easein => new(e => AnimationHover(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_rotation_in_easeout => new(e => AnimationHover(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_rotation_in_easeinout => new(e => AnimationHover(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_rotation_out_linear => new(e => AnimationHover(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_rotation_out_easein => new(e => AnimationHover(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_rotation_out_easeout => new(e => AnimationHover(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_rotation_out_easeinout => new(e => AnimationHover(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_rotation_linear => new(e => AnimationHover(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_rotation_easein => new(e => AnimationHover(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_rotation_easeout => new(e => AnimationHover(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_rotation_easeinout => new(e => AnimationHover(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static BinaryStyle<float, float> hover_rotation_linear_ = new((rotation, duration, e) => AnimationHover(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static BinaryStyle<float, float> hover_rotation_easein_ = new((rotation, duration, e) => AnimationHover(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static BinaryStyle<float, float> hover_rotation_easeout_ = new((rotation, duration, e) => AnimationHover(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static BinaryStyle<float, float> hover_rotation_easeinout_ = new((rotation, duration, e) => AnimationHover(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    // --- Translation factories
    public static UnaryType<Vector2> hover_translation_ = new((translation, e) => e.AnimationTranslation = translation);
    public static UnaryType<float> hover_translation_in_duration_ = new((translation, e) => AnimationHover(e).SetTranslationDurationIn(translation));
    public static UnaryType<float> hover_translation_out_duration_ = new((translation, e) => AnimationHover(e).SetTranslationDurationOut(translation));
    public static UnaryType<float> hover_translation_duration_ = new((translation, e) => AnimationHover(e).SetTranslationDurationIn(translation).SetTranslationDurationOut(translation));

    public static ValueStyle hover_translation_in_linear => new(e => AnimationHover(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_translation_in_easein => new(e => AnimationHover(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_translation_in_easeout => new(e => AnimationHover(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_translation_in_easeinout => new(e => AnimationHover(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_translation_out_linear => new(e => AnimationHover(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_translation_out_easein => new(e => AnimationHover(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_translation_out_easeout => new(e => AnimationHover(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_translation_out_easeinout => new(e => AnimationHover(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_translation_linear => new(e => AnimationHover(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_translation_easein => new(e => AnimationHover(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_translation_easeout => new(e => AnimationHover(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_translation_easeinout => new(e => AnimationHover(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static BinaryStyle<Vector2, float> hover_translation_linear_ = new((translation, duration, e) => AnimationHover(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static BinaryStyle<Vector2, float> hover_translation_easein_ = new((translation, duration, e) => AnimationHover(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static BinaryStyle<Vector2, float> hover_translation_easeout_ = new((translation, duration, e) => AnimationHover(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static BinaryStyle<Vector2, float> hover_translation_easeinout_ = new((translation, duration, e) => AnimationHover(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));


    // --- Color factories
    public static BinaryStyle<Vector4, Vector4> hover_color_ = new((base_color, end, e) => AnimationHover(e).SetBaseColor(base_color).SetEndColor(end));
    public static BinaryStyle<int, int> hover_color_g_ = new((base_color, end, e) => AnimationHover(e).SetBaseColor(new Vector4(new Vector3((float)base_color / 100f), 1f)).SetEndColor(new Vector4(new Vector3((float)end / 100f), 1f)));
    public static UnaryStyle<float> hover_color_in_duration_ = new((d, e) => AnimationHover(e).SetColorDurationIn(d));
    public static UnaryStyle<float> hover_color_out_duration_ = new((d, e) => AnimationHover(e).SetColorDurationOut(d));
    public static UnaryStyle<float> hover_color_duration_ = new((d, e) => AnimationHover(e).SetColorDuration(d));
    public static ValueStyle hover_color_ignore_when_selected => new(e => AnimationHover(e).IgnoreWhenSelected = true);
    
    public static ValueStyle hover_color_in_linear => new(e => AnimationHover(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_color_in_easein => new(e => AnimationHover(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_color_in_easeout => new(e => AnimationHover(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_color_in_easeinout => new(e => AnimationHover(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_color_out_linear => new(e => AnimationHover(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_color_out_easein => new(e => AnimationHover(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_color_out_easeout => new(e => AnimationHover(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_color_out_easeinout => new(e => AnimationHover(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle hover_color_linear => new(e => AnimationHover(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle hover_color_easein => new(e => AnimationHover(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle hover_color_easeout => new(e => AnimationHover(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle hover_color_easeinout => new(e => AnimationHover(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));


    
    
    // --- Scale factories
    public static UnaryType<float> click_scale_ = new((scale, e) => e.AnimationScale = scale);
    public static UnaryType<float> click_scale_in_duration_ = new((scale, e) => AnimationClick(e).SetScaleDurationIn(scale));
    public static UnaryType<float> click_scale_out_duration_ = new((scale, e) => AnimationClick(e).SetScaleDurationOut(scale));
    public static UnaryType<float> click_scale_duration_ = new((scale, e) => AnimationClick(e).SetScaleDurationIn(scale).SetScaleDurationOut(scale));

    public static ValueStyle click_scale_in_linear => new(e => AnimationClick(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_scale_in_easein => new(e => AnimationClick(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_scale_in_easeout => new(e => AnimationClick(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_scale_in_easeinout => new(e => AnimationClick(e).SetScaleEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_scale_out_linear => new(e => AnimationClick(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_scale_out_easein => new(e => AnimationClick(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_scale_out_easeout => new(e => AnimationClick(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_scale_out_easeinout => new(e => AnimationClick(e).SetScaleEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_scale_linear => new(e => AnimationClick(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_scale_easein => new(e => AnimationClick(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_scale_easeout => new(e => AnimationClick(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_scale_easeinout => new(e => AnimationClick(e).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static BinaryStyle<float, float> click_scale_linear_ = new((scale, duration, e) => AnimationClick(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static BinaryStyle<float, float> click_scale_easein_ = new((scale, duration, e) => AnimationClick(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static BinaryStyle<float, float> click_scale_easeout_ = new((scale, duration, e) => AnimationClick(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static BinaryStyle<float, float> click_scale_easeinout_ = new((scale, duration, e) => AnimationClick(e).SetScale(scale).SetScaleDuration(duration).SetScaleEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));


    // --- Rotation factories
    public static UnaryType<float> click_rotation_ = new((rotation, e) => e.AnimationRotation = rotation);
    public static UnaryType<float> click_rotation_in_duration_ = new((rotation, e) => AnimationClick(e).SetRotationDurationIn(rotation));
    public static UnaryType<float> click_rotation_out_duration_ = new((rotation, e) => AnimationClick(e).SetRotationDurationOut(rotation));
    public static UnaryType<float> click_rotation_duration_ = new((rotation, e) => AnimationClick(e).SetRotationDurationIn(rotation).SetRotationDurationOut(rotation));

    public static ValueStyle click_rotation_in_linear => new(e => AnimationClick(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_rotation_in_easein => new(e => AnimationClick(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_rotation_in_easeout => new(e => AnimationClick(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_rotation_in_easeinout => new(e => AnimationClick(e).SetRotationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_rotation_out_linear => new(e => AnimationClick(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_rotation_out_easein => new(e => AnimationClick(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_rotation_out_easeout => new(e => AnimationClick(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_rotation_out_easeinout => new(e => AnimationClick(e).SetRotationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_rotation_linear => new(e => AnimationClick(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_rotation_easein => new(e => AnimationClick(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_rotation_easeout => new(e => AnimationClick(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_rotation_easeinout => new(e => AnimationClick(e).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static BinaryStyle<float, float> click_rotation_linear_ = new((rotation, duration, e) => AnimationClick(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static BinaryStyle<float, float> click_rotation_easein_ = new((rotation, duration, e) => AnimationClick(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static BinaryStyle<float, float> click_rotation_easeout_ = new((rotation, duration, e) => AnimationClick(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static BinaryStyle<float, float> click_rotation_easeinout_ = new((rotation, duration, e) => AnimationClick(e).SetRotation(rotation).SetRotationDuration(duration).SetRotationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    // --- Translation factories
    public static UnaryType<Vector2> click_translation_ = new((translation, e) => e.AnimationTranslation = translation);
    public static UnaryType<float> click_translation_in_duration_ = new((translation, e) => AnimationClick(e).SetTranslationDurationIn(translation));
    public static UnaryType<float> click_translation_out_duration_ = new((translation, e) => AnimationClick(e).SetTranslationDurationOut(translation));
    public static UnaryType<float> click_translation_duration_ = new((translation, e) => AnimationClick(e).SetTranslationDurationIn(translation).SetTranslationDurationOut(translation));

    public static ValueStyle click_translation_in_linear => new(e => AnimationClick(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_translation_in_easein => new(e => AnimationClick(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_translation_in_easeout => new(e => AnimationClick(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_translation_in_easeinout => new(e => AnimationClick(e).SetTranslationEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_translation_out_linear => new(e => AnimationClick(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_translation_out_easein => new(e => AnimationClick(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_translation_out_easeout => new(e => AnimationClick(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_translation_out_easeinout => new(e => AnimationClick(e).SetTranslationEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_translation_linear => new(e => AnimationClick(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_translation_easein => new(e => AnimationClick(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_translation_easeout => new(e => AnimationClick(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_translation_easeinout => new(e => AnimationClick(e).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static BinaryStyle<Vector2, float> click_translation_linear_ = new((translation, duration, e) => AnimationClick(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static BinaryStyle<Vector2, float> click_translation_easein_ = new((translation, duration, e) => AnimationClick(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static BinaryStyle<Vector2, float> click_translation_easeout_ = new((translation, duration, e) => AnimationClick(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static BinaryStyle<Vector2, float> click_translation_easeinout_ = new((translation, duration, e) => AnimationClick(e).SetTranslation(translation).SetTranslationDuration(duration).SetTranslationEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));


    // --- Color factories
    public static BinaryStyle<Vector4, Vector4> click_color_ = new((base_color, end, e) => AnimationClick(e).SetBaseColor(base_color).SetEndColor(end));
    public static BinaryStyle<int, int> click_color_g_ = new((base_color, end, e) => AnimationClick(e).SetBaseColor(new Vector4(new Vector3((float)base_color / 100f), 1f)).SetEndColor(new Vector4(new Vector3((float)end / 100f), 1f)));
    public static UnaryStyle<float> click_color_in_duration_ = new((d, e) => AnimationClick(e).SetColorDurationIn(d));
    public static UnaryStyle<float> click_color_out_duration_ = new((d, e) => AnimationClick(e).SetColorDurationOut(d));
    public static UnaryStyle<float> click_color_duration_ = new((d, e) => AnimationClick(e).SetColorDuration(d));
    public static ValueStyle click_color_ignore_when_selected => new(e => AnimationClick(e).IgnoreWhenSelected = true);
    
    public static ValueStyle click_color_in_linear => new(e => AnimationClick(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_color_in_easein => new(e => AnimationClick(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_color_in_easeout => new(e => AnimationClick(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_color_in_easeinout => new(e => AnimationClick(e).SetColorEaseIn(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_color_out_linear => new(e => AnimationClick(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_color_out_easein => new(e => AnimationClick(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_color_out_easeout => new(e => AnimationClick(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_color_out_easeinout => new(e => AnimationClick(e).SetColorEaseOut(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));

    public static ValueStyle click_color_linear => new(e => AnimationClick(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.Linear)));
    public static ValueStyle click_color_easein => new(e => AnimationClick(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseIn)));
    public static ValueStyle click_color_easeout => new(e => AnimationClick(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseOut)));
    public static ValueStyle click_color_easeinout => new(e => AnimationClick(e).SetColorEase(EaseEffect.GetEaseEffect(Rendering.EasingType.EaseInOut)));
}