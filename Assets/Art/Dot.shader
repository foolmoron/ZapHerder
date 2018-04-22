Shader "Dot"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Cutoff ("Cutoff", Range(0, 1)) = 0.9
		_HueInterval ("Hue Interval", Range(0, 10)) = 1
		_HueSteps ("Hue Steps", Range(1, 36)) = 10
		_InstanceOffsetAmplitude ("InstanceOffsetAmplitude", Range(0, 0.1)) = 0.002
		_DefaultColor ("Default Color", Color) = (0.2, 0.2, 0.2, 1)
		_DepthMaskMin ("Depth Mask Min", Range(-2, 2)) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Geometry+40" "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile_instancing
			#define UNITY_INSTANCING_ENABLED
			
			#include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

			sampler2D _CameraDepthTexture;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;
			float _HueInterval;
			float _HueSteps;
			float _InstanceOffsetAmplitude;
			fixed4 _DefaultColor;
			float _DepthMaskMin;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 world : TEXCOORD0;
				float4 screen : TEXCOORD1;
				float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			v2f vert (appdata v)
			{
				v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex);
				o.screen = ComputeScreenPos(o.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			// Keijiro's hue to rgb
			half3 Hue2RGB(half h)
			{
				h = frac(h) * 6 - 2;
				half3 rgb = saturate(half3(abs(h - 1) - 1, 2 - abs(h), 2 - abs(h - 2)));
				return rgb;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID(i);
				fixed4 c = tex2D(_MainTex, i.uv);
				if (c.a < _Cutoff) {
					discard;
				}

				half hue = (i.world.x + i.world.y + UNITY_GET_INSTANCE_ID(i) * _InstanceOffsetAmplitude) / (_HueInterval);
				hue = floor(hue * _HueSteps) / _HueSteps;
				fixed4 finalColor = fixed4(Hue2RGB(hue), 1);

				float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen)).x);
				finalColor = lerp(finalColor, _DefaultColor, step(1 - depth, _DepthMaskMin));

				return finalColor;
			}
			ENDCG
		}
	}
}
