using PBG.MathLibrary;
using PBG.Graphics;
using PBG.Rendering;

public static class PlacementHelper
{
    /*
    public static ShaderProgram PlacementHelperShader = new ShaderProgram("world/placementHelper.vert","world/placementHelper.frag");
    public static VAO VAO = new();

    private static int _placementModelLocation = PlacementHelperShader.GetLocation("model");
    private static int _placementViewLocation = PlacementHelperShader.GetLocation("view");
    private static int _placementProjectionLocation = PlacementHelperShader.GetLocation("projection");
    private static int _placementSideLocation = PlacementHelperShader.GetLocation("uSide");
    private static int _placementBlockPosLocation = PlacementHelperShader.GetLocation("uBlockPos");
    private static int _placementCameraPosLocation = PlacementHelperShader.GetLocation("uCamPosition");
    private static int _placementRegionLocation = PlacementHelperShader.GetLocation("uRegion");
    */

    public static void Render(Camera camera, Vector3 position, int selectedSide, int selectedRegion)
    {
        /*
        PlacementHelperShader.Bind();

        var model = Matrix4.CreateTranslation(position);
        var view = camera.ViewMatrix;
        var projection = camera.ProjectionMatrix;

        GL.UniformMatrix4(_placementModelLocation, false, ref model);
        GL.UniformMatrix4(_placementViewLocation, false, ref view);
        GL.UniformMatrix4(_placementProjectionLocation, false, ref projection);
        GL.Uniform1(_placementSideLocation, (int)selectedSide);
        GL.Uniform1(_placementRegionLocation, (int)selectedRegion);
        GL.Uniform3(_placementBlockPosLocation, new Vector3(position));
        GL.Uniform3(_placementCameraPosLocation, camera.Position);

        VAO.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        Shader.Error("Block placer rendering error: ");

        VAO.Unbind();

        PlacementHelperShader.Unbind();
        */
    }
}