using System.Diagnostics;
using System.Text.RegularExpressions;
using PBG.MathLibrary;
using PBG;
using PBG.Graphics;
using PBG.Voxel;
using System.Runtime.InteropServices;
using PBG.Data;
using Silk.NET.Input;
using PBG.UI;

public class GLSLManager
{
    private static int functionShader;
    public static Shader DisplayShader;
    public static Descriptor Descriptor;

    private static SSBO<float> _valueSSBO;
    private static float[] _values = [];

    private static int modelLocation = -1;
    private static int projectionLocation = -1;
    private static int sizeLocation = -1;
    private static int ScreenSizeLocation = -1;
    private static int noiseSizeLocation = -1;
    private static int offsetLocation = -1;

    private static string NoiseFragmentPathCopy = "";
    private static string[] _nodeLines = [];
    private static string[] _computeLines = [];

    private static bool _loaded = false;

    public GLSLManager()
    {
        if (_loaded)
            return;

        _loaded = true;
        NoiseFragmentPathCopy = Path.Combine(Game.ShaderPath, "Noise_vulkan", "WorldNoise.frag");

        List<string> nodeFunctions = [];
        List<string> computeFunctions = [];
        List<string> nodeLines = [];
        List<string> computeLines = [];

        int index = 0;
        foreach (var definition1 in BlockData.BlockDefinitions)
        {
            nodeFunctions.Add($"#define {definition1.Name.ToUpper()} {definition1.Block.ID}");
            computeFunctions.Add($"#define {definition1.Name.ToUpper()} ({definition1.Block.ID} | SOLID)");
            index++;
        }
    
        computeFunctions.Add("");

        foreach (var (_, definition) in NodeDefinitionLoader.NodeDefinitions)
        {
            foreach (var include in definition.Includes)
            {
                nodeLines.Add("// --- " + include + " ---");
                computeLines.Add("// --- " + include + " ---"); 
                foreach (var line in NodeDefinitionLoader.GetInclude(definition, include))
                {
                    nodeLines.Add(line);   
                    computeLines.Add(line);
                }
            }
        }

        foreach (var (_, definition) in NodeDefinitionLoader.NodeDefinitions)
        {
            foreach (var (action, function) in definition.GlobalFunctions)
            { 
                nodeLines.Add("// --- " + action + " ---");
                computeLines.Add("// --- " + action + " ---");

                foreach (var line in NodeDefinitionLoader.GetFunction(definition, function))
                {
                    nodeLines.Add(line);
                    computeLines.Add(line);
                }
            }

            foreach (var (action, function) in definition.NodeFunctions)
            {
                nodeLines.Add("// --- " + action + " ---");
                foreach (var line in NodeDefinitionLoader.GetFunction(definition, function))
                {
                    nodeLines.Add(line);
                }
            }

            foreach (var (action, function) in definition.ComputeFunctions)
            {
                computeLines.Add("// --- " + action + " ---");
                foreach (var line in NodeDefinitionLoader.GetFunction(definition, function))
                {
                    if (definition.Precompile)
                    {
                        computeLines.Add(line);
                    }
                    else
                    {
                        computeFunctions.Add(line);
                    }    
                }
            }
        }

        nodeFunctions.Add("");
        computeFunctions.Add("");

        string nodeFunctionsPath = Game.ShaderPath / "Noise_vulkan/NodeFunctions.frag";
        File.WriteAllLines(nodeFunctionsPath, nodeLines);

        string computeFunctionsPath = Game.ShaderPath / "computeShaders" / "world_vulkan" / "world.comp";
        File.WriteAllLines(computeFunctionsPath, computeLines);

        _nodeLines = [..nodeFunctions];
        _computeLines = [..computeFunctions];

        _valueSSBO = new(0);

        CleanFile();

        ShaderInfo info = new()
        {
            VertexShaderPath = Game.ShaderPath / "Utils_vulkan/Rectangle.vert", 
            FragmentShaderPath = Game.ShaderPath / "Noise_vulkan/WorldNoise.frag"
        };
        info.Rasterizer.CullMode = Silk.NET.Vulkan.CullModeFlags.None;

        DisplayShader = new Shader(info);
        DisplayShader.Compile();
        Descriptor = DisplayShader.GetDescriptorSet();
        Descriptor.BindSSBO(_valueSSBO, 1);

        modelLocation = DisplayShader.GetLocation("ubo.model");
        projectionLocation = DisplayShader.GetLocation("ubo.projection");
        sizeLocation = DisplayShader.GetLocation("ubo.size");
        ScreenSizeLocation = DisplayShader.GetLocation("fubo.iScreenSize");
        noiseSizeLocation = DisplayShader.GetLocation("fubo.iNoiseScale");
        offsetLocation = DisplayShader.GetLocation("fubo.iSample");
    }

    public static void Compile()
    {
        List<string > lines = [@"#version 460 core

layout(std430, binding = 1) readonly buffer DataBuffer { float values[]; };

layout(binding = 2) uniform UniformBufferObject {
    vec2 iScreenSize;
    float iNoiseScale;
    vec2 iSample;
} fubo;

#define iScreenSize fubo.iScreenSize
#define iNoiseScale fubo.iNoiseScale
#define iSample fubo.iSample

layout(location = 0) in vec2 TexCoord;

layout(location = 0) out vec4 FragColor;
"];     
        lines.Add("#include \"Noise_vulkan/NodeFunctions.frag\"");
        lines.Add("");
        lines.AddRange(_nodeLines);
        lines.Add("");
        
        HashSet<string> groupNodes = [];
        foreach (var node in NodeManager.NodeCollection.Nodes)
        {
            if (node is not GroupNode groupNode || !groupNodes.Add(groupNode.GroupName))
                continue;

            groupNode.GetFunction(lines);
            lines.Add("");
        }

        lines.Add("void main() {");
        lines.Add("    vec3 display = vec3(0);");
        lines.Add("    vec2 iPosition = (TexCoord + iSample) * iNoiseScale;");
        lines.Add("    vec2 iLocal = iPosition;");

        List<float> values = [];
        NodeManager.NodeCollection.GetLines(lines, values, new());

        lines.Add("    FragColor = vec4(display, 1.0);");
        lines.Add("}");

        _values = [..values];
        _valueSSBO.Renew(_values);

        File.WriteAllLines(NoiseFragmentPathCopy, lines);

        Reload();
    }

    public static void CompileCompute()
    {
        List<string > lines = [
@"#version 460 core
layout(local_size_x = 8, local_size_y = 1, local_size_z = 8) in;

layout(rgba32f, binding = 0) uniform image2D heightMap;

layout(binding = 1) uniform UniformBufferObject {
    ivec3 uChunkWorldPosition;
    int uLevel;
} ubo;
  
"];
        
        lines.AddRange(_nodeLines);
        lines.Add("");

        lines.Add("#include \"computeShaders/world_vulkan/world.comp\"");
        
        HashSet<string> groupNodes = [];
        foreach (var node in NodeManager.NodeCollection.Nodes)
        {
            if (node is not GroupNode groupNode || !groupNodes.Add(groupNode.GroupName))
                continue;

            groupNode.GetFunction(lines);
            lines.Add("");
        }
        

        lines.Add(@"
void main() {
    uvec3 gid = gl_GlobalInvocationID;

    uint x = gid.x;
    uint z = gid.z;

    if (x >= 32 || z >= 32)
    {
        return;
    }

    vec3 display = vec3(0);
    ivec2 iLocal = ivec2(x, z);
    ivec2 iPosition = iLocal + ubo.uChunkWorldPosition.xz;
");
        List<NodeBase> connectedNodeList = [];
        HashSet<NodeBase> visited = [];

        int cacheNodes = 0;

        foreach (var node in NodeManager.NodeCollection.Nodes)
        {
            if (node is CacheNode cacheNode)
            {
                NodeCollection.GetConnectedNodes(cacheNode, connectedNodeList, visited);
                NodeCollection.InitOutputs(connectedNodeList);
                cacheNodes++;
            }
        }
        NodeCollection.GetLines(connectedNodeList, lines, new() { GetCurrentValue = true });
        lines.Add("}");

        File.WriteAllLines(Game.ShaderPath / "computeShaders" / "world_vulkan" / "heightMap.comp", lines);

        WorldGenerator.Reload(cacheNodes); 
    }

    public static void CleanFile()
    {
        List<string > lines = [@"#version 460 core

layout(std430, binding = 1) readonly buffer DataBuffer { float values[]; };

layout(binding = 2) uniform UniformBufferObject {
    vec2 iScreenSize;
    float iNoiseScale;
    vec2 iSample;
} fubo;

#define iScreenSize fubo.iScreenSize
#define iNoiseScale fubo.iNoiseScale
#define iSample fubo.iSample

layout(location = 0) in vec2 TexCoord;

layout(location = 0) out vec4 FragColor;
"];
        
        lines.Add("#include \"Noise_vulkan/NodeFunctions.frag\"");
        lines.Add("");
        lines.AddRange(_nodeLines);
        lines.AddRange([
            "void main() {",
            "    vec3 display = vec3(0);",
            "    vec2 iPosition = (TexCoord + iSample) * iNoiseScale;",
            "    vec2 iLocal = iPosition;",
            "    FragColor = vec4(display, 1.0);",
            "}",
        ]);
        File.WriteAllLines(NoiseFragmentPathCopy, lines);
    }

    public static void UpdateValue(int index, float value)
    {
        if (index < 0 || _values.Length <= index)
            return;

        ulong stride = (ulong)Marshal.SizeOf<float>();
        _values[index] = value;
        _valueSSBO.UpdateSlice(_values, (ulong)index * stride, stride);
    }

    public static void Reload()
    {
        DisplayShader.Renew();
        Descriptor.Dispose();
        Descriptor = DisplayShader.GetDescriptorSet();
        Descriptor.BindSSBO(_valueSSBO, 1);

        modelLocation = DisplayShader.GetLocation("ubo.model");
        projectionLocation = DisplayShader.GetLocation("ubo.projection");
        sizeLocation = DisplayShader.GetLocation("ubo.size");
        ScreenSizeLocation = DisplayShader.GetLocation("fubo.iScreenSize");
        noiseSizeLocation = DisplayShader.GetLocation("fubo.iNoiseScale");
        offsetLocation = DisplayShader.GetLocation("fubo.iSample");
    }

    public static void Render(Matrix4 DisplayProjectionMatrix, Vector2 DisplayPosition, Vector2 DisplaySize, float NoiseSize, Vector2 Offset, Vector4 color)
    {
        Matrix4 model = Matrix4.CreateTranslation((DisplayPosition.X, DisplayPosition.Y, UIController.CumulativeDepth));

        DisplayShader.Bind();
        Descriptor.Bind();

        Descriptor.UniformMatrix4(modelLocation, model);
        Descriptor.UniformMatrix4(projectionLocation, Matrix4.CreateOrthographicOffCenter(0, Game.Width, 0, Game.Height, -2, 2));
        Descriptor.Uniform2(sizeLocation, DisplaySize);
        Descriptor.Uniform2(ScreenSizeLocation, DisplaySize);
        Descriptor.Uniform1(noiseSizeLocation, NoiseSize);
        Descriptor.Uniform2(offsetLocation, Offset);

        GFX.Draw(6, 1, 0, 0);
    }
}