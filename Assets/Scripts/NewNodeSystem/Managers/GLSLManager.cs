using System.Diagnostics;
using System.Text.RegularExpressions;
using PBG.MathLibrary;
using PBG;
using PBG.Graphics;
using PBG.Voxel;

public class GLSLManager
{
    private static int functionShader;
    public static int ComputeFunctionsShader;
    //public static ShaderProgram DisplayShaderProgram;

    //private static VAO _displayVAO = new VAO();
    //private static SSBO<float> _valueSSBO = new();
    private static List<float> _values = [];

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
        NoiseFragmentPathCopy = Path.Combine(Game.ShaderPath, "Noise", "WorldNoise.frag");

        List<string> nodeFunctions = [];
        List<string> computeFunctions = [];
        List<string> nodeLines = [
            "#version 450",
            ""
        ];
        List<string> computeLines = [];

        var regex = new Regex(@"
            ^\s*                                    # leading spaces
            (?:in|out|inout)?\s*                    # optional qualifier (for return, rare)
            ([A-Za-z_]\w*)\s+                       # return type
            ([A-Za-z_]\w*)\s*                       # function name
            \(\s*                                   # opening parenthesis
            (                                       # group: parameter list
                (?:
                    (?:in|out|inout)?\s*            # optional param modifier
                    [A-Za-z_]\w*                    # param type
                    \s+
                    [A-Za-z_]\w*                    # param name
                    \s*
                    (?:,\s*(?:in|out|inout)?\s*[A-Za-z_]\w*\s+[A-Za-z_]\w*\s*)*
                )?
            )                                       # end params
            \)\s*$                                  # closing paren
        ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        bool FunctionCheck(string line, out string newLine)
        {
            newLine = "";
            // if rest of function is on the same line, remove it
            string[] splitLines = line.Split(['{'], StringSplitOptions.RemoveEmptyEntries);
            if (splitLines.Length == 0) return false;
            line = splitLines[0];

            var match = regex.Match(line);
            if (match.Success)
            {
                newLine = line.Trim() + ";";
                return true; 
            }
            return false; 
        }

        int index = 0;
        foreach (var definition1 in BlockData.BlockDefinitions)
        {
            nodeFunctions.Add($"#define {definition1.Name.ToUpper()} {definition1.Block.ID}");
            computeFunctions.Add($"#define {definition1.Name.ToUpper()} ({definition1.Block.ID} | SOLID)");
            index++;
        }
        
        nodeFunctions.Add("");
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
                    if (FunctionCheck(line, out var functionLine))
                    {
                        nodeFunctions.Add(functionLine);
                        computeFunctions.Add(functionLine);
                    }
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
                    if (FunctionCheck(line, out var functionLine))
                    {
                        nodeFunctions.Add(functionLine);
                        computeFunctions.Add(functionLine);
                    }
                }
            }

            foreach (var (action, function) in definition.NodeFunctions)
            {
                nodeLines.Add("// --- " + action + " ---");
                foreach (var line in NodeDefinitionLoader.GetFunction(definition, function))
                {
                    nodeLines.Add(line);
                    if (FunctionCheck(line, out var functionLine))
                    {
                        nodeFunctions.Add(functionLine);
                    }
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
                        if (FunctionCheck(line, out var functionLine))
                        {
                            computeFunctions.Add(functionLine);
                        }
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

        string nodeFunctionsPath = Path.Combine(Game.ShaderPath, "Noise/NodeFunctions.glsl");
        File.WriteAllLines(nodeFunctionsPath, nodeLines);

        string computeFunctionsPath = Game.ShaderPath / "computeShaders" / "world_vulkan" / "world.comp";
        File.WriteAllLines(computeFunctionsPath, computeLines);

        _nodeLines = [..nodeFunctions];
        _computeLines = [..computeFunctions];

        CleanFile();

        /*
        functionShader = ShaderProgram.CompileFragmentShader("Noise/NodeFunctions.glsl");
        ComputeFunctionsShader = ComputeShader.Compile("Noise/ComputeFunctions.comp");
        DisplayShaderProgram = new ShaderProgram("Utils/Rectangle.vert", "Noise/WorldNoise.frag", functionShader);

        modelLocation = DisplayShaderProgram.GetLocation("model");
        projectionLocation = DisplayShaderProgram.GetLocation("projection");
        sizeLocation = DisplayShaderProgram.GetLocation("size");
        ScreenSizeLocation = DisplayShaderProgram.GetLocation("iScreenSize");
        noiseSizeLocation = DisplayShaderProgram.GetLocation("iNoiseScale");
        offsetLocation = DisplayShaderProgram.GetLocation("iSample");
        */
    }

    public static void Compile()
    {
        List<string > lines = [
            "#version 450",
            "",
            "layout(std430, binding = 0) buffer DataBuffer {",
            "    float data[];",
            "} values;",
            "",
            "layout(binding = 1) uniform UniformBufferObject {",
            "   vec2 iScreenSize;",
            "   float iNoiseScale;",
            "   vec2 iSample;",
            "   vec4 iColor;",
            "} ubo;",
            "",
            "layout(location = 0) in vec2 TexCoord;",
            "",
            "layout(location = 0) out vec4 FragColor;",
            ""
        ];
        
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

        _values = values;
        //_valueSSBO.Renew(values);

        File.WriteAllLines(NoiseFragmentPathCopy, lines);

        Reload();
    }

    public static void CompileCompute()
    {
        List<string > lines = [
@"#version 450
layout(local_size_x = 8, local_size_y = 1, local_size_z = 8) in;

layout(rgba32f, binding = 0) uniform image2D heightMap;

layout(binding = 1) uniform UniformBufferObject {
    ivec3 uChunkWorldPosition;
    int uLevel;
} ubo;
  
"];
        
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
        lines.Add("#include \"computeShaders/world_vulkan/world.comp\"");

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
        List<string > lines = [
            "#version 460 core",
            "",
            "layout(std430, binding = 0) buffer DataBuffer {",
            "    float values[];",
            "};",
            "",
            "uniform vec2 iScreenSize;",
            "uniform float iNoiseScale;",
            "uniform vec2 iSample;",
            "uniform vec4 iColor;",
            "in vec2 TexCoord;",
            "",
            "out vec4 FragColor;",
            ""
        ];
        
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
        if (index < 0 || _values.Count <= index)
            return;

        _values[index] = value;
        //_valueSSBO.Update(_values, 0);
    }

    public static void Reload()
    {
        /*
        DisplayShaderProgram.Renew("Utils/Rectangle.vert", "Noise/WorldNoise.frag", functionShader);

        DisplayShaderProgram.Bind();

        modelLocation = DisplayShaderProgram.GetLocation("model");
        projectionLocation = DisplayShaderProgram.GetLocation("projection");
        sizeLocation = DisplayShaderProgram.GetLocation("size");
        ScreenSizeLocation = DisplayShaderProgram.GetLocation("iScreenSize");
        noiseSizeLocation = DisplayShaderProgram.GetLocation("iNoiseScale");
        offsetLocation = DisplayShaderProgram.GetLocation("iSample");

        DisplayShaderProgram.Unbind();
        */
    }

    public static void Render(Matrix4 DisplayProjectionMatrix, Vector2 DisplayPosition, Vector2 DisplaySize, float NoiseSize, Vector2 Offset, Vector4 color)
    {
        /*
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);

        Matrix4 model = Matrix4.CreateTranslation((DisplayPosition.X, DisplayPosition.Y, 2.2f));

        DisplayShaderProgram.Bind();

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(projectionLocation, true, ref DisplayProjectionMatrix);
        GL.Uniform2(sizeLocation, ref DisplaySize);
        GL.Uniform2(ScreenSizeLocation, ref DisplaySize);
        GL.Uniform1(noiseSizeLocation, NoiseSize);
        GL.Uniform2(offsetLocation, ref Offset);

        _displayVAO.Bind(); 
        _valueSSBO.Bind(0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        Shader.Error("Error rendering noise shader 1: ");

        _valueSSBO.Unbind();
        _displayVAO.Unbind();

        DisplayShaderProgram.Unbind();
        */
    }
}