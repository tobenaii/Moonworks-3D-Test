using MoonWorks.Graphics;

namespace Hanamura.AssetManagement;

public class Material : IDisposable
{
    public required GraphicsPipeline Pipeline;
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Pipeline.Dispose();
    }
}