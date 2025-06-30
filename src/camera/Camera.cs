
using Silk.NET.Maths;

using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Silk.NET.Input;
using System.Numerics;

public class Camera
{
    public Vector3 CameraPosition = new(0.0f, 0.0f, 3.0f);
    public Vector3 CameraFront = new(0.0f, 0.0f, -1.0f);
    public Vector3 CameraUp = Vector3.UnitY;
    public Vector3 CameraDirection = Vector3.Zero;
    public float CameraYaw = -90f;
    public float CameraPitch = 0f;
    public float CameraZoom = 45f;

    public static Vector2D<float> LastMousePos;

    public Camera()
    {
        Input.OnMouseMove += OnMouseMove;
    }


    // public Matrix4X4<float> BuildViewProjectionMatrix(Vector2D<float> extent)
    // {
    //     var view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1));
    //     var proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), extent.X / extent.Y, 0.1f, 10.0f);

    //     return proj;
    // }

    private unsafe void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        var lookSensitivity = 0.1f;
        if (LastMousePos == default)
        {
            LastMousePos = new(pos.X, pos.Y);
        }
        else
        {
            var xOffset = (pos.X - LastMousePos.X) * lookSensitivity;
            var yOffset = (pos.Y - LastMousePos.Y) * lookSensitivity;
            LastMousePos = new(pos.X, pos.Y);

            CameraYaw += xOffset;
            CameraPitch -= yOffset;

            //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
            CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);

            CameraDirection.X = MathF.Cos(Scalar.DegreesToRadians(CameraYaw)) * MathF.Cos(Scalar.DegreesToRadians(CameraPitch));
            CameraDirection.Y = MathF.Sin(Scalar.DegreesToRadians(CameraPitch));
            CameraDirection.Z = MathF.Sin(Scalar.DegreesToRadians(CameraYaw)) * MathF.Cos(Scalar.DegreesToRadians(CameraPitch));
            var normal = Vector3.Normalize(new(CameraDirection.X, CameraDirection.Y, CameraDirection.Z));
            CameraFront = new(normal.X, normal.Y, normal.Z);
        }
    }
}
