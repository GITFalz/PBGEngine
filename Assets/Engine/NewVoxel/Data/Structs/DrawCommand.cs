using System.Runtime.InteropServices;

namespace PBG.NewVoxel;

[StructLayout(LayoutKind.Sequential)]
public struct DrawCommand
{
    public uint Count;
    public uint InstanceCount;
    public uint First;
    public uint BaseInstance;
}
