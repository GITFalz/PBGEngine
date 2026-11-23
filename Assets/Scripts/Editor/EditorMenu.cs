using Newtonsoft.Json;
using PBG.Compilation;
using PBG.Core;
using PBG.Data;
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
    private UIField _searchField;
    private UIText _searchCount;
    private UIVCol _searchCol;

    private bool _search = false;
    private float _searchTime = 0;

    private uint _frameSerialAtTextureSet;

    public UIVScroll HierarchyScroll = null!;
    public UIVScroll ComponentsScroll = null!;

    public static bool Focused = false;
    private static bool _setFocused = false;

    public static int GameWidth => (int)(Instance?._engineTexture?.SizeX ?? GameWindow.Width);
    public static int GameHeight => (int)(Instance?._engineTexture?.SizeY ?? GameWindow.Height);
    public static int GameOffsetX => (int)(Instance?._engineTexture?.Origin.X ?? 0);
    public static int GameOffsetY => (int)(Instance?._engineTexture?.Origin.Y ?? 0);

    public static string CurrentNodePath = "";

    public static float Saturation = 1f;

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
        if (GameTime.FpsUpdated)
        {
            _fps?.UpdateText(GameTime.Fps);
        }

        if (GameWindow.EngineLoadingProgress != null)
        {
            var progress = (GameWindow.EngineLoadingProgress.Value * 100).floorToInt();
            _engineLoading?.UpdateText($"Progress: {progress}%");
        }

        if (_search && GameTime.TotalTime > _searchTime + 0.3f)
        {
            _search = false;
            GameWindow.Send(CommandType.SearchClass, _searchField.GetTrimmedText());
        }

        if (Input.IsMousePressed(MouseButton.Left) && GameWindow.GetCursorState() == CursorMode.Normal && !_setFocused)
        {
            Focused = false;
        }

        _setFocused = false;
    }

    void Resize()
    {
        UpdateDescriptors();
    }

    public void UpdateDescriptors()
    {
        /*
        foreach (var descriptor in Descriptors)
        {
            descriptor.UniformMatrix4(ImageShader.Model, Matrix4.CreateTranslation((_engineTexture.Origin.X, _engineTexture.Origin.Y, 0.1f)));
            descriptor.UniformMatrix4(ImageShader.Projection, _controller.GetProjection());
            descriptor.Uniform2(ImageShader.Size, _engineTexture.Size);
            descriptor.Uniform1(ImageShader.Saturation, 1f);
        }
        */
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
        descriptor.Uniform1(ImageShader.Saturation, Saturation);

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

            descriptor.BindTexture(texture, 2);

            EngineTextures[i] = texture;
            Descriptors[i] = descriptor;
        }

        UpdateDescriptors();

        Run = true;
    }

    public static int HeaderGray = 35;

    public static UIElementBase LightVsep => new UIImg(h_full, w_[1], blank_full_g_[60]);
    public static UIElementBase BlackVsep => new UIImg(h_full, w_[1], blank_full_g_[20]);

    public static UIElementBase LightHsep => new UIImg(w_full, h_[1], blank_full_g_[60]);
    public static UIElementBase BlackHsep => new UIImg(w_full, h_[1], blank_full_g_[20]);
    
    private UIElementBase _nav;
    private UIElementBase _start;
    private UIElementBase _stop;
    private UIElementBase _reload;
    private UIText _fps;

    private void OnStart()
    {
        _start?.SetVisible(false);
        _reload?.SetVisible(true);
        _stop?.SetVisible(true);
        _nav?.ApplyChanges(UIChange.Scale);

        ProcessManager.Reload(); 
        Saturation = 1f; 
    }

    private void OnReload()
    {
        ProcessManager.Reload(); 
        Saturation = 1f; 
    }

    private void OnStop()
    {
        _start?.SetVisible(true);
        _reload?.SetVisible(false);
        _stop?.SetVisible(false);
        _nav?.ApplyChanges(UIChange.Scale);

        ProcessManager.Stop(); 
        Saturation = 0.5f; 
    }

    UIElementBase GetUI() =>
        new UIVCol(w_full, h_full, blank_full_g_[30])[
            new UICol(w_full, h_[30])[
                new UIHCol(w_full, h_[30], spacing_[10], ignore_invisible)[
                    new UIText("PBG EDITOR", middle_left, left_[10], fs_[1.5f], bg_white),
                    new UIHCol(invisible, h_full_minus_[6], middle_left, blank_full, hex_["#2d5a3d"], border_ui_[2, 2, 2, 2], border_color_["#3d7a4d"], grow_children, border_[5, 0, 5, 0], spacing_[5])
                    .OnClick(_ => OnReload())[
                        new UIText("Reload", middle_left, left_[3], fs_[1.2f], bg_white),
                        new UIButton(middle_left, icon_[24], w_[15], h_[15], bg_white)
                    ].Out(out _reload),
                    new UIHCol(h_full_minus_[6], middle_left, blank_full, hex_["#2d5a3d"], border_ui_[2, 2, 2, 2], border_color_["#3d7a4d"], grow_children, border_[5, 0, 5, 0], spacing_[5])
                    .OnClick(_ => OnStart())[
                        new UIText("Start", middle_left, left_[3], fs_[1.2f], bg_white),
                        new UIButton(middle_left, icon_[0], w_[15], h_[15], bg_white)
                    ].Out(out _start),
                    new UIHCol(invisible, h_full_minus_[6], middle_left, blank_full, hex_["#833021"], border_ui_[2, 2, 2, 2], border_color_["#b44835"], grow_children, border_[5, 0, 5, 0], spacing_[5])
                    .OnClick(_ => OnStop())[
                        new UIText("Stop", middle_left, left_[3], fs_[1.2f], bg_white),
                        new UIButton(middle_left, icon_[9], w_[15], h_[15], bg_white)
                    ].Out(out _stop)
                ].Out(out _nav),
                new UIText("", mc_[4], middle_right, right_[10]).Out(out _fps)
            ],
            BlackHsep,
            new UIHCol(w_full, h_full)[
                new UIVCol(w_[239], h_full)[
                    LightHsep,
                    new UICol(w_full, h_[30], blank_full_g_[HeaderGray])[
                        new UIText("HIERARCHY", middle_left, left_[7], bg_white, fs_[1.1f])
                    ],
                    BlackHsep,
                    new UIVScroll(mask_children, h_full_minus_[62], w_full, border_[2, 2, 2, 2]).Out(out HierarchyScroll)
                ],
                BlackVsep,
                new UICol(w_full_minus_[480], h_full_minus_[290], blank_full_g_[10]).Out(out _engineTexture).OnClick(_ => { Focused = true; _setFocused = true; })[
                    new UIImg(middle_center, icon_[31], gray_[20], w_[100], h_[100])
                    //new UIText("Progress: 0%", mc_[14], middle_center).Out(out _engineLoading)
                ],
                BlackVsep,
                new UIVCol(w_[239], h_full)[
                    LightHsep,
                    new UICol(w_full, h_[30], blank_full_g_[HeaderGray])[
                        new UIText("INSPECTOR", middle_left, left_[7], bg_white, fs_[1.1f])
                    ],
                    BlackHsep,
                    new UICol(top_center, w_full_minus_[10], h_[20], top_[5], padding_bottom_[5], blank_full_g_[10])[
                        new UIField("", mc_[100], middle_left, left_[5], bg_white, fs_[1.1f])
                        .Out(out _searchField)
                        .OnTextChange(field =>
                        {
                            if (field.GetTrimmedText().Length > 0)
                            {
                                var text = field.ParentElement?.QueryElement("placeholder");
                                text?.SetVisible(false);
                            }
                            else
                            {
                                var text = field.ParentElement?.QueryElement("placeholder");
                                text?.SetVisible(true);
                            }
                            _search = true;
                            _searchTime = GameTime.TotalTime;
                        }),
                        new UIText("placeholder", "Search classes", middle_left, left_[5], hex_["#919191bb"], fs_[1.1f])
                    ],
                    BlackHsep,
                    new UICol(w_full, h_[19], blank_full_g_[25])[
                        new UIText("SUGGESTIONS", middle_left, left_[5], gray_[50]),
                        new UIText("0", middle_right, right_[5], gray_[50]).Out(out _searchCount)
                    ],
                    new UIVCol(min_h_[0], grow_children, w_full, blank_full_g_[25]).Out(out _searchCol),
                    BlackHsep,
                    new UIVScroll(mask_children, scroll_speed_[20], h_calc_[parentSize => Game.Height - _searchCol.BottomLeft.Y, 30], border_bottom_[5], w_full, min_h_[30]).Out(out ComponentsScroll)
                ]
            ],
            LightHsep
        ];

    public void GenerateSearch(string json)
    {
        ClassesSearchJson? graph = JsonHelper.Parse<ClassesSearchJson>(json);
        
        _searchCol.DeleteChildren();
        graph?.GenerateClasses(_searchCol);
        _searchCount.UpdateText(""+(graph?.Classes?.Count ?? 0));

        ComponentsScroll.ParentElement?.QueueUpdateTransformation();
        ComponentsScroll.ParentElement?.QueueUpdateScaling();
    }

    public void GenerateHierarchy(string json)
    {
        SceneGraph? graph = JsonHelper.Parse<SceneGraph>(json);
        
        HierarchyScroll.DeleteChildren();
        HierarchyScroll.Dataset.Remove("selected");
        graph?.GenerateNodes(HierarchyScroll);
    }

    public void GenerateComponents(string json)
    {
        JsonComponentsGraph? graph = JsonHelper.Parse<JsonComponentsGraph>(json);
        
        ComponentsScroll.DeleteChildren();
        graph?.GenerateNodes(ComponentsScroll);
    }

    public void GenerateClassInfo(string json)
    {
        ClassInfosJson? graph = JsonHelper.Parse<ClassInfosJson>(json);
        
        ComponentsScroll.DeleteChildren();
        graph?.GenerateNodes(ComponentsScroll);
    }
}