
struct Camera
{
    // FIXME: Maybe put stuff like
    // view and projection matrices in here?
    vec3 position;
    float near;
    float far;
    float fov;
};

uniform Camera camera;

float NDCtoLinearDepth(float ndc_depth, float near, float far)
{
    return (2 * near * far) / (far + near - ndc_depth * (far - near));
}

float linearDepth()
{
    return NDCtoLinearDepth(gl_FragCoord.z * 2 - 1, camera.near, camera.far);
}

