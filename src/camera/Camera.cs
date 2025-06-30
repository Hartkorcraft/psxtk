
using Silk.NET.Maths;

public class Camera(Vector3D<float> eye, Vector3D<float> target, Vector3D<float> up, float aspect, float fovy, float znear, float zfar)
{
    public Vector3D<float> eye = eye;
    public Vector3D<float> target = target;
    public Vector3D<float> up = up;
    public float aspect = aspect;
    public float fovy = fovy;
    public float znear = znear;
    public float zfar = zfar;

    public Matrix4X4<float> BuildViewProjectionMatrix(Vector2D<float> extent)
    {
        var view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1));
        var proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), extent.X / extent.Y, 0.1f, 10.0f);

        return proj;
    }
}

public class CameraUniform
{

}