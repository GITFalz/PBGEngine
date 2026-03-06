using PBG.Core;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.FileManager;

public class ModelingEditorScene : Scene
{
    public ModelingEditorScene() : base("ModelingEditor") { }

    public override void Load()
    {
        //var skybox = new Skybox();
        var fileManager = new FileManager();
        var colorPicker = new ColorPicker(300, 200, (100, 100));
        var modelingEditor = new GeneralModelingEditor(fileManager);

        var mainNode = NewInternalNode("Root");
        //mainNode.AddComponent(skybox);

        var mainUINode = mainNode.AddChild("MainUI");
        mainUINode.AddComponent(new UIController("Modeling"));

        var timelineUINode = mainNode.AddChild("TimelineUI");
        timelineUINode.AddComponent(new UIController("Timeline"));

        var modelingNode = mainNode.AddChild("Modeling");
        modelingNode.AddComponent(modelingEditor, new Viewport());

        mainNode.AddChild("Models");
        mainNode.AddChild("Viewport").AddComponent(new Viewport());

        var colorPickerNode = mainNode.AddChild("ColorPicker");
        colorPickerNode.AddComponent(new UIController("Color Picker"), colorPicker);

        var fileManagerNode = mainNode.AddChild("FileManager");
        fileManagerNode.AddComponent(new UIController("FileManager"), fileManager);
    }
}