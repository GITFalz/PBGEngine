using System.Diagnostics;
using PBG.MathLibrary;

namespace PBG.Data
{
    public static class GameTime
    {
        public static int Fps = 0;
        public static long Ram = 0;
        public static bool FpsUpdated = false;
        public static float DeltaTime { get; private set; }
        public static float TotalTime { get; private set; }
        public static float FixedTotalTime { get; private set; }
        public static double PhysicsInterpolationT = 0f;
        /// <summary>
        /// delta use to lerp smoothly between the main update and the physics update
        /// </summary>
        public static double PhysicsDelta { get; private set; }
        private static float _time = 0;

        public const int PhysicSteps = 60;
        
        /// <summary>
        /// FixedDeltaTime is the time between each physics update, (only used in the physics thread)
        /// </summary>
        public const double FixedDeltaTime = 1f / PhysicSteps;

        /// <summary>
        /// FixedTime is the time since the last physics update and is the one used to calculate the physics
        /// </summary>
        public static double FixedTime = 0;

        private static float singleDeltaTime = 0;
        
        private static int frameCount = 0;
        private static float elapsedTime = 0;

        public static void Update(float time)
        {
            DeltaTime = time;
            TotalTime += DeltaTime;
            singleDeltaTime = DeltaTime;

            _time += DeltaTime;
            double delta = _time / FixedTime;
            PhysicsDelta = Math.Clamp(delta * 0.95f, 0, 1);
        }

        public static void Render(float delta)
        {
            FpsUpdated = FpsUpdate(delta);
        }

        public static void FixedUpdate(double fixedTime)
        {
            FixedTotalTime = TotalTime;
            FixedTime = fixedTime;
            _time = 0;
        }

        public static float GetSingleDelta()
        {
            float delta = singleDeltaTime;
            singleDeltaTime = 0;
            return delta;
        }

        private static bool FpsUpdate(float delta)
        {
            frameCount++;
            elapsedTime += delta;

            if (elapsedTime >= 1.0f)
            {
                int fps = Mathf.FloorToInt(frameCount / elapsedTime);
                frameCount = 0;
                elapsedTime = 0;

                long memoryBytes = Process.GetCurrentProcess().WorkingSet64;
                Fps = fps;
                Ram = memoryBytes;
            
                return true;
            }

            return false;
        }
    }
}