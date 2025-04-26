using System.Diagnostics;
using System.Numerics;
using MoonTools.ECS;

namespace Hanamura.Systems;

public class MeshCullingSystem : MoonTools.ECS.System
{
    private readonly Filter _culledFilter;
    private readonly Filter _unculledFilter;
    
    public MeshCullingSystem(World world) : base(world)
    {
        _culledFilter = FilterBuilder.Include<Transform>().Include<RenderMesh>().Include<ShouldCull>().Include<CulledMesh>().Build();
        _unculledFilter = FilterBuilder.Include<Transform>().Include<RenderMesh>().Include<ShouldCull>().Exclude<CulledMesh>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        var camera = GetSingletonEntity<MainCamera>();
        var cameraTransform = Get<Transform>(camera);
        
        foreach (var entity in _unculledFilter.Entities)
        {
            var transform = Get<Transform>(entity);
            
            if (Vector3.Distance(cameraTransform.Position, transform.Position) > 20)
            {
                Set(entity, new CulledMesh());
            }
        }
        
        foreach (var entity in _culledFilter.Entities)
        {
            var transform = Get<Transform>(entity);

            if (Vector3.Distance(cameraTransform.Position, transform.Position) < 20)
            {
                Remove<CulledMesh>(entity);
            }
        }
    }
}