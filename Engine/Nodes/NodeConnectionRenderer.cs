using PBG.Data;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Nodes;

[InternalSystemInit(InitPriority.Shader)]
public class NodeConnectionRenderer
{
    public static Shader ConnectorLineShader = null!;
    private static int modelLocation = -1;
    private static int projectionLocation = -1;
    private static int timeLocation = -1;

    private PointsStruct[] _points = [];
    private SSBO<PointsStruct> _connectorLineSSBO = null!;
    private Descriptor _descriptor = null!;

    private int _vertexCount;

    public NodeConnectionRenderer()
    {
        _connectorLineSSBO = new(0, true);

        _descriptor = ConnectorLineShader.GetDescriptorSet();
        _descriptor.BindSSBO(_connectorLineSSBO, 0);
    }

    public static void Init()
    {
        ShaderInfo info = new() 
        {
            VertexShaderFile = "Noise_vulkan/ConnectorLine2.vert",
            FragmentShaderFile = "Noise_vulkan/ConnectorLine2.frag",
        };

        info.DepthStencil.DepthTestEnable = false;
        info.DepthStencil.DepthWriteEnable = false;
        
        ConnectorLineShader = new(info);

        ConnectorLineShader.Compile();

        modelLocation = ConnectorLineShader.GetLocation("ubo.model");
        projectionLocation = ConnectorLineShader.GetLocation("ubo.projection");
        timeLocation = ConnectorLineShader.GetLocation("d.time");
    }

    public void GenerateLines(NodeModule nodeModule)
    {
        var drag = nodeModule.DragConnection;
        _points = new PointsStruct[nodeModule.Connections.Count + (drag != null ? 1 : 0)];

        UpdateLineData(nodeModule, drag);

        _vertexCount = _points.Length * 6;
        _connectorLineSSBO.Renew(_points);
        _descriptor.BindSSBO(_connectorLineSSBO, 0);
    }

    public void UpdateLines(NodeModule nodeModule)
    {
        var drag = nodeModule.DragConnection;
        UpdateLineData(nodeModule, drag);

        _vertexCount = _points.Length * 6;
        _connectorLineSSBO.Update(_points);
    }

    private void UpdateLineData(NodeModule nodeModule, DragConnection? drag_N)
    {
        for(int i = 0; i < nodeModule.Connections.Count; i++) 
        {
            var connection = nodeModule.Connections[i];

            var inputNode = connection.InputNode;
            var outputNode = connection.OutputNode;

            NodeInput input;
            NodeOutput output;

            if (connection.Input.ConnectionType == NodeConnectionType.FlowInput)
            {
                input = inputNode.FlowInput;
            }
            else
            {
                input = inputNode.Inputs[connection.Input.Index];
            }

            if (connection.Output.ConnectionType == NodeConnectionType.FlowOutput)
            {
                output = outputNode.FlowOutput;
            }
            else
            {
                output = outputNode.Outputs[connection.Output.Index];
            }

            var inputPos = input.Position;
            var outputPos = output.Position;

            _points[i].PointA = inputPos;
            _points[i].PointB = outputPos;
            _points[i].ColorA = input.Color;
            _points[i].ColorB = output.Color;
        }

        if (drag_N != null)
        {
            var drag = drag_N.Value;

            var node = drag.Node;

            Vector2 connectionPos;
            Vector4 connectionColor;

            Vector2 mousePos = (Input.MousePosition - (nodeModule.Alignment.Left, nodeModule.Alignment.Top) - nodeModule.Position.Xy) * (1 / nodeModule.Scale);
            Vector4 mouseColor = NodeBase.Mouse;

            if (drag.Type == NodeConnectionType.Output)
            {
                var output = node.Outputs[drag.Index];

                connectionPos = output.Position;
                connectionColor = output.Color;

                _points[^1].PointA = mousePos;
                _points[^1].PointB = connectionPos;
                _points[^1].ColorA = mouseColor;
                _points[^1].ColorB = connectionColor;
            }
            else if (drag.Type == NodeConnectionType.Input)
            {
                var input = node.Inputs[drag.Index];

                connectionPos = input.Position;
                connectionColor = input.Color;

                _points[^1].PointA = connectionPos;
                _points[^1].PointB = mousePos;
                _points[^1].ColorA = connectionColor;
                _points[^1].ColorB = mouseColor;
            }
            else if (drag.Type == NodeConnectionType.FlowOutput)
            {
                var output = node.FlowOutput;

                connectionPos = output.Position;
                connectionColor = output.Color;

                _points[^1].PointA = mousePos;
                _points[^1].PointB = connectionPos;
                _points[^1].ColorA = mouseColor;
                _points[^1].ColorB = connectionColor;
            }
            else if (drag.Type == NodeConnectionType.FlowInput)
            {
                var input = node.FlowInput;

                connectionPos = input.Position;
                connectionColor = input.Color;

                _points[^1].PointA = connectionPos;
                _points[^1].PointB = mousePos;
                _points[^1].ColorA = connectionColor;
                _points[^1].ColorB = mouseColor;
            }
        }
    }

    public void Render(UIController uIController)
    {
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
}

internal struct PointsStruct
{
    public Vector2 PointA;
    public Vector2 PointB;
    public Vector4 ColorA;
    public Vector4 ColorB;
}