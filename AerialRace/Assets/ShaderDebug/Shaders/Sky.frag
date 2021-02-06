#version 450 core
#line 0 1

uniform struct Sky {
    vec3 SunDirection;
    vec3 SunColor;
    vec3 SkyColor;
    vec3 GroundColor;
} sky;

vec3 skyColor(vec3 direction)
{
    vec3 sun = sky.SunColor * pow(max(dot(direction, sky.SunDirection), 0f), 200);
    float directionDot = dot(direction, vec3(0,1,0));
    const float margin = 0.005f;
    float groundMask = smoothstep(-margin, margin, directionDot);
    float skyGradient = max(1-(directionDot - 0.3f), 0);
    vec3 skyColor = sky.SkyColor * groundMask * skyGradient + (sky.GroundColor * (1-groundMask));
    return skyColor + sun;
}



#line 3 0


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
    vec4 unproj = vec4(pos.xyz, 1f) * invViewProj;
    return unproj.xyz / unproj.w;
}

void main(void)
{
    vec3 near = unproject(vec3(uv, 1));
    vec3 far = unproject(vec3(uv, -1));
    vec3 viewDir = normalize(far - near);
    // FIXME: Figure out why we have to do this!
    viewDir.xy *= -1f;

/*
    vec3 sun = SunColor * pow(max(dot(viewDir, SunDirection), 0f), 200);
    float viewDirDot = dot(viewDir /*- vec3(0,1,0)/, vec3(0,1,0));
    const float margin = 0.005f;
    float groundMask = smoothstep(-margin, margin, viewDirDot);
    float skyGradient = max(1-(viewDirDot - 0.3f), 0);
    //float s = groundMask * skyGradient;
    vec3 sky = SkyColor * groundMask * skyGradient + (GroundColor * (1-groundMask));*/
    
    Color = vec4(skyColor(viewDir), 1);
}