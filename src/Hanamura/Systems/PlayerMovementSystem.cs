using System.Numerics;
using MoonTools.ECS;

namespace Hanamura.Systems;

public class PlayerMovementSystem(World world) : MoonTools.ECS.System(world)
{
    public override void Update(TimeSpan delta)
    {
        var player = GetSingletonEntity<Player>();
        var move = GetSingleton<MoveAction>();
        ref var transform = ref Get<Transform>(player);
        transform.Position += new Vector3(move.Direction.X, 0, move.Direction.Y) * 2 * (float)delta.TotalSeconds;
    }
}