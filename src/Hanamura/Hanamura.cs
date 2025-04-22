using System.Numerics;
using Hanamura.Types;
using MoonWorks;
using MoonWorks.Graphics;

namespace Hanamura;

public class Hanamura : Game
{
    private readonly MaterialRef _pipeline;
    private readonly Sampler _sampler;
    private readonly Texture _depthTexture;
    
    public Hanamura(
        AppInfo appInfo, WindowCreateInfo windowCreateInfo, FramePacingSettings framePacingSettings,
        ShaderFormat availableShaderFormats, bool debugMode = false)
        : base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
        AssetManager.Init(RootTitleStorage, GraphicsDevice);
        
        var pipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = MainWindow.SwapchainFormat,
                        BlendState = ColorTargetBlendState.Opaque
                    }
                ],
                HasDepthStencilTarget = true,
                DepthStencilFormat = TextureFormat.D16Unorm
            },
            DepthStencilState = new DepthStencilState()
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = CompareOp.LessOrEqual
            },
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullBack,
            VertexInputState = VertexInputState.CreateSingleBinding<Vertex>(),
        };
        _pipeline = AssetManager.RegisterMaterial(
            pipelineCreateInfo, Assets.Shaders.lit_vert, Assets.Shaders.lit_frag);
        _sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);
        _depthTexture = Texture.Create2D(
            GraphicsDevice,
            MainWindow.Width,
            MainWindow.Height,
            TextureFormat.D16Unorm,
            TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler
        );
    }
    protected override void Update(TimeSpan delta)
    {
        AssetManager.CheckForReloadedAssets();
    }

    private float _rotation;
    protected override void Draw(double alpha)
    {
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(
            float.DegreesToRadians(75f),
            (float) MainWindow.Width / MainWindow.Height,
            0.01f,
            100f
        );
        
        var view = Matrix4x4.CreateLookAt(
            new Vector3(0, 2, -5),
            new Vector3(0, 2, 0),
            Vector3.UnitY
        );
        
        var model = Matrix4x4.CreateRotationY(_rotation);
        _rotation += 0.001f;
        
        var mvpUniform = new TransformVertexUniform(model * view * proj, model);
        
        var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = cmdbuf.AcquireSwapchainTexture(MainWindow);
        if (swapchainTexture != null)
        {
            var renderPass = cmdbuf.BeginRenderPass(
                new DepthStencilTargetInfo(_depthTexture, 1f),
                new ColorTargetInfo(swapchainTexture, LoadOp.Clear, true)
            );
            var mesh = AssetManager.GetMesh(Assets.Meshes.cube);
            var texture = AssetManager.GetTexture(Assets.Textures.tile_green);
            
            renderPass.BindGraphicsPipeline(_pipeline.Ref.Pipeline);
            renderPass.BindVertexBuffers(mesh.Ref.VertexBuffer);
            renderPass.BindIndexBuffer(mesh.Ref.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(mvpUniform);
            renderPass.BindFragmentSamplers(new TextureSamplerBinding(texture, _sampler));
            renderPass.DrawIndexedPrimitives(mesh.Ref.IndexBuffer.Size, 1, 0, 0, 0);
            cmdbuf.EndRenderPass(renderPass);
        }
        GraphicsDevice.Submit(cmdbuf);
    }

    protected override void Destroy()
    {
        _sampler.Dispose();
    }
}