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

        var rotMatrix =
            Matrix4x4.CreateFromYawPitchRoll(
                0,
                float.DegreesToRadians(cameraData.Pitch),
                0
            );
        var forward = Vector3.Transform(Vector3.UnitZ, rotMatrix);
        var up = Vector3.Transform(Vector3.UnitY, rotMatrix);
        var camPos = cameraData.TargetPosition - forward * cameraData.TargetDistance;
        cameraMatrix.View = Matrix4x4.CreateLookAt(
            camPos,
            cameraData.TargetPosition,
            up
        );
    }
}