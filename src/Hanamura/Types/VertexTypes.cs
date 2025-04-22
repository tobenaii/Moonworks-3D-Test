using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;

namespace Hanamura.Types;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex(Vector3 position, Vector3 normal, Vector2 texCoord) : IVertexType
{
    public Vector3 Position = position;
    public Vector3 Normal = normal;
    public Vector2 TexCoord = texCoord;

    public static VertexElementFormat[] Formats { get; } =
    [
        VertexElementFormat.Float3,
        VertexElementFormat.Float3,
        VertexElementFormat.Float2,
    ];

    public static uint[] Offsets { get; } =
    [
        0,
        12,
        24,
    ];

    public override string ToString()
    {
        return Position + " | " + Normal + " | " + TexCoord;
    }
}