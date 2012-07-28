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
 
float4x4 MatrixTransform; 

void SpriteVertexShader(inout float4 vColor : COLOR0, inout float2 texCoord : TEXCOORD0, inout float4 position : POSITION0) 
{ 
    position = mul(position, MatrixTransform); 
} 


struct VertexShaderInput
{
    float4 Position : POSITION0;	
	float2 Trx1 : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;	
	float2 Trx1 : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = input.Position;
	output.Trx1 = input.Trx1;
    return output;
}

///////////////////////////////////////////////////////////
/////////////////////////////////////// Tweakables ////////
///////////////////////////////////////////////////////////

float Radius <string UIName = "Radius";
    string UIWidget = "slider";
    float UIMin = 0.0f;
    float UIMax = 1.0f;
    float UIStep = 0.01f;
	> = .5;
	
float2 Center = {.5, .5};
	
float DirectionAngle <string UIName = "DirectionAngle";
    string UIWidget = "slider";
    float UIMin = -3.14159f;
    float UIMax = 3.14159f;
    float UIStep = 0.1f;
	> = 0.0f;
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
float WaveAngleWidth <string UIName = "WaveAngleWidth";
    string UIWidget = "slider";
    float UIMin = 0.01f;
    float UIMax = 3.14159;
    float UIStep = 0.01f;
	> = .39269908169;  // pi/8
	
///////////////////////////////////////////////////////////
/////////////////////////////////// Pixel shaders  ////////
///////////////////////////////////////////////////////////

float reflect(float angle) {
	if (angle >= 0) {
		return 3.14159 - angle;
	} else {
		return -3.14159 - angle;
	}
}

float ScreenRatio = 16.0f / 9.0f;

float4 PS_wave(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR
{       
	float beginAngle = -WaveAngleWidth / 2;
	float endAngle = beginAngle + WaveAngleWidth;
	
    float x = Center.x;
    float y = Center.y;
    float x1 = texCoord.x;
    float y1 = texCoord.y;

	float angle = atan2(y1 - y, (x1 - x) * ScreenRatio);
	angle += DirectionAngle;

	// normalize the angle
	if (angle > 3.14159) {
		angle -= 3.14159 * 2;
	} if (angle < -3.14159) {
		angle += 3.14159 * 2;		
	}
		
	if (angle >= beginAngle && angle <= endAngle) {
		float2 screenCoord = float2(texCoord.x * ScreenRatio, texCoord.y);
		float2 screenCenter = float2(Center.x * ScreenRatio, Center.y);
		float dist = distance(screenCenter, screenCoord);
		float distFromRadius = Radius - dist;
		if ( distFromRadius > - WaveWidth && distFromRadius < WaveWidth ) {
			//return float4(1,0,0,1); // debug -- a plain red line
			float xdistort = sin((WaveWidth - abs(distFromRadius)) * SmearMultiplier);
			float2 texPrime = { x1 + xdistort, y1 };
			return tex2D(ScnSamp, texPrime);
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
    VertexShader = compile vs_3_0 SpriteVertexShader(); 
	PixelShader = compile ps_3_0 PS_wave();
    }
}

////////////// eof ///
