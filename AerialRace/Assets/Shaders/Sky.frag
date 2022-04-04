#version 450 core

#include <Common/Sky.glsl>

in VertexOutput
{
    vec2 uv;
};

out vec4 Color;

uniform vec3 ViewPos;

uniform mat4 invProj;
uniform mat4 invView;
uniform mat4 invViewProj;

vec3 unproject(vec3 pos)
{
    vec4 unproj = vec4(pos.xyz, 1) * invViewProj;
    return unproj.xyz / unproj.w;
}

void main(void)
{
    vec3 near = unproject(vec3(uv, 1));
    vec3 far = unproject(vec3(uv, -1));
    //vec3 viewDir = normalize(far - near);
    vec3 viewDir = normalize(near - far);
    // FIXME: Figure out why we have to do this!
    //viewDir.xy *= -1;
    //viewDir.y *= -1;

/*
    vec3 sun = SunColor * pow(max(dot(viewDir, SunDirection), 0), 200);
    float viewDirDot = dot(viewDir /*- vec3(0,1,0)/, vec3(0,1,0));
    const float margin = 0.005;
    float groundMask = smoothstep(-margin, margin, viewDirDot);
    float skyGradient = max(1-(viewDirDot - 0.3), 0);
    //float s = groundMask * skyGradient;
    vec3 sky = SkyColor * groundMask * skyGradient + (GroundColor * (1-groundMask));*/
    
    Color = vec4(skyColor(viewDir), 1);
    //Color = vec4(uv, 0, 1);
}