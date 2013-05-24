
sampler TextureSampler : register(s0);

float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR
{
    // Pure black is the cue to disable the solid-color effect
    if (color.r == 0 && color.g == 0 && color.b == 0) {
//    if (color.a == 0) {
       return tex2D(TextureSampler, texCoord) * color.a;
    } else {
       return color * tex2D(TextureSampler, texCoord).a;
    }
}

technique SolidColor
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}

////////////// eof ///
