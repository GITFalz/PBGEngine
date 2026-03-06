using PBG.MathLibrary;
using PBG.Hash;
using PBG.Rendering;
using PBG.Voxel;

public static class TreeGenerator
{
    public static bool Cancel = false;

    public static bool Run(TreeGenerationInfo info, Vector3i start, Action<Vector3i, Block, bool> setBlock)
    {
        if (Trunk(info, start, 0, 0, setBlock))
            return true;

        Cancel = false;
        return false;
    }

    public static bool Trunk(TreeGenerationInfo info, Vector3i start, int currentTrunk, uint index, Action<Vector3i, Block, bool> setBlock)
    {
        uint seed = info.Seed + index;
        if (!BlockData.GetBlock("log_block", out uint logId))
            throw new InvalidOperationException("log_block not found");

        float xt = Hash.HashFloat(start.X + 31.415f, seed);
        float zt = Hash.HashFloat(start.Z + 7587.732f, seed);

        float xa = Mathf.Lerp(info.TiltFactorXMin, info.TiltFactorXMax, xt);
        float za = Mathf.Lerp(info.TiltFactorYMin, info.TiltFactorYMax, zt);
        float ya = ((1 - Math.Abs(xa)) + (1 - Math.Abs(za))) * 0.5f;

        Vector3 angle = Vector3.Normalize((xa, ya, za));

        float length = Mathf.Lerp(info.HeightMin, info.HeightMax, Hash.HashVector3i(start, seed + 9381));
        Vector3i end = Mathf.RoundToInt(start + (angle * length));

        Vector3i lineStart = Mathf.Min(start, end);
        Vector3i lineEnd = Mathf.Max(start, end);

        float factorStart = (float)currentTrunk / (float)info.Count;
        float factorEnd = Mathf.Clampy((float)(currentTrunk + 1f) / (float)info.Count, 0, 1);

        EaseEffect Ease = EaseEffect.GetEaseEffect(PBG.Rendering.EasingType.EaseOut);

        factorStart = Ease.Ease(0, 1, factorStart);
        factorEnd = Ease.Ease(0, 1, factorEnd);

        float thicknessStart = Mathf.Lerp(info.ThicknessStart, info.ThicknessEnd, factorStart);
        float thicknessEnd = Mathf.Lerp(info.ThicknessStart, info.ThicknessEnd, factorEnd);

        int thick = Mathf.CeilToInt(thicknessStart);
        for (int x = lineStart.X - thick; x < lineEnd.X + thick; x++)
        {
            for (int y = lineStart.Y - thick; y < lineEnd.Y + thick; y++)
            {
                for (int z = lineStart.Z - thick; z < lineEnd.Z + thick; z++)
                {
                    if (Cancel) return false;
                    Vector3i pos = (x, y, z);
                    float distance = DistanceToSegment(pos, start, end, out float t, out Vector3 tp);
                    if (distance <= Mathf.Lerp(thicknessStart, thicknessEnd, t))
                    {
                        var logBlock = new Block(BlockState.Solid, logId);
                        logBlock.SetRotation(0xFF);
                        setBlock(pos, logBlock, false);
                    }
                }
            }
        }

        // When trunk is done, generate next trunk(s)
        bool generateLeaves = true;
        if (currentTrunk < info.Count)
        {
            float tb = Hash.HashFloat(info.SplitMin + info.SplitMax + xt + zt, seed + 242424);
            float maxTrunk = Mathf.Lerp(info.SplitMin, info.SplitMax, tb);

            for (uint trunk = 0; trunk < Mathf.CeilToInt(info.SplitMax); trunk++)
            {
                if (trunk <= maxTrunk)
                {
                    generateLeaves = false;
                    if (!Trunk(info, end, currentTrunk + 1, trunk * 89567, setBlock))
                    {
                        return false;
                    }
                }
            }
        }

        // Branches
        float first = Mathf.Lerp(info.BranchFirstTrunkMin, info.BranchFirstTrunkMax, Hash.HashFloat(start.X + start.Y + start.Z + 12345f, seed + 55555));
        if (currentTrunk + 1 >= first)
        {
            int branchCount = Mathf.RoundToInt(Mathf.Lerp(info.BranchCountMin, info.BranchCountMax, Hash.HashFloat(start.X * 12.34f + start.Y * 56.78f + start.Z * 90.12f, seed + 77777)));
            for (int b = 0; b < branchCount; b++)
            {
                float t = (float)b / (float)branchCount;
                t += (Hash.HashFloat(start.X * 98.76f + start.Y * 54.32f + start.Z * 10.98f, seed + (uint)(b * 12345)) - 0.5f) * info.BranchPositionVariance;
                t = Mathf.Clamp01y(t);
                t = Mathf.Lerp(info.BranchTrunkStart, info.BranchTrunkEnd, t);
                Vector3i branchStart = Mathf.RoundToInt(start + new Vector3(end - start) * t);

                float angleY = Mathf.Lerp(info.BranchAngleMin, info.BranchAngleMax, Hash.HashFloat(branchStart.X * 98.76f + branchStart.Y * 54.32f + branchStart.Z * 10.98f, seed + (uint)(b * 12345)));
                float tilt = Mathf.Lerp(info.BranchTiltMin, info.BranchTiltMax, Hash.HashFloat(branchStart.X * 21.21f + branchStart.Y * 43.43f + branchStart.Z * 65.65f, seed + (uint)(b * 54321)));

                float azimuth = Mathf.DegreesToRadians(angleY);
                float elevation = Mathf.DegreesToRadians(tilt);

                Vector3 dir = new Vector3(
                    Mathf.Cos(elevation) * Mathf.Cos(azimuth),
                    Mathf.Sin(elevation),
                    Mathf.Cos(elevation) * Mathf.Sin(azimuth)
                );

                Vector3 branchDirection = Vector3.Normalize(dir);

                float branchLength = Mathf.Lerp(info.BranchLengthMin, info.BranchLengthMax, Hash.HashVector3i(branchStart, seed + (uint)(b * 54321)));
                branchLength *= Mathf.Lerp(1, info.BranchLengthFalloff, t);
                Vector3i branchEnd = branchStart + Mathf.RoundToInt(branchDirection * branchLength);

                float branchThickness = Mathf.Lerp(info.BranchThicknessMin, info.BranchThicknessMax, Hash.HashFloat(branchStart.X * 11.11f + branchStart.Y * 22.22f + branchStart.Z * 33.33f, seed + (uint)(b * 67890)));

                int bthick = Mathf.CeilToInt(branchThickness);
                Vector3i bLineStart = Mathf.Min(branchStart, branchEnd);
                Vector3i bLineEnd = Mathf.Max(branchStart, branchEnd);

                for (int x = bLineStart.X - bthick; x < bLineEnd.X + bthick; x++)
                {
                    for (int y = bLineStart.Y - bthick; y < bLineEnd.Y + bthick; y++)
                    {
                        for (int z = bLineStart.Z - bthick; z < bLineEnd.Z + bthick; z++)
                        {
                            if (Cancel) return false;
                            Vector3i pos = (x, y, z);
                            float distance = DistanceToSegment(pos, branchStart, branchEnd, out float bt, out Vector3 btp);
                            if (distance <= branchThickness)
                            {
                                var logBlock = new Block(BlockState.Solid, logId);
                                logBlock.SetRotation(0xFF);
                                setBlock(pos, logBlock, true);
                            }
                        }
                    }
                }

                // Leaves at the end of the branch
                float positionT = Hash.HashFloat(end.X * 12.34f + end.Y * 56.78f + end.Z * 90.12f, seed + 88888);
                positionT = Mathf.Lerp(info.LeafClusterPositionMin, info.LeafClusterPositionMax, positionT);
                Vector3i position = Mathf.RoundToInt(Mathf.Lerp(new Vector3(branchStart), new Vector3(branchEnd), positionT));
                int leafType = Mathf.Clampy(info.LeafClusterType, 0, _leafClusterTypes.Length - 1);
                if (!_leafClusterTypes[leafType](info, branchDirection, position, setBlock))
                    return false;
                }
        }

        if (generateLeaves)
        {
            // Leaves at the end of the trunk
            float positionT = Hash.HashFloat(end.X * 12.34f + end.Y * 56.78f + end.Z * 90.12f, seed + 88888);
            positionT = Mathf.Lerp(info.LeafClusterPositionMin, info.LeafClusterPositionMax, positionT);
            Vector3i position = Mathf.RoundToInt(Mathf.Lerp(new Vector3(start), new Vector3(end), positionT));
            Vector3 direction = new Vector3(end - start);
            int leafType = Mathf.Clampy(info.LeafClusterType, 0, _leafClusterTypes.Length - 1);
            if (!_leafClusterTypes[leafType](info, direction, position, setBlock))
                return false;
        }

        return true;
    }

    private static readonly Func<TreeGenerationInfo, Vector3, Vector3i, Action<Vector3i, Block, bool>, bool>[] _leafClusterTypes =
    [
        SphereLeafCluster,
        CubeLeafCluster,
        ConeLeafCluster,
        CylinderLeafCluster
    ];

    public static bool SphereLeafCluster(TreeGenerationInfo info, Vector3 direction, Vector3i position, Action<Vector3i, Block, bool> setBlock)
    {
        if (!BlockData.GetBlock("leaf_block", out uint leafId))
            throw new InvalidOperationException("leaf_block not found");
    
        // Random radius
        float radiusT = Hash.HashVector3(direction + new Vector3(position) * 1867f, (uint)info.Seed);
        float radius = Mathf.Lerp(info.LeafClusterRadiusMin, info.LeafClusterRadiusMax, radiusT);
        int r = Mathf.CeilToInt(radius);

        // Random scaling per axis
        float scaleXT = Hash.HashVector3(direction + new Vector3(position) * 3456f, (uint)(info.Seed + 1));
        float scaleYT = Hash.HashVector3(direction + new Vector3(position) * 5678f, (uint)(info.Seed + 2));
        float scaleZT = Hash.HashVector3(direction + new Vector3(position) * 7890f, (uint)(info.Seed + 3));

        float scaleX = Mathf.Lerp(info.LeafClusterScaleXMin, info.LeafClusterScaleXMax, scaleXT);
        float scaleY = Mathf.Lerp(info.LeafClusterScaleYMin, info.LeafClusterScaleYMax, scaleYT);
        float scaleZ = Mathf.Lerp(info.LeafClusterScaleZMin, info.LeafClusterScaleZMax, scaleZT);

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int z = -r; z <= r; z++)
                {
                    if (Cancel) return false;

                    // Apply scaling to create an ellipsoid
                    float dx = x / scaleX;
                    float dy = y / scaleY;
                    float dz = z / scaleZ;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

                    Vector3i blockPosLocal = new Vector3i(x, y, z);

                    // Noise-based density & falloff
                    float t = Hash.HashVector3i(blockPosLocal, (uint)info.Seed) + ((distance / radius) * info.LeafClusterFalloff);

                    if (t > info.LeafClusterDensity)
                        continue;

                    if (distance <= radius)
                    {
                        setBlock(blockPosLocal + position, new Block(BlockState.Solid, leafId), false);
                    }
                }
            }
        }

        return true;
    }

    public static bool CubeLeafCluster(TreeGenerationInfo info, Vector3 direction, Vector3i position, Action<Vector3i, Block, bool> setBlock)
    {
        if (!BlockData.GetBlock("leaf_block", out uint leafId))
            throw new InvalidOperationException("leaf_block not found");
        // Not implemented yet, just use SphereLeafCluster for now
        return true;
    }

    public static bool ConeLeafCluster(TreeGenerationInfo info, Vector3 direction, Vector3i position, Action<Vector3i, Block, bool> setBlock)
    {
        if (!BlockData.GetBlock("leaf_block", out uint leafId))
            throw new InvalidOperationException("leaf_block not found");

        // ----- Base radius -----
        float radiusT = Hash.HashVector3(direction + new Vector3(position) * 1234f, (uint)info.Seed);
        float baseRadius = Mathf.Lerp(info.LeafClusterRadiusMin, info.LeafClusterRadiusMax, radiusT);

        // ----- Height -----
        float heightT = Hash.HashVector3(direction + new Vector3(position) * 2345f, (uint)(info.Seed + 1));
        float height = Mathf.Lerp(info.LeafClusterHeightMin, info.LeafClusterHeightMax, heightT);
        int h = Mathf.CeilToInt(height);

        // ----- Per-axis scaling for radius -----
        float scaleXT = Hash.HashVector3(direction + new Vector3(position) * 3456f, (uint)(info.Seed + 2));
        float scaleZT = Hash.HashVector3(direction + new Vector3(position) * 5678f, (uint)(info.Seed + 3));

        float scaleX = Mathf.Lerp(info.LeafClusterScaleXMin, info.LeafClusterScaleXMax, scaleXT);
        float scaleZ = Mathf.Lerp(info.LeafClusterScaleZMin, info.LeafClusterScaleZMax, scaleZT);

        // ----- Rotation (if FollowDir == true) -----
        System.Numerics.Quaternion rot = System.Numerics.Quaternion.Identity;

        if (info.LeafClusterFollowBranchDirection)
        {
            System.Numerics.Vector3 up = System.Numerics.Vector3.UnitY;
            System.Numerics.Vector3 target = System.Numerics.Vector3.Normalize(Mathf.Num(direction));

            // If target and up are not collinear
            float dot = System.Numerics.Vector3.Dot(up, target);

            if (dot < 0.9999f)
            {
                // Rotation axis
                System.Numerics.Vector3 axis = System.Numerics.Vector3.Cross(up, target);
                axis = System.Numerics.Vector3.Normalize(axis);

                // Rotation angle
                float angle = MathF.Acos(dot);

                rot = System.Numerics.Quaternion.CreateFromAxisAngle(axis, angle);
            }
            else
            {
                // pointing almost straight up, no rotation needed
                rot = System.Numerics.Quaternion.Identity;
            }
        }

        for (int y = 0; y <= h; y++)
        {
            if (Cancel) return false;

            float tHeight = (float)y / h;

            // Cone shrinks toward the top
            float rAtY = baseRadius * (1f - tHeight);

            int r = Mathf.CeilToInt(rAtY);

            for (int x = -r; x <= r; x++)
            {
                for (int z = -r; z <= r; z++)
                {
                    if (Cancel) return false;

                    // --- Apply radius scaling ---
                    float dx = x / scaleX;
                    float dz = z / scaleZ;
                    float radialDist = Mathf.Sqrt(dx * dx + dz * dz);

                    if (radialDist > rAtY) continue;

                    System.Numerics.Vector3 local = new System.Numerics.Vector3(x, y, z);

                    // --- Rotate the cone if needed ---
                    if (info.LeafClusterFollowBranchDirection)
                        local = System.Numerics.Vector3.Transform(local, rot);

                    Vector3i blockPosLocal = new Vector3i(
                        Mathf.RoundToInt(local.X),
                        Mathf.RoundToInt(local.Y),
                        Mathf.RoundToInt(local.Z)
                    );

                    // Noise-based density & falloff
                    float distNorm = radialDist / baseRadius;
                    float noiseVal =
                        Hash.HashVector3i(blockPosLocal, (uint)info.Seed) +
                        distNorm * info.LeafClusterFalloff;

                    if (noiseVal > info.LeafClusterDensity)
                        continue;

                    setBlock(position + blockPosLocal,
                        new Block(BlockState.Solid, leafId), false);
                }
            }
        }

        return true;
    }

    public static bool CylinderLeafCluster(TreeGenerationInfo info, Vector3 direction, Vector3i position, Action<Vector3i, Block, bool> setBlock)
    {
        if (!BlockData.GetBlock("leaf_block", out uint leafId))
            throw new InvalidOperationException("leaf_block not found");
        // Not implemented yet, just use SphereLeafCluster for now
        return true;
    }

    public static bool LeafCluster(TreeGenerationInfo info, Vector3i position, Action<Vector3i, Block> setBlock)
    {
        /*
         also could you make the direction work, the direction is basically the direction of the branch it is being generated on so when a variable in info is true lets say FollowDir, it rotates the shape in that direction, so if the shape was a cone it would point in the direction of "direction" like the tip
        if (!BlockData.GetBlock("leaf_block", out var leafId))
            throw new InvalidOperationException("leaf_block not found");

        int r = Mathf.CeilToInt(info.leafClusterRadius);
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int z = -r; z <= r; z++)
                {
                    if (Cancel) return false;

                    Vector3i blockPosition = (x, y, z);
                    float distance = blockPosition.EuclideanLength;
                    float t = Hash.HashVector3i(blockPosition, (uint)info.seed) + ((distance / info.leafClusterRadius) * info.leafFalloffFactor);
                    if (t > info.leafDensity)
                        continue;

                    if (distance <= info.leafClusterRadius)
                    {
                        setBlock(blockPosition + position, new Block(BlockState.Solid, leafId));
                    }
                }
            }
        }
        */
        return true;
    }

    public static float DistanceToSegment(Vector3 v, Vector3 a, Vector3 b, out float t, out Vector3 tp)
    {
        Vector3 ab = b - a;
        float abLenSq = ab.LengthSquared;

        t = 0f;
        tp = a;

        if (abLenSq == 0f)
        {
            return (v - a).Length;
        }

        float dot = Vector3.Dot(v - a, ab);
        t = dot / abLenSq;

        if (t < 0f)
        {
            tp = a;
            return (v - a).Length;
        }
        else if (t > 1f)
        {
            tp = b;
            return (v - b).Length;
        }
        else
        {
            tp = a + ab * t;
            return (v - tp).Length;
        }
    }
}