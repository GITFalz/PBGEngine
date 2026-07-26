using System.Numerics;
using PBG.Core;
using PBG.Data;
using PBG.UI;
using PBG.Voxel;
using static PBG.UI.Styles;

public class BitMaskDebugger : ScriptingNode
{
    public UIController Controller;
    public VoxelRenderer World;

    private UIVCol _textWindow;

    private UIText _xText;
    private UIText _yText;
    private UIText _zText;

    private float _oldX = 0;
    private float _oldY = 0;
    private float _oldZ = 0;

    private UIText _xTextBlock;
    private UIText _yTextBlock;
    private UIText _zTextBlock;

    private float _oldXBlock = 0;
    private float _oldYBlock = 0;
    private float _oldZBlock = 0;


    private UIText _bitMaskA;
    private UIText _bitMaskB;
    private UIText _bitMaskC;

    private UIText _bitMaskD;
    private UIText _bitMaskE;
    private UIText _bitMaskF;

    private UIText _bitMaskG;
    private UIText _bitMaskH;
    private UIText _bitMaskI;

    

    void Start()
    {
        Controller = Transform.GetComponent<UIController>();
        World = Transform.GetComponent<VoxelRenderer>();

        Controller.AddElement(GetUI());
    }

    void Update()
    {
        bool raycast = VoxelData.Raycast(World, Camera.Position, Camera.front, 100, out Hit hit);
        if (raycast)
        {
            if (Input.IsKeyPressed(Key.K))
            {
                Console.WriteLine(hit.Block.Definition().Name);
                var chunkPos = hit.BlockPosition.ToChunkRelative();
                var blockPos = hit.BlockPosition.ToRelative();

                if (VoxelRenderer.DebugAOMasks.TryGetValue(chunkPos, out var array))
                {
                    var index = ChunkBlocks.GetIndex(blockPos);
                    var mask = array[index];
                    
                    var nums = BitsToGrid(mask);

                    _bitMaskA.UpdateText(nums[0]);
                    _bitMaskB.UpdateText(nums[1]);
                    _bitMaskC.UpdateText(nums[2]);

                    _bitMaskD.UpdateText(nums[3]);
                    _bitMaskE.UpdateText(nums[4]);
                    _bitMaskF.UpdateText(nums[5]);

                    _bitMaskG.UpdateText(nums[6]);
                    _bitMaskH.UpdateText(nums[7]);
                    _bitMaskI.UpdateText(nums[8]);

                    if (_oldXBlock != blockPos.X)
                    {
                        _xTextBlock.UpdateText("Block X: " + blockPos.X);
                        _oldXBlock =  blockPos.X;
                    }

                    if (_oldYBlock != blockPos.Y)
                    {
                        _yTextBlock.UpdateText("Block Y: " + blockPos.Y);
                        _oldYBlock =  blockPos.Y;
                    }

                    if (_oldZBlock != blockPos.Z)
                    {
                        _zTextBlock.UpdateText("Block Z: " + blockPos.Z);
                        _oldZBlock =  blockPos.Z;
                    }
                }
            }
        }

        if (Input.IsKeyPressed(Key.P))
        {
            _textWindow.SetVisible(!_textWindow.Visible);
        }

        if (_textWindow.Visible)
        {
            if (_oldX != Camera.Position.X)
            {
                _xText.UpdateText("X: " + Camera.Position.X);
                _oldX = Camera.Position.X;
            }

            if (_oldY != Camera.Position.Y)
            {
                _yText.UpdateText("Y: " + Camera.Position.Y);
                _oldY = Camera.Position.Y;
            }

            if (_oldZ != Camera.Position.Z)
            {
                _zText.UpdateText("Z: " + Camera.Position.Z);
                _oldZ = Camera.Position.Z;
            }
        }
    }

    UIElementBase GetUI() =>
    new UIVCol(light_sharp_g_[20], bottom_right, grow_children).Out(out _textWindow)[
        new UIButton(w_full, h_[20], light_sharp_g_[30]).OnHold(MoveInfo),
        new UIVCol(grow_children, spacing_[10], border_[20, 20, 20, 20])[
            new UIText("X: 0", mc_[10]).Out(out _xText),
            new UIText("Y: 0", mc_[10]).Out(out _yText),
            new UIText("Z: 0", mc_[10]).Out(out _zText),
            new UIText("Block X: 0", mc_[16]).Out(out _xTextBlock),
            new UIText("Block Y: 0", mc_[16]).Out(out _yTextBlock),
            new UIText("Block Z: 0", mc_[16]).Out(out _zTextBlock),
            new UIText("000", padding_top_[10]).Out(out _bitMaskA),
            new UIText("000").Out(out _bitMaskB),
            new UIText("000").Out(out _bitMaskC),
            new UIText("000", padding_top_[10]).Out(out _bitMaskD),
            new UIText("000").Out(out _bitMaskE),
            new UIText("000").Out(out _bitMaskF),
            new UIText("000", padding_top_[10]).Out(out _bitMaskG),
            new UIText("000").Out(out _bitMaskH),
            new UIText("000").Out(out _bitMaskI)
        ]
    ];

    public static string[] BitsToGrid(uint mask)
    {
        string[] lines = new string[9];
        int li = 0;
        for (int y = 2; y >= 0; y--)
        {
            for (int z = 2; z >= 0; z--)
            {
                string row = "";
                for (int x = 2; x >= 0; x--)
                {
                    int bit = y * 9 + z * 3 + x;
                    row += (mask >> bit) & 1;
                }
                lines[li++] = row;
            }
        }
        return lines;
    }

    void MoveInfo(UIButton _)
    {
        if (Input.MouseDelta == Vector2.Zero)
            return;

        _textWindow.BaseOffset += Input.MouseDelta;
        _textWindow.ApplyChanges(UIChange.Transform);
    }
}