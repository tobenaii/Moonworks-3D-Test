using System.Numerics;
using System.Runtime.InteropServices;
using glTFLoader;
using glTFLoader.Schema;
using JetBrains.Annotations;

namespace Hanamura.AssetPipeline;

public static class GLTFLoader
{
    [UsedImplicitly]
    public record MeshData(
        [UsedImplicitly] string Name,
        Vector3 Position,
        Quaternion Rotation,
        Vector3 Scale,
        VertexData[] Vertices,
        uint[] Indices,
        int[] Children);

    [UsedImplicitly]
    public record struct VertexData(Vector3 Position, Vector3 Normal, Vector2 TexCoord);
    
    public static MeshData[] Load(string gltfPath)
    {
        var gltf = Interface.LoadModel(gltfPath);
        var meshes = new MeshData[gltf.Meshes.Length];
        foreach (var node in gltf.Nodes)
        {
            if (!node.Mesh.HasValue) continue;
            var meshIndex = node.Mesh.Value;
            var positionsBuffer = GetBuffer(gltf, gltfPath, meshIndex, "POSITION");
            var normalsBuffer = GetBuffer(gltf, gltfPath, meshIndex, "NORMAL");
            var uvsBuffer = GetBuffer(gltf, gltfPath, meshIndex, "TEXCOORD_0");
            var indicesBuffer = GetBuffer(gltf, gltfPath, meshIndex, "INDICES");
            
            var positions = MemoryMarshal.Cast<byte, Vector3>(positionsBuffer);
            var normals = MemoryMarshal.Cast<byte, Vector3>(normalsBuffer);
            var uvs = MemoryMarshal.Cast<byte, Vector2>(uvsBuffer);
            var indices = MemoryMarshal.Cast<byte, ushort>(indicesBuffer);
            var uIndices = new uint[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                uIndices[i] = indices[i];
            }
            
            var vertexData = new VertexData[positions.Length];

            for (var i = 0; i < vertexData.Length; i++)
            {
                vertexData[i] = new VertexData(positions[i], normals[i], uvs[i]);
            }

            var position = new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
            var rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            var scale = new Vector3(node.Scale[0], node.Scale[1], node.Scale[2]);
                
            meshes[meshIndex] = new MeshData(gltf.Meshes[meshIndex].Name, position, rotation, scale, vertexData, uIndices, node.Children);
        }

        return meshes;
    }

    private static Span<byte> GetBuffer(Gltf gltf, string gltfFilePath, int index, string attribute)
    {
        var result = new List<byte>();
        var mesh = gltf.Meshes[index];
        foreach (var primitive in mesh.Primitives)
        {
            var positionAccessorIndex = attribute == "INDICES"
                ? primitive.Indices!.Value
                : primitive.Attributes[attribute];
            var accessor = gltf.Accessors[positionAccessorIndex];

            var bufferView = gltf.BufferViews[accessor.BufferView!.Value];
            var bufferData = gltf.LoadBinaryBuffer(bufferView.Buffer, gltfFilePath);
            var startPosition = bufferView.ByteOffset + accessor.ByteOffset;
            var componentSize = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.UNSIGNED_BYTE => 1,
                Accessor.ComponentTypeEnum.BYTE => 1,
                Accessor.ComponentTypeEnum.UNSIGNED_SHORT => 2,
                Accessor.ComponentTypeEnum.SHORT => 2,
                Accessor.ComponentTypeEnum.UNSIGNED_INT => 4,
                Accessor.ComponentTypeEnum.FLOAT => 4,
                _ => throw new FileLoadException("Component type not supported")
            };
            var numComponents = accessor.Type switch
            {
                Accessor.TypeEnum.SCALAR => 1,
                Accessor.TypeEnum.VEC2 => 2,
                Accessor.TypeEnum.VEC3 => 3,
                Accessor.TypeEnum.VEC4 => 4,
                Accessor.TypeEnum.MAT2 => 4,
                Accessor.TypeEnum.MAT3 => 9,
                Accessor.TypeEnum.MAT4 => 16,
                _ => throw new FileLoadException("Component type not supported")
            };

            var totalBytes = accessor.Count * componentSize * numComponents;
            result.AddRange(bufferData.AsSpan(startPosition, totalBytes));
        }
        return result.ToArray();
    }
}