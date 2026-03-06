using System.Diagnostics.CodeAnalysis;
using PBG;
using PBG.Data;
using PBG.UI;
using Silk.NET.Input;

public static class ModelManager
{
    public static List<Model> ModelList = [];
    public static Dictionary<string, Model> Models = [];

    public static Model? SelectedModel = null;
    public static List<Model> SelectedModels = [];

    public static void Update()
    {
        if (Input.IsMousePressed(MouseButton.Left))
        {
            if (SelectedModel != null)
            {
                SelectedModel.IsSelected = false;
                SelectedModel.SelectedVertices.Clear();
                SelectedModel.GenerateVertexColor();
            }
            else
            {

            }
        }

        for (int i = 0; i < ModelList.Count; i++)
        {
            var model = ModelList[i];
            if (model.IsShown)
            {
                model.Update();
            }
        }
    }

    public static void Render()
    {
        /*
        GL.CullFace(TriangleFace.Back);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        for (int i = 0; i < ModelList.Count; i++)
        {
            var model = ModelList[i];
            if (model.IsShown)
            {
                model.Render();
            }
        }

        for (int i = 0; i < ModelList.Count; i++)
        {
            var model = ModelList[i];
            if (model.IsShown && model.ShowWireframe)
            {
                model.RenderWireframe();
            }
        }
        
        GL.CullFace(TriangleFace.Back);
        GL.DepthFunc(DepthFunction.Lequal);
        */
    }

    public static bool LoadModel(string name, [NotNullWhen(true)] out Model? model)
    {
        model = null;
        string fileName = name;
        if (fileName.Length == 0)
        {
            Console.WriteLine("Please enter a valid model name.");
            return false;
        }

        string folderPath = Path.Combine(Game.UndoModelPath, fileName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string newFile = fileName;

        while (Models.ContainsKey(newFile))
        {
            newFile = $"{newFile}_{Models.Count}";
        }

        model = new(GeneralModelingEditor.Instance);
        if (!model.LoadModel(name))
        {
            model = null;
            Console.WriteLine("[Error] : Failed to load model " + name);
            return false;
        }

        model.Name = newFile;
        Models.Add(newFile, model);
        ModelList.Add(model);
        model.UpdateVertexPosition();
        return true;
    }

    public static bool LoadModelFromPath(string path) => LoadModelFromPath(path, out _);

    public static bool LoadModelFromPath(string path, [NotNullWhen(true)] out Model? model)
    {
        model = null;
        if (!File.Exists(path))
        {
            //PopUp.AddPopUp("File does not exist.");
            return false;
        }

        string fileName = Path.GetFileNameWithoutExtension(path);
        string newFile = fileName;

        int i = 1;
        while (Models.ContainsKey(newFile))
        {
            newFile = $"{fileName}_{i}";
            i++;
        }

        var m = new Model(GeneralModelingEditor.Instance);
        if (!m.LoadModelFromPath(path))
            return false;

        model = m;
        model.Name = newFile;
        Models.Add(newFile, model);
        ModelList.Add(model);
        model.UpdateVertexPosition();
        return true;
    }

    public static void Select(Model model)
    {
        if (!SelectedModels.Contains(model))
        {
            SelectedModels.Add(model);
        }

        if (SelectedModel != null)
        {

            SelectedModel.SelectedVertices.Clear();
            SelectedModel.GenerateVertexColor();
        }

        SelectedModel = model;
        if (SelectedModel != null)
        {
            SelectedModel.IsSelected = true;
            SelectedModel.UpdateVertexPosition();
        }
    }

    public static bool UnSelect(Model model)
    {
        model.IsSelected = false;
        SelectedModels.Remove(model);

        if (SelectedModel == model)
        {
            SelectedModel.SelectedVertices.Clear();
            SelectedModel.GenerateVertexColor();
            SelectedModel = null;
            return true;
        }
        return false;
    }

    public static void DeleteModel(Model model)
    {
        Models.Remove(model.Name);
        ModelList.Remove(model);
        if (SelectedModel == model)
        {
            SelectedModel = null;
        }
    }

    public static void Unload()
    {
        for (int i = 0; i < ModelList.Count; i++)
        {
            var model = ModelList[i];
            model.Unload();
        }
    }

    public static void Delete()
    {
        List<Model> copy = [.. ModelList];
        for (int i = 0; i < copy.Count; i++)
        {
            var model = copy[i];
            model.Delete();
        }
    }
}