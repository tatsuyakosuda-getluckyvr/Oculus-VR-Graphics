using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // Enum that defines the sets of resolution at which the ocean simulation can be evaluated
    public enum WaterSimulationResolution
    {
        VeryLow32 = 32,
        Low64 = 64,
        Medium128 = 128,
        High256 = 256,
        Ultra512 = 512
    }

    internal class WaterSiumulationResources
    {
        public float m_Time = 0;
        public RTHandle m_H0s;
        public RTHandle m_DisplacementBuffer;
        public RTHandle m_NormalBuffer;
        public int m_SimulationSize = 0;
        public int m_NumBands = 0;

        public void AllocateSmmulationResources(int simulationRes, int numBands)
        {
            m_SimulationSize = simulationRes;
            m_NumBands = numBands;

            m_H0s = RTHandles.Alloc(m_SimulationSize, m_SimulationSize, m_NumBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);

            m_DisplacementBuffer = RTHandles.Alloc(m_SimulationSize, m_SimulationSize, m_NumBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_NormalBuffer = RTHandles.Alloc(m_SimulationSize, m_SimulationSize, m_NumBands, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat, useMipMap: true, autoGenerateMips: false);
        }

        public bool ValidResources(int simulationRes, int numBands)
        {
            return (simulationRes == m_SimulationSize) && (numBands == m_NumBands) && (m_DisplacementBuffer != null) && (m_NormalBuffer != null);
        }

        public void ReleaseSmmulationResources()
        {
            RTHandles.Release(m_NormalBuffer);
            RTHandles.Release(m_DisplacementBuffer);
            RTHandles.Release(m_H0s);
            m_NormalBuffer = null;
            m_DisplacementBuffer = null;
            m_H0s = null;
            m_SimulationSize = 0;
            m_NumBands = 0;
            m_Time = 0;
        }
    }

    public partial class HDRenderPipeline
    {
        // Various internal constants
        public const int k_WaterHighBandCount = 4;
        public const int k_WaterLowBandCount = 2;
        const int k_OceanMinGridSize = 2;
        const float k_PhillipsPatchScalar = 1000.0f;
        const float k_PhillipsAmplitudeScalar = 15.0f;
        const float k_WaterAmplitudeNormalization = 10.0f;
        const float k_WaterChopinessNormalization = 1.5f;
        const float k_PhillipsGravityConstant = 9.8f;
        const float k_PhillipsWindScalar = 1.0f / k_PhillipsGravityConstant; // Is this a coincidence? Found '0.10146f' by curve fitting
        const float k_PhillipsWindFalloffCoefficient = 0.00034060072f; // PI/(9.8^4);
        const float k_HurricanWindScalar = 0.6148388f * 0.5f;// Half the maximum Max Wind (61.5f) for a 1000m patch

        // Simulation shader and kernels
        ComputeShader m_WaterSimulationCS;
        int m_InitializePhillipsSpectrumKernel;
        int m_EvaluateDispersionKernel;
        int m_EvaluateNormalsKernel;

        // FFT shader and kernels
        ComputeShader m_FourierTransformCS;
        int m_RowPassTi_Kernel;
        int m_ColPassTi_Kernel;

        // Intermediate RTHandles used to render the ocean
        RTHandle m_HtRs = null;
        RTHandle m_HtIs = null;
        RTHandle m_FFTRowPassRs;
        RTHandle m_FFTRowPassIs;

        // Other internal rendering data
        bool m_ActiveWaterSimulation = false;
        Material m_InternalWaterMaterial;
        MaterialPropertyBlock m_OceanMaterialPropertyBlock;
        float m_DispersionTime;
        WaterSimulationResolution m_WaterBandResolution = WaterSimulationResolution.Medium128;
        ShaderVariablesWater m_ShaderVariablesWater = new ShaderVariablesWater();

        void GetFFTKernels(WaterSimulationResolution resolution, out int rowKernel, out int columnKernel)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.Ultra512:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_512");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_512");
                }
                break;
                case WaterSimulationResolution.High256:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_256");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_256");
                }
                break;
                case WaterSimulationResolution.Medium128:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_128");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_128");
                }
                break;
                case WaterSimulationResolution.Low64:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
                case WaterSimulationResolution.VeryLow32:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_32");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_32");
                }
                break;
                default:
                {
                    rowKernel = m_FourierTransformCS.FindKernel("RowPassTi_64");
                    columnKernel = m_FourierTransformCS.FindKernel("ColPassTi_64");
                }
                break;
            }
        }

        void InitializeWaterSystem()
        {
            // If the asset doesn't support water surfaces, nothing to do here
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportWater)
                return;

            m_ActiveWaterSimulation = true;
            m_WaterBandResolution = m_Asset.currentPlatformRenderPipelineSettings.waterSimulationResolution;

            // Simulation shader and kernels
            m_WaterSimulationCS = m_Asset.renderPipelineResources.shaders.waterSimulationCS;
            m_InitializePhillipsSpectrumKernel = m_WaterSimulationCS.FindKernel("InitializePhillipsSpectrum");
            m_EvaluateDispersionKernel = m_WaterSimulationCS.FindKernel("EvaluateDispersion");
            m_EvaluateNormalsKernel = m_WaterSimulationCS.FindKernel("EvaluateNormals");

            // FFT shader and kernels
            m_FourierTransformCS = m_Asset.renderPipelineResources.shaders.fourierTransformCS;
            GetFFTKernels(m_WaterBandResolution, out m_RowPassTi_Kernel, out m_ColPassTi_Kernel);

            int textureRes = (int)m_WaterBandResolution;
            // Allocate all the RTHanles required for the ocean rendering
            m_HtRs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_HtIs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassRs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
            m_FFTRowPassIs = RTHandles.Alloc(textureRes, textureRes, k_WaterHighBandCount, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, wrapMode: TextureWrapMode.Repeat);
        
            // Allocate the additional rendering data
            m_OceanMaterialPropertyBlock = new MaterialPropertyBlock();
            m_InternalWaterMaterial = defaultResources.materials.defaultWaterMaterial;
            m_DispersionTime = 0;
        }

        void ReleaseWaterSystem()
        {
            // If the asset doesn't support oceans, nothing to do here
            if (!m_Asset.currentPlatformRenderPipelineSettings.supportWater)
                return;
            // Grab all the water surfaces in the scene
            var waterSurfaces = UnityEngine.GameObject.FindObjectsOfType<WaterSurface>();

            // Loop through them and display them
            int numWaterSurfaces = waterSurfaces.Length;
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                WaterSurface waterSurface = waterSurfaces[surfaceIdx];
                if (waterSurface != null)
                    waterSurface.simulation.ReleaseSmmulationResources();
            }

            // Release all the RTHandles
            RTHandles.Release(m_FFTRowPassIs);
            RTHandles.Release(m_FFTRowPassRs);
            RTHandles.Release(m_HtIs);
            RTHandles.Release(m_HtRs);
        }

        void GetWaterDispersionTime(float dispersionTime, ref float oceanTime, ref float waveDispersionTime)
        {
            oceanTime = dispersionTime;
            waveDispersionTime = oceanTime * Mathf.Sqrt((int)m_WaterBandResolution / (float) 32);
        }

        void GetWaterWindDirectionAndCurrent(float oceanTime, ref Vector2 outWindDirection, ref Vector2 outCurrentDirection)
        {
            float windDirection = 0.0f * Mathf.Deg2Rad;
            float windDirectionX = Mathf.Cos(windDirection);
            float windDirectionY = Mathf.Sin(windDirection);
            outWindDirection.Set(windDirectionX, windDirectionY);

            float currentDirection = 0.0f * Mathf.Deg2Rad;
            float currentDirectionX = Mathf.Cos(currentDirection);
            float currentDirectionY = Mathf.Sin(currentDirection);
            float oceanCurrent = oceanTime * 0.0f;
            outCurrentDirection.Set(currentDirectionX * oceanCurrent, currentDirectionY * oceanCurrent);
        }

        float GetWindSpeed(float windSpeed, float resolution)
        {
            float effectOfResolutionOnWindSpeed = Mathf.Sqrt(resolution / 32);
            effectOfResolutionOnWindSpeed *= 0.25f;
            return windSpeed * k_HurricanWindScalar * effectOfResolutionOnWindSpeed;
        }

        static public float MaximumWaveHeightFunction(float windSpeed, float S)
        {
            if (windSpeed < 0) windSpeed = 0;
            return 1.0f - Mathf.Exp(-S * windSpeed * windSpeed);
        }

        static public float MaximumWindForPatch(float patchSize)
        {
            float a = Mathf.Sqrt(-1.0f / Mathf.Log(0.999f * 0.999f));
            float b = (0.001f * Mathf.PI * 2.0f) / patchSize;
            float c = k_PhillipsWindScalar * Mathf.Sqrt((1.0f / k_PhillipsGravityConstant) * (a / b));
            return c;
        }

        public float ComputeMaximumWaveHeight(Vector4 oceanWaveAmplitudeScalars, float oceanMaxPatchSize, float oceanWindSpeed)
        {
            float maxiumumWaveHeight = 0.01f;
            for (int i = 0; i < 4; ++i)
            {
                float patchAmplitude = oceanWaveAmplitudeScalars[i] * (oceanMaxPatchSize / k_PhillipsPatchScalar);
                float L = MaximumWindForPatch(k_PhillipsPatchScalar) / MaximumWindForPatch(oceanMaxPatchSize);
                float A = k_WaterAmplitudeNormalization * patchAmplitude;
                float normalizedMaximumHeight = MaximumWaveHeightFunction(oceanWindSpeed * L, k_PhillipsWindFalloffCoefficient);
                maxiumumWaveHeight = Mathf.Max(A * normalizedMaximumHeight, maxiumumWaveHeight);
            }
            return maxiumumWaveHeight;
        }

        void UpdateShaderVariablesOcean(float dispersionTime, WaterSurface currentWater, ref ShaderVariablesWater cb)
        {
            

            // Evaluate the ocean time
            float oceanTime = 0.0f;
            float dynamicOceanDispersionTime = 0.0f;
            GetWaterDispersionTime(dispersionTime, ref oceanTime, ref dynamicOceanDispersionTime);

            // Resolution at which the simulation is ran
            cb._BandResolution = (uint)m_WaterBandResolution;
            cb._DirectionDampener = 0.5f;
            cb._DispersionTime = dynamicOceanDispersionTime;

            cb._PatchSizeScaleRatio = Mathf.Lerp(1.0f, 0.5f, currentWater.oceanMinPatchSize / currentWater.oceanMaxPatchSize);
            cb._MaxWaveHeight = ComputeMaximumWaveHeight(currentWater.waveAmplitude, currentWater.oceanMaxPatchSize, 30.0f);

            float patchSizeScaleFactor = Mathf.Pow(currentWater.oceanMaxPatchSize / currentWater.oceanMinPatchSize, 1.0f / (k_WaterHighBandCount - 1));
            cb._BandPatchUVScale = new Vector4(1.0f, patchSizeScaleFactor, (patchSizeScaleFactor * patchSizeScaleFactor), (patchSizeScaleFactor * patchSizeScaleFactor * patchSizeScaleFactor));
            cb._BandPatchSize = new Vector4(currentWater.oceanMaxPatchSize, currentWater.oceanMaxPatchSize / cb._BandPatchUVScale.y, currentWater.oceanMaxPatchSize / cb._BandPatchUVScale.z, currentWater.oceanMaxPatchSize / cb._BandPatchUVScale.w);
            cb._WaveAmplitude = currentWater.waveAmplitude;

            cb._Choppiness.x = currentWater.waveAmplitude.x * currentWater.choppiness.x * k_WaterChopinessNormalization;
            cb._Choppiness.y = currentWater.waveAmplitude.y * currentWater.choppiness.y * k_WaterChopinessNormalization;
            cb._Choppiness.z = currentWater.waveAmplitude.z * currentWater.choppiness.z * k_WaterChopinessNormalization;
            cb._Choppiness.w = currentWater.waveAmplitude.w * currentWater.choppiness.w * k_WaterChopinessNormalization;

            GetWaterWindDirectionAndCurrent(oceanTime, ref cb._WindDirection, ref cb._WindCurrent);

            // Foam Data
            Vector4 jacobianNormalizer = new Vector4(cb._BandPatchSize.x * cb._BandPatchSize.x,
                                                    cb._BandPatchSize.y * cb._BandPatchSize.y,
                                                    cb._BandPatchSize.z * cb._BandPatchSize.z,
                                                    cb._BandPatchSize.w * cb._BandPatchSize.w) * 0.00000001f;
            cb._JacobianLambda = new Vector4(1 / jacobianNormalizer.x, 1 / jacobianNormalizer.y, 1 / jacobianNormalizer.z, 1 / jacobianNormalizer.w);
            cb._FoamFadeIn = new Vector4(0.01f, 0.01f, 0.01f, 0.01f);
            cb._FoamFadeOut = new Vector4(0.99f, 0.99f, 0.99f, 0.99f);
            cb._FoamJacobianOffset = new Vector4(1, 1, 1, 1);
            cb._FoamFromHeightWeights = new Vector4(1, 1, 1, 1);
            cb._FoamFromHeightFalloff = new Vector4(1, 1, 1, 1);
            cb._FoamFromHeightMinMaxFalloff = new Vector4(1, 1, 1, 0);

            cb._FoamOffsets = Vector2.zero;
            cb._FoamTilling = 1.0f;
            cb._CloudTexturedDilation = 0.5f;

            cb._DeepFoamAmount = 1.0f;
            cb._ShallowFoamAmount = 1.0f;
            cb._SurfaceFoamDilation = 1.0f;
            cb._SurfaceFoamFalloff = 1.0f;

            cb._SurfaceFoamTransition = 0.0f;
            cb._SurfaceFoamNormalsWeight = 1.0f;
            cb._WaveTipsScatteringOffset = 0.0f;
            cb._SSSMaskCoefficient = 1000.0f;

            cb._ScatteringColorTips = new Vector3(currentWater.scatteringColor.r, currentWater.scatteringColor.g, currentWater.scatteringColor.b);
            cb._MaxRefractionDepth = 25.0f;

            cb._Refraction = 0.5f;
            cb._RefractionLow = 2.0f;
            cb._MaxAbsorptionDistance = currentWater.maxAbsorptionDistance;

            cb._OutScatteringCoefficient = -Mathf.Log(0.02f) / currentWater.maxAbsorptionDistance;
            cb._TransparencyColor = new Vector3(currentWater.transparentColor.r, currentWater.transparentColor.g, currentWater.transparentColor.b);

            cb._ScatteringIntensity = currentWater.scatteringFactor * 0.5f;
            cb._FoamCloudLowFrequencyTilling = 5.0f;

            cb._CloudTexturedAmount = 1.0f;

            float scatteringLambertLightingNear = 0.6f;
            float scatteringLambertLightingFar = 0.06f;
            cb._ScatteringLambertLighting = new Vector4(scatteringLambertLightingNear, scatteringLambertLightingFar, Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingNear), Mathf.Lerp(0.5f, 1.0f, scatteringLambertLightingFar));

            Vector4 normalizedWindScalar = new Vector4(0, 0, 0, 0);
            Vector4 maxWindForCurrentPatch = new Vector4(0, 0, 0, 0); ;
            Vector4 normalizedwindExponentialDecay = new Vector4(0, 0, 0, 0);

            float maxWindForBand0 = MaximumWindForPatch(cb._BandPatchSize.x);
            // Curve the normalized wind value by { 1, 1/3^2, 1/3^4, 1/3^6 }. It's an arbitrary curve, there may be a
            // more intuitive function to use.
            cb._WindSpeed.x = GetWindSpeed(30.0f, (int)m_WaterBandResolution) * MaximumWindForPatch(cb._BandPatchSize.x);
            cb._WindSpeed.y = GetWindSpeed(30.0f, (int)m_WaterBandResolution) * MaximumWindForPatch(cb._BandPatchSize.y) * Mathf.Pow(0.7f, 0.333333f * 2);
            cb._WindSpeed.z = GetWindSpeed(30.0f, (int)m_WaterBandResolution) * MaximumWindForPatch(cb._BandPatchSize.z) * Mathf.Pow(0.7f, 0.333333f * 4);
            cb._WindSpeed.w = GetWindSpeed(30.0f, (int)m_WaterBandResolution) * MaximumWindForPatch(cb._BandPatchSize.w) * Mathf.Pow(0.7f, 0.333333f * 6);
            cb._WindSpeed /= maxWindForBand0;

            cb._WindDirection = Vector2.zero;
            cb._WindCurrent = Vector2.zero;
        }

        void UpdateWaterSurfaces(CommandBuffer cmd)
        {
            // If water surface simulation is disabled, skip.
            if (!m_ActiveWaterSimulation)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.WaterSurfaceUpdate)))
            {
                // Number of tiles we will need to dispatch
                int tileCount = (int)m_WaterBandResolution / 8;

                // Bind the noise textures
                GetBlueNoiseManager().BindDitheredRNGData1SPP(cmd);

                // Grab all the water surfaces in the scene
                var waterSurfaces = GameObject.FindObjectsOfType<WaterSurface>();

                // Loop through them and update them
                int numWaterSurfaces = waterSurfaces.Length;
                for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
                {
                    // Grab the current water surface
                    WaterSurface currentWater = waterSurfaces[surfaceIdx];

                    // Update the time
                    float currentSimTime = currentWater.simulation != null ? currentWater.simulation.m_Time + Time.deltaTime / 60 : 0.0f;

                    // Update the constant buffer
                    UpdateShaderVariablesOcean(currentSimTime, currentWater, ref m_ShaderVariablesWater);

                    // Bind the constant buffer
                    ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_WaterSimulationCS, HDShaderIDs._ShaderVariablesWater);

                    // Evaluate the band count
                    int bandCount = currentWater.highBandCound ? k_WaterHighBandCount : k_WaterLowBandCount;

                    // If the function returns false, this means the resources were just created and they need to be initialized.
                    if (!currentWater.CheckResources(cmd, (int)m_WaterBandResolution, k_WaterHighBandCount))
                    {
                        // Make sure the foam is black at the start
                        CoreUtils.SetRenderTarget(cmd, currentWater.simulation.m_NormalBuffer, clearFlag: ClearFlag.Color, Color.black);

                        // Convert the noise to the Phillips spectrum
                        cmd.SetComputeTextureParam(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, HDShaderIDs._H0BufferRW, currentWater.simulation.m_H0s);
                        cmd.DispatchCompute(m_WaterSimulationCS, m_InitializePhillipsSpectrumKernel, tileCount, tileCount, bandCount);
                    }

                    // Update the simulation time
                    currentWater.simulation.m_Time = currentSimTime;

                    // Execute the dispersion
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._H0Buffer, currentWater.simulation.m_H0s);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtRealBufferRW, m_HtRs);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateDispersionKernel, HDShaderIDs._HtImaginaryBufferRW, m_HtIs);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateDispersionKernel, tileCount, tileCount, bandCount);

                    // Bind the constant buffer
                    ConstantBuffer.Push(cmd, m_ShaderVariablesWater, m_FourierTransformCS, HDShaderIDs._ShaderVariablesWater);

                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_HtRs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_HtIs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, m_FFTRowPassRs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_RowPassTi_Kernel, HDShaderIDs._FFTImaginaryBufferRW, m_FFTRowPassIs);
                    cmd.DispatchCompute(m_FourierTransformCS, m_RowPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBuffer, m_FFTRowPassRs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTImaginaryBuffer, m_FFTRowPassIs);
                    cmd.SetComputeTextureParam(m_FourierTransformCS, m_ColPassTi_Kernel, HDShaderIDs._FFTRealBufferRW, currentWater.simulation.m_DisplacementBuffer);
                    cmd.DispatchCompute(m_FourierTransformCS, m_ColPassTi_Kernel, 1, (int)m_WaterBandResolution, bandCount);

                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsKernel, HDShaderIDs._DisplacementBuffer, currentWater.simulation.m_DisplacementBuffer);
                    cmd.SetComputeTextureParam(m_WaterSimulationCS, m_EvaluateNormalsKernel, HDShaderIDs._NormalBufferRW, currentWater.simulation.m_NormalBuffer);
                    cmd.DispatchCompute(m_WaterSimulationCS, m_EvaluateNormalsKernel, tileCount, tileCount, bandCount);

                    // Make sure the mip-maps are generated
                    currentWater.simulation.m_NormalBuffer.rt.GenerateMips();
                }
            }
        }

        struct WaterRenderingParameters
        {
            // Camera parameters
            public uint width;
            public uint height;

            public int gridResolution;
            public int numLODs;
            public Vector3 cameraPosition;
            public float gridSize;
            public Frustum cameraFrustum;
            public int bandResolution;
            public bool global;
            public Vector3 center;
            public Vector2 extent;
            public bool highBandCount;

            public Material waterMaterial;
            public MaterialPropertyBlock mbp;

            public ShaderVariablesWater waterCB;
        }

        WaterRenderingParameters PrepareOceanRenderingParameters(HDCamera camera, WaterRendering settings, WaterSurface currentWater)
        {
            WaterRenderingParameters parameters = new WaterRenderingParameters();

            parameters.gridResolution = (int)settings.gridResolution.value;
            parameters.numLODs = settings.numLevelOfDetais.value;
            parameters.cameraPosition = camera.camera.transform.position;
            parameters.gridSize = settings.gridSize.value;
            parameters.cameraFrustum = camera.frustum;
            parameters.bandResolution = (int)m_WaterBandResolution;
            parameters.global = currentWater.global;
            parameters.center = currentWater.transform.position;
            parameters.extent = currentWater.extent;
            parameters.highBandCount = currentWater.highBandCound;

            parameters.waterMaterial = currentWater.material != null ? currentWater.material : m_InternalWaterMaterial;
            parameters.mbp = m_OceanMaterialPropertyBlock;

            UpdateShaderVariablesOcean(currentWater.simulation.m_Time, currentWater, ref parameters.waterCB);

            return parameters;
        }

        class WaterRenderingData
        {
            // All the parameters required to simulate and render the ocean
            public WaterRenderingParameters parameters;

            // Simulation buffers
            public TextureHandle displacementBuffer;
            public TextureHandle normalBuffer;

            // Water rendered to this buffer
            public TextureHandle colorPyramid;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        void RenderWaterSurfaces(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle colorPyramid)
        {
            // If the ocean is disabled, no need to render or simulate
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water))
                return;

            // Grab all the water surfaces in the scene
            var waterSurfaces = UnityEngine.GameObject.FindObjectsOfType<WaterSurface>();

            // Loop through them and display them
            int numWaterSurfaces = waterSurfaces.Length;
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // If the resources are invalid, we cannot render this surface
                if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, k_WaterHighBandCount))
                    continue;

                using (var builder = renderGraph.AddRenderPass<WaterRenderingData>("Render Water Surfaces", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRendering)))
                {
                    builder.EnableAsyncCompute(false);

                    // Prepare all the internal parameters
                    passData.parameters = PrepareOceanRenderingParameters(hdCamera, settings, currentWater);

                    // Import all the textures into the system
                    passData.displacementBuffer = renderGraph.ImportTexture(currentWater.simulation.m_DisplacementBuffer);
                    passData.normalBuffer = renderGraph.ImportTexture(currentWater.simulation.m_NormalBuffer);
                    passData.colorPyramid = builder.ReadTexture(colorPyramid);

                    // Request the output textures
                    passData.colorBuffer = builder.WriteTexture(colorBuffer);
                    passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                    builder.SetRenderFunc(
                        (WaterRenderingData data, RenderGraphContext ctx) =>
                        {
                            // Bind the constant buffer
                            ConstantBuffer.Push(ctx.cmd, data.parameters.waterCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWater);

                            // Prepare the material property block for the rendering
                            data.parameters.mbp.SetTexture(HDShaderIDs._DisplacementBuffer, data.displacementBuffer);
                            data.parameters.mbp.SetTexture(HDShaderIDs._NormalBuffer, data.normalBuffer);

                            // Bind the render targets and render the ocean
                            CoreUtils.SetRenderTarget(ctx.cmd, data.colorBuffer, data.depthBuffer);

                            // Raise the keyword if it should be raised
                            CoreUtils.SetKeyword(ctx.cmd, "HIGH_RESOLUTION_WATER", data.parameters.highBandCount);

                            data.parameters.mbp.SetTexture(HDShaderIDs._ColorPyramidTexture, data.colorPyramid);

                            if (data.parameters.global)
                            {
                                // Prepare the oobb for the patches
                                OrientedBBox bbox = new OrientedBBox();
                                bbox.right = Vector3.right;
                                bbox.up = Vector3.forward;
                                bbox.extentX = data.parameters.gridSize;
                                bbox.extentY = data.parameters.gridSize;
                                bbox.extentZ = k_WaterAmplitudeNormalization * 2.0f;

                                data.parameters.mbp.SetFloat(HDShaderIDs._GlobalSurface, 1.0f);

                                // Loop through the patches
                                for (int y = -data.parameters.numLODs; y <= data.parameters.numLODs; ++y)
                                {
                                    for (int x = -data.parameters.numLODs; x <= data.parameters.numLODs; ++x)
                                    {
                                        // Compute the center of the patch
                                        bbox.center = new Vector3(x * data.parameters.gridSize, -data.parameters.cameraPosition.y, y * data.parameters.gridSize);

                                        // is this patch visible by the camera?
                                        if (GeometryUtils.Overlap(bbox, data.parameters.cameraFrustum, 6, 8))
                                        {
                                            data.parameters.mbp.SetVector(HDShaderIDs._PatchOffset, new Vector3(x * settings.gridSize.value, data.parameters.center.y, y * settings.gridSize.value));
                                            data.parameters.mbp.SetVector(HDShaderIDs._GridSize, new Vector2(settings.gridSize.value, settings.gridSize.value));
                                            int pachResolution = Mathf.Max(data.parameters.gridResolution >> (Mathf.Abs(x) + Mathf.Abs(y)), k_OceanMinGridSize);
                                            data.parameters.mbp.SetInt(HDShaderIDs._GridRenderingResolution, pachResolution);
                                            data.parameters.mbp.SetVector(HDShaderIDs._CameraOffset, new Vector2(data.parameters.cameraPosition.x, data.parameters.cameraPosition.z));
                                            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.waterMaterial, 0, MeshTopology.Triangles, 6 * pachResolution * pachResolution, 0, data.parameters.mbp);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                data.parameters.mbp.SetFloat(HDShaderIDs._GlobalSurface, 0.0f);

                                Vector2 gridSize = new Vector2(0.0f, 0.0f);
                                gridSize.x = Mathf.Min(data.parameters.extent.x, settings.gridSize.value);
                                gridSize.y = Mathf.Min(data.parameters.extent.y, settings.gridSize.value);
                                data.parameters.mbp.SetVector(HDShaderIDs._GridSize, gridSize);
                                data.parameters.mbp.SetVector(HDShaderIDs._PatchOffset, data.parameters.center);
                                data.parameters.mbp.SetInt(HDShaderIDs._GridRenderingResolution, data.parameters.gridResolution);
                                data.parameters.mbp.SetVector(HDShaderIDs._CameraOffset, new Vector2(data.parameters.cameraPosition.x, data.parameters.cameraPosition.z));
                                ctx.cmd.DrawProcedural(Matrix4x4.identity, data.parameters.waterMaterial, 0, MeshTopology.Triangles, 6 * data.parameters.gridResolution * data.parameters.gridResolution, 0, data.parameters.mbp);
                            }
                        });
                    PushFullScreenDebugTexture(m_RenderGraph, passData.displacementBuffer, FullScreenDebugMode.Water, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, xrTexture: false);
                }
            }
        }
    }
}
