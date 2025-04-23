using MoonWorks.Graphics;

namespace Hanamura.AssetManagement;

public abstract class AssetRef<T>(T asset) where T : IDisposable
{
    public T Ref { get; private set; } = asset;

    public void Update(T asset)
    {
        Ref.Dispose();
        Ref = asset;
    }

    public static implicit operator T(AssetRef<T> asset) => asset.Ref;
}

public class TextureRef(Texture asset) : AssetRef<Texture>(asset);
public class ShaderRef(Shader asset) : AssetRef<Shader>(asset);
public class MeshRef(Mesh asset) : AssetRef<Mesh>(asset);
public class MaterialRef(Material asset, GraphicsPipelineCreateInfo createInfo) : AssetRef<Material>(asset)
{
    public GraphicsPipelineCreateInfo CreateInfo { get; set; } = createInfo;
}