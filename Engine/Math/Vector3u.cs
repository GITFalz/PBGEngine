using System.Runtime.InteropServices;
using PBG.NewVoxel;


namespace PBG.MathLibrary;

public struct Vector3u : IVector<uint>
{
    public static readonly uint ByteSize = (uint)Marshal.SizeOf<Vector3u>();

    public readonly uint ElementCount => 3;
    
    public uint X;
    public uint Y;
    public uint Z;

    public Vector2u Xy
    {
        get => (X, Y);
        set { X = value.X; Y = value.Y; }
    }

    public Vector2u Xz
    {
        get => (X, Z);
        set { X = value.X; Z = value.Y; }
    }

    public Vector2u Yz
    {
        get => (Y, Z);
        set { Y = value.X; Z = value.Y; }
    }

    public Vector3u Xyz
    {
        get => (X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    public static Vector3u Zero = new(0);
    public static Vector3u One = new(1);

    public Vector3u(uint x, uint y, uint z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3u(Vector2u xy, uint z)
    {
        Xy = xy;
        Z = z;
    }

    public Vector3u(uint v)
    {
        X = v; Y = v; Z = v;
    }

    public static implicit operator Vector3u((uint x, uint y, uint z) data) => new(data.x, data.y, data.z);
    public static implicit operator Vector3(Vector3u v) => new(v.X, v.Y, v.Z);

    public static bool operator ==(Vector3u a, Vector3u b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vector3u a, Vector3u b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

    public static Vector3u operator -(Vector3u a, Vector3u b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3u operator +(Vector3u a, Vector3u b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3u operator +(Vector3u a, uint b) => new(a.X + b, a.Y + b, a.Z + b);

    public static Vector3u operator *(Vector3u a, Vector3u b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vector3u operator *(Vector3u a, uint b) => new(a.X * b, a.Y * b, a.Z * b);
    
    public static Vector3u operator /(Vector3u a, Vector3u b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    public static Vector3u operator /(Vector3u a, uint b) => new(a.X / b, a.Y / b, a.Z / b);

    public uint this[int index]
    {
        readonly get 
        {
            return index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                _ => throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'")
            };
        }
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                default: throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'");
            }
        }
    }

    public override string ToString() => $"({X}, {Y}, {Z})";

    public override bool Equals(object? obj)
    {
        if (obj is Vector3u v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}