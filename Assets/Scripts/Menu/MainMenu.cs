using PBG;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Rendering;
using PBG.UI;
using PBG.UI.Creator;
using Silk.NET.Input;
using static PBG.UI.Styles;

public class MainMenu : ScriptingNode
{
    public MainMenuUI mainMenuUI;

    public MainMenu()
    {
        mainMenuUI = new();
    }

    void Start()
    {
        var controller = Transform.GetComponent<UIController>();
        controller.AddElement(mainMenuUI);
    }

    void Awake()
    {
        Scene.DefaultCamera.SetCameraMode(CameraMode.Centered);
    }

    void Update()
    {
        mainMenuUI.Update();
    }
}

public class MainMenuUI : UIScript
{
    private UIVCol MainScreen = null!;
    private UICol PlayScreen = null!;
    private UICol NewWorldScreen = null!;

    private Action EscapeAction = () => { };

    public string? NoisePath = null;
    public UICol? SelectedNoise = null;

    public override UIElementBase Script() =>
    new UICol(Class(w_full, h_full, blank_full_g_[50]), Sub([
        newVCol(Class(w_[200], grow_children, middle_center, spacing_[40]), Sub([
            new UICol("play-button", Class(w_[200], h_[50], light_round_g_[50], slice_100, top_left, hover_scale_duration_[0.7f], hover_scale_[1.2f], hover_scale_easeout),
                OnClick<UICol>(PlayButton),
                Sub([
                new UIText("PLAY", Class(fs_[2], middle_center))
            ])),
            new UICol("editor-button", Class(w_[200], h_[50], light_round_g_[50], slice_100, top_left, hover_scale_duration_[0.7f], hover_scale_[1.2f], hover_scale_easeout),
                OnClick<UICol>(_ => Scene.LoadScene("StructureEditor")),
                Sub([
                new UIText("EDITOR", Class(fs_[2], middle_center))
            ])),
            new UICol("modeling-button", Class(w_[200], h_[50], light_round_g_[50], slice_100, top_left, hover_scale_duration_[0.7f], hover_scale_[1.2f], hover_scale_easeout),
                OnClick<UICol>(_ => {
                    Console.WriteLine("Loading Modeling Editor...");
                    Scene.LoadScene("ModelingEditor");
                }),
                Sub([
                new UIText("MODELING", Class(fs_[2], middle_center))
            ]))
        ]), ref MainScreen),
        newCol(Class(invisible, w_full, h_full), Sub([
            new UICol(Class(h_full_minus_[40], w_half, top_center, top_[20], light_round_g_[60]), Sub([
                new UIText("Select World", Class(mc_[12], fs_[2], top_center, top_[23])),
                new UICol(Class(w_full_minus_[20], h_full_minus_[120], bottom_[60], bottom_center, dark_sharp_g_[50]), Sub([
                    new UIVScroll(Class(w_full_minus_[14], h_full_minus_[14], mask_children, middle_center, spacing_[3]), Sub([

                    ]))
                ])),
                new UICol(Class(w_full, h_[40], bottom_center, bottom_[10]), Sub([
                    new UICol(Class(w_half_minus_[15], h_full, light_sharp_g_[70], middle_left, left_[10]),

                    Sub([
                        new UIText("Modify World", Class(mc_[12], fs_[1.2f], middle_center))
                    ])),
                    new UICol(Class(w_half_minus_[15], h_full, light_sharp_g_[70], middle_right, right_[10]),
                    OnClick<UICol>(NewWorldButton),
                    Sub([
                        new UIText("New World", Class(mc_[9], fs_[1.2f], middle_center))
                    ]))
                ]))
            ])),
        ]), ref PlayScreen),
        newCol(Class(invisible, w_full, h_full), Sub([
            new UICol(Class(h_full_minus_[40], w_[300], left_[20], top_left, top_[20], light_round_g_[60]), Sub([
                new UIText("Select Noise", Class(mc_[12], fs_[1], top_left, left_[15], top_[15])),
                new UICol(Class(w_full_minus_[20], h_full_minus_[45], top_[35], top_center, dark_sharp_g_[50]), Sub([
                    new UIVScroll(Class(w_full_minus_[14], h_full_minus_[14], mask_children, middle_center, spacing_[3]), Sub([
                        /*
                    ..Foreach(NodeManager.GetNodeFiles(), file => {
                        if (!File.Exists(file)) return null;
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        return new UICol(Class(w_full, h_[40], light_sharp_g_[50], data_["path", file]),
                        OnClick<UICol>(c => {
                            if (SelectedNoise != null)
                            {
                                SelectedNoise.Color = (0.5f, 0.5f, 0.5f, 1f);
                                SelectedNoise.UpdateColor();
                            }
                            NoisePath = file;
                            SelectedNoise = c;
                            SelectedNoise.Color = (0.7f, 0.7f, 0.7f, 1f);
                            SelectedNoise.UpdateColor();
                        }),
                        Sub([
                            new UIText(fileName, Class(mc_[28], fs_[1f], middle_left, left_[10]))
                        ]));
                    })*/
                    ]))
                ])),
            ])),
            new UICol(Class(h_full_minus_[40], w_full_minus_[360], right_[20], top_right, top_[20], light_round_g_[60]), Sub([
                new UIText("World Name", Class(mc_[12], fs_[1], top_left, left_[15], top_[15])),
                new UICol(Class(w_[296], h_[40], top_[35], top_left, left_[10], dark_sharp_g_[50]), Sub([
                    new UICol(Class(w_full_minus_[14], h_full_minus_[14], mask_children, middle_center), Sub([
                        new UIField("New World", Class(mc_[28], fs_[1f], middle_left))
                    ]))
                ])),
                new UICol(Class(w_[200], h_[30], light_sharp_g_[70], bottom_right, right_[10], bottom_[10]),
                OnClick<UICol>(_ => CreateWorld()),
                Sub([
                    new UIText("Create World", Class(mc_[12], fs_[1f], middle_center))
                ]))
            ]))
        ]), ref NewWorldScreen)
    ]));

    public void CreateWorld()
    {
        if (NoisePath == null)
            return;

        //NoiseNodeManager.Load(NoisePath);

        Scene.LoadScene("World");
    }

    public void Update()
    {
        if (Input.IsKeyPressed(Key.Escape))
        {
            EscapeAction.Invoke();
        }
    }

    private void NewWorldButton(UICol _)
    {
        NewWorldScreen.QueueUpdateVisibility(true);
        PlayScreen.QueueUpdateVisibility(false);
        EscapeAction = NewWorldEscape;
    }

    private void PlayButton(UICol _)
    {
        MainScreen.QueueUpdateVisibility(false);
        PlayScreen.QueueUpdateVisibility(true);
        EscapeAction = PlayEscape;
    }

    private void NewWorldEscape()
    {
        PlayScreen.QueueUpdateVisibility(true);
        NewWorldScreen.QueueUpdateVisibility(false);
        EscapeAction = PlayEscape;
    }

    private void PlayEscape()
    {
        MainScreen.QueueUpdateVisibility(true);
        PlayScreen.QueueUpdateVisibility(false);
        EscapeAction = () => { };
    }
}