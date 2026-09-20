namespace PBG.MathLibrary;

public struct Vector2u : IVector<uint>
{
    public static readonly uint ByteSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector2u>();

    public readonly uint ElementCount => 2;

    public uint X;
    public uint Y;

    public static Vector2u Zero = new(0);
    public static Vector2u One = new(1);

    public Vector2u(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public Vector2u(uint v)
    {
        X = v; Y = v;
    }

    public static implicit operator Vector2u((uint x, uint y) data) => new(data.x, data.y);
    public static implicit operator Vector2(Vector2u v) => new(v.X, v.Y);

    public static bool operator ==(Vector2u a, Vector2u b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2u a, Vector2u b) => a.X != b.X || a.Y != b.Y;

    public static Vector2u operator -(Vector2u a, Vector2u b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2u operator +(Vector2u a, Vector2u b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2u operator +(Vector2u a, uint b) => new(a.X + b, a.Y + b);

    public static Vector2u operator *(Vector2u a, Vector2u b) => new(a.X * b.X, a.Y * b.Y);
    public static Vector2u operator *(Vector2u a, uint b) => new(a.X * b, a.Y * b);
    
    public static Vector2u operator /(Vector2u a, Vector2u b) => new(a.X / b.X, a.Y / b.Y);
    public static Vector2u operator /(Vector2u a, uint b) => new(a.X / b, a.Y / b);

    public uint this[int index]
    {
        readonly get 
        {
            return index switch
            {
                0 => X,
                1 => Y,
                _ => throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'")
            };
        }
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                default: throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'");
            }
        }
    }

    public override string ToString() => $"({X}, {Y})";

    public override bool Equals(object? obj)
    {
        if (obj is Vector2u v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}