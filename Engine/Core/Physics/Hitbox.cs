

using PBG.MathLibrary;

namespace PBG.Physics
{
    public class Hitbox
    {
        private static Vector3 _origin = Vector3.Zero;

        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }

        private Vector3 HalfY;

        public Vector3 HalfSize;
        public Vector3 Position;
        public Vector3 Center => Position + HalfY;

        public Vector3 CornerX1Y1Z1;
        public Vector3 CornerX2Y1Z1;
        public Vector3 CornerX1Y1Z2;
        public Vector3 CornerX2Y1Z2;
        public Vector3 CornerX1Y2Z1;
        public Vector3 CornerX2Y2Z1;
        public Vector3 CornerX1Y2Z2;
        public Vector3 CornerX2Y2Z2;

        public float Width;
        public float Height;
        public float Depth;

        public Hitbox(Vector3 size)
        {
            if (size.X == 0 || size.Y == 0 || size.Z == 0)
                throw new ArgumentException("Hitbox size cannot be 0");

            Width = size.X;
            Height = size.Y;
            Depth = size.Z;

            Min = new Vector3(-Width / 2, 0, -Depth / 2);
            Max = new Vector3(Width / 2, Height, Depth / 2);

            HalfSize = new Vector3(Width / 2, Height / 2, Depth / 2);
            HalfY = new Vector3(0, Height / 2, 0);

            Position = _origin;

            CornerX1Y1Z1 = Min;
            CornerX2Y1Z1 = (Max.X, Min.Y, Min.Z);
            CornerX1Y1Z2 = (Min.X, Min.Y, Max.Z);
            CornerX2Y1Z2 = (Max.X, Min.Y, Max.Z);
            CornerX1Y2Z1 = (Min.X, Max.Y, Min.Z);
            CornerX2Y2Z1 = (Max.X, Max.Y, Min.Z);
            CornerX1Y2Z2 = (Min.X, Max.Y, Max.Z);
            CornerX2Y2Z2 = Max;
        }

        public void GetMinMax(out Vector3 min, out Vector3 max)
        {
            min = Min + Position;
            max = Max + Position;
        }

        public Vector3[] GetCorners()
        {
            return GetCorners(Position);
        }

        public Vector3[] GetCorners(Vector3 position)
        {
            return [
                position + CornerX1Y1Z1, // 0
                position + CornerX2Y1Z1, // 1
                position + CornerX1Y1Z2, // 2
                position + CornerX2Y1Z2, // 3
                position + CornerX1Y2Z1, // 4
                position + CornerX2Y2Z1, // 5
                position + CornerX1Y2Z2, // 6
                position + CornerX2Y2Z2, // 7
            ];
        }

        public Vector3 min()
        {
            return Min + Position;
        }

        public Vector3 min(Vector3 offset)
        {
            return Min + Position + offset;
        }

        public Vector3 max()
        {
            return Max + Position;
        }

        public Vector3 max(Vector3 offset)
        {
            return Max + Position + offset;
        }
    }
}