using System.Runtime.InteropServices;
using PBG.Mathematics;

namespace PBG.Voxel;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlockVertexData
{
    public float PX, PY, PZ;
    public float UX, UY;
    public float NX, NY, NZ;
    public int TextureIndex;

    public BlockVertexData(Vector3 position, Vector2 uvs, Vector3 normals, int texture)
    {
        PX = position.X; PY = position.Y; PZ = position.Z;
        UX = uvs.X; UY = uvs.Y;
        NX = normals.X; NY = normals.Y; NZ = normals.Z;
        TextureIndex = texture;
    }

    public BlockVertexData(Vector3 position, Vector2 uvs, Vector3 normals, int texture, int side, int corner)
    {
        PX = position.X; PY = position.Y; PZ = position.Z;
        UX = uvs.X; UY = uvs.Y;
        NX = normals.X; NY = normals.Y; NZ = normals.Z;
        TextureIndex = texture | (side << 16) | (corner << 20);
    }

    public override string ToString()
    {
        int texture = TextureIndex & 0xFFFF;
        int ao = TextureIndex >> 16;

        return $"P({PX},{PY},{PZ}) U({UX},{UY}) N({NX},{NY},{NZ}) T:{texture} AO:{ao}";
    }

    public void SetAmbientOcclusion(int ao)
    {
        TextureIndex = (TextureIndex & 0x0000FFFF) | (ao << 16);
    }
}