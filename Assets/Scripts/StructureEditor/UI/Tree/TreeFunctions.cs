using Newtonsoft.Json;
using PBG;
using PBG.MathLibrary;
using PBG.Threads;
using PBG.UI;
using static PBG.UI.Styles;

public partial class StructureNodeUI
{
    private void AnalyseTree()
    {
        int count = _treeAnalyserCount.GetInt();
        void loading(int i)
        {
            nodeManager.TreeUpdateAnalyser = true;
            nodeManager.TreeAnalyserProgress = (float)i / (float)count;
        }
        Action<Vector3i, Vector3i> finished = (a, b) => {
            _treeBoundsMinX.SetText($"{a.X}").UpdateCharacters();
            _treeBoundsMinY.SetText($"{a.Y}").UpdateCharacters();
            _treeBoundsMinZ.SetText($"{a.Z}").UpdateCharacters();
            _treeBoundsMaxX.SetText($"{b.X}").UpdateCharacters();
            _treeBoundsMaxY.SetText($"{b.Y}").UpdateCharacters();
            _treeBoundsMaxZ.SetText($"{b.Z}").UpdateCharacters();
        };
        var process = new StructureTreeBoundingBoxAnalyser((0, 0, 0), GetCurrentTreeInfo(), count, loading, finished);
        TaskPool.QueueAction(process);
    }

    private void SaveTree()
    {
        if (_treeFileName.GetTrimmedText().Length == 0)
            return;

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        var json = JsonConvert.SerializeObject(GetCurrentTreeInfo(), Formatting.Indented, settings);
        File.WriteAllText(Path.Combine(Game.CustomPath, "trees", _treeFileName.GetTrimmedText() + ".json"), json);
    }

    private void LoadTree()
    {
        var path = Path.Combine(Game.CustomPath, "trees", _treeFileName.GetTrimmedText() + ".json");
        if (!File.Exists(path))
            return;

        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        string json = File.ReadAllText(path);
        TreeGenerationInfo? data = JsonConvert.DeserializeObject<TreeGenerationInfo>(json, settings);
        if (data == null)
            return;

        SetTreeInfo(data);
        nodeManager.RegenerateTree();
    }
    
    public UIElementBase TreeSections(params UIElementBase[] sections) => 
    new UIVCol(Class(spacing_[5], w_full, top_right, spacing_[5], grow_children), Sub(sections));

    public UIElementBase TreeSection(string title, params UIElementBase[] contents) => 
    new UIVCol(Class(grow_children, w_full_minus_[10], top_center), Sub([
        new UICol(Class(h_[25], w_full), Sub([
            new UIText(title, Class(mc_[title.Length], fs_[1.2f], middle_left)),
        ])),
        ..contents,
    ]));

    public UIElementBase TreeField(string label, Vector4 data, ref UIField fieldRef) =>
    new UIVCol(Class(grow_children, w_full), Sub([
        new UICol(Class(h_[25], w_full), Sub([
            new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left))
        ])),
        new UICol(Class(h_[25], w_full, blank_sharp_g_[10]),
        OnHoldCol(c => SlideValue(c.GetElement<UIField>(), data.Y, data.Z, data.W)),
        Sub([
            newField(""+data.X, Class(middle_left, left_[7], mc_[8], fs_[1f]), OnTextChange(_ => nodeManager.TreeSettingsChanged = true), ref fieldRef)
        ]))
    ]));

    public UIElementBase TreeField(string label, Vector4 data1, Vector4 data2, ref UIField fieldRef1, ref UIField fieldRef2) =>
    new UIVCol(Class(grow_children, w_full), Sub([
        new UICol(Class(h_[25], w_full), Sub([
            new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left))
        ])),
        new UICol(Class(h_[25], w_full), Sub([
            new UICol(Class(h_[25], w_half_minus_[2], blank_sharp_g_[10]),
            OnHoldCol(c => SlideValue(c.GetElement<UIField>(), data1.Y, data1.Z, data1.W)),
            Sub([
                newField(""+data1.X, Class(middle_left, left_[7], mc_[8], fs_[1f]), OnTextChange(_ => nodeManager.TreeSettingsChanged = true), ref fieldRef1)
            ])),
            new UICol(Class(h_[25], w_half_minus_[2], blank_sharp_g_[10], top_right),
            OnHoldCol(c => SlideValue(c.GetElement<UIField>(), data2.Y, data2.Z, data2.W)),
            Sub([
                newField(""+data2.X, Class(middle_left, left_[7], mc_[8], fs_[1f]), OnTextChange(_ => nodeManager.TreeSettingsChanged = true), ref fieldRef2)
            ]))
        ]))
    ]));

    public UIElementBase TreeOptions(string label, string[] options, int selectedIndex, Action<int> onSelect)
    {
        var col = new UIVCol(Class(grow_children, w_full), Sub([
            new UICol(Class(h_[25], w_full), Sub([
                new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left))
            ]))
        ]));
        onSelect(selectedIndex);
        List<UIElementBase> rows = [];
        for (int i = 0; i < options.Length; i += 3)
        {
            List<UIElementBase> rowOptions = [];
            for (int j = i; j < i + 3 && j < options.Length; j++)
            {
                var option = new UICol(Class(w_[32f], h_[30], blank_sharp_g_[j == selectedIndex ? 40 : 30], _topAlignment[j % 3], data_["option_index", j]),
                OnClickCol(c => {
                    var oldC = col.Dataset.Get<UICol>("selected");
                    if (oldC != null && oldC != c)
                    {
                        oldC.UpdateColor((0.3f, 0.3f, 0.3f, 1f));
                        c.UpdateColor((0.4f, 0.4f, 0.4f, 1f));
                        col.Dataset["selected"] = c;
                        onSelect(c.Dataset.Int("option_index"));
                        nodeManager.TreeSettingsChanged = true;
                    }
                }),
                Sub(
                    new UIText(options[j], Class(mc_[options[j].Length], fs_[1], middle_center))
                ));
                rowOptions.Add(option);
                if (j == selectedIndex)
                {
                    col.Dataset["selected"] = option;
                }
            }
            rows.Add(new UICol(Class(h_[30], w_full, i != 0 ? top_[5] : top_[0]), [..rowOptions]));
        }
        col.AddElements(rows);
        return col;
    }

    public UIElementBase TreeToggle(string label, bool state, Action<bool> onToggle, ref UICol col)
    {
        col = new UICol(Class(h_[25], w_full), Sub(
            new UIText(label, Class(mc_[label.Length], fs_[1f], middle_left)),
            new UIImg(Class(w_[20], h_[20], top_right, right_[2.5f], blank_sharp_g_[state ? 30 : 10], data_["state", state]), OnClickImg(img =>
            {
                bool s = img.Dataset.Bool("state");
                s = !s;
                img.Dataset["state"] = s;
                img.UpdateColor(new Vector4(new Vector3(s ? 0.3f : 0.1f), 1f));
                onToggle(s);
                nodeManager.TreeSettingsChanged = true;
            }))
        ));
        return col;
    }

    private static readonly UIStyleData[] _topAlignment = [top_left, top_center, top_right];
    
    public TreeGenerationInfo GetCurrentTreeInfo()
    {
        var info = new TreeGenerationInfo()
        {
            MinX = _treeBoundsMinX.GetInt(),
            MinY = _treeBoundsMinY.GetInt(),
            MinZ = _treeBoundsMinZ.GetInt(),

            MaxX = _treeBoundsMaxX.GetInt(),
            MaxY = _treeBoundsMaxY.GetInt(),
            MaxZ = _treeBoundsMaxZ.GetInt(),

            Seed = (uint)_treeSeedField.GetInt(),
            Count = Math.Max(1, _treeTrunkCountField.GetInt()),

            HeightMin = _treeTrunkHeightMinField.GetFloat(),
            HeightMax = _treeTrunkHeightMaxField.GetFloat(),

            SplitMin = _treeTrunkSplitMinField.GetFloat(),
            SplitMax = _treeTrunkSplitMaxField.GetFloat(),

            ThicknessStart = _treeTrunkThicknessMinField.GetFloat(),
            ThicknessEnd = _treeTrunkThicknessMaxField.GetFloat(),

            TiltFactorXMin = _treeTiltFactorXMinField.GetFloat(),
            TiltFactorXMax = _treeTiltFactorXMaxField.GetFloat(),
            TiltFactorYMin = _treeTiltFactorYMinField.GetFloat(),
            TiltFactorYMax = _treeTiltFactorYMaxField.GetFloat(),

            BranchCountMin = _treeBranchCountMinField.GetInt(),
            BranchCountMax = _treeBranchCountMaxField.GetInt(),

            BranchPositionVariance = _treeBranchPositionVarianceField.GetFloat(),

            BranchLengthMin = _treeBranchLengthMinField.GetFloat(),
            BranchLengthMax = _treeBranchLengthMaxField.GetFloat(),

            BranchLengthFalloff = _treeBranchLengthFalloffField.GetFloat(),

            BranchThicknessMin = _treeBranchThicknessMinField.GetFloat(),
            BranchThicknessMax = _treeBranchThicknessMaxField.GetFloat(),

            BranchFirstTrunkMin = _treeBranchFirstTrunkMinField.GetInt(),
            BranchFirstTrunkMax = _treeBranchFirstTrunkMaxField.GetInt(),

            BranchTrunkStart = _treeBranchTrunkStartField.GetFloat(),
            BranchTrunkEnd = _treeBranchTrunkEndField.GetFloat(),

            BranchAngleMin = _treeBranchAngleMinField.GetFloat(),
            BranchAngleMax = _treeBranchAngleMaxField.GetFloat(),

            BranchTiltMin = _treeBranchTiltMinField.GetFloat(),
            BranchTiltMax = _treeBranchTiltMaxField.GetFloat(),

            // Leaves
            LeafClusterType = _leavesTypeIndex,
            LeafClusterFollowBranchDirection = _leavesFollowBranchDirection,

            LeafClusterRadiusMin = _leavesRadiusMinField.GetFloat(),
            LeafClusterRadiusMax = _leavesRadiusMaxField.GetFloat(),

            LeafClusterHeightMin = _leavesHeightMinField.GetFloat(),
            LeafClusterHeightMax = _leavesHeightMaxField.GetFloat(),

            LeafClusterPositionMin = _leavesPositionMinField.GetFloat(),
            LeafClusterPositionMax = _leavesPositionMaxField.GetFloat(),

            LeafClusterCountMin = _leavesCountMinField.GetInt(),
            LeafClusterCountMax = _leavesCountMaxField.GetInt(),

            LeafClusterDensity = _leavesDensityField.GetFloat(),

            LeafClusterFalloff = _leavesFalloffField.GetFloat(),

            LeafClusterScaleXMin = _leavesScaleXMinField.GetFloat(),    
            LeafClusterScaleXMax = _leavesScaleXMaxField.GetFloat(),    
            LeafClusterScaleYMin = _leavesScaleYMinField.GetFloat(),    
            LeafClusterScaleYMax = _leavesScaleYMaxField.GetFloat(),    
            LeafClusterScaleZMin = _leavesScaleZMinField.GetFloat(),    
            LeafClusterScaleZMax = _leavesScaleZMaxField.GetFloat()
        };

        return info;
    }

    public void SetTreeInfo(TreeGenerationInfo info)
    {
        _treeBoundsMinX.UpdateText(info.MinX.ToString());
        _treeBoundsMinY.UpdateText(info.MinY.ToString());
        _treeBoundsMinZ.UpdateText(info.MinZ.ToString());

        _treeBoundsMaxX.UpdateText(info.MaxX.ToString());
        _treeBoundsMaxY.UpdateText(info.MaxY.ToString());
        _treeBoundsMaxZ.UpdateText(info.MaxZ.ToString());



        _treeSeedField.UpdateText(info.Seed.ToString());
        _treeTrunkCountField.UpdateText(info.Count.ToString());

        _treeTrunkHeightMinField.UpdateText(info.HeightMin.ToString());
        _treeTrunkHeightMaxField.UpdateText(info.HeightMax.ToString());

        _treeTrunkSplitMinField.UpdateText(info.SplitMin.ToString());
        _treeTrunkSplitMaxField.UpdateText(info.SplitMax.ToString());

        _treeTrunkThicknessMinField.UpdateText(info.ThicknessStart.ToString());
        _treeTrunkThicknessMaxField.UpdateText(info.ThicknessEnd.ToString());

        _treeTiltFactorXMinField.UpdateText(info.TiltFactorXMin.ToString());
        _treeTiltFactorXMaxField.UpdateText(info.TiltFactorXMax.ToString());
        _treeTiltFactorYMinField.UpdateText(info.TiltFactorYMin.ToString());
        _treeTiltFactorYMaxField.UpdateText(info.TiltFactorYMax.ToString());

        _treeBranchCountMinField.UpdateText(info.BranchCountMin.ToString());
        _treeBranchCountMaxField.UpdateText(info.BranchCountMax.ToString());

        _treeBranchPositionVarianceField.UpdateText(info.BranchPositionVariance.ToString());

        _treeBranchLengthMinField.UpdateText(info.BranchLengthMin.ToString());
        _treeBranchLengthMaxField.UpdateText(info.BranchLengthMax.ToString());

        _treeBranchLengthFalloffField.UpdateText(info.BranchLengthFalloff.ToString());

        _treeBranchThicknessMinField.UpdateText(info.BranchThicknessMin.ToString());
        _treeBranchThicknessMaxField.UpdateText(info.BranchThicknessMax.ToString());

        _treeBranchFirstTrunkMinField.UpdateText(info.BranchFirstTrunkMin.ToString());
        _treeBranchFirstTrunkMaxField.UpdateText(info.BranchFirstTrunkMax.ToString());

        _treeBranchTrunkStartField.UpdateText(info.BranchTrunkStart.ToString());
        _treeBranchTrunkEndField.UpdateText(info.BranchTrunkEnd.ToString());

        _treeBranchAngleMinField.UpdateText(info.BranchAngleMin.ToString());
        _treeBranchAngleMaxField.UpdateText(info.BranchAngleMax.ToString());

        _treeBranchTiltMinField.UpdateText(info.BranchTiltMin.ToString());
        _treeBranchTiltMaxField.UpdateText(info.BranchTiltMax.ToString());

        // Leaves
        _leavesTypeIndex = info.LeafClusterType;
        _leavesFollowBranchDirection = info.LeafClusterFollowBranchDirection;
        _leavesFollowBranchDirectionButton.Dataset["state"] = !_leavesFollowBranchDirection;
        _leavesFollowBranchDirectionButton.OnClickAction();

        _leavesRadiusMinField.UpdateText(info.LeafClusterRadiusMin.ToString());
        _leavesRadiusMaxField.UpdateText(info.LeafClusterRadiusMax.ToString());

        _leavesHeightMinField.UpdateText(info.LeafClusterHeightMin.ToString());
        _leavesHeightMaxField.UpdateText(info.LeafClusterHeightMax.ToString());

        _leavesPositionMinField.UpdateText(info.LeafClusterPositionMin.ToString());
        _leavesPositionMaxField.UpdateText(info.LeafClusterPositionMax.ToString());

        _leavesCountMinField.UpdateText(info.LeafClusterCountMin.ToString());
        _leavesCountMaxField.UpdateText(info.LeafClusterCountMax.ToString());

        _leavesDensityField.UpdateText(info.LeafClusterDensity.ToString());

        _leavesFalloffField.UpdateText(info.LeafClusterFalloff.ToString());

        _leavesScaleXMinField.UpdateText(info.LeafClusterScaleXMin.ToString());
        _leavesScaleXMaxField.UpdateText(info.LeafClusterScaleXMax.ToString());
        _leavesScaleYMinField.UpdateText(info.LeafClusterScaleYMin.ToString());
        _leavesScaleYMaxField.UpdateText(info.LeafClusterScaleYMax.ToString());
        _leavesScaleZMinField.UpdateText(info.LeafClusterScaleZMin.ToString());
        _leavesScaleZMaxField.UpdateText(info.LeafClusterScaleZMax.ToString());
    }
}