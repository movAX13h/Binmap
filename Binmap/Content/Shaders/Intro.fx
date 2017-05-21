#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_3
	#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float Time;
float2 Size;
Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};


float rand(float x) { return frac(sin(x) * 4358.5453123); }
float rand(float2 co) { return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5357); }

float invader(float2 p, float n)
{
	p.x = abs(p.x);
	p.y = floor(p.y - 5.0);
	return step(p.x, 2.0) * step(1.0, floor((n / (exp2(floor(p.x - 3.0*p.y))) % 2.0)));
}

float ring(float2 uv, float rnd)
{
	float t = 0.6*(Time + 0.2*rnd);
	float i = floor(t / 2.0);
	float2 pos = 2.0*float2(rand(i*0.123), rand(i*2.371)) - 1.0;
	return lerp(0.2, 0.0, abs(length(uv - pos) - (t % 2.0)));
}

float3 color = float3(0.6, 0.1, 0.3); // red


float4 MainPS(VertexShaderOutput input) : COLOR
{
	//float4 col = 0.0 * tex2D(SpriteTextureSampler,input.TextureCoordinates) * input.Color;

	float2 p = input.TextureCoordinates * Size;
	float2 uv = p / Size - 0.5;

	p.y += 40.0*Time;

	float r = rand(floor(p / 8.0));
	float2 ip = (p % 8.0) - 4.0;

	float a = //lerp(0.1, 0.8, length(uv)) +
		invader(ip, 809999.0*r) * (0.06 + 0.3*ring(uv, r) + max(0.0, 0.2*sin(10.0*r*Time)));

	return input.Color + 0.5*float4(a, a, a, 1.0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
