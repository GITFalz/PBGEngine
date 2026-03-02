namespace PBG.MathLibrary;

public struct Vector4
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    public Vector2 Xy
    {
        get => (X, Y);
        set { X = value.X; Y = value.Y; }
    }

    public Vector2 Xz
    {
        get => (X, Z);
        set { X = value.X; Z = value.Y; }
    }

    public Vector2 Xw
    {
        get => (X, W);
        set { X = value.X; W = value.Y; }
    }

    public Vector2 Yz
    {
        get => (Y, Z);
        set { Y = value.X; Z = value.Y; }
    }

    public Vector2 Yw
    {
        get => (Y, W);
        set { Y = value.X; W = value.Y; }
    }

    public Vector2 Zw
    {
        get => (Z, W);
        set { Z = value.X; W = value.Y; }
    }

    public Vector3 Xyz
    {
        get => (X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    public Vector3 Xyw
    {
        get => (X, Y, W);
        set { X = value.X; Y = value.Y; W = value.Z; }
    }

    public Vector3 Xzw
    {
        get => (X, Z, W);
        set { X = value.X; Z = value.Y; W = value.Z; }
    }

    public Vector3 Yzw
    {
        get => (Y, Z, W);
        set { Y = value.X; Z = value.Y; W = value.Z; }
    }

    public static Vector4 Zero = new(0);
    public static Vector4 One = new(1);

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4(Vector3 v, float w)
    {
        X = v.X; Y = v.Y; Z = v.Z; W = w;
    }

    public Vector4(float v)
    {
        X = v; Y = v; Z = v; W = v;
    }

    public static implicit operator Vector4((float x, float y, float z, float w) data) => new(data.x, data.y, data.z, data.w);

    public static bool operator ==(Vector4 a, Vector4 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
    public static bool operator !=(Vector4 a, Vector4 b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z || a.W != b.W;
    
    public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    public static Vector4 operator -(Vector4 v) => new(-v.X, -v.Y, -v.Z, -v.W);

    public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    public static Vector4 operator *(Vector4 a, Vector4 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    public static Vector4 operator *(Vector4 a, float b) => new(a.X * b, a.Y * b, a.Z * b, a.W * b);
    
    public static Vector4 operator /(Vector4 a, Vector4 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
    public static Vector4 operator /(Vector4 a, float b) => new(a.X / b, a.Y / b, a.Z / b, a.W / b);

    public float this[int index]
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
        if (obj is Vector4 v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }
}