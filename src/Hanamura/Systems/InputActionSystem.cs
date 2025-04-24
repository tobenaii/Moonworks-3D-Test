using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Input;

namespace Hanamura.Systems;

public class InputActionSystem : MoonTools.ECS.System
{
    private readonly Inputs _inputs;

    public InputActionSystem(World world, Inputs inputs) : base(world)
    {
        _inputs = inputs;

        var entity = CreateEntity();
        Set(entity, new MoveAction());
    }

    public override void Update(TimeSpan delta)
    {
        CheckPan();
    }

    private void CheckPan()
    {
        var panDirection = new Vector2();
        if (_inputs.Keyboard.IsDown(KeyCode.W))
        {
            panDirection.Y += 1;
        }

        if (_inputs.Keyboard.IsDown(KeyCode.S))
        {
            panDirection.Y -= 1;
        }

        if (_inputs.Keyboard.IsDown(KeyCode.D))
        {
            panDirection.X += 1;
        }

        if (_inputs.Keyboard.IsDown(KeyCode.A))
        {
            panDirection.X -= 1;
        }

        ref var pan = ref GetSingleton<MoveAction>();
        pan.Direction = panDirection == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(panDirection);
    }
}