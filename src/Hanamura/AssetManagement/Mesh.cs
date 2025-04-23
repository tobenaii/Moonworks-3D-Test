using MoonWorks.Graphics;
using Buffer = MoonWorks.Graphics.Buffer;

namespace Hanamura.AssetManagement;

public class Mesh : IDisposable
{
    public readonly BufferBinding VertexBufferBinding;
    public readonly BufferBinding IndexBufferBinding;
    
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;

    public uint IndexSize => _indexBuffer.Size;

    public Mesh(Buffer vertexBuffer, Buffer indexBuffer)
    {
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;

        VertexBufferBinding = new BufferBinding(_vertexBuffer.Handle);
        IndexBufferBinding = new BufferBinding(_indexBuffer.Handle);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }
}