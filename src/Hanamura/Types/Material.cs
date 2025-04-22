using MoonWorks.Graphics;

namespace Hanamura.Types;

public class Material : IDisposable
{
    public required GraphicsPipeline Pipeline;
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Pipeline.Dispose();
    }
}