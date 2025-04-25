Texture2D Texture : register(t0, space2);
Texture2D<float> ShadowTexture : register(t1, space2);

SamplerState Sampler : register(s0, space2);
SamplerComparisonState ShadowSampler : register(s1, space2);

struct PSInput
{
    float2 TexCoord : TEXCOORD0;
    float3 Normal   : NORMAL;
    float4 LightSpacePos : TEXCOORD1;
};

float ShadowCalculation(float4 fragPosLightSpace)
{
    float3 ndc = fragPosLightSpace.xyz / fragPosLightSpace.w;
    float2 uv = ndc.xy * 0.5f + 0.5f;
    uv.y = 1 - uv.y;
    if (uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) 
        return 1.0f;

    float depthRef = ndc.z - 0.005f;
    // assumes you know your shadow map resolution:
    float2 texelSize = 1.0f / float2(4096, 4096);

    const int K = 2; // radius in texels
    float sum = 0;
    int count = 0;
    for (int y = -K; y <= K; ++y)
        for (int x = -K; x <= K; ++x)
        {
            float2 offset = float2(x, y) * texelSize;
            sum += ShadowTexture.SampleCmpLevelZero(
                       ShadowSampler, uv + offset, depthRef);
            ++count;
        }
    return sum / count;
}


float4 main(PSInput input) : SV_Target
{
    float1 ambient = 0.15;
    float4 texColor = Texture.Sample(Sampler, input.TexCoord);
    float3 lightDir = normalize(float3(1, -1, -1));
    float diffuseIntensity = max(0, dot(normalize(input.Normal), -lightDir));
    float shadow = ShadowCalculation(input.LightSpacePos);
    float3 lighting = (ambient + shadow * diffuseIntensity) * texColor.rgb;
    return float4(lighting, 1);
    //return float4((diffuseIntensity + 0.2) * texColor);
}