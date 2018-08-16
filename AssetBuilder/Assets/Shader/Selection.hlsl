struct VsInput {
	float3 Position : POSITION;
};

struct PsInput {
	float4 Position : SV_POSITION;
	float2 Uv : UV;
};

cbuffer gscb {
    float4x4 VP;
};

PsInput VS(VsInput input, uint VertexID : SV_VertexID) {
    PsInput v = (PsInput)0;
    v.Position = mul(VP, float4(input.Position,1));
    v.Uv = float2(VertexID % 2, VertexID / 2);
	return v;
}

float4 PS(PsInput input) : SV_TARGET {
    float2 cuv = input.Uv * 2 - 1;
    float width = 1 - (0.01 / (input.Position.z / input.Position.w));
    if(abs(cuv.x) < width && abs(cuv.y) < width)
        discard;  
    return float4(0,0,0,1);
}