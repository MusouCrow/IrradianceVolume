#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct VolumeColor {
    float3 colors[6];
    /*
    float3 positiveX;
    float3 negativeX;
    float3 positiveY;
    float3 negativeY;
    float3 positiveZ;
    float3 negativeZ;
    */
};

CBUFFER_START(IrradianceVolume)
sampler3D _IndexVolumeTex1;
sampler3D _IndexVolumeTex2;
sampler3D _IndexVolumeTex3;
sampler3D _IndexVolumeTex4;
sampler3D _IndexVolumeTex5;
sampler3D _IndexVolumeTex6;
float3 _VolumeSize;
float3 _VolumePosition;
float _VolumeInterval;
CBUFFER_END

float3 PositionToVolumeIndex(float3 position) {
    position -= _VolumePosition;
    position /= _VolumeInterval;
    position = floor(position);

    return position;
}

float3 GetVolumePosition(float3 index) {
    float3 position = _VolumePosition;
    position += (_VolumeInterval * floor(index)) + (_VolumeInterval * 0.5);

    return position;
}

float3 GetAmbientColor(float3 normal, float3 colors[6]) {
    float3 nSquared = normal * normal;
    int3 isNegative = normal < 0.0;
    float3 color = nSquared.x * colors[isNegative.x] + 
    nSquared.y * colors[isNegative.y + 2] + nSquared.z * colors[isNegative.z + 4];

    return color;
}

float3 GetIrradiance(float3 position, float3 normal) {
    /*
    float3 pos = PositionToVolumeIndex(position);
    float3 center = GetVolumePosition(pos);
    float3 size = _VolumeSize * 2 + 1;
    */
    float3 pos = position - _VolumePosition;
    float3 size = (_VolumeSize * 2 + 1) * _VolumeInterval;
    float3 coord = pos / size;

    float3 colors[6];
    colors[0] = tex3D(_IndexVolumeTex1, coord);
    colors[1] = tex3D(_IndexVolumeTex2, coord);
    colors[2] = tex3D(_IndexVolumeTex3, coord);
    colors[3] = tex3D(_IndexVolumeTex4, coord);
    colors[4] = tex3D(_IndexVolumeTex5, coord);
    colors[5] = tex3D(_IndexVolumeTex6, coord);
    
    // float3 direction = reflect(normal, _MainLightPosition.xyz);
    float3 color = GetAmbientColor(normal, colors);

    // float index = SAMPLE_TEXTURE3D(_IndexVolumeTex, sampler_IndexVolumeTex, coord).r;
    // float3 color = GetAmbientColor(normal, _VolumeColors[index * 255]);

    // float3 color = _VolumeColors[index * 255].colors[3];
    // float3 color = SAMPLE_TEXTURE3D(_IndexVolumeTex, sampler_IndexVolumeTex, coord);

    return color;
}

#endif