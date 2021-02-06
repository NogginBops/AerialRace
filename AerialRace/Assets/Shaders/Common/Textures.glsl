
const mat3 Linear_sRGB_to_ACEScg = mat3(
    0.4329260731344912, 0.3753918670512633, 0.189346267143399,
    0.08941221865742209, 0.8165493513876458, 0.10300469858914094,
    0.019161516239216973, 0.11815442902213172, 0.9420587449115669);

vec4 texture_ACES(sampler2D tex, vec2 texCoord)
{
    vec4 sRGB = texture(tex, texCoord);

    vec3 ACEScg = sRGB.rgb * Linear_sRGB_to_ACEScg;
    ACEScg = sRGB.rgb;

    return vec4(ACEScg, sRGB.a);
}
