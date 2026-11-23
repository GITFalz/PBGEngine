using PBG.Nodes;
using PBG.Core;
using PBG.UI;

public class UITestScene : Scene
{
    public UITestScene() : base("UITest") { }

    public override void Load()
    {
        var uiTest = new UITest();
        var controller = new UIController("UI Test");

        var mainNode = NewInternalNode("Root");

        mainNode.AddComponent(uiTest, controller);
    }
}