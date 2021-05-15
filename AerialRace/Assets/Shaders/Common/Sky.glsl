
vec3 hemisphere[20] = vec3[](
    vec3(-0.60025203, 0.7780613, -0.18525171),
    vec3(0.110092424, 0.05577207, 0.99235535),
    vec3(0.16228415, 0.7666502, 0.6212176),
    vec3(-0.38165325, 0.9082114, -0.17173469),
    vec3(-0.67863846, 0.3436517, 0.64911735),
    vec3(0.91770756, 0.16868448, -0.35966432),
    vec3(-0.3086987, 0.5930074, -0.74367154),
    vec3(0.21701019, 0.7569682, 0.61636496),
    vec3(0.56069416, 0.4484667, 0.6960601),
    vec3(0.1529028, 0.5414444, -0.8267156),
    vec3(0.13027637, 0.9510324, 0.28029537),
    vec3(0.4550217, 0.11299757, -0.8832818),
    vec3(-0.49450982, 0.86518675, 0.08313805),
    vec3(0.013331805, 0.469217, 0.88298225),
    vec3(-0.49183145, 0.051090028, 0.8691902),
    vec3(-0.82749003, 0.5338597, 0.1739369),
    vec3(-0.10355356, 0.93954515, 0.32639182),
    vec3(0.9296493, 0.090727635, 0.35710037),
    vec3(-0.7980036, 0.056627035, -0.5999863),
    vec3(-0.23320675, 0.9721212, 0.024393916)
);

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

vec3 skyIrradiance(mat3 tangentToWorld)
{
    //return vec3(0);
    //return skyColor(tangentToWorld[2]);

    const int SAMPLES = 20;

    vec3 sum = vec3(0);
    for (int i = 0; i < SAMPLES; i++)
    {
        vec3 t = hemisphere[i];
        // tangentToWorld is column major 
        // so this is the correct multiplication order
        sum += skyColor(normalize(tangentToWorld * vec3(t.x,t.z,t.y)));
    }
    return sum / SAMPLES;
}

