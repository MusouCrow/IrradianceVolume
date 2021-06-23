#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct VolumeColor {
    float3 positiveX;
    float3 negativeX;
    float3 positiveY;
    float3 negativeY;
    float3 positiveZ;
    float3 negativeZ;
};

TEXTURE3D(_IndexVolumeTex);    SAMPLER(sampler_IndexVolumeTex);

CBUFFER_START(IrradianceVolume)
float4 _IndexVolumeTex_ST;
float3 _VolumeSize;
float3 _VolumePosition;
float _VolumeInterval;
StructuredBuffer<VolumeColor> _VolumeColors;
CBUFFER_END

float3 PositionToVolumeIndex(float3 position) {
    position -= _VolumePosition;
    position /= _VolumeInterval;
    position = floor(position);

    return float3(position.x, position.z, position.y);
}

float3 GetVolumePosition(float3 index) {
    float3 position = _VolumePosition;
    index = float3(index.x, index.z, index.y);
    position += (_VolumeInterval * floor(index)) + (_VolumeInterval * 0.5);

    return position;
}

float3 GetVolumeColor(float3 position, float3 center, float3 arrow, float3 direction, float3 color) {
    float3 pos = center + (_VolumeInterval * 0.5 * arrow);
    float v = _VolumeInterval;
    float rate = saturate(v - distance(position, pos) * 0.9) / v;
    float3 vv = dot(direction, arrow) + 1;

    return color * rate * vv;
}

float3 GetAmbientColor(float3 position, float3 center, float3 direction, VolumeColor volumeColor) {
    float3 positiveX = GetVolumeColor(position, center, float3(1, 0, 0), direction, volumeColor.positiveX);
    float3 negativeX = GetVolumeColor(position, center, float3(-1, 0, 0), direction, volumeColor.negativeX);
    float3 positiveY = GetVolumeColor(position, center, float3(0, 1, 0), direction, volumeColor.positiveY);
    float3 negativeY = GetVolumeColor(position, center, float3(0, -1, 0), direction, volumeColor.negativeY);
    float3 positiveZ = GetVolumeColor(position, center, float3(0, 0, 1), direction, volumeColor.positiveZ);
    float3 negativeZ = GetVolumeColor(position, center, float3(0, 0, -1), direction, volumeColor.negativeZ);

    return positiveX + negativeX + positiveY + negativeY + positiveZ + negativeZ;
}

float3 GetIrradiance(float3 position, float3 normal) {
    float3 direction = -_MainLightPosition.xyz * normal;
    float3 indexPos = PositionToVolumeIndex(position);
    float3 center = GetVolumePosition(indexPos);
    float3 size = _VolumeSize * 2 + 1;
    float index = SAMPLE_TEXTURE3D(_IndexVolumeTex, sampler_IndexVolumeTex, indexPos / size).r;
    float3 color = GetAmbientColor(position, center, direction, _VolumeColors[ceil(index * 255)]);

    return color;
}

#endif