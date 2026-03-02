using System.Diagnostics.CodeAnalysis;
using PBG.Graphics;
using PBG.Rendering;
using PBG.Voxel;

public static class ItemDataManager
{
    public static Dictionary<string, ItemData> AllItems = [];
    public static List<ItemData> Items = [];

    public static int BlockCount = 0;
    public static int WeaponCount = 0;

    //public static FBO FBO = new FBO(128, 128, FBOType.Color);
    //public static ShaderProgram BlockShader = new ShaderProgram("Utils/Cube.vert", "Inventory/Data/Block.frag");
    public static TextureArray Image = null!;

    public static List<byte[]> Data = new List<byte[]>();

    public static int CubeModelLocation = -1;  
    public static int CubeProjectionLocation = -1;
    public static int CubeTextureLocation = -1;
    public static int CubeIndicesLocation = -1;

    static ItemDataManager()
    {
        /*
        CubeModelLocation = BlockShader.GetLocation("model"); 
        CubeProjectionLocation = BlockShader.GetLocation("projection");
        CubeTextureLocation = BlockShader.GetLocation("textureArray");
        CubeIndicesLocation = BlockShader.GetLocation("indices");
        */
    }

    public static void GenerateIcons()
    {
        /*
        Data.Clear();

        try
        {
            GL.Viewport(0, 0, 128, 128);
            
            FBO.Bind();

            VoxelRenderer.BaseShader.Bind();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(TriangleFace.Front);
            GL.Enable(EnableCap.DepthTest);

            Matrix4 model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45 + 180)) * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(45 + 90)) * Matrix4.CreateScale(64) * Matrix4.CreateTranslation(64, 64, 0);
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, 128, 128, 0, -64, 64);
            Matrix4 view = Matrix4.Identity;

            GL.UniformMatrix4(VoxelRenderer.BaseShaderLocation.Model, false, ref model);
            GL.UniformMatrix4(VoxelRenderer.BaseShaderLocation.View, false, ref view); 
            GL.UniformMatrix4(VoxelRenderer.BaseShaderLocation.Projection, false, ref projection);
            GL.Uniform1(VoxelRenderer.BaseShaderLocation.Texture, 0);
            GL.Uniform3(VoxelRenderer.BaseShaderLocation.LightDirection, new Vector3(-1, 1, 1) * 2f);

            BlockData.BlockTextureArray.Bind(TextureUnit.Texture0);

            foreach (var (_, item) in AllItems)
            {
                if (item is BlockItemData)
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit);
                    item.GenerateIcon();
                }
            }

            BlockData.BlockTextureArray.Unbind();
            VoxelRenderer.BaseShader.Unbind();

            GL.CullFace(TriangleFace.Back);

            foreach (var (_, item) in AllItems)
            {
                if (item is WeaponItemData)
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit);
                    item.GenerateIcon();
                }   
            }

            FBO.Unbind();

            GL.Viewport(0, 0, Game.Width, Game.Height);

            Image = new(Data, 128, 128);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Critical Error] : Failed to generate icons: {ex.Message}");
            Console.WriteLine($"[Stack Trace] : {ex.StackTrace}");
            throw; // Re-throw to maintain original behavior
        }
        */
    }

    public static void ForeachBlockItems(Action<BlockItemData> action)
    {
        foreach (var (_, item) in AllItems)
        {
            if (item is BlockItemData blockItem)
            {
                action(blockItem);
            }
        }
    }
}
