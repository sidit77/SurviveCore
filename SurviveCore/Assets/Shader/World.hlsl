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

Texture2DArray aotexture;
SamplerState aosampler;

Texture2DArray colortexture;
SamplerState colorsampler;

float4 PS(VS_OUTPUT input) : SV_TARGET {
    float3 color = colortexture.Sample(colorsampler, float3(input.Texcoord, input.TexID)).rgb;
	float ao = aotexture.Sample(aosampler, float3(input.Texcoord, input.AOCase)).a * 0.2 + 0.8;
	return float4(color * ao,1);
}
