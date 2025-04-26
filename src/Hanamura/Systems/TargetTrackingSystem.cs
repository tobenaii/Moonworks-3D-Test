using System.Numerics;
using MoonTools.ECS;

namespace Hanamura.Systems;

public class TargetTrackingSystem : MoonTools.ECS.System
{
    private readonly Filter _followFilter;
    
    public TargetTrackingSystem(World world) : base(world)
    {
        _followFilter = FilterBuilder.Include<Transform>().Include<TrackTarget>().Build();
    }

    private float _derp;
    public override void Update(TimeSpan delta)
    {
        _derp += (float)delta.TotalSeconds;
        foreach (var entity in _followFilter.Entities)
        {
            var target = OutRelationSingleton<Tracks>(entity);
            var follow = Get<TrackTarget>(entity);
            var targetPos = Get<Transform>(target).Position;
            ref var transform = ref Get<Transform>(entity);

            transform.Rotation = Quaternion.CreateFromYawPitchRoll(follow.Yaw, follow.Pitch, 0);

            var finalPos = targetPos - transform.Forward * follow.Distance;
            transform.Position += (finalPos - transform.Position)
                                  * follow.Weight * 100
                                  * (float)delta.TotalSeconds;
        }
    }
}