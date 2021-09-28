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
    [Title("Utility", "High Definition Render Pipeline", "Ocean", "EvaluateSimulationAdditionalData_Ocean (Preview)")]
    class EvaluateSimulationAdditionalData_Ocean : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateSimulationAdditionalData_Ocean()
        {
            name = "Evaluate Simulation Additional Data Ocean (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationAdditionalData_Ocean");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kSurfaceGradientOutputSlotId = 1;
        const string kSurfaceGradientOutputSlotName = "SurfaceGradient";

        const int kLowFrequencySurfaceGradientOutputSlotId = 2;
        const string kLowFrequencySurfaceGradientOutputSlotName = "LowFrequencySurfaceGradient";

        const int kPhaseDetailSurfaceGradientOutputSlotId = 3;
        const string kPhaseDetailSurfaceGradientOutputSlotName = "PhaseDetailSurfaceGradient";

        const int kSimulationFoamOutputSlotId = 4;
        const string kSimulationFoamOutputSlotName = "SimulationFoam";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientOutputSlotId, kSurfaceGradientOutputSlotName, kSurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kLowFrequencySurfaceGradientOutputSlotId, kLowFrequencySurfaceGradientOutputSlotName, kLowFrequencySurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kPhaseDetailSurfaceGradientOutputSlotId, kPhaseDetailSurfaceGradientOutputSlotName, kPhaseDetailSurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kSimulationFoamOutputSlotId, kSimulationFoamOutputSlotName, kSimulationFoamOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kPositionWSInputSlotId,

                // Output
                kSurfaceGradientOutputSlotId,
                kLowFrequencySurfaceGradientOutputSlotId,
                kPhaseDetailSurfaceGradientOutputSlotId,
                kSimulationFoamOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {   
                // Initialize the structure
                sb.AppendLine("OceanAdditionalData oceanAdditionalData;");
                sb.AppendLine("ZERO_INITIALIZE(OceanAdditionalData, oceanAdditionalData);");
                
                // Evaluate the data
                sb.AppendLine("EvaluateOceanAdditionalData({0}, oceanAdditionalData);",
                    GetSlotValue(kPositionWSInputSlotId, generationMode) );

                // Output the data
                sb.AppendLine("$precision3 {0} = oceanAdditionalData.surfaceGradient;",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = oceanAdditionalData.lowFrequencySurfaceGradient;",
                    GetVariableNameForSlot(kLowFrequencySurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = oceanAdditionalData.phaseSurfaceGradient;",
                    GetVariableNameForSlot(kPhaseDetailSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision {0} = oceanAdditionalData.simulationFoam;",
                    GetVariableNameForSlot(kSimulationFoamOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kLowFrequencySurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kPhaseDetailSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision {0} = 0.0",
                    GetVariableNameForSlot(kSimulationFoamOutputSlotId));
            }
        }
    }
}
