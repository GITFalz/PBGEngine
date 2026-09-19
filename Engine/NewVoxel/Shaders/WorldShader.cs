using PBG.Graphics;

namespace PBG.NewVoxel;

[Shader("world_vulkan_new/indirect-world-greedy.vert", "world_vulkan_new/indirect-world-greedy.frag")]
public static class WorldShader
{
    public static Shader Shader;

    [Uniform("ubo.view")]                       public static int View;
    [Uniform("ubo.proj")]                       public static int Projection;
    [Uniform("ubo.uCloseLightSpaceMatrix")]     public static int CloseLightSpaceMatrix;
    [Uniform("ubo.uMiddleLightSpaceMatrix")]    public static int MiddleLightSpaceMatrix;
    [Uniform("ubo.uFarLightSpaceMatrix")]       public static int FarLightSpaceMatrix;
    [Uniform("ubo.closeTexelSize")]             public static int CloseTexelSize;
    [Uniform("ubo.middleTexelSize")]            public static int MiddleTexelSize;
    [Uniform("ubo.farTexelSize")]               public static int FarTexelSize;

    [Uniform("worldInfo.lightDirection")]       public static int LightDirection;
    [Uniform("worldInfo.uDoRealtimeShadows")]   public static int DoRealtimeShadows;
    [Uniform("worldInfo.uDoAmbientOcclusion")]  public static int DoAmbientOcclusion;
    [Uniform("worldInfo.uPlayerPosition")]      public static int PlayerPosition;
    [Uniform("worldInfo.time")]                 public static int Time;
}