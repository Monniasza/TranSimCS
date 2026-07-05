#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

//Algorithms
float3 Rotate(float3 v, float4 q){
    float3 t = 2.0 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

//Global

//Per material

//Per mesh

//Per instance

//Per vertex

//TEXTURES
Texture2D Albedo;
Texture2D Emissive;
SamplerState PointWrap;
sampler2D AlbedoSampler = sampler_state{
    Texture = <Albedo>;
    AddressU = Wrap;
    AddressV = Wrap;
    Filter = Point;
};
sampler2D EmissiveSampler = sampler_state{
    Texture = <Emissive>;
    AddressU = Wrap;
    AddressV = Wrap;
    Filter = Point;
};

//PARAMETERS
float4 AmbientColor = float4(1,1,1,1);
float4x4 WorldViewProjection;
float AlphaCutoff = 0.5;
float EmissiveIsMask = 0;

//DATA STRUCTURES
struct VSInput{
    //Per-vertex
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VSInstance{
    // Per-instance
    float3 Position : BLENDWEIGHT0;
    float4 Rotation : BLENDWEIGHT1;
};

struct VSOutput{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSMain(VSInput input, VSInstance instance){
    VSOutput output;
    float3 worldPos = Rotate(input.Position.xyz, instance.Rotation) + instance.Position;
    output.Position = mul(float4(worldPos, 1), WorldViewProjection);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PSMain(VSOutput input) : COLOR0{
    float4 texColor = tex2D(AlbedoSampler, input.TexCoord) * float4(1, 1, 1, 1-EmissiveIsMask);
    float4 emissiveColor = tex2D(EmissiveSampler, input.TexCoord) * float4(1, 1, 1, EmissiveIsMask);
    float4 color = (texColor * AmbientColor + emissiveColor) * input.Color;
    clip(color.a - AlphaCutoff);
    return color;
}

technique Basic{
    pass Pass0{
        VertexShader = compile VS_SHADERMODEL VSMain();
        PixelShader = compile PS_SHADERMODEL PSMain();
    }
}