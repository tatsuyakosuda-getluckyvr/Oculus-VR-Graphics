#if SHADERPASS != SHADERPASS_FORWARD
#error SHADERPASS_is_not_correctly_define
#endif

#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/MotionVectorVertexShaderCommon.hlsl"
PackedVaryingsType Vert(AttributesMesh inputMesh, AttributesPass inputPass)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return MotionVectorVS(varyingsType, inputMesh, inputPass);
}
#else // _WRITE_TRANSPARENT_MOTION_VECTOR
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}
#endif // _WRITE_TRANSPARENT_MOTION_VECTOR


void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // Compute the tile index
    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif
    
    // Get the surface and built in data
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // Light layers need to be set manually here as there is no mesh renderer
    builtinData.renderingLayers = DEFAULT_LIGHT_LAYERS;
    
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;
    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    // Alias
    float3 diffuseLighting = lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    // Apply the exposure and combine
    diffuseLighting *= GetCurrentExposureMultiplier();
    specularLighting *= GetCurrentExposureMultiplier();
    outColor = float4(diffuseLighting + specularLighting, 1.0);

    // Evaluate the fog and combine
    float3 volColor, volOpacity;
    EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);
    outColor.xyz = outColor.xyz * (1 - volOpacity) + volColor;
}
