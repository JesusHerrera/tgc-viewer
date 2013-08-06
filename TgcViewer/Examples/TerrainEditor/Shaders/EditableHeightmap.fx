/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))
float4x4 matTransform;
//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};





//Input del Vertex Shader
struct VS_INPUT_PositionColoredTextured
{
   float4 Position : POSITION0;
   float4 Color : COLOR;
   float2 Texcoord : TEXCOORD0;

};


//Input del Pixel Shader
struct PS_INPUT_PositionColoredTextured
{
	float2 Texcoord : TEXCOORD0;   
	float4 Color: COLOR0;
};

/**************************************************************************************/
/* PositionColoredTextured */
/**************************************************************************************/

//Vertex Shader
VS_INPUT_PositionColoredTextured vs_PositionColoredTextured(VS_INPUT_PositionColoredTextured input)
{
	VS_INPUT_PositionColoredTextured output;

	//Aplicar escala y desplazamiento
	output.Position = mul(input.Position, matTransform);
	//Proyectar posicion
	output.Position = mul(output.Position, matWorldViewProj);

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;

	output.Color = input.Color;

	
	
	return output;
}



//Pixel Shader
float4 ps_PositionColoredTextured(PS_INPUT_PositionColoredTextured input) : COLOR0
{      
	
	return tex2D(diffuseMap, input.Texcoord);
}

technique PositionColoredTextured
{
   pass Pass_0
   {
	  VertexShader = compile vs_2_0 vs_PositionColoredTextured();
	  PixelShader = compile ps_2_0 ps_PositionColoredTextured();
   }
}


/**************************************************************************************/
/* PositionColoredTexturedWithBrush */
/**************************************************************************************/


float2 brushPosition;
float4 brushColor1=float4(255,0,0,255);
float4 brushColor2=float4(0,0,255,255);
float brushRadius;
float brushHardness;


//Vertex Shader
VS_INPUT_PositionColoredTextured vs_PositionColoredTexturedWithBrush(VS_INPUT_PositionColoredTextured input)
{
	VS_INPUT_PositionColoredTextured output;

	//Aplicar escala y desplazamiento
	
	output.Position = mul(input.Position, matTransform);
	
	//Coloreo el vertice de acuerdo a la posicion y radio del pincel
	float dx = output.Position[0]- brushPosition[0];
	float dz = output.Position[2]- brushPosition[1];
	
	float dl2 = dx*dx+dz*dz;
	float dr2 = brushRadius*brushRadius;

	if(dl2<=dr2*brushHardness/100) 
		output.Color = brushColor1;
	else 
		output.Color = brushColor2;

	output.Color[3] =  0.8*(1 - (dl2/dr2)) ;
	
	
	if(output.Color[3]<0) output.Color[3]=0;
	

	//Proyectar posicion
	output.Position = mul(output.Position, matWorldViewProj);

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;
	

	
	
	return output;
}




//Pixel Shader
float4 ps_PositionColoredTexturedWithBrush(PS_INPUT_PositionColoredTextured input) : COLOR0
{      
	float4 color = tex2D(diffuseMap, input.Texcoord);
	float alpha = input.Color[3]; 
	
	color = input.Color*alpha + color*(1-alpha);
	color[3] = 1;
	
	return color;
}

/*
* Technique PositionColoredTexturedWithBrush
*/
technique PositionColoredTexturedWithBrush
{
   pass Pass_0
   {
	  VertexShader = compile vs_2_0 vs_PositionColoredTexturedWithBrush();
	  PixelShader = compile ps_2_0 ps_PositionColoredTexturedWithBrush();
   }
}
