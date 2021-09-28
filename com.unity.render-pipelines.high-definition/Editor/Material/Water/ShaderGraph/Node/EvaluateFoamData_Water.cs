using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateFoamData_Water (Preview)")]
    class EvaluateFoamData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateFoamData_Water()
        {
            name = "Evaluate Foam Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateFoamData_Water");

        const int kFoamNormalInputSlotId = 0;
        const string kFoamNormalInputSlotName = "FoamNormal";

        const int kLowFrequencySurfaceGradientInputSlotId = 1;
        const string kLowFrequencySurfaceGradientInputSlotName = "LowFrequencySurfaceGradient";

        const int kSurfaceFoamInputSlotId = 2;
        const string kSurfaceFoamInputSlotName = "SurfaceFoam";

        const int kShallowFoamInputSlotId = 3;
        const string kShallowFoamInputSlotName = "ShallowFoam";

        const int kSimulationFoamInputSlotId = 4;
        const string kSimulationFoamInputSlotName = "SimulationFoam";

        const int kFoamFromHeightInputSlotId = 5;
        const string kFoamFromHeightInputSlotName = "FoamFromHeight";

        const int kLowFrequencyHeightInputSlotId = 6;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        const int kInputNormalWSInputSlotId = 7;
        const string kInputNormalWSInputSlotName = "InputNormalWS";

        const int kFoamOutputSlotId = 8;
        const string kFoamOutputSlotName = "Foam";

        const int kFoamTransitionOutputSlotId = 9;
        const string kFoamTransitionOutputSlotName = "FoamTransition";

        const int kFoamSurfaceGradientOutputSlotId = 10;
        const string kFoamSurfaceGradientOutputSlotName = "FoamSurfaceGradient";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kFoamNormalInputSlotId, kFoamNormalInputSlotName, kFoamNormalInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencySurfaceGradientInputSlotId, kLowFrequencySurfaceGradientInputSlotName, kLowFrequencySurfaceGradientInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kSurfaceFoamInputSlotId, kSurfaceFoamInputSlotName, kSurfaceFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kShallowFoamInputSlotId, kShallowFoamInputSlotName, kShallowFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kSimulationFoamInputSlotId, kSimulationFoamInputSlotName, kSimulationFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kFoamFromHeightInputSlotId, kFoamFromHeightInputSlotName, kFoamFromHeightInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kInputNormalWSInputSlotId, kInputNormalWSInputSlotName, kInputNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            
            // Output
            AddSlot(new Vector1MaterialSlot(kFoamOutputSlotId, kFoamOutputSlotName, kFoamOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kFoamTransitionOutputSlotId, kFoamTransitionOutputSlotName, kFoamTransitionOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector3MaterialSlot(kFoamSurfaceGradientOutputSlotId, kFoamSurfaceGradientOutputSlotName, kFoamSurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kFoamNormalInputSlotId,
                kLowFrequencySurfaceGradientInputSlotId,
                kSurfaceFoamInputSlotId,
                kShallowFoamInputSlotId,
                kSimulationFoamInputSlotId,
                kFoamFromHeightInputSlotId,
                kLowFrequencyHeightInputSlotId,
                kInputNormalWSInputSlotId,

                // Output
                kFoamOutputSlotId,
                kFoamTransitionOutputSlotId,
                kFoamSurfaceGradientOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                string foamNormal = GetSlotValue(kFoamNormalInputSlotId, generationMode);
                string lowFrequencySG = GetSlotValue(kLowFrequencySurfaceGradientInputSlotId, generationMode);
                string surfaceFoam = GetSlotValue(kSurfaceFoamInputSlotId, generationMode);
                string shallowFoam = GetSlotValue(kShallowFoamInputSlotId, generationMode);
                string simulationFoam = GetSlotValue(kSimulationFoamInputSlotId, generationMode);
                string foamFromHeight = GetSlotValue(kFoamFromHeightInputSlotId, generationMode);
                string lowFrequencyHeight = GetSlotValue(kLowFrequencyHeightInputSlotId, generationMode);
                string inputNormalWS = GetSlotValue(kInputNormalWSInputSlotId, generationMode);

                sb.AppendLine("FoamData foamData;");
                sb.AppendLine("ZERO_INITIALIZE(FoamData, foamData);");
                
                sb.AppendLine("EvaluateFoamData({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, foamData);",
                    foamNormal,
                    lowFrequencySG,
                    surfaceFoam,
                    shallowFoam,
                    simulationFoam,
                    foamFromHeight,
                    lowFrequencyHeight,
                    inputNormalWS
                );

                sb.AppendLine("$precision {0} = foamData.foamValue;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );

                sb.AppendLine("$precision {0} = foamData.foamTransition;",
                    GetVariableNameForSlot(kFoamTransitionOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = foamData.foamSurfaceGradient;",
                    GetVariableNameForSlot(kFoamSurfaceGradientOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0",
                    GetVariableNameForSlot(kFoamTransitionOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kFoamSurfaceGradientOutputSlotId)
                );
            }
        }
    }
}
