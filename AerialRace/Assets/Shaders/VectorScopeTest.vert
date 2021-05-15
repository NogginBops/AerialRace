#version 450 core

layout (location = 0) in vec4 in_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec4 sceneColor;
};

vec3 RGBtoYUV(vec3 rgb)
{
	float Y =  .299 * rgb.x + .587 * rgb.y + .114 * rgb.z; // Luma
	float U = -.147 * rgb.x - .289 * rgb.y + .436 * rgb.z; // Delta Blue
	float V =  .615 * rgb.x - .515 * rgb.y - .100 * rgb.z; // Delta Red
    return vec3(Y,U,V);
}

//RGB to HSV.
//Source: https://gist.github.com/yiwenl/745bfea7f04c456e0101
vec3 rgb2hsv(vec3 c) {
	float cMax=max(max(c.r,c.g),c.b),
	      cMin=min(min(c.r,c.g),c.b),
	      delta=cMax-cMin;
	vec3 hsv=vec3(0.,0.,cMax);
	if(cMax>cMin){
		hsv.y=delta/cMax;
		if(c.r==cMax){
			hsv.x=(c.g-c.b)/delta;
		}else if(c.g==cMax){
			hsv.x=2.+(c.b-c.r)/delta;
		}else{
			hsv.x=4.+(c.r-c.g)/delta;
		}
		hsv.x=fract(hsv.x/6.);
	}
	return hsv;
}

vec3 rgb_yuv (vec3 rgb, vec2 wbwr, vec2 uvmax) {
	float y = wbwr.y*rgb.r + (1.0 - wbwr.x - wbwr.y)*rgb.g + wbwr.x*rgb.b;
    return vec3(y, uvmax * (rgb.br - y) / (1.0 - wbwr));
}

vec2 angleLengthToVec(float angle, float length)
{
    return vec2(cos(angle) * length, sin(angle) * length);
}

void main(void)
{
    vec3 hsv = rgb2hsv(in_color.rgb);
    vec2 pos = angleLengthToVec(hsv.x * 2 * 3.14, hsv.z);
    gl_Position = vec4(pos * 0.5, 0, 1);
    sceneColor = in_color;
}