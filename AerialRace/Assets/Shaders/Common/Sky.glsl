
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



