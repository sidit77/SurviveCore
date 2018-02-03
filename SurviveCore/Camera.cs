
using System.Numerics;

namespace SurviveCore {
    class Camera {

        public Matrix4x4 CameraMatrix;

        private Quaternion rotation;
        private Vector3 position;
        private float fov;
        private float aspect;
        private float zFar;
        private float zNear;

        public Camera(float fov, float aspect, float zNear, float zFar) {
            rotation = Quaternion.Identity;
            position = new Vector3(0, 0, 0);
            this.zFar = zFar;
            this.zNear = zNear;
            this.fov = fov;
            this.aspect = aspect;
            Update();
        }

        public void Update() {
            Matrix4x4 perspectiveM = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, zNear, zFar);
            Matrix4x4 rotationM = Matrix4x4.CreateFromQuaternion(rotation);
            Matrix4x4 positionM = Matrix4x4.CreateTranslation(-position);
            CameraMatrix = positionM * rotationM * perspectiveM;
        }

        public Quaternion Rotation {
            get => rotation;
            set => rotation = value;
        }

        public Vector3 Position {
            get => position;
            set => position = value;
        }

        public float Fov {
            get => fov;
            set => fov = value;
        }

        public float Aspect {
            get => aspect;
            set => aspect = value;
        }

        public float ZNear {
            get => zNear;
            set => zNear = value;
        }

        public float ZFar {
            get => zFar;
            set => zFar = value;
        }
        
        public Vector3 Forward => Vector3.Transform(forward, Quaternion.Conjugate(rotation));
        public Vector3 Back => Vector3.Transform(back   , Quaternion.Conjugate(rotation));
        public Vector3 Left => Vector3.Transform(left   , Quaternion.Conjugate(rotation));
        public Vector3 Right => Vector3.Transform(right  , Quaternion.Conjugate(rotation));
        public Vector3 Up => Vector3.Transform(up     , Quaternion.Conjugate(rotation));
        public Vector3 Down => Vector3.Transform(down   , Quaternion.Conjugate(rotation));

        private static readonly Vector3 forward = new Vector3( 0,  0, -1);
        private static readonly Vector3 back    = new Vector3( 0,  0,  1);
        private static readonly Vector3 left    = new Vector3(-1,  0,  0);
        private static readonly Vector3 right   = new Vector3( 1,  0,  0);
        private static readonly Vector3 up      = new Vector3( 0,  1,  0);
        private static readonly Vector3 down    = new Vector3( 0, -1,  0);
    }
}
