#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

//TEXTURES
Texture2D TerrainData; //Height for now. More features coming
Texture2D Albedo; //The actual terrain texture
SamplerState PointWrap;
sampler2D TerrainSampler = sampler_state{
    Texture = <TerrainData>;
    AddressU = Clamp;
    AddressV = Clamp;
    Filter = Point;
};
sampler2D AlbedoSampler = sampler_state
{
    Texture = <Albedo>;
    AddressU = Wrap;
    AddressV = Wrap;
    Filter = Point;
};

//PARAMETERS
float4 AmbientColor = float4(1,1,1,1);
float2 HeightRange = float2(-1024, 3072);
float4x4 WorldViewProjection;

//DATA STRUCTURES
struct VSInput{
    //Per-vertex
    float2 Position : POSITION0;
};

struct VSInstance{
    // Per-instance
    float4 PositionRange : BLENDWEIGHT0;
    float4 TextureRange : BLENDWEIGHT1;
};

struct VSOutput{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSMain(VSInput input, VSInstance instance){
    VSOutput output;
    
    float2 minXZ = instance.PositionRange.xy;
    float2 maxXZ = instance.PositionRange.zw;
    float2 minUV = instance.TextureRange.xy;
    float2 maxUV = instance.TextureRange.zw;
    
    float2 ones = float2(1, 1);
    float2 xz = lerp(minXZ, maxXZ, input.Position);
    float2 uv = lerp(minUV, maxUV, input.Position);
    
    float4 uv0l = float4(uv, 0, 0);
    
    //float4 terrainSample = tex2Dlod(TerrainSampler, uv0l);
    float4 terrainSample = float4(0, 0, 0, 0);
    
    float4 worldPosition = float4(xz.x, lerp(HeightRange.x, HeightRange.y, terrainSample.x), xz.y, 1);
    
    output.TexCoord = xz;
    output.Position = mul(worldPosition, WorldViewProjection);
    return output;
}

float4 PSMain(VSOutput input) : COLOR0{
    float4 texColor = tex2D(AlbedoSampler, input.TexCoord);
    float4 color = texColor * AmbientColor;
    return color;
}

technique Basic{
    pass Pass0{
        VertexShader = compile VS_SHADERMODEL VSMain();
        PixelShader = compile PS_SHADERMODEL PSMain();
    }
}