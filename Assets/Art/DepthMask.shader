Shader "Depth Mask" {
 
	SubShader {
		Tags {"Queue" = "Geometry+10" "LightMode" = "ShadowCaster" }
 
		ZWrite On

		Pass {}
	}
}
