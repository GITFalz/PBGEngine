namespace PBG.MathLibrary;

public struct Vector3i
{
    public int X;
    public int Y;
    public int Z;

    public Vector2i Xy
    {
        get => (X, Y);
        set { X = value.X; Y = value.Y; }
    }

    public Vector2i Xz
    {
        get => (X, Z);
        set { X = value.X; Z = value.Y; }
    }

    public Vector2i Yz
    {
        get => (Y, Z);
        set { Y = value.X; Z = value.Y; }
    }

    public Vector3i Xyz
    {
        get => (X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    public static Vector3i Zero = new(0);
    public static Vector3i One = new(1);

    public Vector3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3i(Vector2i xy, int z)
    {
        Xy = xy;
        Z = z;
    }

    public Vector3i(int v)
    {
        X = v; Y = v; Z = v;
    }

    public static implicit operator Vector3i((int x, int y, int z) data) => new(data.x, data.y, data.z);
    public static implicit operator Vector3(Vector3i v) => new(v.X, v.Y, v.Z);

    public static bool operator ==(Vector3i a, Vector3i b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vector3i a, Vector3i b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

    public static Vector3i operator -(Vector3i a, Vector3i b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3i operator -(Vector3i v) => new(-v.X, -v.Y, -v.Z);

    public static Vector3i operator +(Vector3i a, Vector3i b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3i operator *(Vector3i a, Vector3i b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vector3i operator *(Vector3i a, int b) => new(a.X * b, a.Y * b, a.Z * b);
    
    public static Vector3i operator /(Vector3i a, Vector3i b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    public static Vector3i operator /(Vector3i a, int b) => new(a.X / b, a.Y / b, a.Z / b);

    public int this[int index]
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
        if (obj is Vector3i v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}