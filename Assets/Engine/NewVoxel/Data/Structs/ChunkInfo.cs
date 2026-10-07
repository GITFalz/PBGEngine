using System.Runtime.InteropServices;
using PBG.MathLibrary;

namespace PBG.NewVoxel;

public struct ChunkInfo 
{
    public static readonly uint ByteSize = (uint)Marshal.SizeOf<ChunkInfo>();

    public Vector3 Center;
    public float Radius;
    public uint DataOffset;
    public uint VertexCount;
    public uint Active;
    public int SlotIndex;
};