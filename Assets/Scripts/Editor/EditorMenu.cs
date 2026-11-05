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
    private UIImg _engineTexture;

    public static int GameWidth => (int)(Instance?._engineTexture?.SizeX ?? GameWindow.Width);
    public static int GameHeight => (int)(Instance?._engineTexture?.SizeY ?? GameWindow.Height);

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

        if (Game.server != null && Game.server.IsConnected)
            Game.server.Send(Mathf.IntsToString((int)CommandType.Resize, EditorMenu.GameWidth, EditorMenu.GameHeight));
    } 

    void Render()
    {
        if (!Run)
            return;

        int frameIndex = Game.FrameIndex.Value;
        if ((uint)frameIndex >= (uint)Descriptors.Length)
            return;

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
        for (int i = 0; i < EngineTextures.Length; i++)
        {
            EngineTextures[i].Dispose();
            Descriptors[i].Dispose();
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
    new UICol(w_full, h_full, blank_full_g_[30])[
        new UICol(w_full, h_[50], border_ui_[0, 0, 0, 2], border_color_g_[70])[
            new UIButton(top_right, icon_[24], w_[48], h_[48], bg_white).OnClick(_ => ProcessManager.Reload())
        ],
        new UIImg(left_[240], top_[50], w_full_minus_[480], h_full_minus_[290], transparent).Out(out _engineTexture)
    ];
}