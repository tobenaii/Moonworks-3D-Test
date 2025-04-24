using System.Numerics;

namespace Hanamura;

//INPUT ACTIONS
public record struct MoveAction(Vector2 Direction);

//TRANSFORM
public record struct Transform(Vector3 Position, Quaternion Rotation);
public record struct TransformMatrix(Matrix4x4 Transform);

//ANIMATION
public record struct FollowTarget(Vector3 Offset = new(), float Weight = 0.1f);

//CAMERA
public record struct MainCamera();
public record struct CameraMatrix(Matrix4x4 View);

//PLAYER
public record struct Player();

//RENDERING
public record struct RenderMesh(ulong Mesh);

//RELATIONS
public record struct Follows();