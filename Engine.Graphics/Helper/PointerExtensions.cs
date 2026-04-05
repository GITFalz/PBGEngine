using Silk.NET.Core.Native;

public unsafe static class PtrExt
{
    public static nint ToPtr(this string[] array) => SilkMarshal.StringArrayToPtr(array);
    public static nint ToPtr(this string[] array, out nint ptr)
    {
        ptr = SilkMarshal.StringArrayToPtr(array);
        return ptr;
    }

    public static nint ToPtr(this string str) => SilkMarshal.StringToPtr(str);
    public static nint ToPtr(this string str, out nint ptr)
    {
        ptr = SilkMarshal.StringToPtr(str);
        return ptr;
    }

    public static void Free(byte** ptr) => SilkMarshal.Free((nint)ptr);
    public static void Free(this nint ptr) => SilkMarshal.Free(ptr);

    public static string? ToStr(byte* ptr) => SilkMarshal.PtrToString((nint)ptr);
    public static string? ToStr(this nint ptr) => SilkMarshal.PtrToString(ptr);

    public static string[] ToStrArray(byte* ptr, uint count) => SilkMarshal.PtrToStringArray((nint)ptr, (int)count);
    public static string[] ToStrArray(byte* ptr, int count) => SilkMarshal.PtrToStringArray((nint)ptr, count);
    public static string[] ToStrArray(this nint ptr, uint count) => SilkMarshal.PtrToStringArray(ptr, (int)count);
    public static string[] ToStrArray(this nint ptr, int count) => SilkMarshal.PtrToStringArray(ptr, count);
}