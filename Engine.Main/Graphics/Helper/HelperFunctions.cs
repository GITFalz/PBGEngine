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
        if (size % 16 != 0)
            Console.WriteLine($"[Warning] {typeof(T).Name} is {size} bytes which is not 16 byte aligned. " +
                            $"This will cause padding issues in SSBO. " +
                            $"Add {16 - (size % 16)} bytes of padding to your struct.");
    }
}