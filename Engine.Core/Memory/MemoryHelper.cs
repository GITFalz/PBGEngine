using System.Runtime.InteropServices;

namespace PBG.Core;

public unsafe static class MemoryHelper
{
    public static T* Alloc<T>(int count) where T : unmanaged
    {
        return (T*)NativeMemory.Alloc((nuint)(sizeof(T) * count));
    }

    public static T* AllocClear<T>(int count) where T : unmanaged
    {
        var map = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * count));
        Clear(map, count);
        return map;
    }

    public static T** AllocPtr<T>(int count) where T : unmanaged
    {
        return (T**)NativeMemory.Alloc((nuint)(sizeof(T*) * count));
    }



    public static void Clear<T>(T* ptr, int elementCount) where T : unmanaged
    {
        NativeMemory.Clear(ptr, (nuint)(sizeof(T) * elementCount));
    }

    public static void Clear<T>(T** ptr, int elementCount) where T : unmanaged
    {
        NativeMemory.Clear(ptr, (nuint)(sizeof(T*) * elementCount));
    }



    public static void Free<T>(T* ptr) where T : unmanaged
    {
        NativeMemory.Free(ptr);
    }

    public static void Free<T>(T** ptr) where T : unmanaged
    {
        NativeMemory.Free(ptr);
    }
    
    public static void Free<T>(ref T* ptr) where T : unmanaged
    {
        NativeMemory.Free(ptr);
        ptr = null;
    }
}