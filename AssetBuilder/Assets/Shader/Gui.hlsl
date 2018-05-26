struct VS_OUTPUT {
	float4 Position : SV_POSITION;
	float2 Texcoord : TEXCOORD;
	float4 Color : COLOR;
};

cbuffer vscb {
    float4x4 MVP;
};

VS_OUTPUT VS(float2 inPos : POSITION, float4 inOff : OFFSET, float4 inTex : TEXCOORD, float4 inColor : COLOR) {
	VS_OUTPUT output;

	output.Position = mul(MVP,float4(inOff.xy + inOff.zw * inPos, 0, 1));//float4(,1,1)
	output.Texcoord = inTex.xy + inTex.zw * inPos;
	output.Color = inColor;
	return output;
}

Texture2D tex;
SamplerState samp;

float4 PS(VS_OUTPUT input) : SV_TARGET {
    return input.Color * tex.Sample(samp, input.Texcoord);
}
