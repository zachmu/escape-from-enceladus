float Script : STANDARDSGLOBAL <
    string UIWidget = "none";
    string ScriptClass = "scene";
    string ScriptOrder = "postprocess";
    string ScriptOutput = "color";
    string Script = "Technique=Main;";
> = 0.8;

// Standard full-screen imaging value
float4 ClearColor <
    string UIWidget = "color";
    string UIName = "Clear (Bg) Color";
> = {0,0,0,1.0};

float ClearDepth <
    string UIWidget = "None";
> = 1.0;

float2 ViewportSize : VIEWPORTPIXELSIZE <
    string UIName="Screen Size";
    string UIWidget="None";
>;

static float2 ViewportOffset = (float2(0.5,0.5)/ViewportSize);

///////////////////////////////////////////////////////////
///////////////////////////// Render-to-Texture Targets ///
///////////////////////////////////////////////////////////

texture2D ScnMap : RENDERCOLORTARGET <
    float2 ViewPortRatio = {1.0,1.0};
    int MipLevels = 1;
    string Format = "X8R8G8B8" ;
    string UIWidget = "None";
>;

sampler2D ScnSamp = sampler_state {
    texture = <ScnMap>;
    AddressU  = CLAMP;
    AddressV = CLAMP;
    FILTER = MIN_MAG_LINEAR_MIP_POINT;
};

///////////////////////////////////////////////////////////
/////////////////////////////////// Vertex Shaders ////////
///////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 Txr1: TEXCOORD0;
};
 
struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 Txr1: TEXCOORD0;
};


VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
  
    output.Position = input.Position;
	output.Txr1 = input.Txr1;
    return output;
}

///////////////////////////////////////////////////////////
/////////////////////////////////////// Tweakables ////////
///////////////////////////////////////////////////////////

int Direction = 1;

float Radius <string UIName = "Radius";
    string UIWidget = "slider";
    float UIMin = 0.0f;
    float UIMax = 1.0f;
    float UIStep = 0.01f;
	> = .5;
	
float2 Center = {.5, .5};
float BeginAngle <string UIName = "BeginAngle";
    string UIWidget = "slider";
    float UIMin = -3.0f;
    float UIMax = 3.0f;
    float UIStep = 0.01f;
	> = -0.25;
float SmearMultiplier <string UIName = "SmearMultiplier";
    string UIWidget = "slider";
    float UIMin = -10.0f;
    float UIMax = 10.0f;
    float UIStep = 0.01f;
	> = .7;
float WaveWidth <string UIName = "WaveWidth";
    string UIWidget = "slider";
    float UIMin = 0.001f;
    float UIMax = 0.2;
    float UIStep = 0.001f;
	> = .02;
	
///////////////////////////////////////////////////////////
/////////////////////////////////// Pixel shaders  ////////
///////////////////////////////////////////////////////////

float4 PS_wave(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR
{       
	float EndAngle = -BeginAngle;
	
    float x = Center.x;
    float y = Center.y;
    float x1 = texCoord.x;
    float y1 = texCoord.y;
    bool facingRightDirection = 1;
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
			float distFromRadius = Radius - dist;
            if ( distFromRadius > - WaveWidth && distFromRadius < WaveWidth ) {
				float xdistort = sin((WaveWidth - abs(distFromRadius)) * SmearMultiplier);			
				float2 texPrime = { x1 + xdistort, y1};
				return tex2D(ScnSamp, texPrime);
            }
        }
    }

    return tex2D(ScnSamp, texCoord);	
}  

////////////////////////////////////////////////////////////
/////////////////////////////////////// techniques /////////
////////////////////////////////////////////////////////////
RasterizerState DisableCulling
{
    CullMode = NONE;
};

DepthStencilState DepthEnabling
{
	DepthEnable = FALSE;
	DepthWriteMask = ZERO;
};

BlendState DisableBlend
{
	BlendEnable[0] = FALSE;
};

technique Main <
	string Script =
	"RenderColorTarget0=ScnMap;"
		"ClearSetColor=ClearColor;"
    	"ClearSetDepth=ClearDepth;"
		"Clear=Color;"
		"Clear=Depth;"
	    "ScriptExternal=color;"
	"Pass=Pass0;";
> {
    pass Pass0 <
       	string Script= "RenderColorTarget0=;"
			"Draw=Buffer;";
    >
    {
    //VertexShader = compile vs_3_0 VertexShaderFunction();
	PixelShader = compile ps_2_0 PS_wave();
    }
}

////////////// eof ///