

vec3 gammaCorrect(vec3 color, float gamma)
{
    return pow(color, vec3(1.0/gamma));
}

vec3 apply_sRGB_gamma(vec3 color)
{
    // https://en.wikipedia.org/wiki/SRGB#Specification_of_the_transformation
    bvec3 isLinear = lessThan(color, vec3(0.0031308));
    vec3 gamma = 1.055 * pow(color, vec3(1/2.4)) - 0.055;
    return mix(gamma, 12.92 * color, isLinear);
}

vec3 reinhard(vec3 color)
{
    color = color / (color + vec3(1.0));
    return color;
}

// https://github.com/TheRealMJP/BakingLab/blob/master/BakingLab/ACES.hlsl

// sRGB => XYZ => D65_2_D60 => AP1 => RRT_SAT
const mat3 ACESInputMat = mat3(
    0.59719, 0.35458, 0.04823,
    0.07600, 0.90834, 0.01566,
    0.02840, 0.13383, 0.83777
);

// ODT_SAT => XYZ => D60_2_D65 => sRGB
const mat3 ACESOutputMat = mat3(
     1.60475, -0.53108, -0.07367,
    -0.10208,  1.10813, -0.00605,
    -0.00327, -0.07276,  1.07602
);

vec3 RRTAndODTFit(vec3 v)
{
    vec3 a = v * (v + 0.0245786) - 0.000090537;
    vec3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
    return a / b;
}

vec3 ACESFitted(vec3 color)
{
    color = color * ACESInputMat;

    // Apply RRT and ODT
    color = RRTAndODTFit(color);

    color = color * ACESOutputMat;

    // Clamp to [0, 1]
    color = clamp(color, 0, 1);

    return color;
}
