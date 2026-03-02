
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;

namespace PBG.UI;

public abstract class AnimationData(UIElementBase element, EaseEffect effect, float duration, bool loop)
{
    public UIElementBase Element = element;
    public EaseEffect EaseEffect = effect;
    public float Duration = duration;
    public float CurrentTime = 0f;
    public bool delete = false;
    public bool Loop = loop;
    public void Delete() => delete = true;
    public abstract void Ease();
    public abstract void End();
}

public class AnimationTranslationData : AnimationData
{
    public Vector2 start;
    public Vector2 end;
    public Action<Vector2> SetValue;

    public AnimationTranslationData(UIElementBase element, EaseEffect effect, float duration, Vector2 end, bool loop) : base(element, effect, duration, loop)
    {
        start = element.AnimationTranslation;
        SetValue = element.UpdateAnimationTranslation;
        this.end = end;
    }
    
    public override void Ease()
    {
        var t = Mathf.Clampy(CurrentTime / Duration, 0f, 1f);
        var value = EaseEffect.Ease(start, end, t);
        SetValue(value);
    }
    public override void End() 
    {
        SetValue(end);
    }
}

public class AnimationRotationData : AnimationData
{
    public float start;
    public float end;
    public Action<float> SetValue;

    public AnimationRotationData(UIElementBase element, EaseEffect effect, float duration, float end, bool loop) : base(element, effect, duration, loop)
    {
        start = element.AnimationRotation;
        SetValue = element.UpdateAnimationRotation;
        this.end = end;
    }

    public override void Ease()
    {
        var t = Mathf.Clampy(CurrentTime / Duration, 0f, 1f);
        var value = EaseEffect.Ease(start, end, t);
        SetValue(value);
    }
    public override void End()
    {
        SetValue(end);
        
    }
}

public class AnimationScaleData : AnimationData
{
    public float start;
    public float end;
    public Action<float> SetValue;

    public AnimationScaleData(UIElementBase element, EaseEffect effect, float duration, float end, bool loop) : base(element, effect, duration, loop)
    {
        start = element.AnimationScale;
        SetValue = element.UpdateAnimationScale;
        this.end = end;
    }

    public override void Ease()
    {
        var t = Mathf.Clampy(CurrentTime / Duration, 0f, 1f);
        var value = EaseEffect.Ease(start, end, t);
        SetValue(value);
    }
    public override void End()
    {
        SetValue(end);
    }
}

public class AnimationColorData : AnimationData
{
    public Vector4 start;
    public Vector4 end;
    public Func<Vector4, UIElementBase> SetValue;

    public AnimationColorData(UIElementBase element, EaseEffect effect, float duration, Vector4 end, bool loop) : base(element, effect, duration, loop)
    {
        start = element.Color;
        SetValue = element.UpdateColor;
        this.end = end;
    }

    public override void Ease()
    {
        var t = Mathf.Clampy(CurrentTime / Duration, 0f, 1f);
        var value = EaseEffect.Ease(start, end, t);
        SetValue(value);
    }
    public override void End()
    {
        SetValue(end);
    }
}