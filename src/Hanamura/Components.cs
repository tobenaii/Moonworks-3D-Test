using System.Numerics;

namespace Hanamura;

public record struct Transform(Vector3 Position, Quaternion Rotation);
public record struct TransformMatrix(Matrix4x4 Transform);
public record struct CameraData(float Pitch, float Yaw, Vector3 TargetPosition, float TargetDistance);
public record struct CameraMatrix(Matrix4x4 View);
public record struct RenderMesh(ulong Mesh);