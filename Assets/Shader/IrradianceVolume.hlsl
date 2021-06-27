#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(IrradianceVolume)
sampler3D _VolumeTex0;
sampler3D _VolumeTex1;
sampler3D _VolumeTex2;
sampler3D _VolumeTex3;
sampler3D _VolumeTex4;
sampler3D _VolumeTex5;
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

    float3 colors[6];
    colors[0] = tex3D(_VolumeTex0, coord);
    colors[1] = tex3D(_VolumeTex1, coord);
    colors[2] = tex3D(_VolumeTex2, coord);
    colors[3] = tex3D(_VolumeTex3, coord);
    colors[4] = tex3D(_VolumeTex4, coord);
    colors[5] = tex3D(_VolumeTex5, coord);
    
    float3 direction = reflect(-_MainLightPosition.xyz, normal);
    float3 color = GetAmbientColor(direction, colors);

    return color;
}

#endif