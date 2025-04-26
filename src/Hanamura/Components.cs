using System.Numerics;

namespace Hanamura;

//INPUT ACTIONS
public record struct MoveAction(Vector2 Direction);

//TRANSFORM
public record struct Transform(Vector3 Position, Quaternion Rotation)
{
    public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);
    
    public Transform() : this(Vector3.Zero, Quaternion.Identity)
    {
    }

    public Transform(Vector3 position) : this(position, Quaternion.Identity)
    {
    }
}
public record struct TransformMatrix(Matrix4x4 Transform);

//ANIMATION
public record struct TrackTarget(float Distance, float Pitch = 0, float Yaw = 0, float Weight = 0.1f);

//CAMERA
public record struct MainCamera();

//PLAYER
public record struct Player();

//RENDERING
public record struct RenderMesh(ulong Mesh);
public record struct ShouldCull();
public record struct CulledMesh();

//RELATIONS
public record struct Tracks();