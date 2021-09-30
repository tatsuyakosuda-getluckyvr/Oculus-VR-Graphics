using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWater
    {
        // Resolution at which the signal is evaluated
        public uint _BandResolution;
        // Speed of the wind in km/h (or is it m/s?)
        public float _Pad0W;
        // Factor that attenuates the direction of the ocean
        public float _DirectionDampener;
        // Dispersion time
        public float _DispersionTime;

        public float _PatchSizeScaleRatio;
        // Maximum height of the waves
        public float _MaxWaveHeight;
        public float _WaveTipsScatteringCoefficient; 
        public float _CloudTexturedAmount;

        // Patch size for a given band
        public Vector4 _BandPatchSize;
        public Vector4 _BandPatchUVScale;
        public Vector4 _WaveAmplitude;
        public Vector4 _Choppiness;

        // Jacobian parameters
        public Vector4 _JacobianLambda;

        // Foam parameters
        public Vector4 _FoamFadeIn;
        public Vector4 _FoamFadeOut;
        public Vector4 _FoamJacobianOffset;
        public Vector4 _FoamFromHeightWeights;
        public Vector4 _FoamFromHeightFalloff;
        public Vector4 _FoamFromHeightMinMaxFalloff;

        public Vector2 _FoamOffsets;
        public float _FoamTilling;
        public float _CloudTexturedDilation;

        public float _DeepFoamAmount;
        public float _ShallowFoamAmount;
        public float _SurfaceFoamDilation;
        public float _SurfaceFoamFalloff;

        public float _SurfaceFoamTransition;
        public float _SurfaceFoamNormalsWeight;
        public float _WaveTipsScatteringOffset;
        public float _SSSMaskCoefficient;

        // Scattering
        public Vector3 _ScatteringColorTips;
        public float _MaxRefractionDepth;

        public float _Refraction;
        public float _RefractionLow;
        public float _MaxAbsorptionDistance;
        public float _ScatteringBlur;

        public Vector3 _TransparencyColor;
        public float _OutScatteringCoefficient;

        public Vector2 _Pad1W;
        public float _FoamCloudLowFrequencyTilling;
        public float _ScatteringIntensity;
        public Vector4 _ScatteringLambertLighting;

        // Two dimensional vector that describes the wind direction
        public Vector2 _WindDirection;
        // Two dimensional vector that describes the wind current
        public Vector2 _WindCurrent;
        public Vector4 _WindSpeed;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterRendering
    {
        public Vector2 _CameraOffset;
        public Vector2 _GridSize;

        public Vector3 _PatchOffset;
        public uint _GridRenderingResolution;

        public Vector2 _Padding2;
        public uint _TesselationMasks;
        public float _GlobalSurface;
    }
}
