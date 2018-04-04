struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float2 Texcoord : TEXCOORD;
	float4 Color : COLOR;
};

struct CharRenderData {
    float2 TexMin;
    float2 TexSize;
    float2 Size;
};

cbuffer vscb {
    float4x4 MVP;
    float4 color;
    float2 offset;
};

StructuredBuffer<CharRenderData> charrenderdata;

VS_OUTPUT VS(float2 inPos : POSITION, float2 inOff : OFFSET, float scale : SCALE, int inId : CHARID) {
	VS_OUTPUT output;

    CharRenderData crd = charrenderdata[inId];

	output.Position = mul(MVP,float4(offset + inOff + inPos * crd.Size * scale,1,1));
	output.Texcoord = crd.TexMin + inPos * crd.TexSize;
	output.Color = color;
	return output;
}

Texture2D tex;
SamplerState samp;

float4 PS(VS_OUTPUT input) : SV_TARGET {
    float4 final_color = input.Color;
	final_color.a *= tex.Sample(samp, input.Texcoord).a;
	return final_color;
}
