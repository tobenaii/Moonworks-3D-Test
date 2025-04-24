using System.Numerics;
using MoonTools.ECS;

namespace Hanamura.Systems;

public class CameraMatrixUpdateSystem(World world) : MoonTools.ECS.System(world)
{
    public override void Update(TimeSpan delta)
    {
        var camera = GetSingletonEntity<MainCamera>();
        var transform = Get<Transform>(camera);
        
        var forward = Vector3.Transform(Vector3.UnitZ, transform.Rotation);
        var up = Vector3.Transform(Vector3.UnitY, transform.Rotation);
        var position = Get<Transform>(camera).Position;
        
        ref var cameraMatrix = ref GetSingleton<CameraMatrix>();
        cameraMatrix.View = CreateLookAt(
            position,
            position + forward,
            up
        );
    }
    
    private static Matrix4x4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = Vector3.Normalize(target - eye);
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
        var yAxis = Vector3.Cross(zAxis, xAxis);

        return new Matrix4x4(
            xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            -Vector3.Dot(xAxis, eye),
            -Vector3.Dot(yAxis, eye),
            -Vector3.Dot(zAxis, eye),
            1
        );
    }
}