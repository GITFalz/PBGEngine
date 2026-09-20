using System.Runtime.InteropServices;

namespace PBG.MathLibrary;

public struct Vector4u : IVector<uint>
{
    public static readonly uint ByteSize = (uint)Marshal.SizeOf<Vector4u>();

    public readonly uint ElementCount => 4;
    
    public uint X;
    public uint Y;
    public uint Z;
    public uint W;

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
        readonly get => (X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    public Vector3u Xyw
    {
        get => (X, Y, W);
        set { X = value.X; Y = value.Y; W = value.Z; }
    }

    public Vector3u Xzw
    {
        get => (X, Z, W);
        set { X = value.X; Z = value.Y; W = value.Z; }
    }

    public Vector3u Yzw
    {
        get => (Y, Z, W);
        set { Y = value.X; Z = value.Y; W = value.Z; }
    }

    public static Vector4u Zero = new(0);
    public static Vector4u One = new(1);

    public Vector4u(uint x, uint y, uint z, uint w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4u(uint v)
    {
        X = v; Y = v; Z = v; W = v;
    }

    public static implicit operator Vector4u((uint x, uint y, uint z, uint w) data) => new(data.x, data.y, data.z, data.w);
    public static implicit operator Vector4(Vector4u v) => new(v.X, v.Y, v.Z, v.W);

    public static bool operator ==(Vector4u a, Vector4u b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
    public static bool operator !=(Vector4u a, Vector4u b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z || a.W != b.W;
    
    public static Vector4u operator -(Vector4u a, Vector4u b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

    public static Vector4u operator +(Vector4u a, Vector4u b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Vector4u operator +(Vector4u a, uint b) => new(a.X + b, a.Y + b, a.Z + b, a.W + b);

    public static Vector4u operator *(Vector4u a, Vector4u b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    public static Vector4u operator *(Vector4u a, uint b) => new(a.X * b, a.Y * b, a.Z * b, a.W * b);
    
    public static Vector4u operator /(Vector4u a, Vector4u b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
    public static Vector4u operator /(Vector4u a, uint b) => new(a.X / b, a.Y / b, a.Z / b, a.W / b);

    public uint this[int index]
    {
        readonly get 
        {
            return index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
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
                case 3: W = value; break;
                default: throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'");
            }
        }
    }

    public override string ToString() => $"({X}, {Y}, {Z}, {W})";

    public override bool Equals(object? obj)
    {
        if (obj is Vector4u v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }
}