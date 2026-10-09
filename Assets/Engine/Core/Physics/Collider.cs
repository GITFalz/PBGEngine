
using PBG.MathLibrary;

namespace PBG.Physics
{
    public struct Collider
    {
        public Vector3 Min = (0, 0, 0);
        public Vector3 Center = (.5f, .5f, .5f);
        public Vector3 Max = (1, 1, 1);

        public Collider() { }
        public Collider(Collider a, Vector3 origin) { Min = a.Min + origin; Max = a.Max + origin; Center = a.Center + origin; }
        public Collider(Vector3 min, Vector3 max) { Min = min; Max = max; Center = (max + min) / 2f; }

        public bool Intersects(Collider other)
        {
            return (Mathf.Min(Max.X, other.Max.X) >= Mathf.Max(Min.X, other.Min.X)) &&
                (Mathf.Min(Max.Y, other.Max.Y) >= Mathf.Max(Min.Y, other.Min.Y)) &&
                (Mathf.Min(Max.Z, other.Max.Z) >= Mathf.Max(Min.Z, other.Min.Z));
        }

        public static Collider operator +(Collider a, Vector3 b) => new Collider(a, b);
        public static bool operator &(Collider a, Collider b) => a.Intersects(b);

        public (float, Vector3?) Collide(Collider collider, Vector3 velocity)
        {
            float xInvEntry, yInvEntry, zInvEntry;
            float xInvExit, yInvExit, zInvExit;

            Vector3 minA = Min;
            Vector3 maxA = Max;

            Vector3 minB = collider.Min;
            Vector3 maxB = collider.Max;

            //Console.WriteLine($"Testing Collision: MinA: {minA} - MaxA: {maxA} - MinB: {minB} - MaxB: {maxB} going {velocity}");

            if (velocity.X > 0.0f)
            {
                xInvEntry = minB.X - maxA.X;
                xInvExit = maxB.X - minA.X;
            }
            else
            {
                xInvEntry = maxB.X - minA.X;
                xInvExit = minB.X - maxA.X;
            }

            if (velocity.Y > 0.0f)
            {
                yInvEntry = minB.Y - maxA.Y;
                yInvExit = maxB.Y - minA.Y;
            }
            else
            {
                yInvEntry = maxB.Y - minA.Y;
                yInvExit = minB.Y - maxA.Y;
            }

            if (velocity.Z > 0.0f)
            {
                zInvEntry = minB.Z - maxA.Z;
                zInvExit = maxB.Z - minA.Z;
            }
            else
            {
                zInvEntry = maxB.Z - minA.Z;
                zInvExit = minB.Z - maxA.Z;
            }


            float xEntry, yEntry, zEntry;
            float xExit, yExit, zExit;

            if (velocity.X == 0.0f)
            {
                xEntry = float.NegativeInfinity;
                xExit = float.PositiveInfinity;
            }
            else
            {
                xEntry = xInvEntry / velocity.X;
                xExit = xInvExit / velocity.X;
            }

            if (velocity.Y == 0.0f)
            {
                yEntry = float.NegativeInfinity;
                yExit = float.PositiveInfinity;
            }
            else
            {
                yEntry = yInvEntry / velocity.Y;
                yExit = yInvExit / velocity.Y;
            }

            if (velocity.Z == 0.0f)
            {
                zEntry = float.NegativeInfinity;
                zExit = float.PositiveInfinity;
            }
            else
            {
                zEntry = zInvEntry / velocity.Z;
                zExit = zInvExit / velocity.Z;
            }

            //Console.WriteLine($"Entry: {xEntry}, {yEntry}, {zEntry}");
            //Console.WriteLine($"Exit: {xExit}, {yExit}, {zExit}");

            if (xEntry < 0 && yEntry < 0 && zEntry < 0)
                return (1, null);

            if (xEntry > 1.0f || yEntry > 1.0f || zEntry > 1.0f)
                return (1, null);

            float entryTime = Mathf.Max(xEntry, yEntry, zEntry);
            float exitTime = Mathf.Min(xExit, yExit, zExit);

            //Console.WriteLine($"Entry Time: {entryTime}, Exit Time: {exitTime}");

            if (entryTime > exitTime)
                return (1, null);

            Vector3 normal = (
                entryTime == xEntry ? (velocity.X > 0f ? -1f : 1f) : 0f,
                entryTime == yEntry ? (velocity.Y > 0f ? -1f : 1f) : 0f,
                entryTime == zEntry ? (velocity.Z > 0f ? -1f : 1f) : 0f
            );

            return (entryTime, normal);
        }
    }
}