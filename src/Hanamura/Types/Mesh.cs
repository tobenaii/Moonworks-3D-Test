using Buffer = MoonWorks.Graphics.Buffer;

namespace Hanamura.Types;

public class Mesh : IDisposable
{
    public required Buffer VertexBuffer;
    public required Buffer IndexBuffer;
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}