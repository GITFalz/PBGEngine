using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PBG.Core;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;
using PBG.UI.Creator;
using static PBG.UI.Styles;

namespace PBG.Data;

public class Info : ScriptingNode
{
    public static Info Instance = null!;

    // Global Info
    public static UIText FpsText = null!;
    public static UIText GPUText = null!;
    public static UIText RamUsageText = null!;

    // Chunk Info
    public static UIText RenderedChunks = null!;
    public static UIText TotalChunks = null!;
    public static UIText GenerationQueueText = null!;
    public static UIText RenderingQueueText = null!;
    public static UIText GlobalChunkVertexCount = null!;
    public static UIText AverageChunkRenderingText = null!;
    public static UIText AverageChunkGenerationText = null!;

    // Vram
    public static UIText VramTotalText = null!;
    public static UIText VramFreeText = null!;
    public static UIText VramUsedText = null!;
    public static UIText VramPrecentText = null!;

    // Player Info
    public static UIText PositionText = null!;

    private static int _oldVertexCount = 0;
    public static int VertexCount = 0;

    private static int _oldChunkCount = 0;
    public static int ChunkCount = 0;

    public static int _oldTotalChunkCount = 0;
    public static int TotalChunkCount = 0;

    public static int _oldGenerationQueueCount = 0;
    public static int GenerationQueueCount = 0;

    public static int _oldRenderingQueueCount = 0;
    public static int RenderingQueueCount = 0;

    public static float _oldAverageRenderingSpeed = 0;
    public static float AverageRenderingSpeed = 0;

    public static float _oldAverageGenerationSpeed = 0;
    public static float AverageGenerationSpeed = 0;

    public static double TotalGenTime = 0;
    public static double TotalGenCount = 1;
    public static double AvgChunkGenTime = 0;


    //private static VAO _blockVao = new VAO();
    //private static SSBO<InfoBlockData> _blockSSBO = new();

    public static ConcurrentBag<InfoBlockData> _blocks = new ConcurrentBag<InfoBlockData>();
    //private static ShaderProgram _blockShader = new ShaderProgram("info/blocks.vert", "info/blocks.frag");

    private static Action _updateBlocks = () => { };

    private static object lockObj = new object();

    public static bool RenderInfo = false;

    private static Vector3 _oldPosition = Vector3.Zero;

    public Info()
    {
        Instance = this;
    }

    void Awake()
    {

    }

    void Start()
    {
        var controller = Transform.GetComponent<UIController>();
        controller.AddElement(new InfoUI());
        Transform.Disabled = true;
    }

    private class InfoUI : UIScript
    {
        public override UIElementBase Script() =>
        new UIVCol(Class(top_[5], left_[5]), Sub([
            new UIVCol(Class(spacing_[5]), Sub([
                new UIText("--General--", Class(mc_[11], fs_[1])),
                newText("Fps: 9999", Class(mc_[11], fs_[1]), ref FpsText),
                //newText($"GPU: {GL.GetString(StringName.Renderer)}", Class(mc_[100], fs_[1]), ref GPUText),
                newText("0", Class(mc_[15], fs_[1]), ref RamUsageText),
                new UIText("--Chunks--", Class(mc_[11], fs_[1])),
                newText("0", Class(mc_[50], fs_[1]), ref RenderedChunks),
                newText("0", Class(mc_[50], fs_[1]), ref TotalChunks),
                newText("0", Class(mc_[50], fs_[1]), ref GenerationQueueText),
                newText("0", Class(mc_[50], fs_[1]), ref RenderingQueueText),
                newText("0", Class(mc_[50], fs_[1]), ref GlobalChunkVertexCount),
                newText("0", Class(mc_[50], fs_[1]), ref AverageChunkRenderingText),
                newText("0", Class(mc_[50], fs_[1]), ref AverageChunkGenerationText),
                new UIText("--VRam--", Class(mc_[8], fs_[1])),
                newText("0", Class(mc_[100], fs_[1]), ref VramTotalText),
                newText("0", Class(mc_[100], fs_[1]), ref VramFreeText),
                newText("0", Class(mc_[100], fs_[1]), ref VramUsedText),
                newText("0%", Class(mc_[100], fs_[1]), ref VramPrecentText),
                new UIText("--Player--", Class(mc_[8], fs_[1])),
                newText("x y z:  0  0  0", Class(mc_[100], fs_[1]) , ref PositionText)
            ])),
        ]));
    }

    void Update()
    {
        if (!RenderInfo)
            return;

        if (GameTime.FpsUpdated)
        {
            FpsText.UpdateText($"Fps: {GameTime.Fps}");
            RamUsageText.SetText($"Ram: {GameTime.Ram / (1024 * 1024)} Mb").UpdateCharacters();

            GlobalChunkVertexCount?.UpdateText($"Vertices: {VertexCount}");
            RenderedChunks?.UpdateText("Visible Chunks: " + ChunkCount);
            TotalChunks?.UpdateText("Total Chunks: " + TotalChunkCount);
            GenerationQueueText?.UpdateText("Generation Queue: " + GenerationQueueCount);
            RenderingQueueText?.UpdateText("Rendering Queue: " + RenderingQueueCount);
            AverageChunkRenderingText?.UpdateText($"Average rendering speed {AverageRenderingSpeed:F3} ms");
            AverageChunkGenerationText?.UpdateText($"Average generation speed {AverageGenerationSpeed:F3} ms");

            long total = VRAMInfo.GetTotalVRAM();
            long free = VRAMInfo.GetFreeVRAM();
            long used = VRAMInfo.GetUsedVRAM();
            float percentage = VRAMInfo.GetVRAMUsagePercentage();
            VramTotalText.SetText($"Total VRAM: {FormatBytes(total)}").UpdateCharacters();
            VramFreeText.SetText($"Free VRAM: {FormatBytes(free)}").UpdateCharacters();
            VramUsedText.SetText($"Used VRAM: {FormatBytes(used)}").UpdateCharacters();
            VramPrecentText.SetText($"Usage: {percentage:F1}%").UpdateCharacters();
            
        }

        _updateBlocks();
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }

    void Render()
    {
        if (!RenderInfo)
            return;

        RenderBlocks();
    }

    public static void GenerateBlocks()
    {
        //_blockSSBO.Renew(_blocks.ToArray());
    }

    public static void ClearBlocks()
    {
        _blocks.Clear();
    }

    public static void AddBlock(InfoBlockData block)
    {
        lock (lockObj)
        {
            _blocks.Add(block);
        }
    }

    public static void AddBlock(params InfoBlockData[] block)
    {
        foreach (var b in block)
            AddBlock(b);
    }


    public static void UpdateBlocks()
    {
        lock (lockObj)
        {
            _updateBlocks = () =>
            {
                _blocks = [.. _blocks];
                GenerateBlocks();
                _updateBlocks = () => { };
            };
        }
    }

    public void RenderBlocks()
    {
        /*
        if (_blocks.Count == 0)
            return;

        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(true);

        _blockShader.Bind();

        Matrix4 model = Matrix4.Identity;
        Matrix4 view = Scene.DefaultCamera.ViewMatrix;
        Matrix4 projection = Scene.DefaultCamera.ProjectionMatrix;

        int modelLocationA = GL.GetUniformLocation(_blockShader.ID, "model");
        int viewLocationA = GL.GetUniformLocation(_blockShader.ID, "view");
        int projectionLocationA = GL.GetUniformLocation(_blockShader.ID, "projection");

        GL.UniformMatrix4(viewLocationA, true, ref view);
        GL.UniformMatrix4(projectionLocationA, true, ref projection);
        GL.UniformMatrix4(modelLocationA, true, ref model);

        _blockVao.Bind();
        _blockSSBO.Bind(1);

        GL.DrawArrays(PrimitiveType.Triangles, 0, _blocks.Count * 36);

        Shader.Error("Error rendering info blocks");

        _blockSSBO.Unbind();
        _blockVao.Unbind();

        _blockShader.Unbind();

        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        */
    }

    public static void SetPlayerPosition(Vector3 position)
    {
        PositionText?.SetText($"x y z:  {position.X}  {position.Y}  {position.Z}").UpdateCharacters();
    }

    public static void SetChunkVertexCount(int vertexCount)
    {
        VertexCount = vertexCount;
        if (_oldVertexCount != VertexCount)
        {
            
            _oldVertexCount = VertexCount;
        }
    }

    public static void SetChunkRenderCount(int count)
    {
        ChunkCount = count;
        if (_oldChunkCount != ChunkCount)
        {
            
            _oldChunkCount = ChunkCount;
        }
    }

    public static void SetChunkTotalCount(int count)
    {
        TotalChunkCount = count;
        if (_oldTotalChunkCount != TotalChunkCount && TotalChunks != null)
        {
            
            _oldTotalChunkCount = TotalChunkCount;
        }
    }

    public static void SetGenerationQueueCount(int count)
    {
        GenerationQueueCount = count;
        if (_oldGenerationQueueCount != GenerationQueueCount)
        {
            
            _oldGenerationQueueCount = GenerationQueueCount;
        }
    }

    public static void SetRenderingQueueCount(int count)
    {
        RenderingQueueCount = count;
        if (_oldRenderingQueueCount != RenderingQueueCount)
        {
            
            _oldRenderingQueueCount = RenderingQueueCount;
        }
    }

    public static void AverageChunkRenderingSpeed(float time)
    {
        AverageRenderingSpeed = time;
    }

    public static void AverageChunkGenerationSpeed(float time)
    {
        AverageGenerationSpeed = time;
    }

    public static void SetPositionText(Vector3 position)
    {
        if (_oldPosition != position)
        {
            //PositionText.SetText($"Position: X: {position.X}, Y: {position.Y}, Z: {position.Z}", 1.2f).UpdateCharacters();
            _oldPosition = position;
        }
    }


    public static void ToggleOn()
    {
        RenderInfo = true;
        Instance.Transform.Disabled = false;
    }

    public static void ToggleOff()
    {
        RenderInfo = false;
        Instance.Transform.Disabled = true;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InfoBlockData
{
    public Vector3 Position;
    private float Padding1 = 0;
    public Vector3 Size;
    private float Padding2 = 0;
    public Vector4 Color;

    public InfoBlockData(Vector3 position, Vector3 size, Vector4 color)
    {
        Position = position;
        Size = size;
        Color = color;
    }
}