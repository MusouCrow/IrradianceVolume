#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
    position += _VolumeSize + 0.5;
    position /= _VolumeSize * 2 + 1;

    return float3(position.x, position.z, position.y);
}

float3 GetVolumePosition(float3 index) {
    float3 position = _VolumePosition;
    index = float3(index.x, index.z, index.y);
    position += _VolumeInterval * (floor(index) - _VolumeSize);

    return position;
}

float3 GetAmbientColor(float3 position) {
    float3 index = PositionToVolumeIndex(position);
    float3 center = GetVolumePosition(index);
    float3 range = abs(position - center);

    // float index = SAMPLE_TEXTURE3D(_IndexVolumeTex, sampler_IndexVolumeTex, position).r;
    // return _VolumeColors[ceil(index * 255)].negativeY;

    return range / (_VolumeInterval * 0.5);
}

#endif