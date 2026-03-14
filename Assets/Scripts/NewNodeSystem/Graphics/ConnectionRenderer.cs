using PBG.MathLibrary;
using PBG.Data;
using PBG.Graphics;
using PBG.UI;
using PBG;
using Silk.NET.Vulkan;

public class ConnectionRenderer
{
    private static bool _started = false;

    public static Shader ConnectorLineShader = null!;
    private static int modelLocation = -1;
    private static int projectionLocation = -1;
    private static int timeLocation = -1;
    
    private SSBO<PointsStruct> _connectorLineSSBO = null!;
    private Descriptor _descriptor = null!;
    private int _vertexCount;

    public ConnectionRenderer()
    {
        if (!_started)
        {
            ShaderInfo info = new() {
                VertexShaderPath = Game.ShaderPath / "Noise_vulkan/ConnectorLine.vert",
                FragmentShaderPath = Game.ShaderPath / "Noise_vulkan/ConnectorLine.frag",
            };
            
            ConnectorLineShader = new(info);

            ConnectorLineShader.Compile();

            modelLocation = ConnectorLineShader.GetLocation("ubo.model");
            projectionLocation = ConnectorLineShader.GetLocation("ubo.projection");
            timeLocation = ConnectorLineShader.GetLocation("d.time");

            _started = true;
        }

        _connectorLineSSBO = new(0);

        _descriptor = ConnectorLineShader.GetDescriptorSet();
        _descriptor.BindSSBO(_connectorLineSSBO, 0);
    }

    public void GenerateLines(NodeCollection nodeCollection)
    {
        var points = GetLines(nodeCollection);
        _vertexCount = points.Count * 6;
        _connectorLineSSBO.Renew([..points]);
        _descriptor.BindSSBO(_connectorLineSSBO, 0);
    }

    public void UpdateLines(NodeCollection nodeCollection)
    {
        var points = GetLines(nodeCollection);
        _vertexCount = points.Count * 6;
        _connectorLineSSBO.Update([..points]);
    }

    private List<PointsStruct> GetLines(NodeCollection nodeCollection)
    {
        List<PointsStruct> points = [];

        int index = 0;
        foreach (var output in nodeCollection.Outputs)
        {
            if (!output.IsConnected)
                continue;

            for (int i = 0; i < output.Inputs.Count; i++)
            {
                var input = output.Inputs[i];
                output.SetIndex(input, index);

                var inputPos = input.Position;
                var outputPos = output.Position;

                PointsStruct point = new()
                {
                    PointA = (inputPos.X, inputPos.Y),
                    PointB = (outputPos.X, outputPos.Y),
                    ColorA = new Vector4(input.Color, 1),
                    ColorB = new Vector4(output.Color, 1)
                };

                points.Add(point);
                index++;
            }
        }

        _vertexCount = points.Count * 6;

        return points;
    }

    public void RenderLines(UIController uIController)
    {
        if (_vertexCount == 0)
            return;
            
        var viewport = GFX.GetViewport();

        UIController.BindFramebuffer();

        int width = uIController.Alignment.Width;
        int height = uIController.Alignment.Height;

        GFX.Viewport(uIController.Alignment.Left, uIController.Alignment.Top, width, height);

        ConnectorLineShader.Bind();
        _descriptor.Bind();

        Matrix4 model = uIController.ModelMatrix * Matrix4.CreateTranslation((0, 0, UIController.CumulativeDepth));
        Matrix4 projection = uIController.GetProjection();

        _descriptor.Uniform(modelLocation, model);
        _descriptor.Uniform(projectionLocation, projection);
        _descriptor.Uniform(timeLocation, GameTime.TotalTime);

        GFX.Draw((uint)_vertexCount, 1, 0, 0);

        UIController.CumulativeDepth += 0.00001f;

        UIController.UnbindFramebuffer();

        GFX.Viewport(viewport.x, viewport.y, viewport.width, viewport.height);
    }

    private struct PointsStruct
    {
        public Vector2 PointA;
        public Vector2 PointB;
        public Vector4 ColorA;
        public Vector4 ColorB;
    }
}
