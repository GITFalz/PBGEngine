using System.Diagnostics.CodeAnalysis;

public class AnimationManager
{
    public static Dictionary<string, NewAnimation> Animations = [];
    public static bool DisplayError = true;
 
    public static bool Add(NewAnimation animation)
    {
        if (Animations.ContainsKey(animation.Name))
        {
            //if(DisplayError) //PopUp.AddPopUp("Animation already exists");
            return false;
        }

        Animations.Add(animation.Name, animation);
        //if(DisplayError) //PopUp.AddPopUp("Animation added");
        return true;
    }

    public static void Update(NewAnimation animation)
    {
        if (Animations.ContainsKey(animation.Name))
        {
            Animations[animation.Name] = animation;
        }
        else
        {
            //PopUp.AddPopUp("Animation not found");
        }
    }

    public static bool Remove(string name)
    {
        if (!Animations.ContainsKey(name))
        {
            //if(DisplayError) //PopUp.AddPopUp("Animation not found");
            return false;
        }

        Animations.Remove(name);
        //if(DisplayError) //PopUp.AddPopUp("Animation removed");
        return true;
    }

    public static bool TryGet(string name, [NotNullWhen(true)] out NewAnimation? animation)
    {
        if (Animations.TryGetValue(name, out animation))
            return true;

        //if(DisplayError) //PopUp.AddPopUp("Animation not found");
        return false;
    }

    public static bool ChangeName(string oldName, string newName)
    {
        if (!Animations.ContainsKey(oldName))
        {
            //if(DisplayError) //PopUp.AddPopUp("Old Animation name not found");
            return false;
        }

        if (Animations.ContainsKey(newName))
        {
            //if(DisplayError) //PopUp.AddPopUp("Animation name already exists");
            return false;
        }

        Animations.Add(newName, Animations[oldName]);
        Animations.Remove(oldName);
        //if(DisplayError) //PopUp.AddPopUp("Animation name changed");
        return true;
    }

    #region Save
    public static void Save(string name)
    {
        //Save(name, Game.animationPath);
    }

    public static void Save(string name, string path)
    {
        path = Path.Combine(path, name + ".anim");
        if (!Animations.TryGetValue(name, out NewAnimation? animation))
        {
            //if(DisplayError)
                //PopUp.AddPopUp("Failed to save animation: Animation not found");
            return;
        }

        if (File.Exists(path))
        {
            //PopUp.AddConfirmation("Overwrite Animation?", () => SaveRig(animation, path), () => { });
            return;      
        }

        SaveRig(animation, path);
    }

    private static void SaveRig(NewAnimation animation, string path)
    {
        var lines = animation.Save();
        File.WriteAllLines(path, lines);
        //if(DisplayError)
            //PopUp.AddPopUp("Animation saved");
    }
    #endregion

    #region Load
    public static bool Load(string name)
    {
        return false; //return Load(name, Game.animationPath);
    }

    public static bool Load(string name, string path)
    {
        if (Animations.ContainsKey(name)) // Quietly ignore if the rig already exists
        {
            Remove(name);
        }

        path = Path.Combine(path, name + ".anim");
        if (!File.Exists(path))
        {
            //if(DisplayError)
                //PopUp.AddPopUp("Animation does not exist");
            return false;
        }

        if (!LoadAnimation(name, path, out NewAnimation? animation))
        {
            //if(DisplayError)
                //PopUp.AddPopUp("Animation failed to load");
            return false;
        }

        Add(animation);
        //if(DisplayError)
            //PopUp.AddPopUp("Animation loaded");
        return true;
    }

    private static bool LoadAnimation(string name, string path, [NotNullWhen(true)] out NewAnimation? animation)
    {
        if (!AnimationParser.Parse(name, File.ReadAllLines(path), out animation))
        {
            animation = null;
            return false;
        }
        return true;
    }
    #endregion
}