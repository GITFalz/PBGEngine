using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;
using PBG.Parse;

public class NewAnimation
{
    public const int FRAMES = 24;

    public string Name;
    public int ID;
    public Dictionary<string, NewBoneAnimation> BoneAnimations = [];

    public int FrameCount
    {
        get
        {
            int maxLength = 0;
            foreach (var boneAnimation in BoneAnimations.Values)
            {
                maxLength = Mathf.Max(maxLength, boneAnimation.Length);
            }
            return maxLength;
        }
    }

    public NewAnimation(string name, int id)
    {
        Name = name;
        ID = id;
    }

    public bool GetBoneAnimation(string boneName, [NotNullWhen(true)] out NewBoneAnimation? boneAnimation)
    {
        return BoneAnimations.TryGetValue(boneName, out boneAnimation);
    }

    public bool AddBoneAnimation(NewBoneAnimation boneAnimation)
    {
        if (BoneAnimations.ContainsKey(boneAnimation.BoneName))
            return false;

        BoneAnimations.Add(boneAnimation.BoneName, boneAnimation);
        return true;
    }

    public bool RemoveBoneAnimation(string boneName)
    {
        return BoneAnimations.Remove(boneName);
    }

    public bool AddOrUpdateKeyframe(string boneName, PositionKeyframe keyframe)
    {
        if (GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation))
        {
            boneAnimation.AddOrUpdateKeyframe(keyframe);
            return true;
        }
        return false;
    }

    public bool AddOrUpdateKeyframe(string boneName, RotationKeyframe keyframe)
    {
        if (GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation))
        {
            boneAnimation.AddOrUpdateKeyframe(keyframe);
            return true;
        }
        return false;
    }

    public bool AddOrUpdateKeyframe(string boneName, ScaleKeyframe keyframe)
    {
        if (GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation))
        {
            boneAnimation.AddOrUpdateKeyframe(keyframe);
            return true;
        }
        return false;
    }
    public bool RemoveKeyframe(string boneName, IndividualKeyframe keyframe) => GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation) && boneAnimation.RemoveKeyframe(keyframe);
    public bool RemoveKeyframe(string boneName, PositionKeyframe keyframe) => GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation) && boneAnimation.RemoveKeyframe(keyframe);
    public bool RemoveKeyframe(string boneName, RotationKeyframe keyframe) => GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation) && boneAnimation.RemoveKeyframe(keyframe);
    public bool RemoveKeyframe(string boneName, ScaleKeyframe keyframe) => GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation) && boneAnimation.RemoveKeyframe(keyframe);


    public GlobalKeyframe? GetSpecificFrame(string boneName, int index)
    {
        if (GetBoneAnimation(boneName, out NewBoneAnimation? boneAnimation))
        {
            return boneAnimation.GetKeyframe(index);
        }
        return null;
    }

    public List<string> Save()
    {
        List<string> lines = new List<string>();
        foreach (var (name, boneAnimation) in BoneAnimations)
        {
            lines.Add($"Bone: {name}");
            lines.Add("{");
            lines.AddRange(boneAnimation.Save());
            lines.Add("}");
        }
        return lines;
    }
}

public class NewBoneAnimation
{
    public string BoneName;

    public List<PositionKeyframe> PositionKeyframes = [];
    public List<RotationKeyframe> RotationKeyframes = [];
    public List<ScaleKeyframe> ScaleKeyframes = [];

    public int Length { get => Mathf.Max(GetPositionLargestIndex() + 1, GetRotationLargestIndex() + 1, GetScaleLargestIndex() + 1); }

    public NewBoneAnimation(string boneName)
    {
        BoneName = boneName;
    }

    public void Clear()
    {
        PositionKeyframes = [];
        RotationKeyframes = [];
        ScaleKeyframes = [];
    }

    public int GetPositionLargestIndex()
    {
        if (PositionKeyframes.Count == 0)
            return 0;
        int largest = PositionKeyframes[0].Index;
        foreach (var keyframe in PositionKeyframes)
        {
            if (keyframe.Index > largest)
                largest = keyframe.Index;
        }
        return largest;
    }
    public int GetRotationLargestIndex()
    {
        if (RotationKeyframes.Count == 0)
            return 0;
        int largest = RotationKeyframes[0].Index;
        foreach (var keyframe in RotationKeyframes)
        {
            if (keyframe.Index > largest)
                largest = keyframe.Index;
        }
        return largest;
    }
    public int GetScaleLargestIndex()
    {
        if (ScaleKeyframes.Count == 0)
            return 0;
        int largest = ScaleKeyframes[0].Index;
        foreach (var keyframe in ScaleKeyframes)
        {
            if (keyframe.Index > largest)
                largest = keyframe.Index;
        }
        return largest;
    }

    public bool ContainsIndex(int type, int index)
    {
        if (type == 0)
            return PositionKeyframes.Any(k => k.Index == index);
        else if (type == 1)
            return RotationKeyframes.Any(k => k.Index == index);
        else if (type == 2)
            return ScaleKeyframes.Any(k => k.Index == index);
        return false;
    }

    public GlobalKeyframe GetKeyframe(int index)
    {
        PositionKeyframe positionKeyframe = GetPositionKeyframe(index);
        RotationKeyframe rotationKeyframe = GetRotationKeyframe(index);
        ScaleKeyframe scaleKeyframe = GetScaleKeyframe(index);
        GlobalKeyframe frame = new GlobalKeyframe(index, positionKeyframe, rotationKeyframe, scaleKeyframe);
        frame.HasPositionKeyframe = HasExactPositionKeyframe(index);
        frame.HasRotationKeyframe = HasExactRotationKeyframe(index);
        frame.HasScaleKeyframe = HasExactScaleKeyframe(index);
        return frame;
    }

    public PositionKeyframe GetPositionKeyframe(int index)
    {
        if (PositionKeyframes.Count == 0) { return new PositionKeyframe(index); }
        if (PositionKeyframes.Count == 1) { return new PositionKeyframe(index, PositionKeyframes[0].Position) { Easing = PositionKeyframes[0].Easing }; }
        else if (PositionKeyframes.Count > 1)
        {
            for (int i = 0; i < PositionKeyframes.Count - 1; i++)
            {
                PositionKeyframe current = PositionKeyframes[i];
                PositionKeyframe next = PositionKeyframes[i + 1];

                if (current.Index == index || index < current.Index)
                {
                    return new PositionKeyframe(index, current.Position) { Easing = current.Easing };
                }

                if (index > current.Index && index < next.Index)
                {
                    int count = next.Index - current.Index;
                    int currentIndex = index - current.Index;
                    float t = (float)currentIndex / count;
                    Vector3 position = Ease.Apply(next.Easing, current.Position, next.Position, t);
                    return new PositionKeyframe(index, position);
                }
            }
        }
        return new PositionKeyframe(index, PositionKeyframes[^1].Position) { Easing = PositionKeyframes[^1].Easing };
    }

    public RotationKeyframe GetRotationKeyframe(int index)
    {
        if (RotationKeyframes.Count == 0) { return new RotationKeyframe(index); }
        if (RotationKeyframes.Count == 1) { return new RotationKeyframe(index, RotationKeyframes[0].Rotation) { Easing = RotationKeyframes[0].Easing }; }
        else if (RotationKeyframes.Count > 1)
        {
            for (int i = 0; i < RotationKeyframes.Count - 1; i++)
            {
                RotationKeyframe current = RotationKeyframes[i];
                RotationKeyframe next = RotationKeyframes[i + 1];

                if (current.Index == index || index < current.Index)
                {
                    return new RotationKeyframe(index, current.Rotation) { Easing = current.Easing };
                }

                if (index > current.Index && index < next.Index)
                {
                    int count = next.Index - current.Index;
                    int currentIndex = index - current.Index;
                    float t = (float)currentIndex / count;
                    Quaternion rotation = Ease.Apply(next.Easing, current.Rotation, next.Rotation, t);
                    return new RotationKeyframe(index, rotation);
                }
            }
        }
        return new RotationKeyframe(index, RotationKeyframes[^1].Rotation) { Easing = RotationKeyframes[^1].Easing };
    }

    public ScaleKeyframe GetScaleKeyframe(int index)
    {
        if (ScaleKeyframes.Count == 0) { return new ScaleKeyframe(index); }
        if (ScaleKeyframes.Count == 1) { return new ScaleKeyframe(index, ScaleKeyframes[0].Scale) { Easing = ScaleKeyframes[0].Easing }; }
        else if (ScaleKeyframes.Count > 1)
        {
            for (int i = 0; i < ScaleKeyframes.Count - 1; i++)
            {
                ScaleKeyframe current = ScaleKeyframes[i];
                ScaleKeyframe next = ScaleKeyframes[i + 1];

                if (current.Index == index || index < current.Index)
                {
                    return new ScaleKeyframe(index, current.Scale) { Easing = current.Easing };
                }

                if (index > current.Index && index < next.Index)
                {
                    int count = next.Index - current.Index;
                    int currentIndex = index - current.Index;
                    float t = (float)currentIndex / count;
                    Vector3 scale = Ease.Apply(next.Easing, current.Scale, next.Scale, t);
                    return new ScaleKeyframe(index, scale);
                }
            }
        }
        return new ScaleKeyframe(index, ScaleKeyframes[^1].Scale) { Easing = ScaleKeyframes[^1].Easing };
    }



    // Check if a keyframe exists at the exact index
    public bool HasExactPositionKeyframe(int index) => PositionKeyframes.Any(k => k.Index == index);
    public bool HasExactPositionKeyframe(int index, [NotNullWhen(true)] out PositionKeyframe? keyframe) { keyframe = PositionKeyframes.FirstOrDefault(k => k.Index == index); return keyframe != null; }
    public bool HasExactRotationKeyframe(int index) => RotationKeyframes.Any(k => k.Index == index);
    public bool HasExactRotationKeyframe(int index, [NotNullWhen(true)] out RotationKeyframe? keyframe) { keyframe = RotationKeyframes.FirstOrDefault(k => k.Index == index); return keyframe != null; }
    public bool HasExactScaleKeyframe(int index) => ScaleKeyframes.Any(k => k.Index == index);
    public bool HasExactScaleKeyframe(int index, [NotNullWhen(true)] out ScaleKeyframe? keyframe) { keyframe = ScaleKeyframes.FirstOrDefault(k => k.Index == index); return keyframe != null; }
    
    public void OrderKeyframes(int type)
    {
        if (type == 0) OrderPositionKeyframes();
        if (type == 1) OrderRotationKeyframes();
        if (type == 2) OrderScaleKeyframes();
    }
    public void OrderPositionKeyframes() => PositionKeyframes.Sort((a, b) => a.Index.CompareTo(b.Index));
    public void OrderRotationKeyframes() => RotationKeyframes.Sort((a, b) => a.Index.CompareTo(b.Index));
    public void OrderScaleKeyframes() => ScaleKeyframes.Sort((a, b) => a.Index.CompareTo(b.Index));

    public void SetKeyframes(List<PositionKeyframe> positionKeyframes) => PositionKeyframes = positionKeyframes;
    public void SetKeyframes(List<RotationKeyframe> rotationKeyframes) => RotationKeyframes = rotationKeyframes;
    public void SetKeyframes(List<ScaleKeyframe> scaleKeyframes) => ScaleKeyframes = scaleKeyframes;

    public bool AddOrUpdateKeyframe(IndividualKeyframe keyframe)
    {
        if (keyframe is PositionKeyframe positionKeyframe)
            return AddOrUpdateKeyframe(positionKeyframe);
        else if (keyframe is RotationKeyframe rotationKeyframe)
            return AddOrUpdateKeyframe(rotationKeyframe);
        else if (keyframe is ScaleKeyframe scaleKeyframe)
            return AddOrUpdateKeyframe(scaleKeyframe);
        return false;
    }
    public bool AddOrUpdateKeyframe(PositionKeyframe keyframe)
    {
        if (HasExactPositionKeyframe(keyframe.Index, out PositionKeyframe? existingKeyframe))
        {
            existingKeyframe.Position = keyframe.Position;
            existingKeyframe.Easing = keyframe.Easing;
            return false;
        }
        else
        {
            PositionKeyframes.Add(keyframe);
            OrderPositionKeyframes();
            return true;
        }
    }
    public bool AddOrUpdateKeyframe(RotationKeyframe keyframe)
    {
        if (HasExactRotationKeyframe(keyframe.Index, out RotationKeyframe? existingKeyframe))
        {
            existingKeyframe.Rotation = keyframe.Rotation;
            existingKeyframe.Easing = keyframe.Easing;
            return false;
        }
        else
        {
            RotationKeyframes.Add(keyframe);
            OrderRotationKeyframes();
            return true;
        }
    }
    public bool AddOrUpdateKeyframe(ScaleKeyframe keyframe)
    {
        if (HasExactScaleKeyframe(keyframe.Index, out ScaleKeyframe? existingKeyframe))
        {
            existingKeyframe.Scale = keyframe.Scale;
            existingKeyframe.Easing = keyframe.Easing;
            return false;
        }
        else
        {
            ScaleKeyframes.Add(keyframe);
            OrderScaleKeyframes();
            return true;
        }
    }
    public bool RemoveKeyframe(IndividualKeyframe keyframe)
    {
        if (keyframe is PositionKeyframe positionKeyframe)
            return PositionKeyframes.Remove(positionKeyframe);
        else if (keyframe is RotationKeyframe rotationKeyframe)
            return RotationKeyframes.Remove(rotationKeyframe);
        else if (keyframe is ScaleKeyframe scaleKeyframe)
            return ScaleKeyframes.Remove(scaleKeyframe);
        return false;
    }
    public bool RemoveKeyframe(PositionKeyframe keyframe) => PositionKeyframes.Remove(keyframe);
    public bool RemoveKeyframe(RotationKeyframe keyframe) => RotationKeyframes.Remove(keyframe);
    public bool RemoveKeyframe(ScaleKeyframe keyframe) => ScaleKeyframes.Remove(keyframe);


    public List<string> Save()
    {
        List<GlobalKeyframe> keyframes = [];
        for (int i = 0; i < PositionKeyframes.Count; i++)
        {
            PositionKeyframe positionKeyframe = PositionKeyframes[i];
            keyframes.Add(new GlobalKeyframe(positionKeyframe.Index, positionKeyframe.Position, null, null));
        }
        for (int i = 0; i < RotationKeyframes.Count; i++)
        {
            RotationKeyframe rotationKeyframe = RotationKeyframes[i];
            if (HasIndex(keyframes, rotationKeyframe.Index, out GlobalKeyframe? existingKeyframe))
            {
                existingKeyframe.SetRotation(rotationKeyframe.Rotation);
            }
            else
            {
                keyframes.Add(new GlobalKeyframe(rotationKeyframe.Index, null, rotationKeyframe.Rotation, null));
            }
        }
        for (int i = 0; i < ScaleKeyframes.Count; i++)
        {
            ScaleKeyframe scaleKeyframe = ScaleKeyframes[i];
            if (HasIndex(keyframes, scaleKeyframe.Index, out GlobalKeyframe? existingKeyframe))
            {
                existingKeyframe.SetScale(scaleKeyframe.Scale);
            }
            else
            {
                keyframes.Add(new GlobalKeyframe(scaleKeyframe.Index, null, null, scaleKeyframe.Scale));
            }
        }
        keyframes.Sort((a, b) => a.Index.CompareTo(b.Index));
        List<string> lines = [];
        foreach (var keyframe in keyframes)
        {
            lines.AddRange([
                "    Keyframe:",
                "    {",
                $"        Index: {keyframe.Index}"
            ]);
            if (keyframe.HasPositionKeyframe)
            {
                lines.Add($"        Position: {Values(keyframe.Position)}");
                lines.Add($"        P-Ease: {(int)keyframe.Position.Easing}");
            }
            if (keyframe.HasRotationKeyframe)
            {
                lines.Add($"        Rotation: {Values(keyframe.Rotation)}");
                lines.Add($"        R-Ease: {(int)keyframe.Rotation.Easing}");
            }
            if (keyframe.HasScaleKeyframe)
            {
                lines.Add($"        Scale: {Values(keyframe.Scale)}");
                lines.Add($"        S-Ease: {(int)keyframe.Scale.Easing}");
            }
            lines.Add("    }");
        }
        return lines;
    }

    public static string Values(Vector3 vector)
    {
        return $"{Float.Str(vector.X)} {Float.Str(vector.Y)} {Float.Str(vector.Z)}";
    }

    public static string Values(Quaternion quaternion)
    {
        return $"{Float.Str(quaternion.X)} {Float.Str(quaternion.Y)} {Float.Str(quaternion.Z)} {Float.Str(quaternion.W)}";
    }

    public static bool HasIndex(List<GlobalKeyframe> keyframes, int index)
    {
        return keyframes.Any(k => k.Index == index);
    }

    public static bool HasIndex(List<GlobalKeyframe> keyframes, int index, [NotNullWhen(true)] out GlobalKeyframe? keyframe)
    {
        keyframe = keyframes.FirstOrDefault(k => k.Index == index);
        return keyframe != null;
    }
}

public abstract class AbstractKeyframe
{
    public float Time;
    public int Index;

    public AbstractKeyframe() : this(0) { }
    public AbstractKeyframe(int index) { SetIndex(index); }

    public void SetIndex(int index)
    {
        Index = index;
        Time = (float)index / (float)NewAnimation.FRAMES;
    }
}

public class GlobalKeyframe : AbstractKeyframe
{
    public PositionKeyframe Position = PositionKeyframe.Zero(0);
    public RotationKeyframe Rotation = RotationKeyframe.Identity(0);
    public ScaleKeyframe Scale = ScaleKeyframe.One(0);

    // These are used to determine if the bone animation has keyframes for position, rotation, and scale at a specific index
    // This can be used to reduce the amount of data saved if a keyframe wasn't placed for that specific property
    public bool HasPositionKeyframe = false;
    public bool HasRotationKeyframe = false;
    public bool HasScaleKeyframe = false;

    public GlobalKeyframe() : base() { }
    public GlobalKeyframe(int index, PositionKeyframe? position, RotationKeyframe? rotation, ScaleKeyframe? scale) : this(position, rotation, scale) { SetIndex(index); }
    public GlobalKeyframe(int index, Vector3? position, Quaternion? rotation, Vector3? scale) : this(position, rotation, scale) { SetIndex(index); }
    public GlobalKeyframe(Vector3? position, Quaternion? rotation, Vector3? scale) : this(
        position == null ? null : new PositionKeyframe(position.Value),
        rotation == null ? null : new RotationKeyframe(rotation.Value),
        scale == null ? null : new ScaleKeyframe(scale.Value)
    )
    { }
    public GlobalKeyframe(PositionKeyframe? position, RotationKeyframe? rotation, ScaleKeyframe? scale) : base()
    {
        if (position != null) Position = position;
        if (rotation != null) Rotation = rotation;
        if (scale != null) Scale = scale;
        HasPositionKeyframe = position != null;
        HasRotationKeyframe = rotation != null;
        HasScaleKeyframe = scale != null;
    }

    public void SetPosition(Vector3? position)
    {
        if (position == null)
        {
            Position = PositionKeyframe.Zero(Index);
            HasPositionKeyframe = false;
        }
        else
        {
            Position = new PositionKeyframe(Index, position.Value);
            HasPositionKeyframe = true;
        }
    }

    public void SetRotation(Quaternion? rotation)
    {
        if (rotation == null)
        {
            Rotation = RotationKeyframe.Identity(Index);
            HasRotationKeyframe = false;
        }
        else
        {
            Rotation = new RotationKeyframe(Index, rotation.Value);
            HasRotationKeyframe = true;
        }
    }

    public void SetScale(Vector3? scale)
    {
        if (scale == null)
        {
            Scale = ScaleKeyframe.One(Index);
            HasScaleKeyframe = false;
        }
        else
        {
            Scale = new ScaleKeyframe(Index, scale.Value);
            HasScaleKeyframe = true;
        }
    }


    public GlobalKeyframe Lerp(GlobalKeyframe keyframe, float t)
    {
        var lerped = new GlobalKeyframe(Mathf.Lerp(Position, keyframe.Position, t), Quaternion.Slerp(Rotation, keyframe.Rotation, t), Mathf.Lerp(Scale, keyframe.Scale, t));
        lerped.Time = Mathf.Lerp(Time, keyframe.Time, t);
        return lerped;
    }

    public Matrix4 GetLocalTransform()
    {
        return Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
    }


    public override string ToString()
    {
        return $"GlobalKeyframe:\n" +
               $"  Index: {Index}\n" +
               $"  Time: {Time}\n" +
               $"  Position: {Position.Position}\n" +
               $"  Rotation: {Rotation.Rotation}\n" +
               $"  Scale: {Scale.Scale}\n" +
               $"  HasPositionKeyframe: {HasPositionKeyframe}\n" +
               $"  HasRotationKeyframe: {HasRotationKeyframe}\n" +
               $"  HasScaleKeyframe: {HasScaleKeyframe}";
    }
}
 
public abstract class IndividualKeyframe : AbstractKeyframe
{
    public EasingType Easing = EasingType.Linear;

    public IndividualKeyframe() : base() { }
    public IndividualKeyframe(int index) : base(index) { }
}

public class PositionKeyframe : IndividualKeyframe
{
    public static Func<int, PositionKeyframe> Zero = (index) => new PositionKeyframe(index, Vector3.Zero);

    public Vector3 Position = Vector3.Zero;

    public PositionKeyframe() : base() { }
    public PositionKeyframe(int index) : base(index) { }
    public PositionKeyframe(Vector3 position) : base() { Position = position; }
    public PositionKeyframe(int index, Vector3 position) : base(index) { Position = position; }
    public PositionKeyframe(int index, Vector3 position, EasingType easing) : base(index)
    {
        Position = position;
        Easing = easing;
    }

    public static implicit operator Vector3(PositionKeyframe keyframe) { return keyframe.Position; }
}

public class RotationKeyframe : IndividualKeyframe
{
    public static Func<int, RotationKeyframe> Identity = (index) => new RotationKeyframe(index, Quaternion.Identity);

    public Quaternion Rotation = Quaternion.Identity;

    public RotationKeyframe() : base() { }
    public RotationKeyframe(int index) : base(index) { }
    public RotationKeyframe(Quaternion rotation) : base() { Rotation = rotation; }
    public RotationKeyframe(int index, Quaternion rotation) : base(index) { Rotation = rotation; }
    public RotationKeyframe(int index, Quaternion rotation, EasingType easing) : base(index)
    {
        Rotation = rotation;
        Easing = easing;
    }

    public static implicit operator Quaternion(RotationKeyframe keyframe) { return keyframe.Rotation; }
}

public class ScaleKeyframe : IndividualKeyframe
{
    public static Func<int, ScaleKeyframe> One = (index) => new ScaleKeyframe(index, Vector3.One);

    public Vector3 Scale = Vector3.One;

    public ScaleKeyframe() : base() { }
    public ScaleKeyframe(int index) : base(index) { }
    public ScaleKeyframe(Vector3 scale) : base() { Scale = scale; }
    public ScaleKeyframe(int index, Vector3 scale) : base(index) { Scale = scale; }
    public ScaleKeyframe(int index, Vector3 scale, EasingType easing) : base(index)
    {
        Scale = scale;
        Easing = easing;
    }

    public static implicit operator Vector3(ScaleKeyframe keyframe) { return keyframe.Scale; }
}