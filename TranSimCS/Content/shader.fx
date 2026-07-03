#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

//TEXTURES
Texture2D Albedo;
Texture2D Emissive;
sampler2D AlbedoSampler = sampler_state{
    Texture = <Albedo>;
};
sampler2D EmissiveSampler = sampler_state{
    Texture = <Emissive>;
};

//PARAMETERS
float4 AmbientColor = float4(1,1,1,1);
float4x4 WorldViewProjection;
float AlphaCutoff = 0.5;
//int Flags = 0;

//DATA STRUCTURES
struct VSInput{
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

//FLAGS
static const int FlagAlphaClip = 1;

VSOutput VSMain(VSInput input){
    VSOutput output;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PSMain(VSOutput input) : COLOR0{
    float4 texColor = tex2D(AlbedoSampler, input.TexCoord);
    float4 emissiveColor = tex2D(EmissiveSampler, input.TexCoord) * float4(1, 1, 1, 0);
    float4 color = (texColor * AmbientColor + emissiveColor) * input.Color;
    //if (Flags & FlagAlphaClip)
        clip(color.a - AlphaCutoff);
    return color;
}

technique Basic{
    pass Pass0{
        VertexShader = compile VS_SHADERMODEL VSMain();
        PixelShader = compile PS_SHADERMODEL PSMain();
    }
}