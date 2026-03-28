using PBG.Core;
using PBG.Parse;

namespace PBG.MathLibrary;

public struct Vector4i : ISceneSerializable
{
    public int X;
    public int Y;
    public int Z;
    public int W;

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

    public Vector3i Xyw
    {
        get => (X, Y, W);
        set { X = value.X; Y = value.Y; W = value.Z; }
    }

    public Vector3i Xzw
    {
        get => (X, Z, W);
        set { X = value.X; Z = value.Y; W = value.Z; }
    }

    public Vector3i Yzw
    {
        get => (Y, Z, W);
        set { Y = value.X; Z = value.Y; W = value.Z; }
    }

    public static Vector4i Zero = new(0);
    public static Vector4i One = new(1);

    public Vector4i(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4i(int v)
    {
        X = v; Y = v; Z = v; W = v;
    }

    public static implicit operator Vector4i((int x, int y, int z, int w) data) => new(data.x, data.y, data.z, data.w);
    public static implicit operator Vector4(Vector4i v) => new(v.X, v.Y, v.Z, v.W);

    public static bool operator ==(Vector4i a, Vector4i b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
    public static bool operator !=(Vector4i a, Vector4i b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z || a.W != b.W;
    
    public static Vector4i operator -(Vector4i a, Vector4i b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    public static Vector4i operator -(Vector4i v) => new(-v.X, -v.Y, -v.Z, -v.W);

    public static Vector4i operator +(Vector4i a, Vector4i b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    public static Vector4i operator *(Vector4i a, Vector4i b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    public static Vector4i operator *(Vector4i a, int b) => new(a.X * b, a.Y * b, a.Z * b, a.W * b);
    
    public static Vector4i operator /(Vector4i a, Vector4i b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
    public static Vector4i operator /(Vector4i a, int b) => new(a.X / b, a.Y / b, a.Z / b, a.W / b);

    public int this[int index]
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
        if (obj is Vector4i v)
            return this == v;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public List<string> ToStringList() => [X+"", Y+"", Z+"", W+""];
    public Vector4i Set(List<string> list)
    {
        for (int i = 0; i < 4.Min(list.Count); i++)
            this[i] = Int.Parse(list[i], this[i]);
        return this;
    }

    public void Deserialize(List<SceneFieldJson> data)
    {
        if (data.Count > 0 && data[0].TryParse(out int x)) X = x;
        if (data.Count > 1 && data[1].TryParse(out int y)) Y = y;
        if (data.Count > 2 && data[2].TryParse(out int z)) Z = z;
        if (data.Count > 3 && data[3].TryParse(out int w)) W = w;
    }

    public List<SceneFieldJson> Serialize() => [ new(X), new(Y), new(Z), new(W) ];
}