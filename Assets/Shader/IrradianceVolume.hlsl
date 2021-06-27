#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#define K_SH_L0_Basis 0.28209479177387814347403972578039f  // Mathf.Sqrt(1.0f / (4.0f * Mathf.PI));
#define K_SH_L1_Basis 0.48860251190291992158638462283835f // Mathf.Sqrt(3.0f / (4.0f * Mathf.PI));

CBUFFER_START(IrradianceVolume)
sampler3D _VolumeTex0;
sampler3D _VolumeTex1;
sampler3D _VolumeTex2;
sampler3D _VolumeTex3;
float3 _VolumeSize;
float3 _VolumePosition;
float _VolumeInterval;
CBUFFER_END

float3 GetAmbientColor(float3 normal, float3 colors[6]) {
    float3 nSquared = normal * normal;
    int3 isNegative = normal < 0.0;
    float3 color = nSquared.x * colors[isNegative.x] + 
    nSquared.y * colors[isNegative.y + 2] + nSquared.z * colors[isNegative.z + 4];

    return color;
}

float3 GetIrradiance(float3 position, float3 normal) {
    float3 pos = position - _VolumePosition;
    float3 size = (_VolumeSize * 2 + 1) * _VolumeInterval;
    float3 coord = pos / size;
    float3 direction = reflect(-_MainLightPosition.xyz, normal);

    float3 colors[4];
    colors[0] = tex3D(_VolumeTex0, coord).rgb;
    colors[1] = tex3D(_VolumeTex1, coord).rgb;
    colors[2] = tex3D(_VolumeTex2, coord).rgb;
    colors[3] = tex3D(_VolumeTex3, coord).rgb;

    float3 color = float3(0, 0, 0);
    color += K_SH_L0_Basis * colors[0];
    color += K_SH_L1_Basis * colors[1] * direction.y;
    color += K_SH_L1_Basis * colors[2] * direction.z;
    color += K_SH_L1_Basis * colors[3] * direction.x;
    
    return color;
}

#endif