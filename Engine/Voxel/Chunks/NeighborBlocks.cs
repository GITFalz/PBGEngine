using PBG.MathLibrary;

namespace PBG.Voxel
{
    public static partial class VoxelChunkGenerator
    {
        public struct NeighborBlocks
        {
            // Bottom layer (y = -1)
            public Block BottomLeftFront;     // (-1, -1, -1)
            public Block BottomFront;         // ( 0, -1, -1)
            public Block BottomRightFront;    // ( 1, -1, -1)

            public Block BottomLeft;          // (-1, -1,  0)
            public Block Bottom;              // ( 0, -1,  0)
            public Block BottomRight;         // ( 1, -1,  0)

            public Block BottomLeftBack;      // (-1, -1,  1)
            public Block BottomBack;          // ( 0, -1,  1)
            public Block BottomRightBack;     // ( 1, -1,  1)

            // Middle layer (y = 0)
            public Block LeftFront;           // (-1,  0, -1)
            public Block Front;               // ( 0,  0, -1)
            public Block RightFront;          // ( 1,  0, -1)

            public Block Left;                // (-1,  0,  0)
            // Center (0,0,0) is intentionally not stored

            public Block Right;               // ( 1,  0,  0)

            public Block LeftBack;            // (-1,  0,  1)
            public Block Back;                // ( 0,  0,  1)
            public Block RightBack;           // ( 1,  0,  1)

            // Top layer (y = +1)
            public Block TopLeftFront;        // (-1,  1, -1)
            public Block TopFront;            // ( 0,  1, -1)
            public Block TopRightFront;       // ( 1,  1, -1)

            public Block TopLeft;             // (-1,  1,  0)
            public Block Top;                 // ( 0,  1,  0)
            public Block TopRight;            // ( 1,  1,  0)

            public Block TopLeftBack;         // (-1,  1,  1)
            public Block TopBack;             // ( 0,  1,  1)
            public Block TopRightBack;        // ( 1,  1,  1)


            // Constructor using the center position
            public NeighborBlocks(VoxelChunk?[] sideChunks, Vector3i center, VoxelChunk chunk)
            {
                // Bottom layer y-1
                BottomLeftFront  = GetBlock(sideChunks, center + new Vector3i(-1, -1, -1), chunk);
                BottomFront      = GetBlock(sideChunks, center + new Vector3i( 0, -1, -1), chunk);
                BottomRightFront = GetBlock(sideChunks, center + new Vector3i( 1, -1, -1), chunk);

                BottomLeft       = GetBlock(sideChunks, center + new Vector3i(-1, -1,  0), chunk);
                Bottom           = GetBlock(sideChunks, center + new Vector3i( 0, -1,  0), chunk);
                BottomRight      = GetBlock(sideChunks, center + new Vector3i( 1, -1,  0), chunk);

                BottomLeftBack   = GetBlock(sideChunks, center + new Vector3i(-1, -1,  1), chunk);
                BottomBack       = GetBlock(sideChunks, center + new Vector3i( 0, -1,  1), chunk);
                BottomRightBack  = GetBlock(sideChunks, center + new Vector3i( 1, -1,  1), chunk);

                // Middle layer y=0
                LeftFront        = GetBlock(sideChunks, center + new Vector3i(-1,  0, -1), chunk);
                Front            = GetBlock(sideChunks, center + new Vector3i( 0,  0, -1), chunk);
                RightFront       = GetBlock(sideChunks, center + new Vector3i( 1,  0, -1), chunk);

                Left             = GetBlock(sideChunks, center + new Vector3i(-1,  0,  0), chunk);
                Right            = GetBlock(sideChunks, center + new Vector3i( 1,  0,  0), chunk);

                LeftBack         = GetBlock(sideChunks, center + new Vector3i(-1,  0,  1), chunk);
                Back             = GetBlock(sideChunks, center + new Vector3i( 0,  0,  1), chunk);
                RightBack        = GetBlock(sideChunks, center + new Vector3i( 1,  0,  1), chunk);

                // Top layer y+1
                TopLeftFront     = GetBlock(sideChunks, center + new Vector3i(-1,  1, -1), chunk);
                TopFront         = GetBlock(sideChunks, center + new Vector3i( 0,  1, -1), chunk);
                TopRightFront    = GetBlock(sideChunks, center + new Vector3i( 1,  1, -1), chunk);

                TopLeft          = GetBlock(sideChunks, center + new Vector3i(-1,  1,  0), chunk);
                Top              = GetBlock(sideChunks, center + new Vector3i( 0,  1,  0), chunk);
                TopRight         = GetBlock(sideChunks, center + new Vector3i( 1,  1,  0), chunk);

                TopLeftBack      = GetBlock(sideChunks, center + new Vector3i(-1,  1,  1), chunk);
                TopBack          = GetBlock(sideChunks, center + new Vector3i( 0,  1,  1), chunk);
                TopRightBack     = GetBlock(sideChunks, center + new Vector3i( 1,  1,  1), chunk);
            }

            public static Block GetBlock(VoxelChunk?[] sideChunks, Vector3i position, VoxelChunk chunk)
            {
                var relative = VoxelData.BlockToRelative(position);
                if (chunk.InBounds(relative))
                    return chunk.Get(position);

                var chunkRelative = VoxelData.BlockToChunkRelative(position) - chunk.RelativePosition + (1, 1, 1);
                int index = chunkRelative.X + chunkRelative.Z * 3 + chunkRelative.Y * 9;
                var c = sideChunks[index];
                if (c == null)
                    return Block.Air;
                return c.Get(relative);
            }

            private static readonly int[] _sideBlockIndices = [0, 1, 3, 6, 3, 7, 8, 7, 5, 2, 5, 1];

            public readonly Block[] GetFrontBlocks()
            {
                return [
                    BottomLeftFront, BottomFront, BottomRightFront,
                    LeftFront, Front, RightFront,
                    TopLeftFront, TopFront, TopRightFront
                ];
            }

            public readonly Block[] GetRightBlocks()
            {
                return [
                    BottomRightFront, BottomRight, BottomRightBack,
                    RightFront, Right, RightBack,
                    TopRightFront, TopRight, TopRightBack
                ];
            }

            public readonly Block[] GetTopBlocks()
            {
                return [
                    TopLeftFront, TopFront, TopRightFront,
                    TopLeft, Top, TopRight,
                    TopLeftBack, TopBack, TopRightBack
                ];
            }

            public readonly Block[] GetLeftBlocks()
            {
                return [
                    BottomLeftBack, BottomLeft, BottomLeftFront,
                    LeftBack, Left, LeftFront,
                    TopLeftBack, TopLeft, TopLeftFront
                ];
            }

            public readonly Block[] GetBottomBlocks()
            {
                return [
                    BottomRightFront, BottomFront, BottomLeftFront,
                    BottomRight, Bottom, BottomLeft,
                    BottomRightBack, BottomBack, BottomLeftBack
                ];
            }

            public readonly Block[] GetBackBlocks()
            {
                return [
                    BottomRightBack, BottomBack, BottomLeftBack,
                    RightBack, Back, LeftBack,
                    TopRightBack, TopBack, TopLeftBack
                ];
            }
        }
    }
}