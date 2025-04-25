using System.Numerics;
using Hanamura.AssetManagement;
using Hanamura.Systems;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using Filter = MoonTools.ECS.Filter;

namespace Hanamura.Graphics;

public class Renderer : Manipulator
{
    private readonly MaterialRef _mainPipeline;
    private readonly MaterialRef _shadowPipeline;
    
    private readonly Sampler _sampler;
    private readonly Sampler _shadowSampler;
    private readonly Texture _depthStencil;
    private readonly Texture _shadowMap;

    private readonly Window _window;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Filter _renderMeshFilter;
    
    public Renderer(World world, Window window, GraphicsDevice graphicsDevice) : base(world)
    {
        _window = window;
        _graphicsDevice = graphicsDevice;
        _renderMeshFilter = FilterBuilder.Include<RenderMesh>().Include<Transform>().Build();

        var mainPipelineInfo = GetDefaultPipeline();
        mainPipelineInfo.TargetInfo.ColorTargetDescriptions =
        [
            new ColorTargetDescription
            {
                Format = window.SwapchainFormat,
                BlendState = ColorTargetBlendState.Opaque
            }
        ];
        _mainPipeline = AssetManager.RegisterMaterial(
            mainPipelineInfo, Assets.Shaders.lit_vert, Assets.Shaders.lit_frag);

        var shadowPipelineInfo = GetDefaultPipeline();
        shadowPipelineInfo.TargetInfo.DepthStencilFormat = TextureFormat.D32Float;
        _shadowPipeline = AssetManager.RegisterMaterial(
            shadowPipelineInfo, Assets.Shaders.shadow_vert, Assets.Shaders.shadow_frag
        );
        
        _sampler = Sampler.Create(graphicsDevice, SamplerCreateInfo.LinearWrap);
        _shadowSampler = Sampler.Create(graphicsDevice, new SamplerCreateInfo
        {
            MinFilter    = MoonWorks.Graphics.Filter.Nearest,
            MagFilter    = MoonWorks.Graphics.Filter.Nearest,
            MipmapMode   = SamplerMipmapMode.Nearest,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            EnableCompare = true,
            CompareOp = CompareOp.LessOrEqual,
        });
        _depthStencil = Texture.Create2D(
            graphicsDevice,
            window.Width,
            window.Height,
            TextureFormat.D16Unorm,
            TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler
        );
        _shadowMap = Texture.Create2D(
            graphicsDevice,
            4096,
            4096,
            TextureFormat.D32Float,
            TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler
        );
    }

    public void Draw(double alpha)
    {
        var proj = 
            CreatePerspectiveFieldOfView(
                float.DegreesToRadians(75f),
                (float)_window.Width / _window.Height,
                0.01f,
                100f
            );

        var shadowProj = Matrix4x4.CreateOrthographicLeftHanded(25, 25, 1, 25);
        var view = World.GetSingleton<CameraMatrix>().View;
        var shadowView = view;
        var cmdbuf = _graphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = cmdbuf.AcquireSwapchainTexture(_window);
        if (swapchainTexture != null)
        {
            var renderPass = cmdbuf.BeginRenderPass(
                new DepthStencilTargetInfo
                {
                    Texture = _shadowMap,
                    LoadOp = LoadOp.Clear,
                    ClearDepth = 1.0f,
                    StencilLoadOp = LoadOp.DontCare,
                    StoreOp = StoreOp.Store,
                    StencilStoreOp = StoreOp.DontCare
                }
            );
            
            renderPass.BindGraphicsPipeline(_shadowPipeline.Ref.Pipeline);
            foreach (var entity in _renderMeshFilter.Entities)
            {
                var renderMesh = Get<RenderMesh>(entity);
                var transform = Get<TransformMatrix>(entity);
                var mesh = AssetManager.GetMesh(renderMesh.Mesh);
                var mvpUniform = new MatrixUniform(transform.Transform * shadowView * shadowProj);
                renderPass.BindVertexBuffers(mesh.Ref.VertexBufferBinding);
                renderPass.BindIndexBuffer(mesh.Ref.IndexBufferBinding, IndexElementSize.ThirtyTwo);
                cmdbuf.PushVertexUniformData(mvpUniform);
                renderPass.DrawIndexedPrimitives(mesh.Ref.IndexSize, 1, 0, 0, 0);
            }
            cmdbuf.EndRenderPass(renderPass);
            
            renderPass = cmdbuf.BeginRenderPass(
                new DepthStencilTargetInfo(_depthStencil, 1f),
                new ColorTargetInfo(swapchainTexture, LoadOp.Clear)
            );
            var texture = AssetManager.GetTexture(Assets.Textures.pixpal);
            renderPass.BindGraphicsPipeline(_mainPipeline.Ref.Pipeline);
            foreach (var entity in _renderMeshFilter.Entities)
            {
                var renderMesh = Get<RenderMesh>(entity);
                var transform = Get<TransformMatrix>(entity);
                var mesh = AssetManager.GetMesh(renderMesh.Mesh);
                var mvpUniform = new MatrixUniform(transform.Transform * view * proj);
                var modelUniform = new MatrixUniform(transform.Transform);
                var lightUniform = new MatrixUniform(transform.Transform * shadowView * shadowProj);
                renderPass.BindVertexBuffers(mesh.Ref.VertexBufferBinding);
                renderPass.BindIndexBuffer(mesh.Ref.IndexBufferBinding, IndexElementSize.ThirtyTwo);
                cmdbuf.PushVertexUniformData(mvpUniform);
                cmdbuf.PushVertexUniformData(modelUniform, 1);
                cmdbuf.PushVertexUniformData(lightUniform, 2);
                renderPass.BindFragmentSamplers(new TextureSamplerBinding(texture, _sampler), new TextureSamplerBinding(_shadowMap, _shadowSampler));
                renderPass.DrawIndexedPrimitives(mesh.Ref.IndexSize, 1, 0, 0, 0);
            }
            cmdbuf.EndRenderPass(renderPass);
        }
        _graphicsDevice.Submit(cmdbuf);
    }
    
    private static Matrix4x4 CreatePerspectiveFieldOfView(
        float fovY,
        float aspect,
        float znear,
        float zfar)
    {
        var yScale = 1.0f / MathF.Tan(fovY * 0.5f);
        var xScale = yScale / aspect;
        var zRange = zfar - znear;

        return new Matrix4x4(
            xScale, 0, 0, 0,
            0, yScale, 0, 0,
            0, 0, zfar / zRange, 1,
            0, 0, -(znear * zfar) / zRange, 0
        );
    }
    
    private static Matrix4x4 CreateOrthographic(
        float width,
        float height,
        float znear,
        float zfar)
    {
        // scale factors
        var xScale = 2f / width;
        var yScale = 2f / height;
        var zRange = zfar - znear;
        
        return new Matrix4x4(
            xScale, 0f,     0f,           0f,
            0f,     yScale, 0f,           0f,
            0f,     0f,     1f / zRange,  0f,
            0f,     0f,     -znear / zRange, 1f
        );
    }


    private static GraphicsPipelineCreateInfo GetDefaultPipeline()
    {
        return new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
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
            RasterizerState = RasterizerState.CW_CullBack,
            VertexInputState = VertexInputState.CreateSingleBinding<Vertex>(),
        };
    }
}