
using PBG.MathLibrary;
using PBG.Core;
using PBG.Data;
using PBG.Voxel;
using System.Diagnostics;

namespace PBG.Physics
{
    public class PhysicsBody : ScriptingNode
    {
        public float Gravity = 90;

        public bool Run = true;

        public Vector3 Acceleration = Vector3.Zero;
        public Vector3 Velocity = Vector3.Zero;
        public float Drag = 0.2f;
        public float DecelerationFactor = 1;
        public float Mass = 1f;

        public Collider collider;
        public bool IsGrounded;

        public Vector3 physicsPosition;
        private Vector3 previousPosition;

        private Action GravityAction = () => { };

        private struct PhysicsData
        {
            public Vector3 Position;
            public float Time;
        }

        private PhysicsData previousPhysicsState;
        private PhysicsData currentPhysicsState;
        private object stateLock = new object();

        public VoxelRenderer VoxelRenderer = null!;

        public PhysicsBody()
        {
            Name = "PhysicsBody";
            collider = new Collider((-0.4f, 0, -0.4f), (0.4f, 1.75f, 0.4f));
            EnableGravity();
        }

        void Start()
        {
            SetPosition(Transform.Position);
            VoxelRenderer = Scene.QueryComponent<VoxelRenderer>();
        }

        void Update()
        {
            if (!Run)
                return;

            lock (stateLock)
            {
                Transform.Position = Mathf.Lerp(previousPhysicsState.Position, currentPhysicsState.Position, (float)GameTime.PhysicsInterpolationT);
            }       
        }

        void FixedUpdate()
        {
            if (!Run)
                return;

            GravityAction();
            Velocity += Acceleration * (float)GameTime.FixedDeltaTime;
            Velocity *= 1f - Drag * (float)GameTime.FixedDeltaTime;
            float decelerationFactor = IsGrounded ? 10f : DecelerationFactor;
            Acceleration = -decelerationFactor * Velocity;
            CollisionCheck();

            lock (stateLock)
            {
                previousPhysicsState = currentPhysicsState;
                currentPhysicsState = new PhysicsData 
                { 
                    Position = physicsPosition, 
                    Time = GameTime.FixedTotalTime 
                };
            }
        }

        void Exit()
        {
            Acceleration = Vector3.Zero;
            Velocity = Vector3.Zero;
        }


        public void DisableGravity()
        {
            GravityAction = () => { };
        }

        public void EnableGravity()
        {
            GravityAction = ApplyGravity;
        }

        public void SetPosition(Vector3 position)
        {
            physicsPosition = position;
            previousPosition = position;
            Transform.Position = position;
        }

        public void AddForce(Vector3 force)
        {
            Acceleration += force / Mass;
        }

        public void AddForce(Vector3 direction, float maxSpeed)
        {
            Acceleration += direction.Normalized() * maxSpeed / Mass;
        }

        public void ApplyGravity()
        {
            Acceleration.Y -= Gravity;
        }

        public bool CollidesWidthBlockAt(Vector3i position, Block block)
        {
            Collider currentCollider = collider + physicsPosition;
            return BlockCollision.CheckCollision(currentCollider, position, new Block(BlockState.Solid, 0));
        }


        public void CollisionCheck()
        {
            Vector3 checkDistance = Velocity * (float)GameTime.FixedDeltaTime;

            Collider currentCollider = collider + physicsPosition;
            Collider nextCollider = collider + physicsPosition + checkDistance;

            Vector3i min = Mathf.FloorToInt(Mathf.Min(currentCollider.Min, nextCollider.Min));
            Vector3i max = Mathf.FloorToInt(Mathf.Max(currentCollider.Max, nextCollider.Max));

            List<Vector3i> blockPositions = [];

            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int z = min.Z; z <= max.Z; z++)
                    {
                        if (!VoxelRenderer.IsAir_Fast((x, y, z)))
                            blockPositions.Add((x, y, z));
                    }
                }
            }

            Vector3 newPhysicsPosition = physicsPosition;

            // Check Y axis collision first
            (float entryTime, Vector3? normal) entryData = (1f, null);
            Vector3 testingDirection = (0, checkDistance.Y, 0);

            for (int i = 0; i < blockPositions.Count; i++)
            {
                entryData = BlockCollision.GetEntry(currentCollider, testingDirection, blockPositions[i], new Block(BlockState.Solid, 0), entryData);
            }

            if (entryData.normal != null && entryData.normal.Value.Y != 0)
            {
                Velocity.Y = 0;
                newPhysicsPosition.Y += testingDirection.Y * entryData.entryTime;
                IsGrounded = entryData.normal.Value.Y > 0;
            }
            else
            {
                newPhysicsPosition.Y += checkDistance.Y;
            }

            currentCollider = collider + newPhysicsPosition;

            // Check X axis collision
            entryData = (1f, null);
            testingDirection = (checkDistance.X, 0, 0);

            for (int i = 0; i < blockPositions.Count; i++)
            {
                entryData = BlockCollision.GetEntry(currentCollider, testingDirection, blockPositions[i], new Block(BlockState.Solid, 0), entryData);
            }

            if (entryData.normal != null && entryData.normal.Value.X != 0)
            {
                Velocity.X = 0;
                newPhysicsPosition.X += testingDirection.X * entryData.entryTime;
            }
            else
            {
                newPhysicsPosition.X += checkDistance.X;
            }

            currentCollider = collider + newPhysicsPosition;

            // Check Z axis collision
            entryData = (1f, null);
            testingDirection = (0, 0, checkDistance.Z);

            for (int i = 0; i < blockPositions.Count; i++)
            {
                entryData = BlockCollision.GetEntry(currentCollider, testingDirection, blockPositions[i], new Block(BlockState.Solid, 0), entryData);
            }

            if (entryData.normal != null && entryData.normal.Value.Z != 0)
            {
                Velocity.Z = 0;
                newPhysicsPosition.Z += testingDirection.Z * entryData.entryTime;
            }
            else
            {
                newPhysicsPosition.Z += checkDistance.Z;
            }

            if (Velocity.Y != 0)
                IsGrounded = false;

            previousPosition = physicsPosition;
            physicsPosition = newPhysicsPosition;
        }
    }
}