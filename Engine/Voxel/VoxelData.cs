
using PBG.MathLibrary;

namespace PBG.Voxel
{
    public static class VoxelData
    {
        public static readonly Vector2[] UVTable = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
        };

        public static readonly int[,] IndexOffsetLod =
        {
            { -32, 1, 1024, -1, -1024, 32 },
            { -16, 1, 256, -1, -256, 16 },
        };

        public static readonly int[] OcclusionMask = [1 << 16, 1 << 17, 1 << 18, 1 << 19, 1 << 20, 1 << 21];

        public static bool InBounds(int x, int y, int z, int side, int size)
        {
            return side switch
            {
                0 => z - 1 >= 0,
                1 => x + 1 < size,
                2 => y + 1 < size,
                3 => x - 1 >= 0,
                4 => y - 1 >= 0,
                5 => z + 1 < size,
                _ => false
            };
        }

        public static readonly Vector3i[] SideNormal =
        [
            (0, 0, -1), // Front
            (1, 0, 0), // Right
            (0, 1, 0), // Top
            (-1, 0, 0), // Left
            (0, -1, 0), // Bottom
            (0, 0, 1), // Back
        ];

        public static readonly int[] FirstOffsetBase =
        {
            1024, 1024, 32, 1024, 32, 1024
        };

        public static readonly int[] SecondOffsetBase = { 1, 32, 1, 32, 1, 1 };

        public static readonly Func<int, int, int>[] FirstLoopBase =
        {
            (y, z) => 31 - y,
            (y, z) => 31 - y,
            (y, z) => 31 - z,
            (y, z) => 31 - y,
            (y, z) => 31 - z,
            (y, z) => 31 - y,
        };

        public static readonly Func<int, int, int>[] SecondLoopBase =
        {
            (x, z) => 31 - x,
            (x, z) => 31 - z,
            (x, z) => 31 - x,
            (x, z) => 31 - z,
            (x, z) => 31 - x,
            (x, z) => 31 - x,
        };

        public static readonly Func<int, int, Vector3[]>[] GetSideOffsets =
        [
            (width, height) => [(0, 0, 0), (0, height, 0), (width, height, 0), (width, 0, 0)],
            (width, height) => [(1, 0, 0), (1, height, 0), (1, height, width), (1, 0, width)],
            (width, height) => [(0, 1, 0), (0, 1, height), (width, 1, height), (width, 1, 0)],
            (width, height) => [(0, 0, width), (0, height, width), (0, height, 0), (0, 0, 0)],
            (width, height) => [(0, 0, height), (0, 0, 0), (width, 0, 0), (width, 0, height)],
            (width, height) => [(width, 0, 1), (width, height, 1), (0, height, 1), (0, 0, 1)],
        ];

        // based on side offsets
        public static readonly Vector3i[][][] AoOffset =
        [
            [ // Front
                [   SideNormal[0] + (0, -1, 0),    SideNormal[0] + (-1, -1, 0),    SideNormal[0] + (-1, 0, 0)  ],
                [   SideNormal[0] + (-1, 0, 0),    SideNormal[0] + (-1, 1, 0),     SideNormal[0] + (0, 1, 0)   ],
                [   SideNormal[0] + (0, 1, 0),     SideNormal[0] + (1, 1, 0),      SideNormal[0] + (1, 0, 0)   ],
                [   SideNormal[0] + (1, 0, 0),     SideNormal[0] + (1, -1, 0),     SideNormal[0] + (0, -1, 0)  ]
            ],
            [ // Right
                [   SideNormal[1] + (0, -1, 0),    SideNormal[1] + (0, -1, -1),    SideNormal[1] + (0, 0, -1)  ],
                [   SideNormal[1] + (0, 0, -1),    SideNormal[1] + (0, 1, -1),     SideNormal[1] + (0, 1, 0)   ],
                [   SideNormal[1] + (0, 1, 0),     SideNormal[1] +  (0, 1, 1),     SideNormal[1] + (0, 0, 1)   ],
                [   SideNormal[1] + (0, 0, 1),     SideNormal[1] +  (0, -1, 1),    SideNormal[1] + (0, -1, 0)  ]
            ],
            [ // Top
                [   SideNormal[2] + (0, 0, -1),    SideNormal[2] + (-1, 0, -1),    SideNormal[2] + (-1, 0, 0)  ],
                [   SideNormal[2] + (-1, 0, 0),    SideNormal[2] + (-1, 0, 1),     SideNormal[2] + (0, 0, 1)   ],
                [   SideNormal[2] + (0, 0, 1),     SideNormal[2] + (1, 0, 1),      SideNormal[2] + (1, 0, 0)   ],
                [   SideNormal[2] + (1, 0, 0),     SideNormal[2] + (1, 0, -1),     SideNormal[2] +  (0, 0, -1) ]
            ],
            [ // Left
                [   SideNormal[3] + (0, -1, 0),    SideNormal[3] + (0, -1, 1),     SideNormal[3] + (0, 0, 1)   ],
                [   SideNormal[3] + (0, 0, 1),     SideNormal[3] + (0, 1, 1),      SideNormal[3] + (0, 1, 0)   ],
                [   SideNormal[3] + (0, 1, 0),     SideNormal[3] + (0, 1, -1),     SideNormal[3] + (0, 0, -1)  ],
                [   SideNormal[3] + (0, 0, -1),    SideNormal[3] + (0, -1, -1),    SideNormal[3] + (0, -1, 0)  ]
            ],
            [ // Bottom
                [   SideNormal[4] + (-1, 0, 0),    SideNormal[4] + (-1, 0, 1),     SideNormal[4] + (0, 0, 1)   ],
                [   SideNormal[4] + (-1, 0, 0),    SideNormal[4] + (-1, 0, -1),    SideNormal[4] + (0, 0, -1)  ],
                [   SideNormal[4] + (0, 0, -1),    SideNormal[4] + (1, 0, -1),     SideNormal[4] + (1, 0, 0)   ],
                [   SideNormal[4] + (0, 0, 1),     SideNormal[4] + (1, 0, 1),      SideNormal[4] + (1, 0, 0)   ]
            ],
            [ // Back
                [   SideNormal[5] + (0, -1, 0),    SideNormal[5] + (1, -1, 0),     SideNormal[5] + (1, 0, 0)   ],
                [   SideNormal[5] + (1, 0, 0),     SideNormal[5] + (1, 1, 0),      SideNormal[5] + (0, 1, 0)   ],
                [   SideNormal[5] + (0, 1, 0),     SideNormal[5] + (-1, 1, 0),     SideNormal[5] + (-1, 0, 0)  ],
                [   SideNormal[5] + (-1, 0, 0),    SideNormal[5] + (-1, -1, 0),    SideNormal[5] + (0, -1, 0)  ]
            ]
        ];

        // Uv movement
        public static Dictionary<BlockSide, Dictionary<BlockSide, Func<Vector2, Vector2, Vector2, Vector2, (Vector2 a, Vector2 b, Vector2 c, Vector2 d)>>> UvMovements = new()
        {
            {
                BlockSide.Front, new()
                {
                    { BlockSide.Front,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Right,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Top,    (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Left,   (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Bottom, (a, b, c, d) => (c, d, a, b) },
                    { BlockSide.Back,   (a, b, c, d) => (a, b, c, d) }
                }
            },
            {
                BlockSide.Right, new()
                {
                    { BlockSide.Front,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Right,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Top,    (a, b, c, d) => (b, c, d, a) },
                    { BlockSide.Left,   (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Bottom, (a, b, c, d) => (b, c, d, a) },
                    { BlockSide.Back,   (a, b, c, d) => (a, b, c, d) }
                }
            },
            {
                BlockSide.Top, new()
                {
                    { BlockSide.Front,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Right,  (a, b, c, d) => (d, a, b, c) },
                    { BlockSide.Top,    (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Left,   (a, b, c, d) => (b, c, d, a) },
                    { BlockSide.Bottom, (a, b, c, d) => (d, c, b, a) },
                    { BlockSide.Back,   (a, b, c, d) => (c, d, a, b) }
                }
            },
            {
                BlockSide.Left, new()
                {
                    { BlockSide.Front,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Right,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Top,    (a, b, c, d) => (d, a, b, c) },
                    { BlockSide.Left,   (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Bottom, (a, b, c, d) => (d, a, b, c) },
                    { BlockSide.Back,   (a, b, c, d) => (a, b, c, d) }
                }
            },
            {
                BlockSide.Bottom, new()
                {
                    { BlockSide.Front,  (a, b, c, d) => (c, d, a, b) },
                    { BlockSide.Right,  (a, b, c, d) => (d, a, b, c) },
                    { BlockSide.Top,    (a, b, c, d) => (d, c, b, a) },
                    { BlockSide.Left,   (a, b, c, d) => (b, c, d, a) },
                    { BlockSide.Bottom, (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Back,   (a, b, c, d) => (a, b, c, d) }
                }
            },
            {
                BlockSide.Back, new()
                {
                    { BlockSide.Front,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Right,  (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Top,    (a, b, c, d) => (c, d, a, b) },
                    { BlockSide.Left,   (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Bottom, (a, b, c, d) => (a, b, c, d) },
                    { BlockSide.Back,   (a, b, c, d) => (a, b, c, d) }
                }
            },
        };

        public static (Vector2 a, Vector2 b, Vector2 c, Vector2 d) GetRotatedFaceUv(BlockSide from, BlockSide to, Vector2[] uvs) => GetRotatedFaceUv(from, to, uvs[0], uvs[1], uvs[2], uvs[3]);
        public static (Vector2 a, Vector2 b, Vector2 c, Vector2 d) GetRotatedFaceUv(BlockSide from, BlockSide to, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            return UvMovements[from][to](a, b, c, d);
        }

        /// <summary>
        /// Returns the blocks of the raycast
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public static bool RaycastBlocks(Vector3 origin, Vector3 direction, float maxDistance, out List<Vector3i> blocks)
        {
            return Raycast(origin, direction, maxDistance, out blocks, out _);
        }

        /// <summary>
        /// Returns the steps of the raycast
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static bool RaycastSteps(Vector3 origin, Vector3 direction, float maxDistance, out List<Vector3> steps)
        {
            return Raycast(origin, direction, maxDistance, out _, out steps);
        }

        /// <summary>
        /// Returns the blocks, steps and normals of the raycast
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="blocks"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out List<Vector3i> blocks, out List<Vector3> steps)
        {
            blocks = new List<Vector3i>();
            steps = new List<Vector3>();

            if (direction.LengthSquared == 0) return false;
            direction.Normalize();

            Vector3 pos = origin;
            Vector3i blockPos = new Vector3i(
                Mathf.FloorToInt(pos.X),
                Mathf.FloorToInt(pos.Y),
                Mathf.FloorToInt(pos.Z)
            );

            Vector3 safeInvDir = new Vector3(
                direction.X != 0 ? 1 / direction.X : float.MaxValue,
                direction.Y != 0 ? 1 / direction.Y : float.MaxValue,
                direction.Z != 0 ? 1 / direction.Z : float.MaxValue
            );

            Vector3 deltaDist = new Vector3(
                MathF.Abs(safeInvDir.X),
                MathF.Abs(safeInvDir.Y),
                MathF.Abs(safeInvDir.Z)
            );

            Vector3i step = new Vector3i(
                direction.X > 0 ? 1 : -1,
                direction.Y > 0 ? 1 : -1,
                direction.Z > 0 ? 1 : -1
            );

            Vector3 sideDist = new Vector3(
                (direction.X > 0 ? (blockPos.X + 1 - pos.X) : (pos.X - blockPos.X)) * deltaDist.X,
                (direction.Y > 0 ? (blockPos.Y + 1 - pos.Y) : (pos.Y - blockPos.Y)) * deltaDist.Y,
                (direction.Z > 0 ? (blockPos.Z + 1 - pos.Z) : (pos.Z - blockPos.Z)) * deltaDist.Z
            );

            float totalDistance = 0f;

            while (totalDistance <= maxDistance)
            {
                if (sideDist.X < sideDist.Y && sideDist.X < sideDist.Z)
                {
                    totalDistance = sideDist.X;
                    blockPos.X += step.X;
                    sideDist.X += deltaDist.X;
                }
                else if (sideDist.Y < sideDist.Z)
                {
                    totalDistance = sideDist.Y;
                    blockPos.Y += step.Y;
                    sideDist.Y += deltaDist.Y;
                }
                else
                {
                    totalDistance = sideDist.Z;
                    blockPos.Z += step.Z;
                    sideDist.Z += deltaDist.Z;
                }

                if (totalDistance > maxDistance) break;
                blocks.Add(blockPos);
                steps.Add(direction * totalDistance);
            }

            return blocks.Count > 0;
        }

        public static bool Raycast(VoxelRenderer renderer, Vector3 origin, Vector3 direction, float maxDistance, out Hit hit)
        {
            hit = Hit.Base;

            if (direction.LengthSquared == 0)
                return false;
            direction.Normalize();

            Vector3 pos = origin;
            Vector3i blockPos = new Vector3i(
                Mathf.FloorToInt(pos.X),
                Mathf.FloorToInt(pos.Y),
                Mathf.FloorToInt(pos.Z)
            );

            Vector3 safeInvDir = new Vector3(
                direction.X != 0 ? 1 / direction.X : float.MaxValue,
                direction.Y != 0 ? 1 / direction.Y : float.MaxValue,
                direction.Z != 0 ? 1 / direction.Z : float.MaxValue
            );

            Vector3 deltaDist = new Vector3(
                MathF.Abs(safeInvDir.X),
                MathF.Abs(safeInvDir.Y),
                MathF.Abs(safeInvDir.Z)
            );

            Vector3i step = new Vector3i(
                direction.X > 0 ? 1 : -1,
                direction.Y > 0 ? 1 : -1,
                direction.Z > 0 ? 1 : -1
            );

            Vector3 sideDist = new Vector3(
                (direction.X > 0 ? (blockPos.X + 1 - pos.X) : (pos.X - blockPos.X)) * deltaDist.X,
                (direction.Y > 0 ? (blockPos.Y + 1 - pos.Y) : (pos.Y - blockPos.Y)) * deltaDist.Y,
                (direction.Z > 0 ? (blockPos.Z + 1 - pos.Z) : (pos.Z - blockPos.Z)) * deltaDist.Z
            );

            float totalDistance = 0f;
            Vector3i normal;
            int side;

            while (totalDistance <= maxDistance)
            {
                if (sideDist.X < sideDist.Y && sideDist.X < sideDist.Z)
                {
                    totalDistance = sideDist.X;
                    blockPos.X += step.X;
                    sideDist.X += deltaDist.X;
                    normal = (-step.X, 0, 0);
                    side = normal.X > 0 ? 1 : 3;
                }
                else if (sideDist.Y < sideDist.Z)
                {
                    totalDistance = sideDist.Y;
                    blockPos.Y += step.Y;
                    sideDist.Y += deltaDist.Y;
                    normal = (0, -step.Y, 0);
                    side = normal.Y > 0 ? 2 : 4;
                }
                else
                {
                    totalDistance = sideDist.Z;
                    blockPos.Z += step.Z;
                    sideDist.Z += deltaDist.Z;
                    normal = (0, 0, -step.Z);
                    side = normal.Z > 0 ? 5 : 0;
                }

                if (totalDistance > maxDistance) break;

                if (renderer.GetBlock(blockPos, out var block) && block.IsSolid())
                {
                    hit.Distance = totalDistance;
                    hit.Normal = normal;
                    hit.BlockPosition = blockPos;
                    hit.Block = block;
                    hit.Side = side;

                    Vector3 hitPos = origin + direction * totalDistance;
                    Vector2 local;
                    if (normal.X != 0)
                        local = new Vector2(hitPos.Z - blockPos.Z, hitPos.Y - blockPos.Y);
                    else if (normal.Y != 0)
                        local = new Vector2(hitPos.X - blockPos.X, hitPos.Z - blockPos.Z);
                    else
                        local = new Vector2(hitPos.X - blockPos.X, hitPos.Y - blockPos.Y);

                    local = new Vector2(Mathf.Clampy(local.X, 0, 1), Mathf.Clampy(local.Y, 0, 1));

                    const float centerSize = 0.6f;
                    float min = 0.5f - centerSize / 2f;
                    float max = 0.5f + centerSize / 2f;

                    if (local.X >= min && local.X <= max && local.Y >= min && local.Y <= max)
                    {
                        hit.Region = 0;
                    }
                    else
                    {
                        float dx = local.X - 0.5f;
                        float dy = local.Y - 0.5f;

                        float absX = MathF.Abs(dx);
                        float absY = MathF.Abs(dy);

                        if (absX > absY)
                        {
                            hit.Region = dx < 0 ? 1 : 3;
                        }
                        else
                        {
                            hit.Region = dy < 0 ? 4 : 2;
                        }
                    }
                        
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the block positions between two positions
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        public static HashSet<Vector3i> GetBlockPositions(Vector3 pos1, Vector3 pos2)
        {
            return GetNewBlockPositions(pos1, pos2, []);
        }

        /// <summary>
        /// Returns the new block positions between two positions
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="blocks"></param>
        public static HashSet<Vector3i> GetNewBlockPositions(Vector3 pos1, Vector3 pos2, HashSet<Vector3i> blocks)
        {
            Vector3i min = Mathf.FloorToInt((Mathf.Min(pos1.X, pos2.X), Mathf.Min(pos1.Y, pos2.Y), Mathf.Min(pos1.Z, pos2.Z)));
            Vector3i max = Mathf.FloorToInt((Mathf.Max(pos1.X, pos2.X), Mathf.Max(pos1.Y, pos2.Y), Mathf.Max(pos1.Z, pos2.Z)));

            for (int x = min.X; x <= max.X; x++)
                for (int y = min.Y; y <= max.Y; y++)
                    for (int z = min.Z; z <= max.Z; z++)
                        blocks.Add(new Vector3i(x, y, z));

            Console.WriteLine($"GetNewBlockPositions: {min} - {max} - {blocks.Count}");

            return blocks;
        }

        public static bool BlockOnBorder(Vector3i blockPosition, out List<Vector3i> offsets)
        {
            offsets = [];
            blockPosition = BlockToRelative(blockPosition);

            if (blockPosition.X == 0) offsets.Add((-1, 0, 0));
            else if (blockPosition.X == 31) offsets.Add((1, 0, 0));

            if (blockPosition.Y == 0) offsets.Add((0, -1, 0));
            else if (blockPosition.Y == 31) offsets.Add((0, 1, 0));

            if (blockPosition.Z == 0) offsets.Add((0, 0, -1));
            else if (blockPosition.Z == 31) offsets.Add((0, 0, 1));

            if (offsets.Count >= 2) // if the block is on 2 or more sides of the chunk, add the chunks that is on the corner
            {
                Vector3i offset = (0, 0, 0);
                foreach (var o in offsets)
                {
                    offset += o;
                }
                offsets.Add(offset);
            }
            return offsets.Count > 0;
        }

        public static Vector3i BlockToChunk(Vector3i position) => (position.X & ~31, position.Y & ~31, position.Z & ~31);
        public static Vector3i BlockToRelative(Vector3i position) => (position.X & 31, position.Y & 31, position.Z & 31);
        public static Vector3i BlockToChunkRelative(Vector3i position) => (position.X >> 5, position.Y >> 5, position.Z >> 5);
        public static Vector3i ChunkRelative(Vector3i position) => BlockToChunkRelative(position);
    }

    public struct Hit
    {
        public static Hit Base = new();
        public float Distance = float.PositiveInfinity;
        public Vector3i Normal = (0, 0, 0);
        public Vector3i BlockPosition = (0, 0, 0);
        public Block Block = Block.Air;
        public int Side;
        public int Region = 0;

        public Hit(float distance, Vector3i normal, Block block)
        {
            Distance = distance;
            Normal = normal;
            Block = block;
        }

        public int GetRotationId(string cardinal)
        {
            int facing = cardinal switch {
                "east" => 1,
                "south" => 2,
                "west" => 3,
                _ => 0,
            };
            return facing | (Region << 2) | (Side << 5);
        }
    }
}