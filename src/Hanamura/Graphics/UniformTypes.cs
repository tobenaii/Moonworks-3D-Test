using System.Numerics;

namespace Hanamura.Graphics;

public struct MatrixUniform(Matrix4x4 matrix)
{
    public Matrix4x4 Matrix4 = matrix;
}