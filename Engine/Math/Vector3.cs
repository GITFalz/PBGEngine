namespace PBG.MathLibrary;

public struct Vector3
{
    public float X;
    public float Y;
    public float Z;

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

    public Vector2 Yz
    {
        get => (Y, Z);
        set { Y = value.X; Z = value.Y; }
    }

    public Vector3 Xyz
    {
        get => (X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }
    
    public readonly static Vector3 UnitX = (1, 0, 0);
    public readonly static Vector3 UnitY = (0, 1, 0);
    public readonly static Vector3 UnitZ = (0, 0, 1);

    public readonly static Vector3 Zero = new(0);
    public readonly static Vector3 One = new(1);

    public readonly float Length => Mathf.Sqrt((X * X) + (Y * Y) + (Z * Z));
    public readonly float LengthSquared => (X * X) + (Y * Y) + (Z * Z);

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    public Vector3(Vector3 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public Vector3(float v)
    {
        X = v; Y = v; Z = v;
    }

    public readonly Vector3 Normalized()
    {
        Vector3 v = this;
        v.Normalize();
        return v;
    }

    public void Normalize()
    {
        float scale = 1.0f / Length;
        X *= scale;
        Y *= scale;
        Z *= scale;
    }

    public static Vector3 Normalize(Vector3 v) => v.Normalized();

    public static float Dot(Vector3 left, Vector3 right)
    {
        return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
    }

    public static void Dot(Vector3 left, Vector3 right, out float result)
    {
        result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
    }

    public static float CalculateAngle(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
        return MathF.Acos(Math.Clamp(dot, -1f, 1f));
    }

    public static Vector3 Cross(Vector3 left, Vector3 right)
    {
        Cross(left, right, out var result);
        return result;
    }
    public static void Cross(Vector3 left, Vector3 right, out Vector3 result)
    {
        result.X = (left.Y * right.Z) - (left.Z * right.Y);
        result.Y = (left.Z * right.X) - (left.X * right.Z);
        result.Z = (left.X * right.Y) - (left.Y * right.X);
    }

    public static Vector3 Transform(Vector3 vec, Quaternion quat)
    {
        Transform(in vec, in quat, out Vector3 result);
        return result;
    }

    public static void Transform(in Vector3 vec, in Quaternion quat, out Vector3 result)
    {
        Vector3 xyz = quat.Xyz;
        Cross(xyz, vec, out Vector3 temp);
        Vector3 temp2 = vec * quat.W;
        temp += temp2;
        temp2 = Cross(xyz, temp);
        temp2 *= 2f;
        result = vec + temp2;
    }

    public static Vector3 TransformPosition(Vector3 position, Matrix4 matrix)
    {
        return new Vector3(
            matrix.M11 * position.X + matrix.M12 * position.Y + matrix.M13 * position.Z + matrix.M14,
            matrix.M21 * position.X + matrix.M22 * position.Y + matrix.M23 * position.Z + matrix.M24,
            matrix.M31 * position.X + matrix.M32 * position.Y + matrix.M33 * position.Z + matrix.M34
        );
    }

    public static Vector3 TransformNormal(Vector3 normal, Matrix4 matrix)
    {
        return Vector3.Normalize(new Vector3(
            matrix.M11 * normal.X + matrix.M12 * normal.Y + matrix.M13 * normal.Z,
            matrix.M21 * normal.X + matrix.M22 * normal.Y + matrix.M23 * normal.Z,
            matrix.M31 * normal.X + matrix.M32 * normal.Y + matrix.M33 * normal.Z
        ));
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        return (b - a).Length;
    }

    public static float DistanceSquared(Vector3 a, Vector3 b)
    {
        return (b - a).LengthSquared;
    }

    public static implicit operator Vector3((float x, float y, float z) data) => new(data.x, data.y, data.z);
    public static implicit operator (float x, float y, float z)(Vector3 data) => (data.X, data.Y, data.Z);

    public static bool operator ==(Vector3 a, Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator !=(Vector3 a, Vector3 b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vector3 operator *(Vector3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
    public static Vector3 operator *(float a, Vector3 b) => new(b.X * a, b.Y * a, b.Z * a);

    public static Vector3 operator /(Vector3 a, Vector3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    public static Vector3 operator /(Vector3 a, float b) => new(a.X / b, a.Y / b, a.Z / b);

    public float this[int index]
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
        if (obj is Vector3 v)
            return this == v;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}