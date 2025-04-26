using System.Numerics;
using Hanamura.AssetManagement;
using Hanamura.Systems;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using Random = System.Random;
using Renderer = Hanamura.Graphics.Renderer;

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
            new TargetTrackingSystem(_world),
            new MeshCullingSystem(_world),
            
            //Transform
            new TransformMatrixUpdateSystem(_world)
        ];

        const int sqrCount = 50;
        var random = new Random();
        for (var x = -sqrCount; x < sqrCount; x++)
        {
            for (var y = -sqrCount; y < sqrCount; y++)
            {
                var pos = new Vector3(x, 0, y);
                var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, random.NextSingle() * 360);
                CreateMesh(Assets.Meshes.test_plant, pos, rotation);
            }
        }
        
        CreateMesh(Assets.Meshes.ground, shouldCull: false);
        
        var player = CreatePlayer();
        
        var mainCamera = CreateCamera(player, float.DegreesToRadians(60), float.DegreesToRadians(0));
        _world.Set(mainCamera, new MainCamera());
    }

    private Entity CreatePlayer()
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new Player());
        _world.Set(entity, new Transform());
        _world.Set(entity, new TransformMatrix());
        _world.Set(entity, new MoveAction());
        _world.Set(entity, new RenderMesh(Assets.Meshes.cube));
        return entity;
    }
    
    private Entity CreateCamera(Entity followTarget, float pitch, float yaw)
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new Transform());
        _world.Set(entity, new TrackTarget(Distance: 5, pitch, yaw));
        _world.Relate(entity, followTarget, new Tracks());
        return entity;
    }
    
    private void CreateMesh(ulong mesh, Vector3 position = new(), Quaternion rotation = new(), bool shouldCull = true)
    {
        var entity = _world.CreateEntity();
        if (shouldCull)
        {
            _world.Set(entity, new ShouldCull());
        }
        _world.Set(entity, new RenderMesh(mesh));
        _world.Set(entity, new Transform()
        {
            Position = position,
            Rotation = rotation
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