struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float3 WorldPos : POSITION;
	float2 Texcoord : TEXCOORD;
	int    AOCase   : AOCASE;
};

cbuffer cbPerObject {
    float4x4 MVP;
};

VS_OUTPUT VS(float4 inPos : POSITION, float2 inTex : TEXCOORD, int inAO : AOCASE) {
	VS_OUTPUT output;

	output.Position =  mul(MVP, inPos);
	output.Texcoord = inTex;
	output.WorldPos = inPos.xyz;
	output.AOCase   = inAO;
	return output;
}

Texture2DArray aotexture;
SamplerState aosampler;

float4 PS(VS_OUTPUT input) : SV_TARGET {
	return float4(1,1,1,1) * aotexture.Sample(aosampler, float3(input.Texcoord, input.AOCase)).a;//aotexture.Sample(aosampler, input.Texcoord);//
}