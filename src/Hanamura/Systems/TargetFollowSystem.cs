using MoonTools.ECS;

namespace Hanamura.Systems;

public class TargetFollowSystem : MoonTools.ECS.System
{
    private readonly Filter _followFilter;
    
    public TargetFollowSystem(World world) : base(world)
    {
        _followFilter = FilterBuilder.Include<Transform>().Include<FollowTarget>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in _followFilter.Entities)
        {
            var target = OutRelationSingleton<Follows>(entity);
            var follow = Get<FollowTarget>(entity);
            var targetPos = Get<Transform>(target).Position + follow.Offset;
            ref var transform = ref Get<Transform>(entity);
            transform.Position += (targetPos - transform.Position)
                                  * follow.Weight * 100
                                  * (float)delta.TotalSeconds;
            
        }
    }
}