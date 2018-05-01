
using System.Numerics;

namespace SurviveCore {
    public class Camera {

        public Matrix4x4 CameraMatrix;

        private Quaternion rotation;
        private Vector3 position;
        private float fov;
        private float aspect;
        private float zFar;
        private float zNear;

        private Frustum frustum;

        public Frustum Frustum => frustum;
        
        public Camera(float fov, float aspect, float zNear, float zFar) {
            rotation = Quaternion.Identity;
            position = new Vector3(0, 0, 0);
            this.zFar = zFar;
            this.zNear = zNear;
            this.fov = fov;
            this.aspect = aspect;
            Update(false);
            frustum = new Frustum(CameraMatrix);
        }

        public void Update(bool frustum = true) {
            Matrix4x4 perspectiveM = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, zNear, zFar);
            Matrix4x4 rotationM = Matrix4x4.CreateFromQuaternion(rotation);
            Matrix4x4 positionM = Matrix4x4.CreateTranslation(-position);
            CameraMatrix = positionM * rotationM * perspectiveM;
            if(frustum)
                this.frustum.Update(CameraMatrix);
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
        
        public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Quaternion.Conjugate(rotation)));
        public Vector3 Back    => Vector3.Normalize(Vector3.Transform( Vector3.UnitZ, Quaternion.Conjugate(rotation)));
        public Vector3 Left    => Vector3.Normalize(Vector3.Transform(-Vector3.UnitX, Quaternion.Conjugate(rotation)));
        public Vector3 Right   => Vector3.Normalize(Vector3.Transform( Vector3.UnitX, Quaternion.Conjugate(rotation)));
        public Vector3 Down    => Vector3.Normalize(Vector3.Transform(-Vector3.UnitY, Quaternion.Conjugate(rotation)));
        public Vector3 Up      => Vector3.Normalize(Vector3.Transform( Vector3.UnitY, Quaternion.Conjugate(rotation)));
        
    }
    
    public class Frustum {

        private readonly Plane[] planes;

        public Frustum(Matrix4x4 matrix) {
            planes = new Plane[6];
            Update(matrix);
        }

        public void Update(Matrix4x4 matrix) {

            Vector4 R1 = new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41);
            Vector4 R2 = new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42);
            Vector4 R3 = new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43);
            Vector4 R4 = new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44);

            planes[0] = Plane.Normalize(new Plane(R4 + R1));
            planes[1] = Plane.Normalize(new Plane(R4 - R1));
            planes[2] = Plane.Normalize(new Plane(R4 - R2));
            planes[3] = Plane.Normalize(new Plane(R4 + R2));
            planes[4] = Plane.Normalize(new Plane(R4 + R3));
            planes[5] = Plane.Normalize(new Plane(R4 - R3));

        }

        public bool Intersection(Vector3 p) {
            for(int i = 0; i < 6; i++) {
                if(Plane.DotCoordinate(planes[i], p) < 0)
                    return false;
            }
            return true;
        }

        public bool Intersection(Vector3 p, float r) {
            for(int i = 0; i < 6; i++) {
                if(Plane.DotCoordinate(planes[i], p) < -r)
                    return false;
            }
            return true;
        }

        public bool Intersection(Vector3 min, Vector3 max) {
            for(int i = 0; i < 6; i++) {
                if(Vector3.Dot(planes[i].Normal, new Vector3(planes[i].Normal.X < 0 ? min.X : max.X, planes[i].Normal.Y < 0 ? min.Y : max.Y, planes[i].Normal.Z < 0 ? min.Z : max.Z)) < -planes[i].D)
                    return false;
            }
            return true;
        }
    }
}
