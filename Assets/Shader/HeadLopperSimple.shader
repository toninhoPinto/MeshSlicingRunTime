Shader "Custom/HeadLopperSimple"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Color("Color", Color) = (1,0,0,1)
		_DarkColor("Dark Color", Color) = (1,0,0,1)
		_CutOff("CutOff", Range(0,1)) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
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
				float2 uv : TEXCOORD0;
				float3 normals : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				// that transforms from tangent to world space
				float3 normals : NORMAL;
			};

			sampler2D _MainTex;
			sampler2D _BumpMap;
			float4 _MainTex_ST;
			float4 _Color;
			float4 _DarkColor;
			float _CutOff;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.normals = UnityObjectToWorldNormal(v.normals);

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);	

				half3 tnormal = UnpackNormal(tex2D(_BumpMap, i.uv));
				// transform normal from tangent to world space

				float ndotL = max(0, dot(i.normals, _WorldSpaceLightPos0.xyz));
				fixed4 baseCol = lerp(_Color, _DarkColor, step(ndotL, _CutOff));

				baseCol = lerp(_Color, baseCol, _DarkColor.a);

				UNITY_APPLY_FOG(i.fogCoord, col);

				return baseCol;
			}
			ENDCG
		}
	}
			//Fallback "Diffuse"
}
