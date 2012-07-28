// Effect uses a scrolling overlay texture to make different parts of
// an image fade in or out at different speeds.

//float2 OverlayScroll;

float Radius;
float2 Center;

sampler TextureSampler : register(s0);
//sampler OverlaySampler : register(s1);


float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the texture color.
    float4 tex = tex2D(TextureSampler, texCoord);
    
    // Look up the fade speed from the scrolling overlay texture.
    //float fadeSpeed = tex2D(OverlaySampler, OverlayScroll + texCoord).x;
    
    // Apply a combination of the input color alpha and the fade speed.
    //tex *= saturate((color.a - fadeSpeed) * 2.5 + 1);
    
    float dist = distance(Center, texCoord);
    if ( dist > Radius - .01 && dist < Radius + .01 ) {
        return float4(1,0,0,1);
    } else {
        return tex;
        //return float4(1,0,0,1) * tex.a;
    }

    //return tex * .5;
    //return color;
}


technique Desaturate
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}
