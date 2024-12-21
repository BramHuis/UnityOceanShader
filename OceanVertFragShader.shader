Shader "Unlit/OceanShader"
{
    Properties
    {
        // Properties for your shader
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Buffers from the compute shader
            StructuredBuffer<float3> vertexPositions;
            StructuredBuffer<int> triangleIndices;

            struct appdata
            {
                uint index : SV_VertexID;  // Access the vertex index
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                uint index = triangleIndices[v.index];

                // Get the vertex position from the buffer
                float3 position = vertexPositions[index];
                o.pos = UnityObjectToClipPos(float4(position, 1.0));
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(0.0, 0.5, 1.0, 1.0);  // Example color for the ocean
            }

            ENDCG
        }
    }
}
