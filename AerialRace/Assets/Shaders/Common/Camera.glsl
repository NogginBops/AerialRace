
/*
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
*/

layout (row_major, std140) uniform u_CameraBlock
{
    vec4 position;
    float near;
    float far;
    float fov;
} u_Camera;

float NDCtoLinearDepth(float ndc_depth, float near, float far)
{
    return (2 * u_Camera.near * u_Camera.far) / (u_Camera.far + u_Camera.near - ndc_depth * (far - near));
}

float linearDepth()
{
    return NDCtoLinearDepth(gl_FragCoord.z * 2 - 1, u_Camera.near, u_Camera.far);
}

