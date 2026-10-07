using PBG.MathLibrary;
using PBG.NewVoxel;

namespace PBG.Physics
{
    public static class BlockCollision
    {
        public static (float, Vector3?) GetEntry(Collider playerCollider, Vector3 velocity, Vector3 blockPosition, Block block, (float entryTime, Vector3?) entryData)
        {
            var definition = BlockData.BlockDefinitions[block.ID];
            for (int i = 0; i < definition.Colliders[Block.Rotation()].Length; i++)
            {
                var collider = definition.Colliders[Block.Rotation()][i];
                if (!((playerCollider + velocity) & (collider + blockPosition)))
                    continue;

                (float newEntry, Vector3? newNormal) = playerCollider.Collide(collider + blockPosition, velocity);

                if (newNormal == null)
                    continue;

                if (newEntry < entryData.entryTime)
                    entryData = (newEntry - 0.01f, newNormal.Value);
            }

            return entryData;
        }

        public static bool CheckCollision(Collider playerCollider, Vector3 blockPosition, Block block)
        {
            var definition = BlockData.BlockDefinitions[block.ID];
            for (int i = 0; i < definition.Colliders[Block.Rotation()].Length; i++)
            {
                var collider = definition.Colliders[Block.Rotation()][i];
                if (playerCollider & (collider + blockPosition))
                    return true;
            }

            return false;
        }
    }
}