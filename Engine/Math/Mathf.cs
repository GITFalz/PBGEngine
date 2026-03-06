using Vec4 = System.Numerics.Vector4;
using Vec3 = System.Numerics.Vector3;
using Vec2 = System.Numerics.Vector2;

namespace PBG.MathLibrary
{
    public static partial class Mathf
    {
        #region FLOOR
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static int FloorToInt(double value) => (int)Math.Floor(value);
        public static Vector3i FloorToInt(Vector3 value) => new Vector3i(FloorToInt(value.X), FloorToInt(value.Y), FloorToInt(value.Z));
        public static Vector2i FloorToInt(Vector2 value) => new Vector2i(FloorToInt(value.X), FloorToInt(value.Y));

        public static int floorToInt(this float value) => FloorToInt(value);
        public static int floorToInt(this double value) => FloorToInt(value);
        public static Vector2i floorToInt(this Vector2 value) => FloorToInt(value);
        public static Vector3i floorToInt(this Vector3 value) => FloorToInt(value);

        public static int Fti(this float value) => FloorToInt(value);
        public static int Fti(this double value) => FloorToInt(value);
        public static Vector2i Fti(this Vector2 value) => FloorToInt(value);
        public static Vector3i Fti(this Vector3 value) => FloorToInt(value);
        
        public static float Floor(float value) => (float)Math.Floor(value);
        public static double Floor(double value) => Math.Floor(value);
        public static Vector2 Floor(Vector2 value) => new Vector2(Floor(value.X), Floor(value.Y));
        public static Vector3 Floor(Vector3 value) => new Vector3(Floor(value.X), Floor(value.Y), Floor(value.Z));
        public static Vector4 Floor(Vector4 value) => new Vector4(Floor(value.X), Floor(value.Y), Floor(value.Z), Floor(value.W));

        public static void Floor(this ref float value) => value = Floor(value);
        public static void Floor(this ref double value) => value = Floor(value);
        public static void Floor(this ref Vector2 value) => value = Floor(value);
        public static void Floor(this ref Vector3 value) => value = Floor(value);
        public static void Floor(this ref Vector4 value) => value = Floor(value);
        #endregion

        #region ROUND
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static Vector2i RoundToInt(Vector2 value) => new(RoundToInt(value.X), RoundToInt(value.Y));
        public static Vector3i RoundToInt(Vector3 value) => new(RoundToInt(value.X), RoundToInt(value.Y), RoundToInt(value.Z));
        public static Vector4i RoundToInt(Vector4 value) => new(RoundToInt(value.X), RoundToInt(value.Y), RoundToInt(value.Z), RoundToInt(value.W));

        public static int Rti(this float value) => RoundToInt(value);
        public static Vector2i Rti(this Vector2 value) => RoundToInt(value);
        public static Vector3i Rti(this Vector3 value) => RoundToInt(value);
        public static Vector4i Rti(this Vector4 value) => RoundToInt(value);

        public static float Round(float value) => (float)Math.Round(value);
        public static Vector2 Round(Vector2 value) => new Vector2(Round(value.X), Round(value.Y));
        public static Vector3 Round(Vector3 value) => new Vector3(Round(value.X), Round(value.Y), Round(value.Z));
        public static Vector4 Round(Vector4 value) => new Vector4(Round(value.X), Round(value.Y), Round(value.Z), Round(value.W));

        public static void Round(this ref float value) => value = Round(value);
        public static void Round(this ref Vector2 value) => value = Round(value);
        public static void Round(this ref Vector3 value) => value = Round(value);
        public static void Round(this ref Vector4 value) => value = Round(value);
        #endregion

        #region CEIL
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
        public static Vector2i CeilToInt(Vector2 value) => new(CeilToInt(value.X), CeilToInt(value.Y));
        public static Vector3i CeilToInt(Vector3 value) => new(CeilToInt(value.X), CeilToInt(value.Y), CeilToInt(value.Z));
        public static Vector4i CeilToInt(Vector4 value) => new(CeilToInt(value.X), CeilToInt(value.Y), CeilToInt(value.Z), CeilToInt(value.W));

        public static float Ceil(float value) => (float)Math.Ceiling(value);
        public static Vector2 Ceil(Vector2 value) => new Vector2(Ceil(value.X), Ceil(value.Y));
        public static Vector3 Ceil(Vector3 value) => new Vector3(Ceil(value.X), Ceil(value.Y), Ceil(value.Z));
        public static Vector4 Ceil(Vector4 value) => new Vector4(Ceil(value.X), Ceil(value.Y), Ceil(value.Z), Ceil(value.W));

        public static void Ceil(this ref float value) => value = Ceil(value);
        public static void Ceil(this ref Vector2 value) => value = Ceil(value);
        public static void Ceil(this ref Vector3 value) => value = Ceil(value);
        public static void Ceil(this ref Vector4 value) => value = Ceil(value);

        public static int Cti(this float value) => CeilToInt(value);
        public static Vector2i Cti(this Vector2 value) => CeilToInt(value);
        public static Vector3i Cti(this Vector3 value) => CeilToInt(value);
        public static Vector4i Cti(this Vector4 value) => CeilToInt(value);
        #endregion

        #region CLAMP
        public static float Clampy(float value, float min, float max) => value < min ? min : value > max ? max : value;
        public static int Clampy(int value, int min, int max) => value < min ? min : value > max ? max : value;
        public static uint Clampy(uint value, uint min, uint max) => value < min ? min : value > max ? max : value;
        public static byte Clampy(byte value, byte min, byte max) => value < min ? min : value > max ? max : value;
        public static float Clamp01y(float value) => value < 0 ? 0 : value > 1 ? 1 : value;
        public static int Clamp01y(int value) => value < 0 ? 0 : value > 1 ? 1 : value;

        public static Vector2 Clampy(Vector2 value, Vector2 min, Vector2 max) => new Vector2(Clampy(value.X, min.X, max.X), Clampy(value.Y, min.Y, max.Y));
        public static Vector3 Clampy(Vector3 value, Vector3 min, Vector3 max) => new Vector3(Clampy(value.X, min.X, max.X), Clampy(value.Y, min.Y, max.Y), Clampy(value.Z, min.Z, max.Z));
        public static Vector4 Clampy(Vector4 value, Vector4 min, Vector4 max) => new Vector4(Clampy(value.X, min.X, max.X), Clampy(value.Y, min.Y, max.Y), Clampy(value.Z, min.Z, max.Z), Clampy(value.W, min.W, max.W));

        public static Vector2i Clampy(Vector2i value, Vector2i min, Vector2i max) => new Vector2i(Clampy(value.X, min.X, max.X), Clampy(value.Y, min.Y, max.Y));
        public static Vector3i Clampy(Vector3i value, Vector3i min, Vector3i max) => new Vector3i(Clampy(value.X, min.X, max.X), Clampy(value.Y, min.Y, max.Y), Clampy(value.Z, min.Z, max.Z));
        public static Vector4i Clampy(Vector4i value, Vector4i min, Vector4i max) => new Vector4i(Clampy(value.X, min.X, max.X), Clampy(value.Y, min.Y, max.Y), Clampy(value.Z, min.Z, max.Z), Clampy(value.W, min.W, max.W));

        public static void ClampSety(this ref float value, float min, float max) => value = Clampy(value, min, max);
        public static void ClampSety(this ref int value, int min, int max) => value = Clampy(value, min, max);
        public static void ClampSety(this ref byte value, byte min, byte max) => value = Clampy(value, min, max);
        public static void Clamp01Sety(this ref float value) => value = Clamp01y(value);
        public static void Clamp01Sety(this ref int value) => value = Clamp01y(value);

        public static void ClampSety(this ref Vector2 value, Vector2 min, Vector2 max) => value = Clampy(value, min, max);
        public static void ClampSety(this ref Vector3 value, Vector3 min, Vector3 max) => value = Clampy(value, min, max);
        public static void ClampSety(this ref Vector4 value, Vector4 min, Vector4 max) => value = Clampy(value, min, max);

        public static void ClampSety(this ref Vector2i value, Vector2i min, Vector2i max) => value = Clampy(value, min, max);
        public static void ClampSety(this ref Vector3i value, Vector3i min, Vector3i max) => value = Clampy(value, min, max);
        public static void ClampSety(this ref Vector4i value, Vector4i min, Vector4i max) => value = Clampy(value, min, max);
        #endregion
        
        public static int Pow(int power, int value) => (int)Math.Pow(power, value);
        public static float Pow(float power, float value) => (float)Math.Pow(power, value);
        public static Vector2 Pow(Vector2 power, Vector2 value) => ((float)Math.Pow(power.X, value.X), (float)Math.Pow(power.Y, value.Y));
        public static Vector3 Pow(Vector3 power, Vector3 value) => ((float)Math.Pow(power.X, value.X), (float)Math.Pow(power.Y, value.Y), (float)Math.Pow(power.Z, value.Z));
        public static double Pow(double power, double value) => Math.Pow(power, value);

        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static Vector2 Sqrt(Vector2 value) => ((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y));
        public static Vector3 Sqrt(Vector3 value) => ((float)Math.Sqrt(value.X), (float)Math.Sqrt(value.Y), (float)Math.Sqrt(value.Z));
        public static double Sqrt(double value) => Math.Sqrt(value);

        public static float Lerp(float a, float b, float t) => a + t * (b - a);
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a + (b - a) * t;


        public static int Sign(float value) => value > 0 ? 1 : value < 0 ? -1 : 0;
        public static int sign(this float value) => Sign(value);
        public static int SignNo0(float value) => value < 0 ? -1 : 1;
        public static int signNo0(this float value) => SignNo0(value);
        public static Vector2 Sign(Vector2 value) => (Sign(value.X), Sign(value.Y));
        public static Vector3 Sign(Vector3 value) => (Sign(value.X), Sign(value.Y), Sign(value.Z));

        public static float Step(float size, float value)
        {
            if (size == 0) return value;

            float steps = value / size;
            return (float)(Math.Truncate(steps) * size);
        }

        public static float Abs(float value) => value < 0 ? -value : value;
        public static int Abs(int value) => value < 0 ? -value : value;
        public static Vector3 Abs(Vector3 value) => new Vector3(Abs(value.X), Abs(value.Y), Abs(value.Z));
        public static Vector2 Abs(Vector2 value) => new Vector2(Abs(value.X), Abs(value.Y));

        public static float Fraction(float value) => value - Floor(value);
        public static Vector2 Fraction(Vector2 value) => new Vector2(Fraction(value.X), Fraction(value.Y));
        public static Vector3 Fraction(Vector3 value) => new Vector3(Fraction(value.X), Fraction(value.Y), Fraction(value.Z));

        public static float Sin(float value) => (float)Math.Sin(value);
        public static Vector2 Sin(Vector2 value) => new Vector2(Sin(value.X), Sin(value.Y));
        public static Vector3 Sin(Vector3 value) => new Vector3(Sin(value.X), Sin(value.Y), Sin(value.Z));

        public static float Cos(float value) => (float)Math.Cos(value);
        public static Vector2 Cos(Vector2 value) => new Vector2(Cos(value.X), Cos(value.Y));
        public static Vector3 Cos(Vector3 value) => new Vector3(Cos(value.X), Cos(value.Y), Cos(value.Z));

        public static float Tan(float value) => (float)Math.Tan(value); 
        public static Vector2 Tan(Vector2 value) => new Vector2(Tan(value.X), Tan(value.Y));
        public static Vector3 Tan(Vector3 value) => new Vector3(Tan(value.X), Tan(value.Y), Tan(value.Z));

        public static float Mod(float value, float mod) => value - (mod * Floor(value / mod));
        public static Vector2 Mod(Vector2 value, float mod) => new Vector2(Mod(value.X, mod), Mod(value.Y, mod));
        public static Vector3 Mod(Vector3 value, float mod) => new Vector3(Mod(value.X, mod), Mod(value.Y, mod), Mod(value.Z, mod));
        public static Vector3 Mod(Vector3 value, Vector3 mod) => new Vector3(Mod(value.X, mod.X), Mod(value.Y, mod.Y), Mod(value.Z, mod.Z));

        #region FLIP
        public static Vector2 Flip(this Vector2 v) => (v.Y, v.X);
        public static Vector3 Flip(this Vector3 v) => (v.Z, v.Y, v.X);
        public static Vector4 Flip(this Vector4 v) => (v.W, v.Z, v.Y, v.X);

        public static Vector2i Flip(this Vector2i v) => (v.Y, v.X);
        public static Vector3i Flip(this Vector3i v) => (v.Z, v.Y, v.X);
        public static Vector4i Flip(this Vector4i v) => (v.W, v.Z, v.Y, v.X);
        #endregion

        /// <summary>
        /// turns the range [a, b] into [0, 1] based on t
        /// </summary>
        public static float LerpI(float a, float b, float t)
        {
            if (a == b)
                return 1;

            return (t - a) / (b - a);
        }


        public static int toPackedColor(this Vector4 c) => ((c.X * 255).Fti() << 24) | ((c.Y * 255).Fti() << 16) | ((c.Z * 255).Fti() << 8) | (c.W * 255).Fti();

        public static float DeltaAngle(float a, float b)
        {
            float diff = (b - a + 180f) % 360f - 180f;
            return diff < -180f ? diff + 360f : diff;
        }

        public static System.Numerics.Matrix4x4 Num(Matrix4 m) =>
            new System.Numerics.Matrix4x4(
                m.M11, m.M21, m.M31, m.M41,
                m.M12, m.M22, m.M32, m.M42,
                m.M13, m.M23, m.M33, m.M43,
                m.M14, m.M24, m.M34, m.M44
            );
        public static System.Numerics.Matrix4x4 num(this Matrix4 matrix) => Num(matrix);

        public static Vec4 Num(Vector4 vector) => new Vec4(vector.X, vector.Y, vector.Z, vector.W);
        public static Vec4 num(this Vector4 matrix) => Num(matrix);

        public static Vec3 Num(Vector3 vector) => new Vec3(vector.X, vector.Y, vector.Z);
        public static Vec3 num(this Vector3 matrix) => Num(matrix);

        public static Vec2 Num(Vector2 vector) => new Vec2(vector.X, vector.Y);
        public static Vec2 num(this Vector2 matrix) => Num(matrix);

        public static float RadToDeg(float radians) => radians * (180f / MathF.PI);
        public static float DegToRad(float degrees) => degrees * (MathF.PI / 180f);

        public static Vector3 ToDegrees(Vector3 radians)
        {
            return new Vector3(
                RadToDeg(radians.X),
                RadToDeg(radians.Y),
                RadToDeg(radians.Z)
            );
        }

        public static Vector2 ToDegrees(Vector2 radians)
        {
            return new Vector2(
                RadToDeg(radians.X),
                RadToDeg(radians.Y)
            );
        }

        public static Vector3 ToRadians(Vector3 degrees)
        {
            return new Vector3(
                DegToRad(degrees.X),
                DegToRad(degrees.Y),
                DegToRad(degrees.Z)
            );
        }

        public static Vector2 ToRadians(Vector2 degrees)
        {
            return new Vector2(
                DegToRad(degrees.X),
                DegToRad(degrees.Y)
            );
        }

        // Vector4 Adding
        public static Vec4 Add(Vec4 vector, Vector4 add) => new Vec4(vector.X + add.X, vector.Y + add.Y, vector.Z + add.Z, vector.W + add.W);
        public static Vector4 Add(Vector4 vector, Vec4 add) => new Vector4(vector.X + add.X, vector.Y + add.Y, vector.Z + add.Z, vector.W + add.W);

        // Vector3 Adding
        public static Vec3 Add(Vec3 vector, Vector3 add) => new Vec3(vector.X + add.X, vector.Y + add.Y, vector.Z + add.Z);
        public static Vector3 Add(Vector3 vector, Vec3 add) => new Vector3(vector.X + add.X, vector.Y + add.Y, vector.Z + add.Z);

        // Vector2 Adding
        public static Vec2 Add(Vec2 vector, Vector2 add) => new Vec2(vector.X + add.X, vector.Y + add.Y);
        public static Vector2 Add(Vector2 vector, Vec2 add) => new Vector2(vector.X + add.X, vector.Y + add.Y);


        public static Vector3 Xyz(Vector4 vector) => (vector.X, vector.Y, vector.Z);
        public static Vec3 Xyz(Vec4 vector) => new Vec3(vector.X, vector.Y, vector.Z);
        
        public static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);
        public static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);
        public static Vector3 RadiansToDegrees(Vector3 radians) => new Vector3(
                RadToDeg(radians.X),
                RadToDeg(radians.Y),
                RadToDeg(radians.Z)
            );

        public static Vector3 DegreesToRadians(Vector3 degrees) => new Vector3(
                DegToRad(degrees.X),
                DegToRad(degrees.Y),
                DegToRad(degrees.Z)
            );

        public static Vector3 RotateAround(Vector3 point, Vector3 center, Vector3 axis, float angleDegrees)
        {
            Vector3 translatedPoint = point - center;
            float angleRadians = DegToRad(angleDegrees);
            Quaternion rotation = Quaternion.FromAxisAngle(axis, angleRadians);
            Vector3 rotatedPoint = Vector3.Transform(translatedPoint, rotation);
            return rotatedPoint + center;
        }

        public static Vector3 RotatePoint(Vector3 point, Vector3 center, Vector3 axis, float degrees)
        {
            float radians = DegToRad(degrees);
            float sinHalfAngle = MathF.Sin(radians / 2);

            axis.Normalize();
            Vector3 relativePoint = point - center;

            Quaternion rotation = new Quaternion(axis * sinHalfAngle, MathF.Cos(radians / 2));
            Quaternion pQuat = new Quaternion(relativePoint, 0);
            Quaternion rotatedQuat = rotation * pQuat * rotation.Inverted();

            return (rotatedQuat.X, rotatedQuat.Y, rotatedQuat.Z) + center;
        }

        public static Vector3 RotateAround(Vector3 point, Vector3 center, Quaternion rotation)
        {
            Vector3 translatedPoint = point - center;
            Vector3 rotatedPoint = Vector3.Transform(translatedPoint, rotation);
            return rotatedPoint + center;
        }

        public static Quaternion RotateAround(Vector3 axis, Quaternion rotation, float angle) => Quaternion.FromAxisAngle(axis, angle) * rotation;

        public static Vector2? WorldToScreen(Vector3 worldPosition, System.Numerics.Matrix4x4 projectionMatrix, System.Numerics.Matrix4x4 viewMatrix, float width, float height) => WorldToScreen(worldPosition, projectionMatrix, viewMatrix, width, height, out _);
        public static Vector2? WorldToScreen(Vector3 worldPosition, System.Numerics.Matrix4x4 projectionMatrix, System.Numerics.Matrix4x4 viewMatrix, float width, float height, out float clipW)
        {
            Vec4 viewSpace = Vec4.Transform(
                new Vec4(Num(worldPosition), 1.0f),
                viewMatrix
            );

            Vec4 clipSpace = Vec4.Transform(viewSpace, projectionMatrix);
            clipW = clipSpace.W;

            if (clipSpace.W <= 0)
                return null;

            float ndcX = clipSpace.X / clipSpace.W;
            float ndcY = clipSpace.Y / clipSpace.W;

            Vector2 screenPos = new Vector2(
                (ndcX + 1.0f) * width * 0.5f,
                (1.0f - ndcY) * height * 0.5f
            );

            return screenPos;
        }

        public static float GetAngleBetweenPoints(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            float angleRadians = MathF.Atan2(direction.X, direction.Y);
            float angleDegrees = angleRadians * (180f / MathF.PI);
            if (angleDegrees < 0)
                angleDegrees += 360f;
            return angleDegrees;
        }


        public static bool IsPointNearLine(Vector2 pointA, Vector2 pointB, Vector2 point, float distance)
        {
            Vector2 lineDirection = pointB - pointA;
            Vector2 pointToStart = point - pointA;
            float lineLengthSq = lineDirection.LengthSquared;
            float dot = Vector2.Dot(pointToStart, lineDirection);
            float t = Clampy(dot / lineLengthSq, 0f, 1f);
            Vector2 closestPoint = pointA + (lineDirection * t);
            return (point - closestPoint).LengthSquared <= distance * distance;
        }

        public static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);

        public static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            const float epsilon = 0.0001f; // you can tweak this

            float d1 = Sign(pt, v1, v2);
            float d2 = Sign(pt, v2, v3);
            float d3 = Sign(pt, v3, v1);

            bool hasNeg = (d1 < -epsilon) || (d2 < -epsilon) || (d3 < -epsilon);
            bool hasPos = (d1 > epsilon) || (d2 > epsilon) || (d3 > epsilon);

            return !(hasNeg && hasPos);
        }


        // Noise manipulation functions
        public static float PLerp(float min, float max, float value)
        {
            float midpoint = (min + max) / 2;
            if (value < min || value > max)
                return 0;

            float distanceFromMidpoint = Mathf.Abs(value - midpoint);
            float totalDistance = (max - min) / 2;

            float proximityValue = 1 - (distanceFromMidpoint / totalDistance);

            return proximityValue;
        }

        public static float SLerp(float min, float max, float value)
        {
            if (value <= min) return 0f;
            if (value >= max) return 1f;
            return (value - min) / (max - min);
        }

        public static float NoiseLerp(float noiseA, float noiseB, float minB, float maxA, float t)
        {
            if (maxA - minB == 0)
                return noiseA;

            float nt = (Clampy(t, minB, maxA) - minB) / (maxA - minB);
            return Lerp(noiseA, noiseB, nt);
        }


        public static string ConvertGLSL(Vector2 vector) => $"vec2({vector.X}, {vector.Y})";

        public static Vector3 Vec3(Vector2 v, float z) => (v.X, v.Y, z);

        public static void GetSmallestBoundingBox(IEnumerable<Vertex> vertices, out Vector3 min, out Vector3 max)
        {
            if (!vertices.Any())
            {
                min = Vector3.Zero;
                max = Vector3.Zero;
                return;
            }

            List<Vector3> positions = [];
            foreach (var vertex in vertices)
            {
                positions.Add(vertex.Position);
            }

            Vector3 center = Vector3.Zero;
            Vector3 rotationAxis = (0, 1, 0);
            foreach (var vertex in vertices)
            {
                center += vertex.Position;
            }
            center /= vertices.Count();

            Vector3 axisX = (1, 0, 0);

            List<Edge> edges = Edge.GetEdges(vertices);
            List<Vector3> copy = [.. positions];

            if (edges.Count == 0)
            {
                min = Vector3.Zero;
                max = Vector3.Zero;
                return;
            }

            Vector3 direction = edges[0].GetDirection();
            float angle = RadToDeg(Vector3.CalculateAngle(axisX, direction));

            for (int i = 0; i < copy.Count; i++)
            {
                copy[i] = RotateAround(copy[i], center, rotationAxis, angle);
            }

            Vector3 minC = copy[0];
            Vector3 maxC = copy[0];

            for (int i = 1; i < copy.Count; i++)
            {
                minC = Min(minC, copy[i]);
                maxC = Max(maxC, copy[i]);
            }

            Vector3 size = maxC - minC;
            min = minC;
            max = maxC;

            for (int i = 1; i < edges.Count; i++)
            {
                copy = [.. positions];

                direction = edges[i].GetDirection();
                float a = RadToDeg(Vector3.CalculateAngle(axisX, direction));

                for (int j = 0; j < copy.Count; j++)
                {
                    copy[j] = RotateAround(copy[j], center, rotationAxis, a);
                }

                minC = copy[0];
                maxC = copy[0];

                for (int j = 1; j < copy.Count; j++)
                {
                    minC = Min(minC, copy[j]);
                    maxC = Max(maxC, copy[j]);
                }

                Vector3 sizeC = maxC - minC;

                //Console.WriteLine("Size original: " + size + " Volume: " + size.X * size.Y * size.Z);
                //Console.WriteLine("Size rotated: " + sizeC + " Volume: " + sizeC.X * sizeC.Y * sizeC.Z);

                if (sizeC.X * sizeC.Z < size.X * size.Z)
                {
                    min = minC;
                    max = maxC;
                    size = sizeC;
                    angle = a;
                }
            }

            foreach (var vertex in vertices)
            {
                Vector3 rotatedPoint = RotateAround(vertex, center, rotationAxis, angle);
                rotatedPoint.Y = 0;
                vertex.SetPosition(rotatedPoint);
            }

            min.Y = 0;
            max.Y = 0;
        }

        /// <summary>
        /// Converts a point in barycentric coordinates to UV coordinates based on the triangle's vertices and their UVs.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="uvA"></param>
        /// <param name="uvB"></param>
        /// <param name="uvC"></param>
        /// <returns></returns>
        public static Vector2 BarycentricToUv(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            float areaABC = Abs(Sign(a, b, c));
            float areaPBC = Abs(Sign(p, b, c));
            float areaPCA = Abs(Sign(p, c, a));

            float u = areaPBC / areaABC;
            float v = areaPCA / areaABC;
            float w = 1 - u - v;

            return new Vector2(
                u * uvA.X + v * uvB.X + w * uvC.X,
                u * uvA.Y + v * uvB.Y + w * uvC.Y
            );
        }

        public static Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;

            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }

        public static Vector2 PerspectiveCorrectUv(
            Vector3 bary,
            Vector2 uvA, Vector2 uvB, Vector2 uvC,
            float wA, float wB, float wC)
        {
            // q = 1/w
            float qA = 1f / wA;
            float qB = 1f / wB;
            float qC = 1f / wC;

            // u' = uv * q
            Vector2 uvAq = uvA * qA;
            Vector2 uvBq = uvB * qB;
            Vector2 uvCq = uvC * qC;

            // interpolated q
            float qInterp = qA * bary.X + qB * bary.Y + qC * bary.Z;

            // perspective-correct interpolation
            Vector2 uv =
                (uvAq * bary.X + uvBq * bary.Y + uvCq * bary.Z) / qInterp;

            return uv;
        }

        public static Vector3 PerspectiveCorrectPosition(
            Vector3 bary,
            Vector3 a, Vector3 b, Vector3 c,
            float wA, float wB, float wC)
        {
            // q = 1/w
            float qA = 1f / wA;
            float qB = 1f / wB;
            float qC = 1f / wC;

            // u' = uv * q
            Vector3 aq = a * qA;
            Vector3 bq = b * qB;
            Vector3 cq = c * qC;

            // interpolated q
            float qInterp = qA * bary.X + qB * bary.Y + qC * bary.Z;

            // perspective-correct interpolation
            Vector3 pos = (aq * bary.X + bq * bary.Y + cq * bary.Z) / qInterp;
            return pos;
        }
    }
}