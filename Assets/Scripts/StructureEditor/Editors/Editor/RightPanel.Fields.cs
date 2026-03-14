using PBG.MathLibrary;
using PBG.UI;

public partial class StructureEditor
{
    public partial class RightPanel
    {
        public UICol BlockSelectionPanel = null!;
        public UIImg CurrentBlockImg = null!;
        public UIVScroll BlockCollection = null!;

        private UIField SizeXField = null!;
        private UIField SizeYField = null!;
        private UIField SizeZField = null!;

        private UIField PositionXField = null!;
        private UIField PositionYField = null!;
        private UIField PositionZField = null!;

        private UIVScroll BoundingBoxPanel = null!;
        private UIVScroll ExtendersPanel = null!;
        private UIVScroll ConnectionPointsPanel = null!;
        private UIVScroll CategoriesPanel = null!;
        private UIVScroll AvoidPanel = null!;
        private UIVScroll RulesetPointsPanel = null!;

        private UIVCol SettingsSection = null!;
        private UIVCol BoundingBoxSection = null!;
        private UIVCol ExtendersSection = null!;
        private UIVCol ConnectionPointSection = null!;
        private UIVCol RulesetPointSection = null!;

        private UIVCol? PreviousSection;

        public int SizeX
        {
            get => SizeXField.GetInt(1);
            set => SizeXField.UpdateText(value.ToString());
        }
        public int SizeY
        {
            get => SizeYField.GetInt(1);
            set => SizeYField.UpdateText(value.ToString());
        }
        public int SizeZ
        {
            get => SizeZField.GetInt(1);
            set => SizeZField.UpdateText(value.ToString());
        }
        public Vector3i Size
        {
            get => (SizeX, SizeY, SizeZ);
            set { SizeX = value.X; SizeY = value.Y; SizeZ = value.Z; }
        }

        public int PositionX
        {
            get => PositionXField.GetInt(0);
            set => PositionXField.UpdateText(value.ToString());
        }
        public int PositionY
        {
            get => PositionYField.GetInt(0);
            set => PositionYField.UpdateText(value.ToString());
        }
        public int PositionZ
        {
            get => PositionZField.GetInt(0);
            set => PositionZField.UpdateText(value.ToString());
        }
        public Vector3i Position
        {
            get => (PositionX, PositionY, PositionZ);
            set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; }
        }


        private UIField ExtenderSizeXField = null!;
        private UIField ExtenderSizeZField = null!;

        private UIField ExtenderPositionXField = null!;
        private UIField ExtenderPositionYField = null!;
        private UIField ExtenderPositionZField = null!;


        public int ExtenderSizeX
        {
            get => ExtenderSizeXField.GetInt(1);
            set => ExtenderSizeXField.UpdateText(value.ToString());
        }
        public int ExtenderSizeZ
        {
            get => ExtenderSizeZField.GetInt(1);
            set => ExtenderSizeZField.UpdateText(value.ToString());
        }
        public Vector3i ExtenderSize
        {
            get => (ExtenderSizeX, 0, ExtenderSizeZ);
            set { ExtenderSizeX = value.X; ExtenderSizeZ = value.Z; }
        }

        public int ExtenderPositionX
        {
            get => ExtenderPositionXField.GetInt(0);
            set => ExtenderPositionXField.UpdateText(value.ToString());
        }
        public int ExtenderPositionY
        {
            get => ExtenderPositionYField.GetInt(0);
            set => ExtenderPositionYField.UpdateText(value.ToString());
        }
        public int ExtenderPositionZ
        {
            get => ExtenderPositionZField.GetInt(0);
            set => ExtenderPositionZField.UpdateText(value.ToString());
        }
        public Vector3i ExtenderPosition
        {
            get => (ExtenderPositionX, ExtenderPositionY, ExtenderPositionZ);
            set { ExtenderPositionX = value.X; ExtenderPositionY = value.Y; ExtenderPositionZ = value.Z; }
        }


        private UIField ConnectionPositionXField = null!;
        private UIField ConnectionPositionYField = null!;
        private UIField ConnectionPositionZField = null!;

        public float ConnectionPositionX
        {
            get => ConnectionPositionXField.GetFloat(0);
            set => ConnectionPositionXField.UpdateText(value.ToString());
        }
        public float ConnectionPositionY
        {
            get => ConnectionPositionYField.GetFloat(0);
            set => ConnectionPositionYField.UpdateText(value.ToString());
        }
        public float ConnectionPositionZ
        {
            get => ConnectionPositionZField.GetFloat(0);
            set => ConnectionPositionZField.UpdateText(value.ToString());
        }
        public Vector3 ConnectionPosition
        {
            get => (ConnectionPositionX, ConnectionPositionY, ConnectionPositionZ);
            set { ConnectionPositionX = value.X; ConnectionPositionY = value.Y; ConnectionPositionZ = value.Z; }
        }


        private UIField RulesetPositionXField = null!;
        private UIField RulesetPositionYField = null!;
        private UIField RulesetPositionZField = null!;

        public float RulesetPositionX
        {
            get => RulesetPositionXField.GetFloat(0);
            set => RulesetPositionXField.UpdateText(value.ToString());
        }
        public float RulesetPositionY
        {
            get => RulesetPositionYField.GetFloat(0);
            set => RulesetPositionYField.UpdateText(value.ToString());
        }
        public float RulesetPositionZ
        {
            get => RulesetPositionZField.GetFloat(0);
            set => RulesetPositionZField.UpdateText(value.ToString());
        }
        public Vector3 RulesetPosition
        {
            get => (RulesetPositionX, RulesetPositionY, RulesetPositionZ);
            set { RulesetPositionX = value.X; RulesetPositionY = value.Y; RulesetPositionZ = value.Z; }
        }

        public StructureBoundingBox? SelectedBoundingBox = null;
        public StructureExtender? SelectedExtender = null;
        public ConnectionPoint? SelectedConnection = null;
        public RulesetPoint? SelectedRuleset = null;
    }
}