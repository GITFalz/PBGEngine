using PBG.MathLibrary;

namespace PBG.Rendering
{
    public static class CameraData
    {
        public static float FOV
        {
            get => _fov;
            set
            {
                _fov = value;
                SetLODChunksProjectionMatrix();
            }
        }
        private static float _fov = 70;

        public static float LODChunksNearPlane
        {
            get => _lodChunksNearPlane;
            set
            {
                _lodChunksNearPlane = value;
                SetLODChunksProjectionMatrix();
            }
        }
        private static float _lodChunksNearPlane = 0.1f;

        public static float LODChunksFarPlane
        {
            get => _lodChunksFarPlane;
            set
            {
                _lodChunksFarPlane = value;
                SetLODChunksProjectionMatrix();
            }
        }
        private static float _lodChunksFarPlane = 10000f;

        public static Matrix4 LODChunksProjectionMatrix = Matrix4.Identity;


        public static void SetLODChunksProjectionMatrix()
        {
            LODChunksProjectionMatrix = Matrix4.CreatePerspective(
                Mathf.DegToRad(FOV),
                Game.Width / Game.Height,
                _lodChunksNearPlane,
                _lodChunksFarPlane
            );
        }
    }
}