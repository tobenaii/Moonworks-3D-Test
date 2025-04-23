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
            new CameraMatrixUpdateSystem(_world),
            new TransformMatrixUpdateSystem(_world)
        ];
        
        CreateCamera();
        CreateGround();
    }

    private void CreateCamera()
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new CameraData(90, Vector3.Zero, 10));
        _world.Set(entity, new CameraMatrix());
    }

    private void CreateGround()
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new RenderMesh(Assets.Meshes.ground));
        _world.Set(entity, new Transform());
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