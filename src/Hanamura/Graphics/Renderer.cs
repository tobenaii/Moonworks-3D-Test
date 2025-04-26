using System.Numerics;
using Hanamura.AssetManagement;
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
        _renderMeshFilter = FilterBuilder.Include<RenderMesh>().Include<Transform>().Include<TransformMatrix>().Exclude<CulledMesh>().Build();

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
                0.1f,
                10f
            );

        var shadowProj = Matrix4x4.CreateOrthographicOffCenterLeftHanded(-13, 13, -6, 9, 1, 5);

        var mainCamera = GetSingletonEntity<MainCamera>();
        var mainCamTransform = Get<Transform>(mainCamera);
        var view = CreateLookAt(mainCamTransform);
        var shadowTarget = GetFrustumCenter(view);
        var shadowTransform = new Transform(shadowTarget + Vector3.UnitY * 5, Quaternion.CreateFromYawPitchRoll(0, float.DegreesToRadians(90), 0));
        var shadowView = CreateLookAt(shadowTransform);
        var cmdbuf = _graphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = cmdbuf.AcquireSwapchainTexture(_window);
        if (swapchainTexture != null)
        {
            var renderPass = cmdbuf.BeginRenderPass(
                new DepthStencilTargetInfo(_shadowMap, 1f)
            );
            
            renderPass.BindGraphicsPipeline(_shadowPipeline.Ref.Pipeline);
            foreach (var entity in _renderMeshFilter.Entities)
            {
                var renderMesh = Get<RenderMesh>(entity);
                var model = Get<TransformMatrix>(entity).Transform;
                var mesh = AssetManager.GetMesh(renderMesh.Mesh);
                var mvpUniform = new MatrixUniform(model * shadowView * shadowProj);
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
                var model = Get<TransformMatrix>(entity).Transform;
                var mesh = AssetManager.GetMesh(renderMesh.Mesh);
                var mvpUniform = new MatrixUniform(model * view * proj);
                var modelUniform = new MatrixUniform(model);
                var lightUniform = new MatrixUniform(model * shadowView * shadowProj);
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
                CompareOp = CompareOp.LessOrEqual,
            },
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CW_CullBack with { EnableDepthClip = true },
            VertexInputState = VertexInputState.CreateSingleBinding<Vertex>(),
        };
    }

    private static Matrix4x4 CreateLookAt(Transform transform)
    {
        var forward = Vector3.Transform(Vector3.UnitZ, transform.Rotation);
        var position = transform.Position;

        var eye = transform.Position;
        var target = position + forward;
        var up = Vector3.Transform(Vector3.UnitY, transform.Rotation);

        var zAxis = Vector3.Normalize(target - eye);
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
        var yAxis = Vector3.Cross(zAxis, xAxis);

        return new Matrix4x4(
            xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            -Vector3.Dot(xAxis, eye),
            -Vector3.Dot(yAxis, eye),
            -Vector3.Dot(zAxis, eye),
            1
        );
    }

    private static Vector3 GetFrustumCenter(Matrix4x4 view)
    {
        // invert the view matrix to get camera‐to‐world
        if (!Matrix4x4.Invert(view, out var invView))
            throw new InvalidOperationException("Could not invert view matrix");

        // camera position is the translation part
        var camPos = new Vector3(invView.M41, invView.M42, invView.M43);

        // camera forward in view‐space is usually -Z (or +Z depending on your convention)
        // here we assume right‐handed, so forward = (0,0,-1)
        var forward = Vector3.TransformNormal(new Vector3(0, 0, -1), invView);
        forward = Vector3.Normalize(forward);

        if (Math.Abs(forward.Y) < 1e-6f)
            throw new InvalidOperationException("Camera ray is parallel to the Y=0 plane");

        // intersect with y=0
        float t = -camPos.Y / forward.Y;
        return camPos + forward * t;
    }
}