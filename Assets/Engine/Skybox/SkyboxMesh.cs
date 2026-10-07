using PBG.MathLibrary;
using PBG.Graphics;
using Buffer = Silk.NET.Vulkan.Buffer;

public class SkyboxMesh
{
    private VBO<Vector3> _vertVbo;
    private VBO<Vector2> _uvVbo;
    private VBO<int> _textureVbo;
    private IBO _ibo;

    private Buffer[] _buffers = [];
    private ulong[] _offsets = [];



    public SkyboxMesh()
    {
        Vector3[] vertices =
        [
            // Front
            (-1, -1, -1),
            (1, -1, -1),
            (1, 1, -1),
            (-1, 1, -1),

            // Right
            (1, -1, -1),
            (1, -1, 1),
            (1, 1, 1),
            (1, 1, -1),

            // Top
            (-1, 1, -1),
            (1, 1, -1),
            (1, 1, 1),
            (-1, 1, 1),

            // Left
            (-1, -1, -1),
            (-1, 1, -1),
            (-1, 1, 1),
            (-1, -1, 1),

            // Bottom
            (-1, -1, -1),
            (1, -1, -1),
            (1, -1, 1),
            (-1, -1, 1),

            // Back
            (-1, -1, 1),
            (1, -1, 1),
            (1, 1, 1),
            (-1, 1, 1)
        ];

        Vector2[] uvs =
        [
            (0, 0), (1, 0), (1, 1), (0, 1),
            (0, 0), (1, 0), (1, 1), (0, 1),
            (0, 0), (1, 0), (1, 1), (0, 1),
            (0, 0), (1, 0), (1, 1), (0, 1),
            (0, 0), (1, 0), (1, 1), (0, 1),
            (0, 0), (1, 0), (1, 1), (0, 1)
        ];
        
        int[] textureIndices = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,];

        uint[] indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 18, 17, 18, 16, 19,
            20, 22, 21, 22, 20, 23
        ];

        _vertVbo = new(vertices);
        _uvVbo = new(uvs);
        _textureVbo = new(textureIndices);
        _ibo = new(indices);

        _buffers = [_vertVbo.Buffer, _uvVbo.Buffer, _textureVbo.Buffer];
        _offsets = [0, 0, 0];
    }

    public void Render()
    {
        VBOBase.Bind(_buffers, _offsets);
        _ibo.Bind();

        GFX.DrawIndexed(36, 1, 0, 0, 0);
    }
}