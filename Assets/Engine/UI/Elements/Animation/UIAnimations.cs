using PBG.MathLibrary;
using PBG.Rendering;

namespace PBG.UI.Animation
{
    public class UIAnimation
    {
        public AnimationTranslation? AnimationTranslationData = null;
        public AnimationScale? AnimationScaleData = null;
        public AnimationRotation? AnimationRotationData = null;
        public AnimationColor? AnimationColorData = null;

        public bool IgnoreWhenSelected = false;

        // Translation
        public UIAnimation SetTranslation(Vector2 value) { (AnimationTranslationData ??= new()).Translation = value; return this; }

        public UIAnimation SetTranslationDurationIn(float value) { (AnimationTranslationData ??= new()).DurationIn = value; return this; }
        public UIAnimation SetTranslationDurationOut(float value) { (AnimationTranslationData ??= new()).DurationOut = value; return this; }
        public UIAnimation SetTranslationDuration(float value) { SetTranslationDurationIn(value); SetTranslationDurationOut(value);  return this; }

        public UIAnimation SetTranslationEaseIn(EaseEffect effect) { (AnimationTranslationData ??= new()).EaseInEffect = effect; return this; }
        public UIAnimation SetTranslationEaseOut(EaseEffect effect) { (AnimationTranslationData ??= new()).EaseOutEffect = effect; return this; }
        public UIAnimation SetTranslationEase(EaseEffect effect) { SetTranslationEaseIn(effect); SetTranslationEaseOut(effect); return this; }

        // Rotation
        public UIAnimation SetRotation(float value) { (AnimationRotationData ??= new()).Rotation = value; return this; }

        public UIAnimation SetRotationDurationIn(float value) { (AnimationRotationData ??= new()).DurationIn = value; return this; }
        public UIAnimation SetRotationDurationOut(float value) { (AnimationRotationData ??= new()).DurationOut = value; return this; }
        public UIAnimation SetRotationDuration(float value) { SetRotationDurationIn(value); SetRotationDurationOut(value); return this; }

        public UIAnimation SetRotationEaseIn(EaseEffect effect) { (AnimationRotationData ??= new()).EaseInEffect = effect; return this; }
        public UIAnimation SetRotationEaseOut(EaseEffect effect) { (AnimationRotationData ??= new()).EaseOutEffect = effect; return this; }
        public UIAnimation SetRotationEase(EaseEffect effect) { SetRotationEaseIn(effect); SetRotationEaseOut(effect); return this; }

        public UIAnimation LoopRotation() { (AnimationRotationData ??= new()).Loop = true; return this; }

        // Scale
        public UIAnimation SetScale(float value) { (AnimationScaleData ??= new()).Scale = value; return this; }

        public UIAnimation SetScaleDurationIn(float value) { (AnimationScaleData ??= new()).DurationIn = value; return this; }
        public UIAnimation SetScaleDurationOut(float value) { (AnimationScaleData ??= new()).DurationOut = value; return this; }
        public UIAnimation SetScaleDuration(float value) { SetScaleDurationIn(value); SetScaleDurationOut(value); return this; }

        public UIAnimation SetScaleEaseIn(EaseEffect effect) { (AnimationScaleData ??= new()).EaseInEffect = effect; return this; }
        public UIAnimation SetScaleEaseOut(EaseEffect effect) { (AnimationScaleData ??= new()).EaseOutEffect = effect; return this; }
        public UIAnimation SetScaleEase(EaseEffect effect) { SetScaleEaseIn(effect); SetScaleEaseOut(effect); return this; }

        // Color
        public UIAnimation SetBaseColor(Vector4 color) { (AnimationColorData ??= new()).BaseColor = color; return this; }
        public UIAnimation SetEndColor(Vector4 color) { (AnimationColorData ??= new()).EndColor = color; return this; }

        public UIAnimation SetColorDurationIn(float value) { (AnimationColorData ??= new()).DurationIn = value; return this; }
        public UIAnimation SetColorDurationOut(float value) { (AnimationColorData ??= new()).DurationOut = value; return this; }
        public UIAnimation SetColorDuration(float value) { SetColorDurationIn(value); SetColorDurationOut(value); return this; }

        public UIAnimation SetColorEaseIn(EaseEffect effect) { (AnimationColorData ??= new()).EaseInEffect = effect; return this; }
        public UIAnimation SetColorEaseOut(EaseEffect effect) { (AnimationColorData ??= new()).EaseOutEffect = effect; return this; }
        public UIAnimation SetColorEase(EaseEffect effect) { SetColorEaseIn(effect); SetColorEaseOut(effect); return this; }
        
        public void Enter(UIController controller, UIElementBase element, ref Action deleteAction)
        {
            if (IgnoreWhenSelected && element.IsSelected)
                return;
                
            deleteAction.Invoke();
            deleteAction = () => { };
            In(AnimationTranslationData, controller, element, ref deleteAction);
            In(AnimationScaleData, controller, element, ref deleteAction);
            In(AnimationRotationData, controller, element, ref deleteAction);
            In(AnimationColorData, controller, element, ref deleteAction);
        }

        public void Exit(UIController controller, UIElementBase element, ref Action deleteAction)
        {
            if (IgnoreWhenSelected && element.IsSelected)
                return;
                
            deleteAction.Invoke();
            deleteAction = () => { };
            Out(AnimationTranslationData, controller, element, ref deleteAction);
            Out(AnimationScaleData, controller, element, ref deleteAction);
            Out(AnimationRotationData, controller, element, ref deleteAction);
            Out(AnimationColorData, controller, element, ref deleteAction);
        }

        public static void In(AnimationInfo? info, UIController controller, UIElementBase element, ref Action deleteAction)
        {
            if (info != null)
            {
                var data = info.GetInData(element);
                controller.AnimationList.Add(data);
                deleteAction += data.Delete;
            }
        }
        
        public static void Out(AnimationInfo? info, UIController controller, UIElementBase element, ref Action deleteAction)
        {
            if (info != null)
            {
                var data = info.GetOutData(element);
                controller.AnimationList.Add(data);
                deleteAction += data.Delete;
            }  
        }
    }
    
    public abstract class AnimationInfo
    {
        public bool Loop = false;
        public float DurationIn = 1f;
        public float DurationOut = 1f;
        public EaseEffect EaseInEffect = EaseEffect.GetEaseEffect(Rendering.EasingType.Linear);
        public EaseEffect EaseOutEffect = EaseEffect.GetEaseEffect(Rendering.EasingType.Linear);

        public abstract AnimationData GetInData(UIElementBase element);
        public abstract AnimationData GetOutData(UIElementBase element);
    }

    public class AnimationTranslation : AnimationInfo
    {
        public Vector2 Translation = Vector2.Zero;
        public override AnimationTranslationData GetInData(UIElementBase element) => new(element, EaseInEffect, DurationIn, Translation, Loop);
        public override AnimationTranslationData GetOutData(UIElementBase element) => new(element, EaseOutEffect, DurationOut, (0, 0), Loop);
    }

    public class AnimationRotation : AnimationInfo
    {
        public float Rotation = 0f;
        public override AnimationRotationData GetInData(UIElementBase element) => new(element, EaseInEffect, DurationIn, Rotation, Loop);
        public override AnimationRotationData GetOutData(UIElementBase element) => new(element, EaseOutEffect, DurationOut, 0f, Loop);
    }

    public class AnimationScale : AnimationInfo
    {
        public float Scale = 1f;
        public override AnimationScaleData GetInData(UIElementBase element) => new(element, EaseInEffect, DurationIn, Scale, Loop);
        public override AnimationScaleData GetOutData(UIElementBase element) => new(element, EaseOutEffect, DurationOut, 1f, Loop);
    }

    public class AnimationColor : AnimationInfo
    {
        public Vector4 BaseColor = (1f, 1f, 1f, 1f);
        public Vector4 EndColor = (1f, 1f, 1f, 1f);
        public override AnimationColorData GetInData(UIElementBase element) => new(element, EaseInEffect, DurationIn, EndColor, Loop);
        public override AnimationColorData GetOutData(UIElementBase element) => new(element, EaseOutEffect, DurationOut, BaseColor, Loop);
    }
}