using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

/*
public class Animation
{
    public const int FRAMES = 24;

    public string Name;
    public Dictionary<string, BoneAnimation> BoneAnimations = new Dictionary<string, BoneAnimation>();

    public Animation(string name)
    {
        Name = name;
    }

    public AnimationKeyframe? GetFrame(string boneName)
    {
        if (BoneAnimations.TryGetValue(boneName, out var boneAnimation))
            return boneAnimation.GetFrame();
        return null;
    }

    public void AddBoneAnimation(string boneName)
    {
        if (!BoneAnimations.ContainsKey(boneName))
        {
            BoneAnimations.Add(boneName, new BoneAnimation());
        }
    }

    public void AddBoneAnimation(BoneAnimation boneAnimation)
    {
        if (!BoneAnimations.ContainsKey(boneAnimation.Name))
        {
            BoneAnimations.Add(boneAnimation.Name, boneAnimation);
        }
    }

    public void AddBoneAnimation(string boneName, out BoneAnimation outAnimation)
    {
        if (BoneAnimations.TryGetValue(boneName, out BoneAnimation? value))
        {
            outAnimation = value;
            return;
        }

        BoneAnimation boneAnimation = new BoneAnimation();
        BoneAnimations.Add(boneName, boneAnimation);
        outAnimation = boneAnimation;
    }

    public void RemoveBoneAnimation(string boneName)
    {
        if (BoneAnimations.TryGetValue(boneName, out BoneAnimation? value))
        {
            value.Clear();
            BoneAnimations.Remove(boneName);
        }
    }

    public bool AddOrUpdateKeyframe(string boneName, AnimationKeyframe keyframe)
    {
        if (BoneAnimations.TryGetValue(boneName, out var boneAnimation))
        {
            return boneAnimation.AddOrUpdateKeyframe(keyframe);
        }
        else
        {
            BoneAnimation newBoneAnimation = new BoneAnimation();
            var b = newBoneAnimation.AddOrUpdateKeyframe(keyframe);
            BoneAnimations.Add(boneName, newBoneAnimation);
            return b;
        }
    }

    public bool RemoveKeyframe(string boneName, int index, [NotNullWhen(true)] out AnimationKeyframe? keyframe)
    {
        keyframe = null;
        if (BoneAnimations.TryGetValue(boneName, out var boneAnimation))
        {
            return boneAnimation.RemoveKeyframe(index, out keyframe);
        }
        return false;
    }

    public bool RemoveKeyframe(string boneName, AnimationKeyframe keyframe)
    {
        if (BoneAnimations.TryGetValue(boneName, out var boneAnimation))
        {
            return boneAnimation.RemoveKeyframe(keyframe);
        }
        return false;
    }

    public AnimationKeyframe? GetSpecificFrame(string boneName, int index)
    {
        if (BoneAnimations.TryGetValue(boneName, out var boneAnimation))
        {
            return boneAnimation.GetSpecificFrame(index);
        }
        return null;
    }

    public bool HasBoneAnimation(string boneName)
    {
        return BoneAnimations.ContainsKey(boneName);
    }

    public bool TryGetBoneAnimation(string boneName, [NotNullWhen(true)] out BoneAnimation? boneAnimation)
    {
        return BoneAnimations.TryGetValue(boneName, out boneAnimation);
    }

    public List<string> Save()
    {
        List<string> lines = new List<string>();
        foreach (var boneAnimation in BoneAnimations)
        {
            lines.Add($"Bone: {boneAnimation.Key}");
            lines.Add("{");
            foreach (var keyframe in boneAnimation.Value.Keyframes)
            {
                lines.AddRange(keyframe.Save());
            }
            lines.Add("}");
        }
        return lines;
    }

    public void Clear()
    {
        foreach (var boneAnimation in BoneAnimations.Values)
        {
            boneAnimation.Clear();
        }
        BoneAnimations = [];
    }
}

public class BoneAnimation
{
    public string Name = string.Empty;

    public List<PositionKeyframe> PositionKeyframes = [];
    public List<RotationKeyframe> RotationKeyframes = [];
    public List<ScaleKeyframe> ScaleKeyframes = [];

    public List<GlobalKeyframe> Keyframes = [];

    public Func<GlobalKeyframe?> GetFrame;
    public float elapsedTime = 0;
    int index = 0;

    public BoneAnimation(string name)
    {
        Name = name;
        GetFrame = GetNullFrame;
    }
    public BoneAnimation() { GetFrame = GetNullFrame; }
    public GlobalKeyframe? GetNullFrame() { return null; }
    public GlobalKeyframe? GetFrameSingle() { return Keyframes[0]; }

    public GlobalKeyframe? GetFrameMultiple()
    {
        ResetIndexCheck();

        float t1 = Keyframes[index].Time;
        float t2 = Keyframes[index + 1].Time;

        if (elapsedTime >= t2)
        {
            index++;

            ResetIndexCheck();

            t1 = Keyframes[index].Time;
            t2 = Keyframes[index + 1].Time;
        }

        float t = Mathf.LerpI(t1, t2, elapsedTime);

        elapsedTime += GameTime.DeltaTime;
        return Keyframes[index].Lerp(Keyframes[index + 1], t);
    }

    public AnimationKeyframe? GetSpecificFrame(int index)
    {
        if (Keyframes.Count > 0)
        {
            for (int i = 0; i < Keyframes.Count; i++)
            {
                AnimationKeyframe keyframe = Keyframes[i];
                if (index < keyframe.Index)
                    continue;

                if (i < Keyframes.Count - 1)
                {
                    AnimationKeyframe nextKeyframe = Keyframes[i + 1];
                    if (index >= nextKeyframe.Index)
                        continue;

                    elapsedTime = (float)index / (float)Animation.FRAMES;
                    float t1 = keyframe.Time;
                    float t2 = nextKeyframe.Time;
                    float t = Mathf.LerpI(t1, t2, elapsedTime);
                    return keyframe.Lerp(nextKeyframe, t);
                }
                else
                {
                    elapsedTime = (float)index / (float)Animation.FRAMES;
                    return keyframe;
                }
            }
        }
        return null;
    }

    public void OrderPositionKeyframes() { PositionKeyframes = [.. PositionKeyframes.OrderBy(k => k.Time)]; }
    public void OrderRotationKeyframes() { RotationKeyframes = [.. RotationKeyframes.OrderBy(k => k.Time)]; }
    public void OrderScaleKeyframes() { ScaleKeyframes = [.. ScaleKeyframes.OrderBy(k => k.Time)]; }
    public void OrderKeyframes() { Keyframes = [.. Keyframes.OrderBy(k => k.Time)]; }

    /// <summary>
    /// Get the index of the keyframe in the list
    /// Returns -1 if the keyframe is not found
    /// Assumes the list is sorted by time
    /// </summary>
    /// <param name="keyframe"></param>
    /// <returns></returns>
    public int GetKeyframePlace(GlobalKeyframe keyframe)
    {
        if (Keyframes.Contains(keyframe))
            return Keyframes.IndexOf(keyframe);
        return -1;
    }

    /// <summary>
    /// Get the keyframe before the given keyframe, if it exists
    /// Assumes the list is sorted by time
    /// </summary>
    /// <param name="keyframe"></param>
    /// <returns></returns>
    public GlobalKeyframe? GetBefore(GlobalKeyframe keyframe)
    {
        if (Keyframes.Count <= 1)
            return null;

        int index = GetKeyframePlace(keyframe);
        if (index <= 0)
            return null;

        return Keyframes[index - 1];
    }

    /// <summary>
    /// Add or updates a keyframe to the animation and sort the keyframes by time
    /// </summary>
    /// <param name="keyframe"></param>
    public bool AddOrUpdateKeyframe(GlobalKeyframe keyframe)
    {
        var existing = Keyframes.FirstOrDefault(k => k.Index == keyframe.Index);

        if (existing != null)
        {
            existing.Position = keyframe.Position;
            existing.Rotation = keyframe.Rotation;
            existing.Scale = keyframe.Scale;
            return false;
        }
        else
        {
            Keyframes.Add(keyframe);
            OrderKeyframes();
        }

        GetFrame = Keyframes.Count > 1 ? GetFrameMultiple : GetFrameSingle;
        return true;
    }

    public void SetKeyframes(List<AnimationKeyframe> keyframes)
    {
        Keyframes = keyframes;
        OrderKeyframes();
        GetFrame = Keyframes.Count > 1 ? GetFrameMultiple : GetFrameSingle;
    }

    public bool RemoveKeyframe(int index, [NotNullWhen(true)] out AnimationKeyframe? removedKeyframe)
    {
        removedKeyframe = null;
        bool removed = false;
        if (Keyframes.Count > 0)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == index)
                {
                    removedKeyframe = keyframe;
                    Keyframes.Remove(keyframe);
                    removed = true;
                    break;
                }
            }

            if (Keyframes.Count == 0)
            {
                GetFrame = GetNullFrame;
                elapsedTime = 0;
                index = 0;
            }
            else
            {
                OrderKeyframes();
                GetFrame = Keyframes.Count > 1 ? GetFrameMultiple : GetFrameSingle;
            }
        }
        return removed;
    }

    public bool RemoveKeyframe(AnimationKeyframe keyframe)
    {
        if (Keyframes.Count > 0 && Keyframes.Remove(keyframe))
        {
            if (Keyframes.Count == 0)
            {
                GetFrame = GetNullFrame;
                elapsedTime = 0;
                index = 0;
            }
            else
            {
                OrderKeyframes();
                GetFrame = Keyframes.Count > 1 ? GetFrameMultiple : GetFrameSingle;
            }
            return true;
        }
        return false;
    }

    public bool ContainsIndex(int index)
    {
        if (Keyframes.Count > 0)
        {
            foreach (var keyframe in Keyframes)
            {
                if (keyframe.Index == index)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool ResetIndexCheck()
    {
        if (index >= Keyframes.Count - 1)
        {
            index = 0;
            elapsedTime = 0;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        Keyframes = [];
        GetFrame = GetNullFrame;
        elapsedTime = 0;
        index = 0;
    }
}
*/