float4x4 WorldViewProj;
float3x3 WorldIT;
float4x4 World;
float3 SunDir;
float toneMappingStrength=1.0;
texture2D DiffuseMap;
sampler DiffuseMapSampler = sampler_state
{
	Texture = <DiffuseMap>;
};
struct VSO // Vertex shader output = pixel shader input
{
	float4 pos: POSITION;
	float2 tex: TEXCOORD0;
	float3 normal: NORMAL;
	float3 worldPos: TEXCOORD1;
};
VSO VS(float4 inPos : POSITION, float3 normal : NORMAL, float2 tex : TEXCOORD)
{
	VSO ret;
	ret.tex = inPos.xz;
	ret.pos = mul(inPos, WorldViewProj);
	ret.normal = normalize(mul(normal, WorldIT));
	ret.worldPos = mul(inPos, World).xyz;
	return ret;
}

float aces(float x) {
  const float a = 2.51;
  const float b = 0.03;
  const float c = 2.43;
  const float d = 0.59;
  const float e = 0.14;
  return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

float4 PS(VSO vso) : COLOR
{
	clip(vso.worldPos.y);
	float Ld=dot(vso.normal, SunDir) + dot(vso.normal, normalize(float3(-.2, 1, -.8)));
	Ld=aces(Ld);
	float4 diffuse = tex2D(DiffuseMapSampler, vso.tex) * Ld;
	return float4(diffuse.rgb, 1);
}

float4 PS_Height(VSO vso) : COLOR
{
	return float4( vso.worldPos.y, 0, 0, 0 );
}

float4 PS_Refraction(VSO vso) : COLOR
{
	float Ld=dot(vso.normal, SunDir) + dot(vso.normal, normalize(float3(-.2, 1, -.8)));
	Ld=aces(Ld);
	float4 diffuse = tex2D(DiffuseMapSampler, vso.tex) * Ld;
	return float4(diffuse.rgb, 1);
}

technique Island
{
	pass P0
	{
		VertexShader = compile vs_4_0_level_9_3 VS();
		PixelShader = compile ps_4_0_level_9_3 PS();
	}
	pass P1
	{
		VertexShader = compile vs_4_0_level_9_3 VS();
		PixelShader = compile ps_4_0_level_9_3 PS_Height();
	}
	pass P2
	{
		VertexShader = compile vs_4_0_level_9_3 VS();
		PixelShader = compile ps_4_0_level_9_3 PS_Refraction();
	}
}