namespace PBG.MathLibrary;

public struct Vector2
{
    public float X;
    public float Y;

    public static Vector2 Zero = new(0);
    public static Vector2 One = new(1);

    public readonly float Length => Mathf.Sqrt((X * X) + (Y * Y));
    public readonly float LengthSquared => (X * X) + (Y * Y);

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vector2(float v)
    {
        X = v; Y = v;
    }

    public static float Dot(Vector2 left, Vector2 right)
    {
        return (left.X * right.X) + (left.Y * right.Y);
    }

    public static void Dot(Vector2 left, Vector2 right, out float result)
    {
        result = (left.X * right.X) + (left.Y * right.Y);
    }

    public static float DistanceSquared(Vector2 a, Vector2 b)
    {
        return (b - a).LengthSquared;
    }

    public static implicit operator Vector2(System.Numerics.Vector2 v) => new(v.X, v.Y);
    public static implicit operator Vector2((float x, float y) data) => new(data.x, data.y);

    public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2 a, Vector2 b) => a.X != b.X || a.Y != b.Y;

    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.X * b.X, a.Y * b.Y);
    public static Vector2 operator *(Vector2 a, float b) => new(a.X * b, a.Y * b);

    public static Vector2 operator /(Vector2 a, Vector2 b) => new(a.X / b.X, a.Y / b.Y);
    public static Vector2 operator /(Vector2 a, float b) => new(a.X / b, a.Y / b);

    public float this[int index]
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
        if (obj is Vector2 v)
            return this == v;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}