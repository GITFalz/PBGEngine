using System.Runtime.InteropServices;
using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using Silk.NET.Input;
using Plane = System.Numerics.Plane;

namespace PBG.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GpuPlane
    {
        public Vector3 Normal;
        public float Distance;
    }

    public class Camera : ScriptingNode
    {
        public float SPEED { get; private set; } = 75f;
        public int SCREEN_WIDTH { get; set; }
        public int SCREEN_HEIGHT { get; set; }
        public float VERTICAL_SENSITIVITY { get; private set; } = 20f;
        public float HORIZONTAL_SENSITIVITY { get; private set; } = 20f;
        public float SCROLL_SENSITIVITY { get; private set; } = 0.4f;

        //-- Viewport --
        private int _left;
        private int _right;
        private int _bottom;
        private int _top;

        public float FOV
        {
            get => CameraData.FOV;
            set => CameraData.FOV = value;
        }

        public Vector3 Position = (0, 0, 0);
        public Vector3 Center = (0, 0, 0);

        public float Pitch = 0;
        public float Yaw = -90;
        public float Distance = 32;

        public string Cardinal { get => GetCardinal(); }

        public Vector3 up = Vector3.UnitY;
        public Vector3 front = -Vector3.UnitZ;
        public Vector3 right = Vector3.UnitX;

        public Vector2 lastPos;

        public Matrix4 ViewMatrix;
        public Matrix4 ProjectionMatrix;

        private float SMOOTH_FACTOR = 100f;

        private Vector2 _targetMouseDelta;
        private Vector2 _currentMouseDelta = Vector2.Zero;

        public CameraMode _cameraMode = CameraMode.Fixed;

        private Dictionary<CameraMode, Action> _cameraModes;
        private Action _updateAction = () => { };

        public Action FirstMove = () => { };

        public Func<bool> CanZoom = () => true;

        private Plane[] frustumPlanes = new Plane[6];
        private GpuPlane[] _gpuPlanes = new GpuPlane[6];
        public Vector2 input => Input.MovementInput;

        //public int FrustumUBO;

        public Camera() : this(new()) {}
        public Camera(int width, int height, Vector3 position)
        {
            SCREEN_WIDTH = width;
            SCREEN_HEIGHT = height;
            Position = position;

            _cameraModes = new Dictionary<CameraMode, Action>
            {
                {CameraMode.Free, FreeCamera},
                {CameraMode.Fixed, FixedCamera},
                {CameraMode.Follow, FollowCamera},
                {CameraMode.Centered, CenteredCamera},
                {CameraMode.Orbit, OrbitCamera}
            };

            FirstMove = FirstMove1;

            _updateAction = _cameraModes[_cameraMode];

            //FrustumUBO = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.UniformBuffer, FrustumUBO);
            //GL.NamedBufferStorage(FrustumUBO, 96, IntPtr.Zero, BufferStorageFlags.DynamicStorageBit);
        }

        public Camera(CameraSettings settings)
        {
            Viewport(settings.Viewport);
            Position = settings.Position;

            _cameraModes = new Dictionary<CameraMode, Action>
            {
                {CameraMode.Free, FreeCamera},
                {CameraMode.Fixed, FixedCamera},
                {CameraMode.Follow, FollowCamera},
                {CameraMode.Centered, CenteredCamera},
                {CameraMode.Orbit, OrbitCamera}
            };

            FirstMove = FirstMove1;

            _updateAction = _cameraModes[_cameraMode];
        }   

        public void Viewport((int left, int right, int bottom, int top) data) => Viewport(data.left, data.right, data.bottom, data.top);
        public void Viewport(int left, int right, int bottom, int top)
        {
            _left = left; _right = right; _bottom = bottom; _top = top;
            Resize();
        }

        //public void ApplyViewport() => GL.Viewport(_left, _bottom, SCREEN_WIDTH, SCREEN_HEIGHT);

        //public void SetAsActive() => Scene.SetCameraAsActive(this);

        void Resize()
        {
            SCREEN_WIDTH = (Game.Width - (_left + _right)).Max(1);
            SCREEN_HEIGHT = (Game.Height - (_top + _bottom)).Max(1);
            GetProjectionMatrix();
        }

        public Matrix4 GetViewMatrix()
        {
            ViewMatrix = Matrix4.CreateLookAt(Position, Position + front, up);
            return ViewMatrix;
        }

        public Matrix4 GetProjectionMatrix()
        {
            ProjectionMatrix = Matrix4.CreatePerspective(
                Mathf.DegToRad(FOV),
                (float)SCREEN_WIDTH / (float)SCREEN_HEIGHT,
                0.1f,
                10000f
            );
            return ProjectionMatrix;
        }
        
        public void CalculateFrustumPlanes()
        {
            Matrix4 viewProjectionMatrix = GetViewProjectionMatrix();
            System.Numerics.Matrix4x4 viewProjectionMatrixNumerics = Mathf.Num(viewProjectionMatrix);

            // Extract the frustum planes from the view-projection matrix
            frustumPlanes[0] = new Plane( // Left
                viewProjectionMatrixNumerics.M14 + viewProjectionMatrixNumerics.M11,
                viewProjectionMatrixNumerics.M24 + viewProjectionMatrixNumerics.M21,
                viewProjectionMatrixNumerics.M34 + viewProjectionMatrixNumerics.M31,
                viewProjectionMatrixNumerics.M44 + viewProjectionMatrixNumerics.M41
            );

            frustumPlanes[1] = new Plane( // Right
                viewProjectionMatrixNumerics.M14 - viewProjectionMatrixNumerics.M11,
                viewProjectionMatrixNumerics.M24 - viewProjectionMatrixNumerics.M21,
                viewProjectionMatrixNumerics.M34 - viewProjectionMatrixNumerics.M31,
                viewProjectionMatrixNumerics.M44 - viewProjectionMatrixNumerics.M41
            );

            frustumPlanes[2] = new Plane( // Bottom
                viewProjectionMatrixNumerics.M14 + viewProjectionMatrixNumerics.M12,
                viewProjectionMatrixNumerics.M24 + viewProjectionMatrixNumerics.M22,
                viewProjectionMatrixNumerics.M34 + viewProjectionMatrixNumerics.M32,
                viewProjectionMatrixNumerics.M44 + viewProjectionMatrixNumerics.M42
            );

            frustumPlanes[3] = new Plane( // Top
                viewProjectionMatrixNumerics.M14 - viewProjectionMatrixNumerics.M12,
                viewProjectionMatrixNumerics.M24 - viewProjectionMatrixNumerics.M22,
                viewProjectionMatrixNumerics.M34 - viewProjectionMatrixNumerics.M32,
                viewProjectionMatrixNumerics.M44 - viewProjectionMatrixNumerics.M42
            );

            frustumPlanes[4] = new Plane( // Near
                viewProjectionMatrixNumerics.M14 + viewProjectionMatrixNumerics.M13,
                viewProjectionMatrixNumerics.M24 + viewProjectionMatrixNumerics.M23,
                viewProjectionMatrixNumerics.M34 + viewProjectionMatrixNumerics.M33,
                viewProjectionMatrixNumerics.M44 + viewProjectionMatrixNumerics.M43
            );

            frustumPlanes[5] = new Plane( // Far
                viewProjectionMatrixNumerics.M14 - viewProjectionMatrixNumerics.M13,
                viewProjectionMatrixNumerics.M24 - viewProjectionMatrixNumerics.M23,
                viewProjectionMatrixNumerics.M34 - viewProjectionMatrixNumerics.M33,
                viewProjectionMatrixNumerics.M44 - viewProjectionMatrixNumerics.M43
            );

            // Normalize the planes
            for (int i = 0; i < 6; i++)
            {
                var plane = frustumPlanes[i];
                plane = Plane.Normalize(plane);
                
                _gpuPlanes[i].Normal = new(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
                _gpuPlanes[i].Distance = plane.D;

                frustumPlanes[i] = plane;
            }

            //GL.BindBuffer(BufferTarget.UniformBuffer, FrustumUBO);
            //GL.NamedBufferSubData(FrustumUBO, 0, 96, _gpuPlanes);
        }

        public void BindPlanes(int bindingPoint)
        {
            //GL.BindBuffer(BufferTarget.UniformBuffer, FrustumUBO);
            //GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingPoint, FrustumUBO);
        }

        public bool FrustumIntersectsSphere(System.Numerics.Vector3 center, float radius)
        {
            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                var plane = frustumPlanes[i];
                if (Plane.DotCoordinate(plane, center) < -radius)
                {
                    return false;
                }
            }
            return true;
        }

        public System.Numerics.Matrix4x4 GetNumericsViewMatrix()
        {
            return Mathf.Num(ViewMatrix);
        }

        public System.Numerics.Matrix4x4 GetNumericsProjectionMatrix()
        {
            return Mathf.Num(ProjectionMatrix);
        }

        public Matrix4 GetViewProjectionMatrix()
        {
            return ViewMatrix * ProjectionMatrix;
        }

        public void SetCameraMode(CameraMode mode)
        {
            _cameraMode = mode;
            _updateAction = _cameraModes[mode];
        }

        public CameraMode GetCameraMode()
        {
            return _cameraMode;
        }

        public void UpdateVectors()
        {
            front.X = MathF.Cos(Mathf.DegToRad(Pitch)) * MathF.Cos(Mathf.DegToRad(Yaw));
            front.Y = MathF.Sin(Mathf.DegToRad(Pitch));
            front.Z = MathF.Cos(Mathf.DegToRad(Pitch)) * MathF.Sin(Mathf.DegToRad(Yaw));

            front = Vector3.Normalize(front);
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public Vector3 Yto0(Vector3 v)
        {
            v.Y = 0;
            return Vector3.Normalize(v);
        }

        public Vector3 FrontYto0()
        {
            Vector3 v = front;
            v.Y = 0;
            return Vector3.Normalize(v);
        }

        public Vector3 Front()
        {
            return front;
        }

        public Vector3 RightYto0()
        {
            Vector3 v = right;
            v.Y = 0;
            return Vector3.Normalize(v);
        }

        public string GetCardinal()
        {
            var dir = FrontYto0();
            if (dir.LengthSquared == 0)
                return "north";

            dir.Y = 0;
            dir.Normalize();

            if (MathF.Abs(dir.X) > MathF.Abs(dir.Z))
                return dir.X > 0 ? "east" : "west";
            else
                return dir.Z > 0 ? "south" : "north";
        }

        public void Lock()
        {
            _updateAction = () => { };
        }

        public void Unlock()
        {
            _updateAction = _cameraModes[_cameraMode];
        }

        public void Update()
        {
            _updateAction.Invoke();
            GetViewMatrix();
            CalculateFrustumPlanes();
        }

        public void SetCameraSpeed(float speed) => SPEED = speed;

        private void FreeCamera()
        {
            float speed = SPEED * GameTime.DeltaTime;

            if (input != Vector2.Zero)
            {
                Position += Yto0(front) * input.Y * speed;
                Position -= Yto0(right) * input.X * speed;
            }

            if (Input.IsKeyDown(Key.Space))
            {
                Position.Y += speed;
            }

            if (Input.IsKeyDown(Key.ShiftLeft))
            {
                Position.Y -= speed;
            }

            FirstMove.Invoke();

            RotateCamera();
            UpdateVectors();
        }

        private void FixedCamera()
        {

        }

        private void FollowCamera()
        {
            RotateCamera();
            UpdateVectors();
        }

        private void CenteredCamera()
        {
            Position = Center;
            RotateCamera();
            UpdateVectors();
        }

        public void OrbitCamera() => OrbitCamera(100f);

        public void OrbitCamera(float maxDistance)
        {
            Vector2 mouseDelta = Input.GetMouseDelta();
            Yaw += mouseDelta.X * 0.1f;
            Pitch -= mouseDelta.Y * 0.1f;
            Pitch = Math.Clamp(Pitch, -89f, 89f);

            OrbitCamera(Yaw, Pitch, maxDistance);
        }

        public void OrbitCamera(float yaw, float pitch, float maxDistance)
        {
            Yaw = yaw;
            Pitch = pitch;

            if (CanZoom())
            {
                Distance -= Input.GetMouseScrollDelta().Y * SCROLL_SENSITIVITY * Distance * 0.1f;
                Distance = Math.Clamp(Distance, 1f, maxDistance);
            }

            float yawRad = Mathf.DegToRad(Yaw);
            float pitchRad = Mathf.DegToRad(Pitch);

            Position.X = Center.X - Distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad);
            Position.Y = Center.Y - Distance * Mathf.Sin(pitchRad);
            Position.Z = Center.Z - Distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad);

            front = Vector3.Normalize(Center - Position);
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public void FirstMove1()
        {
            lastPos = Input.GetMousePosition();
            FirstMove = FirstMove2;
        }

        public void FirstMove2()
        {
            lastPos = Input.GetMousePosition();
            FirstMove = () => { };
        }

        private float _targetYaw, _targetPitch;
        private void RotateCamera()
        {
            Vector2 mouseDelta = Input.GetMouseDelta();
            Vector2 delta = mouseDelta * 0.003f;

            _targetYaw   += delta.X * HORIZONTAL_SENSITIVITY;
            _targetPitch -= delta.Y * VERTICAL_SENSITIVITY;
            _targetPitch  = Mathf.Clampy(_targetPitch, -89.0f, 89.0f);

            float t = 1f - MathF.Exp(-SMOOTH_FACTOR * (float)GameTime.DeltaTime);
            Yaw   = Mathf.Lerp(Yaw,   _targetYaw,   t);
            Pitch = Mathf.Lerp(Pitch, _targetPitch, t);
        }
    }

    public enum CameraMode
    {
        Free,
        Fixed,
        Follow,
        Centered,
        Orbit
    }
}

public struct CameraSettings
{
    public (int left, int right, int bottom, int top) Viewport;
    public Vector3 Position = Vector3.Zero;

    public CameraSettings()
    {
        Viewport = (0, 0, 0, 0);
    }
}