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
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            sampler2D _ColorTex;
            float4 _MainTex_TexelSize;
            float _CellSize;
            float _Size;
            
            // Get grid coordinates from instance ID
            int2 GetGridCoord(uint instanceID)
            {
                int width = _MainTex_TexelSize.z;
                return int2(instanceID % width, instanceID / width);
            }
            
            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                
                // Get grid coordinates
                int2 gridCoord = GetGridCoord(instanceID);
                
                // Sample position texture to check if cell is occupied
                float4 positionData = tex2Dlod(_MainTex, float4(gridCoord * _MainTex_TexelSize.xy, 0, 0));
                float4 colorData = tex2Dlod(_ColorTex, float4(gridCoord * _MainTex_TexelSize.xy, 0, 0));
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
                float2 quadPos = quadVertices[v.vertexID] * _Size;
                
                // Calculate world position
                float2 worldPos = (particlePos + quadPos) * _CellSize;
                
                // Center the grid
                float2 gridCenter = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w) * 0.5 * _CellSize;
                worldPos -= gridCenter;
                
                // Convert to clip space
                o.vertex = UnityObjectToClipPos(float4(worldPos, 0, 1));
                o.worldPos = float3(worldPos, 0);

                if(positionData.w > 1.0)
                {
                    colorData = float4(1,1,1,1);
                }
                
                o.color = colorData;
                o.uv = quadPos + 0.5;
                o.normal = float3(0, 0, 1);

                // Hide empty cells by moving them off-screen
                if (isOccupied < 0.5) {
                    o.vertex = float4(2000, 2000, 2000, 1);
                }
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV);
                if (dist > 0.5 * _Size) {
                    discard;
                }

                return i.color;
            }


            ENDCG
        }
    }
}