struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float2 Texcoord : TEXCOORD;
};

struct CharRenderData {
    float2 TexMin;
    float2 TexSize;
    float2 Size;
    float2 Unused;
};

cbuffer vscb {
    float4x4 MVP;
};

StructuredBuffer<CharRenderData> charrenderdata;

VS_OUTPUT VS(float4 inPos : POSITION, float2 inTex : TEXCOORD, float2 inOff : OFFSET, float scale : SCALE, float4 inColor : COLOR, int inId : CHARID) {
	VS_OUTPUT output;

    CharRenderData crd = charrenderdata[inId];

	output.Position = mul(MVP,float4(inOff + inPos * crd.Size * scale,1,1));
	output.Texcoord = crd.TexMin + inTex * crd.TexSize;
	return output;
}

Texture2D tex;
SamplerState samp;

float4 PS(VS_OUTPUT input) : SV_TARGET {
	float a = tex.Sample(samp, input.Texcoord).a;
	return float4(float3(1,1,1),a);
}
