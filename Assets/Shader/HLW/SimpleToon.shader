Shader "Custom/SimpleToon"
{
	Properties
	{
		_Color("Color", Color) = (1,0,0,1)
		_DarkColor("Dark Color", Color) = (1,0,0,1)
		_CutOff("CutOff", Range(0,1)) = 0

		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Width("Outline Width", Range(0,.2)) = 0
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

			float4 _Color;
			float4 _DarkColor;
			float _CutOff;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.normals = UnityObjectToWorldNormal(v.normals);

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float ndotL = max(0, dot(i.normals, _WorldSpaceLightPos0.xyz));
				fixed4 baseCol = lerp(_Color, _DarkColor, step(ndotL, _CutOff));

				baseCol = lerp(_Color, baseCol, _DarkColor.a);

				UNITY_APPLY_FOG(i.fogCoord, col);

				return baseCol;
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
					float3 normals : NORMAL;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					// that transforms from tangent to world space
					float3 normals : NORMAL;
				};

				float4 _OutlineColor;
				float _Width;

				v2f vert(appdata v)
				{
					v2f o;
					v.vertex += float4(v.normals,0)*_Width;

					o.vertex = UnityObjectToClipPos(v.vertex);

					o.normals = UnityObjectToWorldNormal(v.normals);

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
