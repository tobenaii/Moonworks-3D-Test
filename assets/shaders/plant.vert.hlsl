cbuffer UniformBlock : register(b0, space1)
{
    float4x4 MatrixTransform : packoffset(c0);
    float4x4 ModelTransform : packoffset(c4);
};

struct Input
{
    float3 Position : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
};

struct Output
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float3 Normal   : NORMAL;
};

Output main(Input input)
{
    Output output;
    output.Position = mul(MatrixTransform, float4(input.Position, 1.0));
    output.TexCoord = input.TexCoord;
    output.Normal = normalize(mul((float3x3)ModelTransform, input.Normal));
    return output;
}
