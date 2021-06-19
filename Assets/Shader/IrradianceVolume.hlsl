#ifndef __IrradianceVolume__
#define __IrradianceVolume__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE3D(_IndexVolumeTex);    SAMPLER(sampler_IndexVolumeTex);

CBUFFER_START(IrradianceVolume)
float4 _IndexVolumeTex_ST;
float3 _VolumeSize;
float3 _VolumePosition;
float _VolumeInterval;
CBUFFER_END

float3 PositionToIndex(float3 position) {
    // position -= _VolumePosition;
    position /= _VolumeInterval;
    position += _VolumeSize;

    return position / (_VolumeSize * 2 + 1);
}

float3 GetAmbientColor(float3 index) {
    // float3 index = PositionToIndex(position);
    float3 color = SAMPLE_TEXTURE3D(_IndexVolumeTex, sampler_IndexVolumeTex, index);

    return color;
}

#endif