﻿#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
Texture2D SpriteTexture2;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};
sampler2D SpriteTextureSampler2 = sampler_state
{
    Texture = <SpriteTexture2>;
};

// struct VertexShaderOutput
// {
// 	float4 Position : SV_POSITION;
// 	float4 Color : COLOR0;
// 	float2 TextureCoordinates : TEXCOORD0;
// };

// float4 MainPS(VertexShaderOutput input) : COLOR
// {
// 	float4 col = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
//     col.rgb = (col.r + col.g + col.b) / 3.0f;
//     // col.a = .5f;
//     return col;
// }

float4 MainPS(float2 TextCoord : TEXCOORD0) : COLOR0
{
    float4 color1 = tex2D(SpriteTextureSampler, TextCoord);
    float4 color2 = tex2D(SpriteTextureSampler2, TextCoord);

    return  color1 + color2;

}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};