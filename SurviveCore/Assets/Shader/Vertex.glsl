#version 450 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 texcoord;
layout(location = 2) in float normal;
layout(location = 3) in float texid;
layout(location = 4) in float aoid;

uniform mat4 mvp;

out VS_OUT {
	vec3 normal;
	vec3 position;
	vec2 uv;
	flat int texid;
	flat int aoid;
} vs_out;

const vec3[6] ns = vec3[6](vec3(1,0,0),vec3(0,1,0),vec3(0,0,1),vec3(-1,0,0),vec3(0,-1,0),vec3(0,0,-1));

void main(){
	vs_out.normal = ns[int(normal * 256)];
	vs_out.position = position;
	vs_out.uv = texcoord;
	vs_out.texid = int(texid * 256);
	vs_out.aoid = int(aoid * 256);
    gl_Position = mvp * vec4(position, 1);
}