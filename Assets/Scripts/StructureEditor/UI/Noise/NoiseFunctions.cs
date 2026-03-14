using PBG;
using PBG.MathLibrary;
using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private void NoiseBasic()
    {
        var text = _noiseNodesPanel.GetElement<UICol>()?.GetElement<UIText>();
        if (text == null) return;
        if (text.GetTrimmedText() != "Basic")
        {
            text.UpdateText("Basic");
            nodeManager.NodeType = "Basic";
            RegenerateNodeList();
            NodeManager.NodeEditorType = NodeEditorType.Node;
            NodeManager.Clear();
        }
    }

    private void NoiseGroup()
    {
        var text = _noiseNodesPanel.GetElement<UICol>()?.GetElement<UIText>();
        if (text == null) return;
        if (text.GetTrimmedText() != "Group")
        {
            text.UpdateText("Group");
            nodeManager.NodeType = "Group";
            RegenerateGroupList();
            NodeManager.NodeEditorType = NodeEditorType.Group;
            NodeManager.Clear();
            var inputNode = new GroupInputNode(null, NodeManager.NodeCollection, (0, 100), [], []);
            var outputNode = new GroupOutputNode(NodeManager.NodeCollection, (800, 100), []);
            NodeManager.AddNode(inputNode);
            NodeManager.AddNode(outputNode);
        }
    }

    private void NoiseSave()
    {
        if (nodeManager.NodeType == "Basic")
        {
            int oldFileCount = NodeManager.GetCurrentNodeCount();
            NodeManager.Save();
            int newFileCount = NodeManager.GetCurrentNodeCount();
            if (newFileCount != oldFileCount)
            {
                RegenerateNodeList();
            }
        }
        else if (nodeManager.NodeType == "Group")
        {
            int oldFileCount = NodeManager.GetCurrentGroupCount();
            NodeManager.SaveGroup();
            int newFileCount = NodeManager.GetCurrentGroupCount();
            if (newFileCount != oldFileCount)
            {
                RegenerateGroupList();
                nodeManager.NodeSelector.RegenerateGroupList();
            }
        }
    }

    private void NoiseLoad()
    {
        if (nodeManager.NodeType == "Basic")
        {
            NodeManager.Load();
        }
        else if (nodeManager.NodeType == "Group")
        {
            NodeManager.LoadGroup();
        }
    }


    private UIElementBase[] GenerateBasicElements() => GenerateElements(Game.MainPath / "custom" / "nodes", "basic");
    private UIElementBase[] GenerateGroupElements() => GenerateElements(Game.MainPath / "custom" / "groups", "group");

    private UIElementBase[] GenerateElements(string folderPath, string type)
    {
        List<UIElementBase> fileCollections = [];
        var files = Directory.GetFiles(folderPath);

        foreach (var file in files)
        {
            if (Path.GetExtension(file) == ".json")
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                fileCollections.Add(
                    new UICol("file-element", Class(left_[5], top_[5], w_full_minus_[10], h_[30], blank_sharp_g_[20]),
                    OnClickCol(type == "group" ? _=>LoadGroup(fileName) : _=>LoadBasic(fileName)), Sub([
                        new UIText(fileName.Length > 25 ? fileName[..25] : fileName, Class(middle_left, left_[5], mc_[Mathf.Min(fileName.Length, 25)], fs_[1])),
                        new UIText("X", Class(middle_right, right_[5], mc_[1], fs_[1.2f]), OnClickText(_ => NodeManager.DeleteFile(file)))
                    ]))
                );
            }
        }
        return [.. fileCollections];
    }

    private void LoadBasic(string fileName) { NodeManager.SetName(fileName); NodeManager.Load(); }
    private void LoadGroup(string fileName) { NodeManager.SetName(fileName); NodeManager.LoadGroup(); }
}