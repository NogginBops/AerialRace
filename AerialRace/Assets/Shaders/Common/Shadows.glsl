
uniform bool UseShadows;

uniform sampler2DArrayShadow ShadowCascades;

// Split1|Split2|Split3|Unused
uniform vec4 CascadeSplits;

uniform mat4 worldToLightSpace[4];

//float LinearDepth(float ndc_depth, float near, float far)
//{
//    return 
//}

float linearStep(float value, float start, float end)
{
    if (value < start) return 0;
    if (value > end) return 1;
    return (value - start) / (end - start);
}

int ShadowCascadeFromDepth(float depth)
{
    if (depth < CascadeSplits.x)
        return 0;
    else if (depth < CascadeSplits.y)
        return 1;
    else if (depth < CascadeSplits.z)
        return 2;
    else if (depth < CascadeSplits.w)
        return 3;
    else return 4;
}

vec4 GetShadowCascadeDebugColorF(float cascade)
{
    if (cascade <= 0) return mix(vec4(1, 0, 0, 1), vec4(0, 1, 0, 1), cascade);
    else if (cascade <= 1) return mix(vec4(0, 1, 0, 1), vec4(0, 0, 1, 1), cascade - 1);
    else if (cascade <= 2) return mix(vec4(0, 0, 1, 1), vec4(0, 1, 1, 1), cascade - 2);
    else if (cascade <= 3) return mix(vec4(0, 1, 1, 1), vec4(1, 0, 1, 1), cascade - 3);
    else return vec4(1, 0, 1, 1);
}

vec4 GetShadowCascadeDebugColor(int cascade)
{
    switch(cascade)
    {
        case 0: return vec4(1, 0, 0, 1);
        case 1: return vec4(0, 1, 0, 1);
        case 2: return vec4(0, 0, 1, 1);
        case 3: return vec4(0, 1, 1, 1);
        default: return vec4(1, 0, 1, 1);
    }
}

float ShadowCalculation(vec3 worldPos, float depth, vec3 normal, vec3 lightDir)
{
    if (!UseShadows) return 0;

    int level = ShadowCascadeFromDepth(depth);

    vec4 fragPosLightSpace = vec4(worldPos, 1) * worldToLightSpace[level];

    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;

    float bias = max(0.005 * (1.0 - abs(dot(normal, lightDir))), 0.0005f);
    //bias = biasAmount;
    //bias = 0.03 * (1.0 - abs(dot(normal, lightDir)));
    //bias = biasAmount;
    //bias = biasAmount;

    // FIXME: There might be a better way to do this!...
    // TODO: When we have proper CSM we don't need this anymore as all fragments are going to be
    // inside atleast one shadow map
    //if (min(min(projCoords.x, projCoords.y), 1 - max(projCoords.x, projCoords.y)) < 0) return 0;

    float pcf1 = texture(ShadowCascades, vec4(projCoords.xy, level, currentDepth - bias));
    return pcf1;
}
