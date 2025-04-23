using System.Numerics;
using MoonTools.ECS;

namespace Hanamura.Systems;

public class CameraMatrixUpdateSystem : MoonTools.ECS.System
{
    private readonly Filter _cameraFilter;

    public CameraMatrixUpdateSystem(World world) : base(world)
    {
        _cameraFilter = FilterBuilder.Include<CameraMatrix>().Include<CameraData>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        ref var cameraData = ref GetSingleton<CameraData>();
        ref var cameraMatrix = ref GetSingleton<CameraMatrix>();

        var yaw = float.DegreesToRadians(cameraData.Yaw);
        var pitch = float.DegreesToRadians(cameraData.Pitch);
        var rotMatrix = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, 0);
        var forward = Vector3.Transform(Vector3.UnitZ, rotMatrix);
        var up = Vector3.Transform(Vector3.UnitY, rotMatrix);
        var camPos = cameraData.TargetPosition - forward * cameraData.TargetDistance;

        cameraMatrix.View = CreateLookAt(
            camPos,
            cameraData.TargetPosition,
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