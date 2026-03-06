using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;

public class Rig
{
    public string Name { get; private set; } = "Rig";
    public RootBone RootBone = new RootBone("RootBone");
    public Dictionary<string, Bone> Bones = [];
    public List<Bone> BonesList = [];

    public Rig(string name)
    {
        Name = name;
        Create();
    }

    public void Create()
    {
        Bones = [];
        RootBone.GetBones(Bones);
    }

    public void Initialize()
    {
        BonesList = [];

        int i = 0;
        foreach (var (_, bone) in Bones)
        {
            BonesList.Add(bone);
            bone.Index = i;
            i++;
        }
    }

    public bool GetBone(string name, [NotNullWhen(true)] out Bone? bone)
    {
        return Bones.TryGetValue(name, out bone);
    }

    public Rig Copy()
    {
        var copy = new Rig(Name);
        copy.RootBone = RootBone.Copy();
        copy.Create();
        copy.Initialize();
        return copy;
    }

    public List<Matrix4> GetGlobalAnimatedMatrices()
    {
        List<Matrix4> matrices = [];
        foreach (var (_, bone) in Bones)
        {
            matrices.Add(bone.GlobalAnimatedMatrix);
        }
        return matrices;
    }

    public void Delete()
    {
        RootBone.Clear();
        Bones = [];
        BonesList = [];
    }
}