using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;
using PBG.Parse;

public static class RigManager
{
    public static Dictionary<string, Rig> Rigs = [];

    public static bool Add(Rig rig, bool displayError = true)
    {
        if (Rigs.ContainsKey(rig.Name))
        {
            //if (displayError) PopUp.AddPopUp("Rig already exists");
            return false;
        }

        Rigs.Add(rig.Name, rig);
        //if (displayError) PopUp.AddPopUp("Rig added");
        return true;
    }

    public static void Update(Rig rig)
    {
        if (Rigs.ContainsKey(rig.Name))
        {
            Rigs[rig.Name] = rig;
        }
        else
        {
            //PopUp.AddPopUp("Rig not found");
        }
    }

    public static bool Remove(string name, bool displayError = true)
    {
        if (!Rigs.ContainsKey(name))
        {
            //if (displayError) PopUp.AddPopUp("Rig not found");
            return false;
        }

        Rigs.Remove(name);
        //if (displayError) PopUp.AddPopUp("Rig removed");
        return true;
    }

    public static bool TryGet(string name, [NotNullWhen(true)] out Rig? rig, bool displayError = true)
    {
        if (Rigs.TryGetValue(name, out rig))
            return true;

        //if (displayError) PopUp.AddPopUp("Rig not found");
        return false;
    }

    public static bool ChangeName(string oldName, string newName, bool displayError = true)
    {
        if (!Rigs.ContainsKey(oldName))
        {
            //if (displayError) PopUp.AddPopUp("Old rig name not found");
            return false;
        }

        if (Rigs.ContainsKey(newName))
        {
            //if (displayError) PopUp.AddPopUp("Rig name already exists");
            return false;
        }

        Rigs.Add(newName, Rigs[oldName]);
        Rigs.Remove(oldName);
        //if (displayError) PopUp.AddPopUp("Rig name changed");
        return true;
    }

    #region Save
    public static void Save(string name, bool displayError = true)
    {
        //Save(name, Game.rigPath, displayError);
    }

    public static void SaveRigToPath(Rig rig, string path)
    {
        if (File.Exists(path))
        {

        }
        SaveRig(rig, path);
    }

    private static void SaveRig(Rig rig, string path)
    {
        Console.WriteLine($"Saving {rig.Bones.Count} from rig {rig.Name} to {path}");
        List<string> lines = [rig.Bones.Count.ToString()];

        foreach (var (name, bone) in rig.Bones)
        {
            string parentName = bone is ChildBone child ? child.Parent.Name : name;
            lines.Add($"name {name}");
            lines.Add($"parent {parentName}");
            lines.Add($"p {Float.Str(bone.Position.X)}, {Float.Str(bone.Position.Y)}, {Float.Str(bone.Position.Z)}");
            lines.Add($"r {Float.Str(bone.Rotation.X)}, {Float.Str(bone.Rotation.Y)}, {Float.Str(bone.Rotation.Z)}, {Float.Str(bone.Rotation.W)}");
            lines.Add($"s {Float.Str(bone.Scale.X)}, {Float.Str(bone.Scale.Y)}, {Float.Str(bone.Scale.Z)}");
        }

        File.WriteAllLines(path, lines);
        //PopUp.AddPopUp("Rig saved");
    }
    #endregion

    #region Load
    public static bool Load(string name)
    {
        return false; //return Load(name, Game.rigPath);
    }

    public static bool Load(string name, string path)
    {
        if (Rigs.ContainsKey(name)) // Quietly ignore if the rig already exists
            return false;

        path = Path.Combine(path, name + ".rig");
        if (!File.Exists(path))
        {
            //PopUp.AddPopUp("Rig does not exist");
            return false;
        }

        if (!LoadRig(name, path, out Rig? rig))
        {
            //PopUp.AddPopUp("Rig failed to load");
            return false;
        }

        Add(rig);
        //PopUp.AddPopUp("Rig loaded");
        return true;
    }

    public static bool LoadRigFromPath(string path, [NotNullWhen(true)] out Rig? rig)
    {
        rig = null;
        if (!path.EndsWith(".rig"))
        {
            Console.WriteLine("Failed to load rig at path: '" + path + "'");
            return false;
        }

        string name = Path.GetFileNameWithoutExtension(path);
        return LoadRig(name, path, out rig);
    }

    private static bool LoadRig(string name, string path, [NotNullWhen(true)] out Rig? rig)
    {
        rig = null;
        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            Console.WriteLine($"Rig file is empty at path: '{path}'");
            return false;
        }

        int count = Int.Parse(lines[0]);
        if (count == 0)
        {
            Console.WriteLine($"Rig file has no bones at path: '{path}'");
            return false;
        }

        rig = new Rig(name);
        Dictionary<string, Bone> bones = [];

        for (int i = 1; i < lines.Length; i += 5)
        {
            string boneName;
            string parentName;
            Vector3 position;
            Quaternion rotation;
            Vector3 scale;

            string[] values = lines[i].Split(' ');
            if (values.Length != 2)
            {
                Console.WriteLine("Invalid bone name at line " + i);
                return false;
            }
            boneName = values[1];

            values = lines[i + 1].Split(' ');
            if (values.Length != 2)
            {
                Console.WriteLine("Invalid parent name at line " + (i + 1));
                return false;
            }
            parentName = values[1];

            values = lines[i + 2].Split(' ');
            if (values.Length != 4)
            {
                Console.WriteLine("Invalid position at line " + (i + 2));
                return false;
            }
            position = new Vector3(
                Float.Parse(values[1].Trim(',')),
                Float.Parse(values[2].Trim(',')),
                Float.Parse(values[3].Trim(','))
            );

            values = lines[i + 3].Split(' ');
            if (values.Length != 5)
            {
                Console.WriteLine("Invalid rotation at line " + (i + 3));
                return false;
            }
            rotation = new Quaternion(
                Float.Parse(values[1].Trim(',')),
                Float.Parse(values[2].Trim(',')),
                Float.Parse(values[3].Trim(',')),
                Float.Parse(values[4].Trim(','))
            );

            values = lines[i + 4].Split(' ');
            if (values.Length != 4)
            {
                Console.WriteLine("Invalid scale at line " + (i + 4));
                return false;
            }

            try
            {
                scale = new Vector3(
                    Float.Parse(values[1].Trim(',')),
                    Float.Parse(values[2].Trim(',')),
                    Float.Parse(values[3].Trim(','))
                );
            }
            catch (Exception)
            {
                scale = Vector3.One; // Default scale if parsing fails
            }
            

            if (boneName == parentName) // This means it is the root bone
            {
                RootBone root = new RootBone(boneName);
                root.Position = position;
                root.Rotation = rotation;
                root.Scale = scale;
                rig.RootBone = root;
                bones.Add(boneName, rig.RootBone);
            }
            else
            {
                if (!bones.TryGetValue(parentName, out Bone? parent))
                {
                    Console.WriteLine("Parent bone not found: " + parentName + " at line " + (i + 1));
                    return false;
                }

                ChildBone child = new ChildBone(boneName, parent);
                child.Position = position;
                child.Rotation = rotation;
                child.Scale = scale;
                bones.Add(boneName, child);
            }
        }
        
        rig.Create();
        rig.Initialize();
        rig.RootBone.UpdateGlobalTransformation();
        return true;
    }
    #endregion
}