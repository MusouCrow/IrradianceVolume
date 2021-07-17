#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

sampler3D _VolumeTex0;
sampler3D _VolumeTex1;
sampler3D _VolumeTex2;
sampler3D _VolumeTex3;
sampler3D _VolumeTex4;
sampler3D _VolumeTex5;

CBUFFER_START(IrradianceVolume)
float3 _VolumeSize;
float3 _VolumePosition;
float _VolumeInterval;
CBUFFER_END

float3 GetAmbientColor(float3 normal, float4 coord) {
    float3 nSquared = normal * normal;
    float3 colorX = normal.x >= 0.0 ? tex3Dlod(_VolumeTex0, coord).rgb : tex3Dlod(_VolumeTex1, coord).rgb;
    float3 colorY = normal.y >= 0.0 ? tex3Dlod(_VolumeTex2, coord).rgb : tex3Dlod(_VolumeTex3, coord).rgb;
    float3 colorZ = normal.z >= 0.0 ? tex3Dlod(_VolumeTex4, coord).rgb : tex3Dlod(_VolumeTex5, coord).rgb;
    float3 color = nSquared.x * colorX + nSquared.y * colorY + nSquared.z * colorZ;

    return color;
}

float3 GetIrradiance(float3 position, float3 normal) {
    float3 pos = position - _VolumePosition;
    float3 size = (_VolumeSize * 2 + 1) * _VolumeInterval;
    float4 coord = float4(pos / size, 0);
    float3 direction = reflect(-_MainLightPosition.xyz, normal);
    float3 color = GetAmbientColor(direction, coord);

    return color;
}

#endif