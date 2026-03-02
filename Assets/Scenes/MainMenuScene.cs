using PBG.Core;
using PBG.UI;

public class MainMenuScene : Scene
{
    public MainMenuScene() : base("MainMenu") { }

    public override void Load()
    {
        var mainMenu = new MainMenu();
        var controller = new UIController("Main Menu");

        var mainNode = NewInternalNode("Root");

        mainNode.AddComponent(mainMenu, controller);
    }
}