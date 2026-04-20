using PBG.UI.Creator;
using PBG.UI;
using PBG.Core;
using static PBG.UI.Styles;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.MathLibrary;
using PBG.Data;
using PBG;
using PBG.Threads;
using Newtonsoft.Json;

public partial class StructureNodeUI(StructureNodeManager nodeManager) : UIScript
{
    public override void PreScript()
    {
        _rightStructurePanel = nodeManager.StructureEditor.GetRightPanel();
        _leftStructurePanel = nodeManager.StructureEditor.GetLeftPanel();
    }

    public override void AfterScript()
    {
        TreeElements = [_leftTreePanel, _centerPanel, _rightTreePanel];
        NoiseElements = [_leftNoiseSection, _rightNoisePanel];
        StructureElements = [_leftStructurePanel, _rightStructurePanel, _centerPanel];
    }


    public override UIElementBase Script() =>
    new UICol(w_full, h_full)[
        NavigationBar(),
        LeftPanel(),
        CenterPanel(),
        RightPanel()
    ];

    public void SetName(UIField field) => NodeManager.FileName = field.GetTrimmedText();
    public void Update()
    {
        if (GameTime.FpsUpdated)
        {
            _fpsText.UpdateText("fps: " + GameTime.Fps);
            _ramText.UpdateText("ram: " + GameTime.Ram / (1024 * 1024) + " Mb");
        }
    }

    public void SlideValue(UIField? field, float min, float max, float increment)
    {
        float delta = Input.GetMouseDelta().X;
        if (delta == 0 || field == null)
        {
            return;
        }
        float value = field.GetFloat();
        float oldValue = value;
        value += increment * delta;
        value = Mathf.Clampy(value, min, max);

        if (value != oldValue)
        {
            field.SetText($"{value}").UpdateCharacters();
            nodeManager.TreeSettingsChanged = true;
        } 
    }

    public void RegenerateGroupList()
    {
        _sidePanelFileList.DeleteChildren();
        var fileElements = GenerateGroupElements();
        _sidePanelFileList.AddElements(fileElements);
        UIController.AddElements(fileElements);
    }

    public void RegenerateNodeList()
    {
        _sidePanelFileList.DeleteChildren();
        var fileElements = GenerateBasicElements();
        _sidePanelFileList.AddElements(fileElements);
        UIController.AddElements(fileElements);
    }

    public static void ResetGroupInputValues(string type, int count, float[] values)
    {
        CurrentGroupInputType?.UpdateColor((0.4f, 0.4f, 0.4f, 1f));
        CurrentGroupInputType = type switch
        {
            "float" => _groupFloatButton,
            "int" => _groupIntButton,
            "vec2" => _grouPBGector2Button,
            "ivec2" => _grouPBGector2iButton,
            "vec3" => _grouPBGector3Button,
            "ivec3" => _grouPBGector3iButton,
            _ => _groupFloatButton
        };
        CurrentGroupInputType?.UpdateColor((0.5f, 0.5f, 0.5f, 1f));
        _grouPBGalueIndex0.SetVisible(count >= 1);
        _grouPBGalueIndex1.SetVisible(count >= 2);
        _grouPBGalueIndex2.SetVisible(count >= 3);
        if (count >= 1) _grouPBGalueIndex0.GetElement<UIField>()?.UpdateText(values[0]+"");
        if (count >= 2) _grouPBGalueIndex1.GetElement<UIField>()?.UpdateText(values[1]+"");
        if (count >= 3) _grouPBGalueIndex2.GetElement<UIField>()?.UpdateText(values[2]+"");
        _groupInputSettings.ApplyChanges(UIChange.Scale);
    }
}