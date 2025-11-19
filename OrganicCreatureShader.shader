Shader "TheIsle/OrganicCreature"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Intensity", Range(0,2)) = 1.0
        
        // Organic skin properties
        _SkinColor ("Skin Tint", Color) = (1,1,1,1)
        _SubsurfaceColor ("Subsurface Color", Color) = (0.8, 0.4, 0.3, 1)
        _SubsurfacePower ("Subsurface Power", Range(0.1, 5)) = 1.0
        _SubsurfaceDistortion ("Subsurface Distortion", Range(0, 2)) = 0.5
        
        // Roughness instead of smoothness for more natural look
        _Roughness ("Roughness", Range(0,1)) = 0.8
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        
        // Ambient and rim lighting
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 1.0
        _RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1)
        _RimPower ("Rim Power", Range(0.1, 8)) = 2.0
        _RimStrength ("Rim Strength", Range(0, 3)) = 1.0
        
        // Occlusion
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        LOD 200
        
        CGPROGRAM
        #pragma surface surf OrganicLighting fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _RoughnessMap;
        sampler2D _OcclusionMap;
        
        half _BumpScale;
        fixed4 _SkinColor;
        fixed4 _SubsurfaceColor;
        half _SubsurfacePower;
        half _SubsurfaceDistortion;
        half _Roughness;
        half _AmbientStrength;
        fixed4 _RimColor;
        half _RimPower;
        half _RimStrength;
        half _OcclusionStrength;
        
        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _SkinColor;
            o.Albedo = c.rgb;
            
            // Normal mapping
            fixed4 normalTex = tex2D(_BumpMap, IN.uv_MainTex);
            o.Normal = UnpackScaleNormal(normalTex, _BumpScale);
            
            // Roughness (inverted smoothness for more natural feel)
            half roughness = tex2D(_RoughnessMap, IN.uv_MainTex).r * _Roughness;
            o.Gloss = 1.0 - roughness;
            o.Specular = 0.02; // Very low specular for organic materials
            
            o.Alpha = c.a;
        }
        
        // Custom lighting model for organic materials
        half4 LightingOrganicLighting (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half3 h = normalize(lightDir + viewDir);
            
            // Diffuse with subsurface approximation
            half NdotL = dot(s.Normal, lightDir);
            half diffuse = max(0, NdotL);
            
            // Subsurface scattering approximation
            half3 subsurfaceNormal = s.Normal + _SubsurfaceDistortion * s.Normal;
            half subsurface = max(0, dot(-lightDir, subsurfaceNormal));
            subsurface = pow(subsurface, _SubsurfacePower);
            
            // Combine diffuse and subsurface
            half3 lighting = diffuse + subsurface * _SubsurfaceColor.rgb * 0.5;
            
            // Rim lighting for better silhouette definition
            half rim = 1.0 - max(0, dot(normalize(viewDir), s.Normal));
            rim = pow(rim, _RimPower) * _RimStrength;
            
            // Specular (very subtle for organic look)
            half NdotH = max(0, dot(s.Normal, h));
            half spec = pow(NdotH, s.Gloss * 128) * s.Specular;
            
            // Ambient enhancement for low light conditions
            half3 ambient = ShadeSH9(half4(s.Normal, 1)) * _AmbientStrength;
            
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * lighting * atten + 
                   spec * _LightColor0.rgb * atten + 
                   rim * _RimColor.rgb +
                   ambient * s.Albedo;
            c.a = s.Alpha;
            
            return c;
        }
        
        ENDCG
    }
    FallBack "Diffuse"
}
