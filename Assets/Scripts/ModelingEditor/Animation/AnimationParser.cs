using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;
using PBG.Parse;

public static class AnimationParser
{
    public static NewAnimation currentAnimation = null!;
    public static NewBoneAnimation? currentBoneAnimation;

    public static bool Parse(string name, string[] lines, [NotNullWhen(true)] out NewAnimation? animation)
    {
        animation = null;
        currentAnimation = new NewAnimation(name, 0);
        currentBoneAnimation = null;

        AnimationData data = new AnimationData();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
                continue;

            while (true)
            {
                line = lines[i];
                values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 0)
                    break;

                string key = values[0].Trim();

                if (!_currentParseAction.TryGetValue(key, out ParseAction? action))
                {
                    Console.WriteLine($"Action not found for {key} at line {i + 1}");
                    return false;
                }

                bool result = action(ref i, line, ref data);
                if (!result)
                {
                    break;
                }
            }
        }

        animation = currentAnimation;
        return true;
    }
    
    private delegate bool ParseAction(ref int index, string line, ref AnimationData data);
    private static readonly Dictionary<string, ParseAction> _boneData = new()
    {
        { "Bone:", (ref int index, string line, ref AnimationData data) =>
            {
                index++;
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"line: {line}"); 
                if (values.Length != 2)
                    return false;

                string name = values[1];
                currentBoneAnimation = new NewBoneAnimation(name);
                return true;
            }
        },
        { "{", (ref int index, string line, ref AnimationData data) =>
            {
                index++;
                return true;
            }
        },
        { "Keyframe:", (ref int index, string line, ref AnimationData data) =>
            {
                index++;
                _currentParseAction = _keyframeData;
                data.Clear();
                return true;
            }
        },
        { "}", (ref int index, string line, ref AnimationData data) =>
            {
                if (currentBoneAnimation != null)
                    currentAnimation.AddBoneAnimation(currentBoneAnimation);
                return false;
            }
        }
    };

    private static readonly Dictionary<string, ParseAction> _keyframeData = new()
    {
        { "Keyframe:", (ref int index, string line, ref AnimationData data) =>
            {
                index++;
                return true;
            }
        },
        { "{", (ref int index, string line, ref AnimationData data) =>
            {
                index++;
                return true;
            }
        },
        { "Position:", (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 4)
                    return false;

                data.Position = new Vector3(Float.Parse(values[1]), Float.Parse(values[2]), Float.Parse(values[3]));
                index++;
                return true;
            }
        },
        { "P-Ease:" , (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 2)
                    return false;

                int ease = Int.Parse(values[1], 0);
                data.PositionEasing = Ease.GetEasingType(ease);
                index++;
                return true;
            }
        },
        { "Rotation:", (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < 4)
                    return false;

                if (values.Length != 5)
                {
                    data.Rotation = Quaternion.FromEuler( Float.Parse(values[1]), Float.Parse(values[2]), Float.Parse(values[3]));
                }
                else
                {
                    data.Rotation = new Quaternion(Float.Parse(values[1]), Float.Parse(values[2]), Float.Parse(values[3]), Float.Parse(values[4]));
                }
                index++;
                return true;
            }
        },
        { "R-Ease:" , (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 2)
                    return false;

                int ease = Int.Parse(values[1], 0);
                data.RotationEasing = Ease.GetEasingType(ease);
                index++;
                return true;
            }
        },
        { "Scale:", (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 2)
                    return false;

                data.Scale = new Vector3(Float.Parse(values[1]), Float.Parse(values[2]), Float.Parse(values[3]));
                index++;
                return true;
            }
        },
        { "S-Ease:" , (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 2)
                    return false;

                int ease = Int.Parse(values[1], 0);
                data.ScaleEasing = Ease.GetEasingType(ease);
                index++;
                return true;
            }
        },
        { "Index:", (ref int index, string line, ref AnimationData data) =>
            {
                var values = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 2)
                    return false;

                data.Index = Int.Parse(values[1], 0);
                index++;
                return true;
            }
        },
        { "}", (ref int index, string line, ref AnimationData data) =>
            {
                index++;
                if (currentBoneAnimation != null)
                {
                    if (data.Position.HasValue)
                    {
                        PositionKeyframe positionKeyframe = new PositionKeyframe(data.Index, data.Position.Value, data.PositionEasing);
                        currentBoneAnimation.AddOrUpdateKeyframe(positionKeyframe);
                    }
                    if (data.Rotation.HasValue)
                    {
                        RotationKeyframe rotationKeyframe = new RotationKeyframe(data.Index, data.Rotation.Value, data.RotationEasing);
                        currentBoneAnimation.AddOrUpdateKeyframe(rotationKeyframe);
                    }
                    if (data.Scale.HasValue)
                    {
                        ScaleKeyframe scaleKeyframe = new ScaleKeyframe(data.Index, data.Scale.Value, data.ScaleEasing);
                        currentBoneAnimation.AddOrUpdateKeyframe(scaleKeyframe);
                    }
                }
                    
                _currentParseAction = _boneData;
                return true;
            }
        }
    };

    private static Dictionary<string, ParseAction> _currentParseAction = _boneData;

    private struct AnimationData
    {
        public Vector3? Position = null;
        public EasingType PositionEasing = EasingType.Linear;
        public Quaternion? Rotation = null;
        public EasingType RotationEasing = EasingType.Linear;
        public Vector3? Scale = null;
        public EasingType ScaleEasing = EasingType.Linear;
        public int Index;

        public AnimationData()
        {
            Index = 0;
        }

        public void Clear()
        {
            Position = null;
            PositionEasing = EasingType.Linear;
            Rotation = null;
            RotationEasing = EasingType.Linear;
            Scale = null;
            ScaleEasing = EasingType.Linear;
            Index = 0;
        }
    }
}