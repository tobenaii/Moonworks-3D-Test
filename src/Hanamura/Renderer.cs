using System.Numerics;
using Hanamura.AssetManagement;
using Hanamura.Types;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using Filter = MoonTools.ECS.Filter;

namespace Hanamura;

public class Renderer : Manipulator
{
    private readonly MaterialRef _pipeline;
    private readonly Sampler _sampler;
    private readonly Texture _depthTexture;

    private readonly Window _window;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Filter _renderMeshFilter;
    
    public Renderer(World world, Window window, GraphicsDevice graphicsDevice) : base(world)
    {
        _window = window;
        _graphicsDevice = graphicsDevice;
        _renderMeshFilter = FilterBuilder.Include<RenderMesh>().Include<Transform>().Build();
        
        var pipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = window.SwapchainFormat,
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
        _sampler = Sampler.Create(graphicsDevice, SamplerCreateInfo.LinearWrap);
        _depthTexture = Texture.Create2D(
            graphicsDevice,
            window.Width,
            window.Height,
            TextureFormat.D16Unorm,
            TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler
        );
    }

    public void Draw(double alpha)
    {
        var proj =
            Matrix4x4.CreatePerspectiveFieldOfView(
                float.DegreesToRadians(75f),
                (float)_window.Width / _window.Height,
                0.01f,
                100f
            );

        var view = World.GetSingleton<CameraMatrix>().View;
        var cmdbuf = _graphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = cmdbuf.AcquireSwapchainTexture(_window);
        if (swapchainTexture != null)
        {
            var renderPass = cmdbuf.BeginRenderPass(
                new DepthStencilTargetInfo(_depthTexture, 1f),
                new ColorTargetInfo(swapchainTexture, LoadOp.Clear, true)
            );
            var texture = AssetManager.GetTexture(Assets.Textures.tile_green);
            
            renderPass.BindGraphicsPipeline(_pipeline.Ref.Pipeline);
            foreach (var entity in _renderMeshFilter.Entities)
            {
                var renderMesh = Get<RenderMesh>(entity);
                var transform = Get<TransformMatrix>(entity);
                var mesh = AssetManager.GetMesh(renderMesh.Mesh);
                var mvpUniform = new TransformVertexUniform(transform.Transform * view * proj, transform.Transform);
                renderPass.BindVertexBuffers(mesh.Ref.VertexBufferBinding);
                renderPass.BindIndexBuffer(mesh.Ref.IndexBufferBinding, IndexElementSize.ThirtyTwo);
                cmdbuf.PushVertexUniformData(mvpUniform);
                renderPass.BindFragmentSamplers(new TextureSamplerBinding(texture, _sampler));
                renderPass.DrawIndexedPrimitives(mesh.Ref.IndexSize, 1, 0, 0, 0);
            }
            cmdbuf.EndRenderPass(renderPass);
        }
        _graphicsDevice.Submit(cmdbuf);
    }
}