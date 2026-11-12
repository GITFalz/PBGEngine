using Newtonsoft.Json;
using PBG.Compilation;
using PBG.Core;
using PBG.Graphics;
using PBG.Helper;
using PBG.MathLibrary;
using PBG.Pipes;
using PBG.UI;

namespace PBG.Editor;

public class EditorMenu : ScriptingNode
{
    public static EditorMenu Instance = null!;
    public UIController _controller = null!;
    public bool Run = false;
    public Descriptor[] Descriptors = [];
    public Texture[] EngineTextures = [];

    private UIElementBase _ui;
    private UICol _engineTexture;
    private UIText _engineLoading;

    private uint _frameSerialAtTextureSet;

    public UIVScroll HierarchyScroll = null!;

    public static int GameWidth => (int)(Instance?._engineTexture?.SizeX ?? GameWindow.Width);
    public static int GameHeight => (int)(Instance?._engineTexture?.SizeY ?? GameWindow.Height);
    public static int GameOffsetX => (int)(Instance?._engineTexture?.Origin.X ?? 0);
    public static int GameOffsetY => (int)(Instance?._engineTexture?.Origin.Y ?? 0);

    public EditorMenu()
    {
        Instance = this;
    }

    void Start()
    {
        Console.WriteLine("Created ui");
        _controller = Transform.GetComponent<UIController>();

        _ui = GetUI();
        _controller.AddElement(_ui);
    } 

    void Update()
    {
        if (GameWindow.EngineLoadingProgress != null)
        {
            var progress = (GameWindow.EngineLoadingProgress.Value * 100).floorToInt();
            _engineLoading.UpdateText($"Progress: {progress}%");
        }
    }

    void Render()
    {
        if (!Run)
            return;

        uint frameSerial = unchecked((uint)Game.FrameIndex.Value);
        if (frameSerial == 0 || frameSerial == _frameSerialAtTextureSet || Descriptors.Length == 0)
            return;

        int frameIndex = (int)((frameSerial - 1) % (uint)Descriptors.Length);
        GFX.RequestSharedImageFrame(frameSerial);

        UIController.BindFramebuffer();

        ImageShader.Shader.Bind();

        var descriptor = Descriptors[frameIndex];

        descriptor.Bind();
        
        descriptor.UniformMatrix4(ImageShader.Model, Matrix4.CreateTranslation((_engineTexture.Origin.X, _engineTexture.Origin.Y, 0.1f)));
        descriptor.UniformMatrix4(ImageShader.Projection, _controller.GetProjection());
        descriptor.Uniform2(ImageShader.Size, _engineTexture.Size);

        GFX.Draw(6, 1, 0, 0);

        UIController.UnbindFramebuffer();
    }

    public void SetEditorTextures(int width, int height, Texture[] textures)
    {
        GFX.WaitIdle();
        uint currentFrameSerial = unchecked((uint)Game.FrameIndex.Value);
        _frameSerialAtTextureSet = GFX.IsSharedImageFrameSubmitted(currentFrameSerial) ? currentFrameSerial : 0;

        for (int i = 0; i < EngineTextures.Length; i++)
        {
            Descriptors[i].Dispose();
            EngineTextures[i].Dispose();
        }

        EngineTextures = new Texture[textures.Length];
        Descriptors = new Descriptor[textures.Length];

        for (int i = 0; i < textures.Length; i++)
        {
            var texture = textures[i];
            var descriptor = ImageShader.Shader.GetDescriptorSet();

            descriptor.BindTexture(texture, 1);

            EngineTextures[i] = texture;
            Descriptors[i] = descriptor;
        }

        Run = true;
    }

    UIElementBase GetUI() =>
        new UIVCol(w_full, h_full, blank_full_g_[30])[
            new UIHCol(w_full, h_[30], spacing_[10])[
                new UIText("PBG EDITOR", middle_left, left_[10], fs_[1.5f], bg_white),
                new UIHCol(h_full_minus_[6], middle_left, blank_sharp, hex_["#2d5a3d"], border_ui_[2, 2, 2, 2], border_color_["#3d7a4d"], grow_children, border_[5, 0, 5, 0], spacing_[5]).OnClick(_ => ProcessManager.Reload())[
                    new UIButton(middle_left, icon_[24], w_[25], h_[25], bg_white),
                    new UIText("Reload", middle_left, fs_[1.2f], bg_white)
                ]
            ],
            new UIImg(w_full, h_[2], blank_full_g_[70]),
            new UIHCol(w_full, h_full)[
                new UIVCol(w_[238], h_full)[
                    new UICol(w_full, h_[30])[
                        new UIText("HIERARCHY", middle_center, bg_white)
                    ],
                    new UIImg(w_full, h_[2], blank_full_g_[70]),
                    new UIVScroll(mask_children, grow_children, w_full, spacing_[2], border_[2, 2, 2, 2]).Out(out HierarchyScroll)
                ],
                new UICol(w_full_minus_[480], h_full_minus_[290], transparent).Out(out _engineTexture)[
                    new UIText("Progress: 0%", mc_[14], middle_center).Out(out _engineLoading)
                ]
            ],
            new UIImg(w_full, h_[2], blank_full_g_[70])
        ];

    public void GenerateHierarchy(string json)
    {
        JsonSerializerSettings jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        SceneGraph? graph;

        try
        {
            graph = JsonConvert.DeserializeObject<SceneGraph>(json, jsonSettings);
        }
        catch
        {
            Console.WriteLine("Failed to parse scene hierarchy json");
            return;
        }
        

        HierarchyScroll.DeleteChildren();
        graph?.GenerateNodes(HierarchyScroll);
        //HierarchyScroll.ApplyChanges(UIChange.Scale);
    }

    public class SceneGraph
    {
        public SceneNode[] Nodes { get; set; } = [];

        public void GenerateNodes(UIVScroll hierarchy)
        {
            foreach (var node in Nodes)
            {
                var sub = node.GenerateNodes(0, hierarchy);
                hierarchy.UIController += sub;
            }
        }
    }

    public class SceneNode
    {
        public string Name { get; set; } = "N/A";
        public SceneNode[] Nodes { get; set; } = [];

        public UIElementBase GenerateNodes(int offset, UICol collection, string path = "")
        {
            path += Name;
            var subCol = new UIVCol(grow_children, w_full, spacing_[2])[
                new UICol(border_[offset * 5, 0, 0, 0], h_[20], blank_sharp, bg_transparent, w_full_minus_[4], hover_color_[(0.3f, 0.3f, 0.3f, 0f), (0.5f, 0.5f, 0.5f, 0.5f)], hover_color_duration_[0.1f], hover_color_easeinout).OnClick(_ => GameWindow.Send(CommandType.NodeComponents, path))[
                    new UIText(Name, middle_left, left_[5])
                ]
            ];

            foreach (var node in Nodes)
            {
                node.GenerateNodes(offset + 1, subCol, path + '/');
            }

            collection.AddElement(subCol);

            return subCol;
        }
    }
}