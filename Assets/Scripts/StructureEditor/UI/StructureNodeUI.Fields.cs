using PBG.UI;

public partial class StructureNodeUI
{
    public List<UIElementBase> TreeElements;
    public List<UIElementBase> NoiseElements;
    public List<UIElementBase> StructureElements;

    public UIVCol _noiseNodesPanel = null!;
    public UIVScroll _sidePanelFileList = null!;

    // Tree
    public UIVCol _leftTreePanel = null!;
    public UIVScroll _rightTreePanel = null!;
    public UICol _centerPanel = null!;

    // Noise
    public UIVCol _leftNoiseSection = null!;
    public UICol _rightNoisePanel = null!;
    public UIVCol _noisePaletteCollection = null!;
    public UIVScroll _noisePaletteBlockSelection = null!;

    // Structure
    public UIElementBase _leftStructurePanel = null!;
    public UIElementBase _rightStructurePanel = null!;


    public static UIField _groupInputName = null!;
    public static UIVCol _groupInputSettings = null!;
    public static UICol? CurrentGroupInputType = null;
    public static UICol _groupFloatButton = null!;
    public static UICol _groupIntButton = null!;
    public static UICol _grouPBGector2Button = null!;
    public static UICol _grouPBGector2iButton = null!;
    public static UICol _grouPBGector3Button = null!;
    public static UICol _grouPBGector3iButton = null!;
    public static UICol _grouPBGalueIndex0 = null!;
    public static UICol _grouPBGalueIndex1 = null!;
    public static UICol _grouPBGalueIndex2 = null!;




    // Point node position
    public UIVCol _pointNodePaletteCollection = null!;
    public UIVScroll _pointNodePaletteBlockSelection = null!;

    public UIText _fpsText = null!;
    public UIText _ramText = null!;

    // Base
    private UIField _treeSeedField = null!;
    
    // Trunk
    private UIField _treeTrunkCountField = null!;
    private UIField _treeTrunkHeightMinField = null!;
    private UIField _treeTrunkHeightMaxField = null!;
    private UIField _treeTrunkSplitMinField = null!;
    private UIField _treeTrunkSplitMaxField = null!;
    private UIField _treeTrunkThicknessMinField = null!;
    private UIField _treeTrunkThicknessMaxField = null!;

    // Tilt
    private UIField _treeTiltFactorXMinField = null!;
    private UIField _treeTiltFactorXMaxField = null!;
    private UIField _treeTiltFactorYMinField = null!;
    private UIField _treeTiltFactorYMaxField = null!;

    // Branches
    private UIField _treeBranchCountMinField = null!;
    private UIField _treeBranchCountMaxField = null!;
    private UIField _treeBranchPositionVarianceField = null!;
    private UIField _treeBranchLengthMinField = null!;
    private UIField _treeBranchLengthMaxField = null!;
    private UIField _treeBranchLengthFalloffField = null!;
    private UIField _treeBranchThicknessMinField = null!;
    private UIField _treeBranchThicknessMaxField = null!;
    private UIField _treeBranchFirstTrunkMinField = null!;
    private UIField _treeBranchFirstTrunkMaxField = null!;
    private UIField _treeBranchTrunkStartField = null!;
    private UIField _treeBranchTrunkEndField = null!;
    private UIField _treeBranchAngleMinField = null!;
    private UIField _treeBranchAngleMaxField = null!;
    private UIField _treeBranchTiltMinField = null!;
    private UIField _treeBranchTiltMaxField = null!;

    // Leaves
    private int _leavesTypeIndex = 0;
    private bool _leavesFollowBranchDirection = false;
    private UICol _leavesFollowBranchDirectionButton = null!;
    private UIField _leavesRadiusMinField = null!;
    private UIField _leavesRadiusMaxField = null!;
    private UIField _leavesHeightMinField = null!;
    private UIField _leavesHeightMaxField = null!;
    private UIField _leavesPositionMinField = null!;
    private UIField _leavesPositionMaxField = null!;
    private UIField _leavesCountMinField = null!;
    private UIField _leavesCountMaxField = null!;
    private UIField _leavesDensityField = null!;
    private UIField _leavesFalloffField = null!;
    private UIField _leavesScaleXMinField = null!;
    private UIField _leavesScaleXMaxField = null!;
    private UIField _leavesScaleYMinField = null!;
    private UIField _leavesScaleYMaxField = null!;
    private UIField _leavesScaleZMinField = null!;
    private UIField _leavesScaleZMaxField = null!;

    // Analyser
    private UIField _treeAnalyserCount = null!;
    public UIImg TreeAnalyserLoadingBar = null!;

    private UIField _treeBoundsMinX = null!;
    private UIField _treeBoundsMinY = null!;
    private UIField _treeBoundsMinZ = null!;

    private UIField _treeBoundsMaxX = null!;
    private UIField _treeBoundsMaxY = null!;
    private UIField _treeBoundsMaxZ = null!;

    private UIField _treeFileName = null!;
}