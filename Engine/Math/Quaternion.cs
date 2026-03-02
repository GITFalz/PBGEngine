namespace PBG.MathLibrary
{
    public struct Quaternion
    {
        public float X, Y, Z, W;
        public Vector3 XYZ => (X, Y, Z);

        public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);

        public Quaternion(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        public Quaternion(Vector3 vectorPart, float scalarPart)
        {
            X = vectorPart.X;
            Y = vectorPart.Y;
            Z = vectorPart.Z;
            W = scalarPart;
        }

        public float LengthSquared() => X * X + Y * Y + Z * Z + W * W;
        public float Length() => MathF.Sqrt(LengthSquared());

        public Quaternion Normalized()
        {
            float len = Length();
            if (len < 1e-6f) return Identity;
            return new Quaternion(X / len, Y / len, Z / len, W / len);
        }

        public Quaternion Conjugate() => new Quaternion(-X, -Y, -Z, W);

        public Quaternion Inverse()
        {
            float lenSq = LengthSquared();
            if (lenSq < 1e-10f) return Identity;
            var c = Conjugate();
            return new Quaternion(c.X / lenSq, c.Y / lenSq, c.Z / lenSq, c.W / lenSq);
        }

        public static Quaternion FromAxisAngle(Vector3 axis, float angleRadians)
        {
            axis = Vector3.Normalize(axis);
            float half = angleRadians * 0.5f;
            float s = MathF.Sin(half);
            return new Quaternion(axis.X * s, axis.Y * s, axis.Z * s, MathF.Cos(half));
        }

        public static Quaternion FromEuler(float pitchRad, float yawRad, float rollRad)
        {
            float cp = MathF.Cos(pitchRad * 0.5f), sp = MathF.Sin(pitchRad * 0.5f);
            float cy = MathF.Cos(yawRad   * 0.5f), sy = MathF.Sin(yawRad   * 0.5f);
            float cr = MathF.Cos(rollRad  * 0.5f), sr = MathF.Sin(rollRad  * 0.5f);

            return new Quaternion(
                sr * cp * cy - cr * sp * sy,
                cr * sp * cy + sr * cp * sy,
                cr * cp * sy - sr * sp * cy,
                cr * cp * cy + sr * sp * sy
            );
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from = Vector3.Normalize(from);
            to   = Vector3.Normalize(to);
            float dot = Vector3.Dot(from, to);

            if (dot >= 1.0f - 1e-6f) return Identity;

            if (dot <= -1.0f + 1e-6f)
            {
                Vector3 perp = MathF.Abs(from.X) < 0.9f
                    ? Vector3.Cross(from, Vector3.UnitX)
                    : Vector3.Cross(from, Vector3.UnitY);
                return FromAxisAngle(perp, MathF.PI);
            }

            Vector3 cross = Vector3.Cross(from, to);
            return new Quaternion(cross.X, cross.Y, cross.Z, 1f + dot).Normalized();
        }

        public (float pitch, float yaw, float roll) ToEuler()
        {
            float pitch = MathF.Asin(Math.Clamp(2f * (W * X - Y * Z), -1f, 1f));
            float yaw   = MathF.Atan2(2f * (W * Y + X * Z), 1f - 2f * (X * X + Y * Y));
            float roll  = MathF.Atan2(2f * (W * Z + X * Y), 1f - 2f * (X * X + Z * Z));
            return (pitch, yaw, roll);
        }

        public static Quaternion operator *(Quaternion a, Quaternion b) =>
            new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
            );

        public Vector3 Rotate(Vector3 v)
        {
            var qv = new Quaternion(v.X, v.Y, v.Z, 0f);
            var r  = this * qv * Conjugate();
            return new Vector3(r.X, r.Y, r.Z);
        }

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Quaternion(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            ).Normalized();
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            float dot = a.X*b.X + a.Y*b.Y + a.Z*b.Z + a.W*b.W;

            if (dot < 0f) { b = new Quaternion(-b.X, -b.Y, -b.Z, -b.W); dot = -dot; }

            if (dot > 0.9995f) return Lerp(a, b, t);

            float theta    = MathF.Acos(dot);
            float sinTheta = MathF.Sin(theta);
            float wa = MathF.Sin((1f - t) * theta) / sinTheta;
            float wb = MathF.Sin(t * theta)         / sinTheta;

            return new Quaternion(
                wa * a.X + wb * b.X,
                wa * a.Y + wb * b.Y,
                wa * a.Z + wb * b.Z,
                wa * a.W + wb * b.W
            );
        }

        public Quaternion Inverted()
        {
            float lenSq = LengthSquared();
            if (lenSq < 1e-10f) return Identity;
            var c = Conjugate();
            return new Quaternion(c.X / lenSq, c.Y / lenSq, c.Z / lenSq, c.W / lenSq);
        }

        public override string ToString() => $"Quaternion({X:F4}, {Y:F4}, {Z:F4}, {W:F4})";
    }
}