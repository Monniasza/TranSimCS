#version 460

layout(location = 0) in vec4 psColor;
layout(location = 1) in vec2 psTexCoord;

layout(std140, binding = 0) uniform WorldViewProjection{
    uniform mat4 matrix;
	vec4 ambientColor;
    float alphaCutoff;
    float emissiveIsMask;
    float x0;
    float x1;
} world;
layout(binding=0) uniform sampler2D albedo;
layout(binding=1) uniform sampler2D emissive;

layout(location = 0) out vec4 fragColor;

void main(){
    vec4 texMask = vec4(1, 1, 1, 1 - world.emissiveIsMask);
    vec4 emissiveMask = vec4(1, 1, 1, world.emissiveIsMask);
    vec4 texColor = texture(albedo, psTexCoord) * texMask;
    vec4 emissiveColor = texture(emissive, psTexCoord) * emissiveMask;
    vec4 color = (texColor * world.ambientColor + emissiveColor) * psColor;
    if(color.a < world.alphaCutoff) discard;
    fragColor = color;
}