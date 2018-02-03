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
uniform bool enable_fog = true;
uniform vec4 fog_color = vec4(0.18, 0.3, 0.3, 1);

void main(){
    final_color = texture(colortexture, vec3(fs_in.uv, fs_in.texid));	//vec4(1,1,1,1)) * max(0.6, 0.6 * dot(fs_in.normal, -normalize(fs_in.position - pos)))
	if(ao) {
		final_color *= texture(aotexture, vec3(fs_in.uv, fs_in.aoid)).r;
	}
	if(enable_fog){
	    final_color = mix(final_color, fog_color, clamp((length(pos.xz - fs_in.position.xz) - 205)/30, 0, 1));
	}
}