#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler TextureSampler : register(s0);
float surface_alpha;

// Assume surface_color and surface_alpha are input variables
// background_color is a constant or input variable
float4 LitPassFragment(float2 uv : TEXCOORD0) : COLOR
{
    float4 surface_color = tex2D(TextureSampler, uv) * surface_alpha;
    float4 background_color = float4(0.2, 0.2, 0.2, 1.0); // example background color
    return surface_color + (background_color * (1 - surface_alpha));
}


technique GaussianBlur
{
    pass Pass1
    {
#if SM4
        PixelShader = compile ps_4_0_level_9_1 PixelShaderF();
#elif SM3
        PixelShader = compile ps_3_0 PixelShaderF();
#else
        PixelShader = compile ps_2_0 LitPassFragment();
#endif
    }
}