using PBG.Core;
using PBG.UI;
using PBG.Editor;

public class EditorScene : Scene
{
    public EditorScene() : base("Editor") { }

    public override void Load()
    {
        var mainNode = NewInternalNode("Root");

        var controller = new UIController("Main Menu");
        var editorMenu = new EditorMenu();

        mainNode.AddComponent(controller, editorMenu);
    }
}