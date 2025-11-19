Shader "Custom/OrganicCreatureWithDamage"
{
    Properties
    {
        // Textura base del dinosaurio
        _MainTex ("Base Texture (Albedo)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)

        // Textura de normales (opcional)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1.0

        // Propiedades físicas
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        // ═══════════════════════════════════════════════════════════
        // 🩸 SISTEMA DE DAÑO VISUAL
        // ═══════════════════════════════════════════════════════════
        [Space(20)]
        [Header(Damage System)]
        _DamageTex ("Damage Texture (Scratches)", 2D) = "white" {}
        _DamageColor ("Damage Tint Color", Color) = (1, 0, 0, 1) // Rojo por defecto
        _DamageAmount ("Damage Visibility", Range(0,1)) = 0.0
        _DamageIntensity ("Damage Intensity", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _DamageTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_DamageTex;
        };

        half _Glossiness;
        half _Metallic;
        half _BumpScale;
        fixed4 _Color;

        // Parámetros de daño
        fixed4 _DamageColor;
        half _DamageAmount;
        half _DamageIntensity;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. COLOR BASE
            // ═══════════════════════════════════════════════════════════
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // ═══════════════════════════════════════════════════════════
            // 2. TEXTURA DE DAÑO CON FONDO TRANSPARENTE
            // ═══════════════════════════════════════════════════════════
            fixed4 damageTexColor = tex2D(_DamageTex, IN.uv_DamageTex);

            // Calcular la intensidad de la textura de daño (luminosidad)
            // Esto convierte el negro a 0 y los rasguños claros a valores altos
            float damageMask = dot(damageTexColor.rgb, float3(0.299, 0.587, 0.114)); // Luminance

            // También considerar el canal alpha si está disponible
            damageMask = max(damageMask, damageTexColor.a);

            // Aplicar el tinte rojo solo donde hay rasguños (damageMask > 0)
            fixed3 damageColor = _DamageColor.rgb * _DamageIntensity;

            // Mezclar el color base con los rasguños rojos
            // Solo se aplica donde damageMask > 0 (rasguños), el fondo negro queda transparente
            float damageStrength = damageMask * _DamageAmount;
            fixed3 finalColor = lerp(baseColor.rgb, damageColor, damageStrength);

            o.Albedo = finalColor;

            // ═══════════════════════════════════════════════════════════
            // 3. NORMAL MAP (opcional)
            // ═══════════════════════════════════════════════════════════
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Normal.xy *= _BumpScale;

            // ═══════════════════════════════════════════════════════════
            // 4. PROPIEDADES FÍSICAS
            // ═══════════════════════════════════════════════════════════
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = baseColor.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
