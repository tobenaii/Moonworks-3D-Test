Texture2D Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

struct PSInput
{
    float2 TexCoord : TEXCOORD0;
    float3 Normal   : NORMAL;
};

float4 main(PSInput input) : SV_Target
{
    float4 texColor = Texture.Sample(Sampler, input.TexCoord);
    float3 lightDir = normalize(float3(1, -1, -1));
    float diffuseIntensity = max(0, dot(normalize(input.Normal), -lightDir));
    return float4((diffuseIntensity + 0.2) * texColor);
}