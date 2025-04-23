using System.Numerics;
using MoonTools.ECS;

namespace Hanamura.Systems;

public class TransformMatrixUpdateSystem : MoonTools.ECS.System
{
    private readonly Filter _transformFilter;
    
    public TransformMatrixUpdateSystem(World world) : base(world)
    {
        _transformFilter = FilterBuilder.Include<TransformMatrix>().Include<Transform>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in _transformFilter.Entities)
        {
            var transform = Get<Transform>(entity);
            ref var transformMatrix = ref Get<TransformMatrix>(entity);

            transformMatrix.Transform =
                Matrix4x4.CreateScale(Vector3.One) *
                Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                Matrix4x4.CreateTranslation(transform.Position);
        }
    }
}