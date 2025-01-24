Shader "Unlit/OceanShader"
{
    Properties
    {
        // Properties for your shader
    }

    SubShader
    {
        Tags {"RenderType" = "Transparent" "Queue" = "Transparent"}
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Buffers from the compute shader
            StructuredBuffer<float3> vertexPositions;
            StructuredBuffer<int> triangleIndices;
            StructuredBuffer<float3> normalBuffer;

            float4 shallowWaterColor;
            float4 deepWaterColor;
            float oceanColorBlendDepth;

            float specularHighlightShininess;

            float3 mainLightDirection;
            float4 mainLightColor; 

            float4 ambientLight;
            float ambientLightStrength;

            float4 fresnelColor;
            float fresnelPower;

            int isDepthBased;
            float refractionStrength;
            
            sampler2D _CameraDepthTexture;
            sampler2D _CameraOpaqueTdddexture;
            
            struct appdata 
            {
                uint index : SV_VERTEXID;  // Used to access the vertex index
            };

            struct v2f
            {
                float4 vertex : SV_POSITION; 
                float2 uv : TEXCOORD0;
                float oceanDistance : TEXCOORD1;
                nointerpolation float3 worldPos : TEXCOORD2;
                nointerpolation float3 normal: NORMAL;
            };

            // Helper function to compute refracted UVs
            float2 RefractUV(float2 uv, float strength, float3 normal) {
                float strengthMod = strength * (isDepthBased ? pow(uv.y, 4.0) * 5.0 : 1.0);
                uv += strengthMod * length(normal) - strengthMod * 1.2;
                return uv;
            }

            v2f vert(appdata v)
            {
                v2f o;
                uint vertexIndex = triangleIndices[v.index];
                float3 vertexPosition = vertexPositions[vertexIndex];

                o.worldPos = mul(unity_ObjectToWorld, float4(vertexPosition, 1.0)).xyz;

                o.oceanDistance = distance(o.worldPos, _WorldSpaceCameraPos);
                
                o.vertex = UnityObjectToClipPos(vertexPosition);

                float2 uvFlipped = (o.vertex.xy / o.vertex.w) * 0.5 + 0.5;
                o.uv = float2(uvFlipped.x, 1.0 - uvFlipped.y);
                
                o.normal = normalBuffer[v.index];

                return o;
            }  

            fixed4 frag(v2f i) : SV_Target
            {    
                float3 lightDir = normalize(mainLightDirection); // Example light direction
                float diffuse = max(dot(i.normal, lightDir), 0.0);

                // Specular highlights
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);  // Direction to the camera
                float3 halfwayDir = normalize(lightDir + viewDir);  // Halfway vector
                float spec = pow(max(dot(i.normal, halfwayDir), 0.0), specularHighlightShininess);
                fixed4 specularColor = mainLightColor * spec;

                float nonOceanDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)); // Get the depth of all objects in the scene looking through the shader
                float distanceThroughOcean = nonOceanDepth - i.oceanDistance; // Get the distance through the ocean to an object
                float lerpFactor = smoothstep(0.0, oceanColorBlendDepth, distanceThroughOcean);
                fixed4 waterColor = lerp(shallowWaterColor, deepWaterColor, lerpFactor);

                float fresnel = pow(1.0 - dot(viewDir, i.normal), fresnelPower);

                // Refraction logic
                float2 refractedUV = RefractUV(i.uv, refractionStrength, i.normal);
                float3 refractedColor = tex2D(_CameraOpaqueTdddexture, refractedUV).rgb;

                waterColor.rgb = lerp(waterColor.rgb, refractedColor, 0.2);
                waterColor.rgb += fresnel * fresnelColor.rgb;
                waterColor.rgb *= diffuse;
                waterColor.rgb += ambientLight.rgb * ambientLightStrength;
                waterColor.rgb += specularColor.rgb;
                waterColor.rgb = saturate(waterColor.rgb);

                return waterColor;
            }

            ENDCG
        }
    }
}
