//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATERSYSTEMDEF_CS_HLSL
#define WATERSYSTEMDEF_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWater)
    uint _BandResolution;
    float _Pad0W;
    float _DirectionDampener;
    float _DispersionTime;
    float _PatchSizeScaleRatio;
    float _MaxWaveHeight;
    float _WaveTipsScatteringCoefficient;
    float _CloudTexturedAmount;
    float4 _BandPatchSize;
    float4 _BandPatchUVScale;
    float4 _WaveAmplitude;
    float4 _Choppiness;
    float4 _JacobianLambda;
    float4 _FoamFadeIn;
    float4 _FoamFadeOut;
    float4 _FoamJacobianOffset;
    float4 _FoamFromHeightWeights;
    float4 _FoamFromHeightFalloff;
    float4 _FoamFromHeightMinMaxFalloff;
    float2 _FoamOffsets;
    float _FoamTilling;
    float _CloudTexturedDilation;
    float _DeepFoamAmount;
    float _ShallowFoamAmount;
    float _SurfaceFoamDilation;
    float _SurfaceFoamFalloff;
    float _SurfaceFoamTransition;
    float _SurfaceFoamNormalsWeight;
    float _WaveTipsScatteringOffset;
    float _SSSMaskCoefficient;
    float3 _ScatteringColorTips;
    float _MaxRefractionDepth;
    float _Refraction;
    float _RefractionLow;
    float _MaxAbsorptionDistance;
    float _ScatteringBlur;
    float3 _TransparencyColor;
    float _OutScatteringCoefficient;
    float2 _Pad1W;
    float _FoamCloudLowFrequencyTilling;
    float _ScatteringIntensity;
    float4 _ScatteringLambertLighting;
    float2 _WindDirection;
    float2 _WindCurrent;
    float4 _WindSpeed;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float2 _CameraOffset;
    float2 _GridSize;
    float3 _PatchOffset;
    uint _GridRenderingResolution;
    float2 _Padding2;
    uint _TesselationMasks;
    float _GlobalSurface;
CBUFFER_END


#endif
