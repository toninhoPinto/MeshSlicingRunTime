Shader "Custom/BleedingEmissive"
{ 
	Properties
	{
		_Color("Color", Color) = (1,0,0,1)

		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Width("Outline Width", Range(0,.2)) = 0
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
		LOD 100
		ZWrite On

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
		// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
	};

	float4 _Color;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);

		UNITY_TRANSFER_FOG(o,o.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{

	return _Color;
	}
		ENDCG
	}

		Pass
	{
		Cull Front
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
				// make fog work
		#pragma multi_compile_fog

		#include "UnityCG.cginc"

			struct appdata
		{
			float4 vertex : POSITION;
		};

		struct v2f
		{
			UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
		};

		float4 _OutlineColor;
		float _Width;

		v2f vert(appdata v)
		{
			v2f o;
			v.vertex += (v.vertex-float4(0,0,0,1) ) * _Width;
			o.vertex = UnityObjectToClipPos(v.vertex);

			UNITY_TRANSFER_FOG(o,o.vertex);
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{

			return _OutlineColor;
		}
			ENDCG
		}
	}
		//Fallback "Diffuse"
}
