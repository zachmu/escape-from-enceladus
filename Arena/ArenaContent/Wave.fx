// Effect uses a scrolling overlay texture to make different parts of
// an image fade in or out at different speeds.

//float2 OverlayScroll;

// Direction enum: Left, Right, Up, Down
int Direction = 1;

float Radius;
float2 Center;
float BeginAngle = -1;
float EndAngle = 1;

sampler TextureSampler : register(s0);

float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the texture color.
    float4 tex = tex2D(TextureSampler, texCoord);
    
    float x = Center.x;
    float y = Center.y;
    float x1 = texCoord.x;
    float y1 = texCoord.y;
    bool facingRightDirection = 0;
    if (Direction == 1) {
       facingRightDirection = (x1 >= x);
    } else if (Direction == 0) {
       facingRightDirection = (x1 <= x);
    }

    if (facingRightDirection) {
        float tan = (y1 - y) / (x1 - x);
        float angle = atan(tan);

        if (angle >= BeginAngle && angle <= EndAngle) {
            float dist = distance(Center, texCoord);
            if ( dist > Radius - .001 && dist < Radius + .001 ) {
                return float4(1,0,0,1);
            }

            //return tex * .5;
            //return color;
        }
    }

    return tex;
}


technique Desaturate
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}
