using System.Runtime.CompilerServices;
using PBG.MathLibrary;
using PBG.Noise;
using PBG;
using PBG.Data;
using PBG.Graphics;
using PBG.Rendering;
using PBG.Threads;

public static class StructureHeightShader
{
    /*
    public static ShaderProgram HeightShader = new("StructureEditor/structure/height.vert", "StructureEditor/structure/height.frag");
    public static SSBO<Vector4i> HeightSSBO = new();
    public static VAO HeightVAO = new();

    public static ShaderProgram BorderShader = new("StructureEditor/structure/border.vert", "StructureEditor/structure/border.frag");
    public static SSBO<int> BorderSSBO = new();
    public static VAO BorderVAO = new();

    public static int ModelLocation = HeightShader.GetLocation("model");
    public static int ViewLocation = HeightShader.GetLocation("view");
    public static int ProjectionLocation = HeightShader.GetLocation("projection");
    public static int DistanceLocation = HeightShader.GetLocation("dist");
    public static int TimeLocation = HeightShader.GetLocation("uTime");
    public static int ScaleLocation = HeightShader.GetLocation("uHeightScale");
    public static int EffectRadiusLocation = HeightShader.GetLocation("uEffectRadius");
    public static int CameraLocation = HeightShader.GetLocation("uCameraPosition");

    public static int BorderModelLocation = BorderShader.GetLocation("model");
    public static int BorderViewLocation = BorderShader.GetLocation("view");
    public static int BorderProjectionLocation = BorderShader.GetLocation("projection");
    public static int BorderDistanceLocation = HeightShader.GetLocation("dist");
    public static int BorderTimeLocation = HeightShader.GetLocation("uTime");
    public static int BorderEffectRadiusLocation = BorderShader.GetLocation("uEffectRadius");
    public static int BorderTopLocation = BorderShader.GetLocation("uTop");
    public static int BorderCameraLocation = HeightShader.GetLocation("uCameraPosition");
    */

    public static int FaceCount = 0;
    public static int Distance = 0;

    private static Vector3i _oldPosition = Vector3i.Zero;
    private static Vector3i _currentPosition = Vector3i.Zero;
    private static HologramProcess? _currentProcess = null;

    /*
    {
        int position,
        int height,
        int side,
        int (nothing yet)
    }
    */

    public class HologramProcess(int x, int z, int distance) : ThreadProcess
    {
        int width = distance * 2;
        List<int> heights = [];
        List<Vector4i> heightData = [];

        public override bool Function()
        {
            for (int X = 0; X < width; X++)
            {
                for (int Z = 0; Z < width; Z++)
                {
                    if (Failed) 
                        return false;

                    var height = SamplePosition(X + x - distance, Z + z - distance);
                    heights.Add(height);
                }
            }

            int i = 0;
            for (int X = 0; X < width; X++)
            {
                for (int Z = 0; Z < width; Z++)
                {
                    if (Failed) 
                        return false;

                    int height = heights[i];
                    
                    var front =       GetExposedHeight(height, height, X, Z,  0, -1, width, heights);
                    var frontRight =  GetExposedHeight(0, height, X, Z,  1, -1, width, heights);
                    var right =       GetExposedHeight(height, height, X, Z,  1,  0, width, heights);
                    var rightBack =   GetExposedHeight(0, height, X, Z,  1,  1, width, heights);
                    var back  =       GetExposedHeight(height, height, X, Z,  0,  1, width, heights);
                    var backLeft  =   GetExposedHeight(0, height, X, Z, -1,  1, width, heights);
                    var left  =       GetExposedHeight(height, height, X, Z, -1,  0, width, heights);
                    var leftFront  =  GetExposedHeight(0, height, X, Z, -1, -1, width, heights);
                    
                    SetNeighborExist(X, Z, height, i, 0, left.height, leftFront.exposed, front.exposed, frontRight.exposed, right.height, heightData);
                    SetNeighborExist(X, Z, height, i, 1, front.height, frontRight.exposed, right.exposed, rightBack.exposed, back.height, heightData);
                    SetNeighborExist(X, Z, height, i, 5, right.height, rightBack.exposed, back.exposed, backLeft.exposed, left.height, heightData);
                    SetNeighborExist(X, Z, height, i, 3, back.height, backLeft.exposed, left.exposed, leftFront.exposed, front.height, heightData);

                    int a = left.height == height ? 8 : 0;
                    int b = back.height == height ? 4 : 0;
                    int c = right.height == height ? 2 : 0;
                    int d = front.height == height ? 1 : 0;
                    int ab = backLeft.height == height ? 128 : 0;
                    int bc = rightBack.height == height ? 64 : 0;
                    int cd = frontRight.height == height ? 32 : 0;
                    int da = leftFront.height == height ? 16 : 0;
                    heightData.Add((i, height - 1, 2, a | b | c | d | ab | bc | cd | da));

                    i++;
                }
            }

            return true;
        }

        public override void OnCompleteBase()
        {
            if (Succeded)
            {
                //BorderSSBO.Renew(heights);
                //HeightSSBO.Renew(heightData);
                FaceCount = heightData.Count;
                Distance = distance;
                _currentPosition = _oldPosition;
            }
            _currentProcess = null;
        }
    }

    public static void GenerateHeight(int x, int z, int distance)
    {
        _currentProcess?.Break();
        var process = new HologramProcess(x, z, distance);
        _currentProcess = process;
        TaskPool.QueueAction(process, TaskPriority.High);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int exposed, int height) GetExposedHeight(int @default, int height, int x, int z, int dx, int dz, int width, List<int> heights)
    {
        int nx = x + dx;
        int nz = z + dz;

        if ((uint)nx < (uint)width && (uint)nz < (uint)width)
        {
            int index = nz + nx * width;
            int nh = heights[index];
            return (Mathf.Max(height - nh, 0), nh);
        }

        return (0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetNeighborExist(int x, int z, int height, int position, int side, int aHeight, int abHeight, int bHeight, int bcHeight, int cHeight, List<Vector4i> heightData)
    {
        if (bHeight > 0)
        {
            int start = height - bHeight;
            for (int y = start; y < height; y++)
            {
                int a = (aHeight > abHeight && y >= abHeight && y < aHeight) ? 8 : 0;
                int b = y+1 < height ? 4 : 0;
                int c = (cHeight > bcHeight && y >= bcHeight && y < cHeight) ? 2 : 0;
                int d = y > start ? 1 : 0;
                int ab = (aHeight > abHeight && y+1 >= abHeight && y+1 < aHeight) ? 128 : 0;
                int bc = (cHeight > bcHeight && y+1 >= bcHeight && y+1 < cHeight) ? 64 : 0;
                int cd = (cHeight > bcHeight && y-1 >= bcHeight && y-1 < cHeight) ? 32 : 0;
                int da = (aHeight > abHeight && y-1 >= abHeight && y-1 < aHeight) ? 16 : 0;
                heightData.Add((position, y, side, a | b | c | d | ab | bc | cd | da));
            }
        }
    }

    public static int SamplePosition(int x, int z)
    {
        Vector2 position = (x + 0.001f, z + 0.001f) * new Vector2(0.01f, 0.01f);
        float result = (NoiseLib.Noise(position) + 1) * 0.5f;
        return Mathf.Lerp(0, 60, result).Fti();
    }

    public static void Update(Camera camera)
    {
        Vector3 position = (camera.Position.X, 0, camera.Position.Z);
        Vector3i newPosition = Mathf.RoundToInt(position / 100);

        if (_oldPosition != newPosition)
        {
            _oldPosition = newPosition;
            GenerateHeight(_oldPosition.X * 100, _oldPosition.Z * 100, 200); 
        }
    }

    public static void Render(Camera camera)
    {
        /*
        GL.Disable(EnableCap.CullFace);

        HeightShader.Bind();

        Vector3 offset = (_currentPosition.X * 100, 0, _currentPosition.Z * 100);

        var model = Matrix4.CreateTranslation(offset);
        var view = camera.ViewMatrix;
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FOV), (float)(Game.Width - 480) / (float)(Game.Height - 60), 0.1f, 10000f);

        GL.UniformMatrix4(ModelLocation, true, ref model);
        GL.UniformMatrix4(ViewLocation, true, ref view);
        GL.UniformMatrix4(ProjectionLocation, true, ref projection);
        GL.Uniform1(DistanceLocation, Distance);
        GL.Uniform1(TimeLocation, GameTime.TotalTime);
        GL.Uniform1(ScaleLocation, 60f);
        GL.Uniform1(EffectRadiusLocation, 120f);
        GL.Uniform3(CameraLocation, camera.Position - offset);

        HeightVAO.Bind();
        HeightSSBO.Bind(0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, FaceCount * 6);

        HeightSSBO.Unbind();
        HeightVAO.Unbind();

        HeightShader.Unbind();    

        GL.Enable(EnableCap.CullFace);   

        BorderShader.Bind();

        offset = (_currentPosition.X * 100, 0, _currentPosition.Z * 100);
 
        model = Matrix4.CreateTranslation(offset);
        view = camera.ViewMatrix;
        projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FOV), (float)(Game.Width - 480) / (float)(Game.Height - 60), 0.1f, 10000f);

        GL.UniformMatrix4(BorderModelLocation, true, ref model);
        GL.UniformMatrix4(BorderViewLocation, true, ref view);
        GL.UniformMatrix4(BorderProjectionLocation, true, ref projection);
        GL.Uniform1(BorderDistanceLocation, Distance);
        GL.Uniform1(BorderTimeLocation, GameTime.TotalTime);
        GL.Uniform1(BorderEffectRadiusLocation, 120f);
        //GL.Uniform1(BorderTopLocation, Distance);
        GL.Uniform3(BorderCameraLocation, camera.Position - offset);

        BorderVAO.Bind();
        BorderSSBO.Bind(0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 100 * 6);

        BorderSSBO.Unbind();
        BorderVAO.Unbind();

        BorderShader.Unbind();    
        */
    }
}