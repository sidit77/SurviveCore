#version 450 core

out vec4 final_color;

layout(binding = 0) uniform sampler2DArray colortexture;
layout(binding = 1) uniform sampler2DArray aotexture;

in VS_OUT {
	vec3 normal;
	vec3 position;
	vec2 uv;
	flat int texid;
	flat int aoid;
} fs_in;

uniform vec3 pos;
uniform bool ao;

void main(){
    final_color = texture(colortexture, vec3(fs_in.uv, fs_in.texid));	//vec4(1,1,1,1)) * max(0.6, 0.6 * dot(fs_in.normal, -normalize(fs_in.position - pos)))
	if(ao) {
		final_color *= texture(aotexture, vec3(fs_in.uv, fs_in.aoid)).r;
	}
	final_color = vec4(1,1,1,1) - final_color;
}