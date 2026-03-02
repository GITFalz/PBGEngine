using PBG.MathLibrary;

namespace PBG.Rendering
{
    public class CameraEffect
    {
        public EaseEffect? LastEffect { get; private set; } = null;
        public EaseEffect? CurrentEffect { get; private set; } = null;
        public EaseEffect? NextEffect { get; private set; } = null;
    }

    public enum EasingType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
    }

    public abstract class EaseEffect
    {
        public float Duration { get; set; } = 1f;
        public float ElapsedTime { get; private set; } = 0f;

        public bool IsFinished => ElapsedTime >= Duration;
        public EaseEffect? FollowingEffect = null;

        public EaseEffect(float duration = 1f)
        {
            Duration = duration;
        }

        public EaseEffect(EaseEffect followingEffect, float duration = 1f)
        {
            Duration = duration;
            FollowingEffect = followingEffect;
        }

        public void Update()
        {
            //ElapsedTime += GameTime.DeltaTime;
        }

        public void Reset()
        {
            ElapsedTime = 0f;
        }

        public EaseEffect? GetFollowingEffect()
        {
            return FollowingEffect;
        }

        public abstract float Ease(float start, float end, float t);
        public Vector2 Ease(Vector2 start, Vector2 end, float t) => (Ease(start.X, end.X, t), Ease(start.Y, end.Y, t));
        public Vector3 Ease(Vector3 start, Vector3 end, float t) => (Ease(start.X, end.X, t), Ease(start.Y, end.Y, t), Ease(start.Z, end.Z, t));
        public Vector4 Ease(Vector4 start, Vector4 end, float t) => (Ease(start.X, end.X, t), Ease(start.Y, end.Y, t), Ease(start.Z, end.Z, t), Ease(start.W, end.W, t));

        public static EaseEffect GetEaseEffect(EasingType easing, float factor = 2f)
        {
            return easing switch
            {
                EasingType.Linear => new LinearEaseEffect(),
                EasingType.EaseIn => new EaseInEffect(factor),
                EasingType.EaseOut => new EaseOutEffect(factor),
                EasingType.EaseInOut => new EaseInOutEffect(factor),
                _ => throw new NotImplementedException("Ease effect " + easing + " not found"),
            };
        }
    }

    public class LinearEaseEffect : EaseEffect
    {
        public override float Ease(float start, float end, float t)
        {
            return start + (end - start) * t;
        }
    }

    public class EaseInEffect(float easeFactor = 2f) : EaseEffect
    {
        public float EaseFactor = easeFactor;

        public override float Ease(float start, float end, float t)
        {
            return start + (end - start) * (float)Math.Pow(t, EaseFactor);
        }
    }

    public class EaseOutEffect(float easeFactor = 2f) : EaseEffect
    {
        public float EaseFactor = easeFactor;

        public override float Ease(float start, float end, float t)
        {
            return start + (end - start) * (1 - (float)Math.Pow(1 - t, EaseFactor));
        }
    }

    public class EaseInOutEffect(float easeFactor = 2f) : EaseEffect
    {
        public float EaseFactor = easeFactor;

        public override float Ease(float start, float end, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            float delta = end - start;

            if (t < 0.5f)
            {
                float u = (float)Math.Pow(t * 2f, EaseFactor) * 0.5f;
                return start + delta * u;
            }
            else
            {
                float u = (float)Math.Pow((1f - t) * 2f, EaseFactor) * 0.5f;
                return start + delta * (1f - u);
            }
        }
    }
}