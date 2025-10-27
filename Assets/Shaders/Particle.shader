Shader "Custom/Particle"
{
    Properties
    {
        _MainTex ("Position Texture", 2D) = "white" {}
        _ColorTex ("Color Texture", 2D) = "white" {}
        _CellSize ("Cell Size", Float) = 0.1
        _Size("Size", Range(0,20)) = 4
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };
            
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ColorTex);
            SAMPLER(sampler_ColorTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _ColorTex_ST;
                float _CellSize;
                float _Size;
            CBUFFER_END
            
            // Get grid coordinates from instance ID
            int2 GetGridCoord(uint instanceID)
            {
                int width = _MainTex_TexelSize.z;
                return int2(instanceID % width, instanceID / width);
            }
            
            Varyings vert (Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                
                // Get grid coordinates
                int2 gridCoord = GetGridCoord(instanceID);
                
                // Sample position texture to check if cell is occupied
                float2 texCoord = gridCoord * _MainTex_TexelSize.xy;
                float4 positionData = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, texCoord, 0);
                float4 colorData = SAMPLE_TEXTURE2D_LOD(_ColorTex, sampler_ColorTex, texCoord, 0);
                float2 particlePos = positionData.xy;
                float isOccupied = positionData.w;

                float2 quadVertices[6] = {
                    float2(-0.5, -0.5), // Bottom-left
                    float2(0.5, -0.5),  // Bottom-right
                    float2(0.5, 0.5),   // Top-right
                    float2(-0.5, -0.5), // Bottom-left
                    float2(0.5, 0.5),   // Top-right
                    float2(-0.5, 0.5)   // Top-left
                };

                // Get vertex position based on vertex ID
                float2 quadPos = quadVertices[input.vertexID] * _Size;
                
                // Calculate world position
                float2 worldPos = (particlePos + quadPos) * _CellSize;
                
                // Center the grid
                float2 gridCenter = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w) * 0.5 * _CellSize;
                worldPos -= gridCenter;
                
                // Convert to clip space
                float3 positionWS = float3(worldPos, 0);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;

                if(positionData.w > 1.0)
                {
                    colorData = float4(1,1,1,1);
                }
                
                output.color = colorData;
                output.uv = quadPos + 0.5;
                output.normalWS = float3(0, 0, 1);

                // Hide empty cells by moving them off-screen
                if (isOccupied < 0.5) {
                    output.positionHCS = float4(2000, 2000, 2000, 1);
                }
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float2 centeredUV = input.uv - 0.5;
                float dist = length(centeredUV);
                if (dist > 0.5 * _Size) {
                    discard;
                }

                return input.color;
            }

            ENDHLSL
        }
    }
}