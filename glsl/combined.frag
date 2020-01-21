uniform sampler2D Texture0;
uniform sampler2D Texture1;
uniform sampler2D Texture2;
uniform sampler2D Texture3;
uniform sampler2D Texture4;
uniform sampler2D Texture5;
uniform sampler2D Texture6;
uniform sampler2D Palette;

uniform bool EnableDepthPreview;
uniform float DepthTextureScale;

varying vec4 vTexCoord;
varying vec2 vTexMetadata;
varying vec4 vChannelMask;
varying vec4 vDepthMask;
varying vec2 vTexSampler;

varying vec4 vColorFraction;
varying vec4 vRGBAFraction;
varying vec4 vPalettedFraction;

float jet_r(float x)
{
	return x < 0.7 ? 4.0 * x - 1.5 : -4.0 * x + 4.5;
}

float jet_g(float x)
{
	return x < 0.5 ? 4.0 * x - 0.5 : -4.0 * x + 3.5;
}

float jet_b(float x)
{
	return x < 0.3 ? 4.0 * x + 0.5 : -4.0 * x + 2.5;
}
//роутер для выбора текстуры. он приходит из UnpackChannelAttributes, если в вертексбуфере, в
// в последнем столбце например 65, то это возьмет primarySampler (3 координату из UnpackChannelAttributes) 
// приведет к vTexSampler.s=1 и выберет Texture1 в атрибутах шейдера

vec4 Sample(float samplerIndex, vec2 pos)
{
	if (samplerIndex < 0.5)
		return texture2D(Texture0, pos);
	else if (samplerIndex < 1.5)
		return texture2D(Texture1, pos);
	else if (samplerIndex < 2.5)
		return texture2D(Texture2, pos);
	else if (samplerIndex < 3.5)
		return texture2D(Texture3, pos);
	else if (samplerIndex < 4.5)
		return texture2D(Texture4, pos);
	else if (samplerIndex < 5.5)
		return texture2D(Texture5, pos);

	return texture2D(Texture6, pos);
}

void main()
{
	vec4 x = Sample(vTexSampler.s, vTexCoord.st); //возвращает структуру (R,G,B,A) из текстуры
	
	//для attrib.s=1, vChannelMask будет vec4(1,0,0,0)
	//dot это просто перемножение каждой компоненты vec4 на такую же компоненту из другого vec4
	//dot(x, vChannelMask) это будет Х координата для цвета в палитре ,
	//vTexMetadata.s будет Y коориданатой палитры. 
	//vChannelMask определяет в каком байте (R,G,B,A) находится позицию цвета в палитре.
	//получаем, что х который равен цвету из текстуры, будет обреза с помощью vChannelMask до какой то компоненты r,g,b,a
	// , чтобы понять, в какой из них хранится указатель на цвет палитры.
	
	vec2 p = vec2(dot(x, vChannelMask), vTexMetadata.s);
	vec4 c = vPalettedFraction * texture2D(Palette, p) + vRGBAFraction * x + vColorFraction * vTexCoord;

	// Discard any transparent fragments (both color and depth)
	if (c.a == 0.0)
		discard;

	float depth = gl_FragCoord.z;
	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vTexSampler.t, vTexCoord.pq);
		depth = depth + DepthTextureScale * dot(y, vDepthMask);
	}

	// Convert to window coords
	gl_FragDepth = 0.5 * depth + 0.5;

	if (EnableDepthPreview)
	{
		float x = 1.0 - gl_FragDepth;
		float r = clamp(jet_r(x), 0.0, 1.0);
		float g = clamp(jet_g(x), 0.0, 1.0);
		float b = clamp(jet_b(x), 0.0, 1.0);
		gl_FragColor = vec4(r, g, b, 1.0);
	}
	else
	{
		gl_FragColor = c;
		

	
	}
	
}
