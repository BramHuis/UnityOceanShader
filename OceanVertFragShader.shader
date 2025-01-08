Shader "Unlit/OceanShader"
{
    Properties
    {
        // Properties for your shader
    }

    SubShader
    {
        Tags {"RenderType" = "Transparent" "Queue" = "Transparent"}
        // ZWrite Off
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

            sampler2D _CameraDepthTexture;
            
            struct appdata
            {
                uint index : SV_VERTEXID;  // Used to access the vertex index
            };

            struct v2f
            {
                float4 vertex : SV_POSITION; 
                float2 uv : TEXCOORD0;
                float oceanDistance : TEXCOORD1;
                float3 normal: NORMAL;
            };

            v2f vert(appdata v)
            {
                v2f o;
                uint vertexIndex = triangleIndices[v.index];
                float4 vertexPosition = float4(vertexPositions[vertexIndex], 1);

                float3 worldPos = mul(unity_ObjectToWorld, vertexPosition).xyz;
                o.oceanDistance = distance(worldPos, _WorldSpaceCameraPos);
                
                o.vertex = UnityObjectToClipPos(vertexPosition);
                
                
                float2 uvFlipped = (o.vertex.xy / o.vertex.w) * 0.5 + 0.5;
                o.uv = float2(uvFlipped.x, 1.0 - uvFlipped.y);
                
                o.normal = normalBuffer[vertexIndex];

                return o;
            } 

            fixed4 frag(v2f i) : SV_Target
            {   
                float3 lightDir = normalize(float3(0, -1.0, 0)); // Example light direction
                float diffuse = max(dot(i.normal, lightDir), 0.0);

                float nonOceanDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)); // Get the depth of all objects in the scene looking through the shader

                float distanceThroughOcean = nonOceanDepth - i.oceanDistance; // Get the distance through the ocean to an object

                if (distanceThroughOcean > oceanColorBlendDepth) { // If the distance is more than the oceanColorBlendDepth, the water is deep so color it with deepWaterColor
                    return deepWaterColor * diffuse; 
                } else {
                    // Otherwise, interpolate between shallow and deep colors based on the distance
                    float lerpFactor = distanceThroughOcean / oceanColorBlendDepth;
                    return lerp(shallowWaterColor, deepWaterColor, lerpFactor) * diffuse;
                }
            }

            ENDCG
        }
    }
}
