using System.Numerics;
using Hanamura.AssetManagement;
using Hanamura.Systems;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;

namespace Hanamura;

public class Hanamura : Game
{
    private readonly Renderer _renderer;
    private readonly World _world;
    private readonly List<MoonTools.ECS.System> _systems;
    
    public Hanamura(
        AppInfo appInfo, WindowCreateInfo windowCreateInfo, FramePacingSettings framePacingSettings,
        ShaderFormat availableShaderFormats, bool debugMode = false)
        : base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
        AssetManager.Init(RootTitleStorage, GraphicsDevice);
        _world = new World();
        _renderer = new Renderer(_world, MainWindow, GraphicsDevice);
        _systems =
        [
            //Inputs
            new InputActionSystem(_world, Inputs),
            
            //Logic
            new PlayerMovementSystem(_world),
            new TargetFollowSystem(_world),
            
            //Transforms
            new CameraMatrixUpdateSystem(_world),
            new TransformMatrixUpdateSystem(_world)
        ];
        
        CreateMesh(Assets.Meshes.ground);
        CreateMesh(Assets.Meshes.cube);
        
        var player = CreatePlayer();
        CreateCamera(player);
    }

    private Entity CreatePlayer()
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new Player());
        _world.Set(entity, new Transform());
        _world.Set(entity, new MoveAction());
        return entity;
    }
    
    private void CreateCamera(Entity followTarget)
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new MainCamera());
        _world.Set(entity, new CameraMatrix());
        _world.Set(entity, new Transform()
        {
            Position = new Vector3(0, 5, -5),
            Rotation = Quaternion.CreateFromYawPitchRoll(0, 45, 0)
        });
        _world.Set(entity, new FollowTarget(new Vector3(0, 5, -5)));
        _world.Relate(entity, followTarget, new Follows());
    }

    private void CreateMesh(ulong mesh, Vector3 position = new())
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new RenderMesh(mesh));
        _world.Set(entity, new Transform()
        {
            Position = position
        });
        _world.Set(entity, new TransformMatrix());
    }

    protected override void Update(TimeSpan delta)
    {
        AssetManager.CheckForReloadedAssets();

        foreach (var system in _systems)
        {
            system.Update(delta);
        }
    }

    protected override void Draw(double alpha)
    {
        _renderer.Draw(alpha);
    }

    protected override void Destroy()
    {
    }
}