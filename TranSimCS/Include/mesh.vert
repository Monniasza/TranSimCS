#version 460

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;
layout(location = 3) in uint albedoID;
layout(location = 4) in uint emissiveID;

layout(location = 5) in vec3 instancePosition;
layout(location = 6) in vec4 instanceRotation;

layout(std140, binding = 0) uniform WorldViewProjection{
    uniform mat4 matrix;
	vec4 ambientColor;
    float alphaCutoff;
    float emissiveIsMask;
    float x0;
    float x1;
} world;
layout(location = 0) out vec4 psColor;
layout(location = 1) out vec2 psTexCoord;

vec3 Rotate(vec3 v, vec4 q){
    vec3 t = 2.0 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

void main(){
	vec3 worldPosition = instancePosition + Rotate(position, instanceRotation);
	
	psColor = color;
	psTexCoord = texCoord;
	gl_Position = world.matrix * vec4(worldPosition, 1);
}