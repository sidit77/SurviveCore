struct VsInput {
	float3 Position : POSITION;
	float  Radius   : RADIUS;
	float4 Color    : COLOR;
};

struct PsInput {
	float4 Position : SV_POSITION;
	float4 Color    : COLOR;
};

cbuffer gscb {
    float4x4 VP;
    float3 Right;
    float3 Up;
};

VsInput VS(VsInput input) {
	return input;
}

[maxvertexcount(4)] 
void GS(point VsInput vertex[1], inout TriangleStream<PsInput> stream) {
	float rad = vertex[0].Radius;
	PsInput v = (PsInput)0;
	v.Color = vertex[0].Color;
	for (int i = 0; i < 4; i++) {
		v.Position = mul(VP,float4(vertex[0].Position + Right * rad * (i % 2 * 2 - 1) + Up * rad * (i / 2 * 2 - 1), 1));
		stream.Append(v);
	}
}

float4 PS(PsInput input) : SV_TARGET {
    return input.Color;
}