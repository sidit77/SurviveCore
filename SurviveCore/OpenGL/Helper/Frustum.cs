using System.Numerics;

namespace SurviveCore.OpenGL.Helper {

    class Frustum {

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
