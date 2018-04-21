struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float3 WorldPos : POSITION;
	float2 Texcoord : TEXCOORD;
	int    AOCase   : AOCASE;
	int    TexID    : TEXID;
};

cbuffer cbPerObject {
    float4x4 MVP;
};

VS_OUTPUT VS(float4 inPos : POSITION, float2 inTex : TEXCOORD, int inAO : AOCASE, int inTid : TEXID) {
	VS_OUTPUT output;

	output.Position =  mul(MVP, inPos);
	output.Texcoord = inTex;
	output.WorldPos = inPos.xyz;
	output.AOCase   = inAO;
	output.TexID    = inTid;
	return output;
}

Texture2DArray aotexture : register(t0);
SamplerState aosampler : register(s0);

Texture2DArray colortexture : register(t1);
SamplerState colorsampler : register(s1);

cbuffer cbPerFrame{
    float4 fogcolor;
    float3 pos;
    int enabled;
}

float4 PS(VS_OUTPUT input) : SV_TARGET {
    float4 color = colortexture.Sample(colorsampler, float3(input.Texcoord, input.TexID)).rgba;

    float ao = aotexture.Sample(aosampler, float3(input.Texcoord, input.AOCase)).a * 0.2 + 0.8;
   
    [flatten] if((enabled & 1) != 0){
	   color.rgb *= ao;
	}
	[flatten] if((enabled & 2) != 0){
        color = lerp(color,fogcolor, clamp((length(pos.xz - input.WorldPos.xz) - 215)/30, 0, 1));
    }
	
	return color;
}
