using System.Runtime.InteropServices;

namespace PBG.Graphics;

public unsafe static class HelperFunctions
{
    public static bool Empty<T>(this T[] array) where T : struct => array.Length == 0;
    public static void MemCpyTo<T>(void* source, void* data, ulong destinationSizeInBytes, ulong sourceBytesToCopy) where T : unmanaged
    {
        Buffer.MemoryCopy(source, data, destinationSizeInBytes, sourceBytesToCopy);
    }

    public static void MemCpyTo<T>(T[] array, void* data, long destinationSizeInBytes, long sourceBytesToCopy) where T : unmanaged
    {
        fixed (T* Pdata = array)
        {
            Buffer.MemoryCopy(Pdata, data, destinationSizeInBytes, sourceBytesToCopy);
        }
    }

    public static void MemCpyTo<T>(T[] array, void* data, ulong destinationSizeInBytes, ulong sourceBytesToCopy) where T : unmanaged
    {
        fixed (T* Pdata = array)
        {
            Buffer.MemoryCopy(Pdata, data, destinationSizeInBytes, sourceBytesToCopy);
        }
    }

    public static void MemCpyFrom<T>(void* data, T[] array, long destinationSizeInBytes, long sourceBytesToCopy) where T : unmanaged
    {
        fixed (T* Pdata = array)
        {
            Buffer.MemoryCopy(data, Pdata, destinationSizeInBytes, sourceBytesToCopy);
        }
    }

    public static void MemCpyFrom<T>(void* data, T[] array, ulong destinationSizeInBytes, ulong sourceBytesToCopy) where T : unmanaged
    {
        fixed (T* Pdata = array)
        {
            Buffer.MemoryCopy(data, Pdata, destinationSizeInBytes, sourceBytesToCopy);
        }
    }

    public static void CheckAlignment<T>() where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();

        if (size <= 8 || size % 16 == 0) return;

        int paddedSize = (size + 15) & ~15;

        Console.WriteLine(
            $"[Warning] {typeof(T).Name} is {size} bytes, which is not a multiple of 16. " +
            $"If it contains a vec3 or vec4, GLSL will align the struct to 16 bytes " +
            $"(stride becomes {paddedSize}), and each vec3 also starts on a 16-byte boundary. " +
            $"A tightly packed C# layout like {{ vec3; vec2; }} will not match the shader. " +
            $"Fix: add explicit padding after each vec3 (e.g. a float, or use Vector4), " +
            $"not just at the end of the struct.");
    }
}