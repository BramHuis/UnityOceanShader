Shader "Unlit/OceanShader"
{
    Properties
    {
        // Properties for your shader
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Buffers from the compute shader
            StructuredBuffer<float3> vertexPositions;
            StructuredBuffer<int> triangleIndices;
            StructuredBuffer<float3> normalBuffer;

            struct appdata
            {
                uint index : SV_VertexID;  // Access the vertex index
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal: NORMAL;
            };

            v2f vert(appdata v)
            {
                v2f o;
                uint index = triangleIndices[v.index];

                // Get the vertex position from the buffer
                float3 position = vertexPositions[index];
                o.pos = TransformObjectToHClip(position);
                o.normal = normalBuffer[index];
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 lightDir = normalize(float3(0, -1.0, 0)); // Example light direction
                float diffuse = max(dot(i.normal, lightDir), 0.0);
                return half4(diffuse * 0.1, diffuse * 0.1, diffuse * 0.6, 0.5);
            }

            ENDHLSL
        }
    }
}
