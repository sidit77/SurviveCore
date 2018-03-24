struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float2 Texcoord : TEXCOORD;
};

cbuffer vscb {
    float4x4 MVP;
};

VS_OUTPUT VS(float4 inPos : POSITION, float2 inTex : TEXCOORD) {
	VS_OUTPUT output;

	output.Position =  mul(MVP,inPos);
	output.Texcoord = inTex;
	return output;
}

Texture2D tex;
SamplerState samp;

float4 PS(VS_OUTPUT input) : SV_TARGET {
	float a = tex.Sample(samp, input.Texcoord).a;
	return float4(float3(1,1,1),a);
}
