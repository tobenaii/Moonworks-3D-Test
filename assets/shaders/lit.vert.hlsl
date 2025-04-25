cbuffer UniformBlock : register(b0, space1)
{
    float4x4 Transform : packoffset(c0);
};

cbuffer UniformBlock : register(b1, space1)
{
    float4x4 ModelTransform : packoffset(c0);
};

cbuffer UniformBlock : register(b2, space1)
{
    float4x4 LightTransform : packoffset(c0);
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
    float4 LightSpacePos : TEXCOORD1;
};

Output main(Input input)
{
    Output output;
    output.Position = mul(Transform, float4(input.Position, 1.0));
    output.TexCoord = input.TexCoord;
    output.Normal = normalize(mul((float3x3)ModelTransform, input.Normal));
    output.LightSpacePos = mul(LightTransform, float4(input.Position, 1.0));
    return output;
}
