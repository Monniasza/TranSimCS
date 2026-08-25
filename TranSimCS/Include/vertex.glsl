#version 460

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;

layout(location = 3) in vec3 instancePosition;
layout(location = 4) in vec4 instanceRotation;

uniform 