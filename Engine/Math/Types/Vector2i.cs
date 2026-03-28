using PBG.Core;
using PBG.Parse;

namespace PBG.MathLibrary;

public struct Vector2i : ISceneSerializable
{
    public int X;
    public int Y;

    public static Vector2i Zero = new(0);
    public static Vector2i One = new(1);

    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Vector2i(int v)
    {
        X = v; Y = v;
    }

    public static implicit operator Vector2i((int x, int y) data) => new(data.x, data.y);
    public static implicit operator Vector2(Vector2i v) => new(v.X, v.Y);

    public static bool operator ==(Vector2i a, Vector2i b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2i a, Vector2i b) => a.X != b.X || a.Y != b.Y;

    public static Vector2i operator -(Vector2i a, Vector2i b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2i operator -(Vector2i v) => new(-v.X, -v.Y);

    public static Vector2i operator +(Vector2i a, Vector2i b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2i operator *(Vector2i a, Vector2i b) => new(a.X * b.X, a.Y * b.Y);
    public static Vector2i operator *(Vector2i a, int b) => new(a.X * b, a.Y * b);
    
    public static Vector2i operator /(Vector2i a, Vector2i b) => new(a.X / b.X, a.Y / b.Y);
    public static Vector2i operator /(Vector2i a, int b) => new(a.X / b, a.Y / b);

    public int this[int index]
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
        if (obj is Vector2i v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public List<string> ToStringList() => [X+"", Y+""];
    public Vector2i Set(List<string> list)
    {
        for (int i = 0; i < 2.Min(list.Count); i++)
            this[i] = Int.Parse(list[i], this[i]);
        return this;
    }

    public void Deserialize(List<SceneFieldJson> data)
    {
        if (data.Count > 0 && data[0].TryParse(out int x)) X = x;
        if (data.Count > 1 && data[1].TryParse(out int y)) Y = y;
    }

    public List<SceneFieldJson> Serialize() => [ new(X), new(Y) ];
}