using PBG.MathLibrary;
using PBG.Data;
using PBG.Graphics;
using PBG.UI;

public class ConnectionRenderer
{
    //public static ShaderProgram ConnectorLineShaderProgram = new ShaderProgram("Noise/ConnectorLine.vert", "Noise/ConnectorLine.frag");
    //private static VAO _connectorLineVAO = new VAO();
    //private SSBO<PointsStruct> _connectorLineSSBO = new(new List<PointsStruct>());
    private int _vertexCount;

    public void GenerateLines(NodeCollection nodeCollection)
    {
        var points = GetLines(nodeCollection);
        _vertexCount = points.Count * 6;
        //_connectorLineSSBO.Renew(points);
    }

    public void UpdateLines(NodeCollection nodeCollection)
    {
        var points = GetLines(nodeCollection);
        _vertexCount = points.Count * 6;
        //_connectorLineSSBO.Update(points, 0);
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

        //_connectorLineSSBO.Renew(points);
        _vertexCount = points.Count * 6;

        return points;
    }

    public void RenderLines(UIController uIController)
    {
        /*
        int[] viewport = new int[4];
        GL.GetInteger(GetPName.Viewport, viewport);

        UIController.BindFramebuffer();

        int width = uIController.Alignment.Width;
        int height = uIController.Alignment.Height;

        GL.Viewport(uIController.Alignment.Left, uIController.Alignment.Bottom, width, height);

        ConnectorLineShaderProgram.Bind();

        int modelLocation = ConnectorLineShaderProgram.GetLocation("model");
        int projectionLocation = ConnectorLineShaderProgram.GetLocation("projection");
        int timeLocation = ConnectorLineShaderProgram.GetLocation("time");

        Matrix4 model = uIController.ModelMatrix * Matrix4.CreateTranslation((0, 0, UIController.CumulativeDepth));
        Matrix4 projection = uIController.GetProjection();

        GL.UniformMatrix4(modelLocation, false, ref model);
        GL.UniformMatrix4(projectionLocation, false, ref projection);

        GL.Uniform1(timeLocation, GameTime.TotalTime);

        _connectorLineSSBO.Bind(0);
        _connectorLineVAO.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);

        _connectorLineVAO.Unbind();
        _connectorLineSSBO.Unbind();

        ConnectorLineShaderProgram.Unbind();

        UIController.CumulativeDepth += 0.00001f;

        UIController.UnbindFramebuffer();

        GL.Viewport(viewport[0], viewport[1], viewport[2], viewport[3]);
        */
    }

    private struct PointsStruct
    {
        public Vector2 PointA;
        public Vector2 PointB;
        public Vector4 ColorA;
        public Vector4 ColorB;
    }
}
