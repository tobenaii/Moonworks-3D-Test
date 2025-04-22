using System.Numerics;

namespace Hanamura.Types;

public struct TransformVertexUniform(Matrix4x4 modelViewProjection, Matrix4x4 model)
{
    public Matrix4x4 ModelViewProjection = modelViewProjection;
    public Matrix4x4 Model = model;
}