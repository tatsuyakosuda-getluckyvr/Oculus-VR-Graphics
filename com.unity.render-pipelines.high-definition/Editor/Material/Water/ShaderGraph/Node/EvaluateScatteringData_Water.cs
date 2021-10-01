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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateScatteringData_Water (Preview)")]
    class EvaluateScatteringData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateScatteringData_Water()
        {
            name = "Evaluate Scattering Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateScatteringData_Water");

        const int kNormalWSInputSlotId = 0;
        const string kNormalWSInputSlotName = "NormalWS";

        const int kLowFrequencyNormalWSInputSlotId = 1;
        const string kLowFrequencyNormalWSInputSlotName = "LowFrequencyNormalWS";

        const int kPositionWSInputSlotId = 2;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kScreenPositionInputSlotId = 3;
        const string kScreenPositionInputSlotName = "ScreenPosition";

        const int kLowFrequencyHeightInputSlotId = 4;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        const int kSSSMaskInputSlotId = 5;
        const string kSSSMaskInputSlotName = "SSSMask";

        const int kScatteringFoamInputSlotId = 6;
        const string kScatteringFoamInputSlotName = "ScatteringFoam";

        const int kScatteringColorOutputSlotId = 7;
        const string kScatteringColorOutputSlotName = "ScatteringColor";

        const int kFoamScatteringTintOutputSlotId = 8;
        const string kFoamScatteringTintOutputSlotName = "FoamScatteringTint";

        const int kRefractionColorOutputSlotId = 9;
        const string kRefractionColorOutputSlotName = "RefractionColor";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kNormalWSInputSlotId, kNormalWSInputSlotName, kNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSInputSlotId, kLowFrequencyNormalWSInputSlotName, kLowFrequencyNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector2MaterialSlot(kScreenPositionInputSlotId, kScreenPositionInputSlotName, kScreenPositionInputSlotName, SlotType.Input, Vector2.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kSSSMaskInputSlotId, kSSSMaskInputSlotName, kSSSMaskInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kScatteringFoamInputSlotId, kScatteringFoamInputSlotName, kScatteringFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kScatteringColorOutputSlotId, kScatteringColorOutputSlotName, kScatteringColorOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kFoamScatteringTintOutputSlotId, kFoamScatteringTintOutputSlotName, kFoamScatteringTintOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kRefractionColorOutputSlotId, kRefractionColorOutputSlotName, kRefractionColorOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kNormalWSInputSlotId,
                kLowFrequencyNormalWSInputSlotId,
                kPositionWSInputSlotId,
                kScreenPositionInputSlotId,
                kLowFrequencyHeightInputSlotId,
                kSSSMaskInputSlotId,
                kScatteringFoamInputSlotId,

                // Output
                kScatteringColorOutputSlotId,
                kFoamScatteringTintOutputSlotId,
                kRefractionColorOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {   
                // Initialize the structure
                sb.AppendLine("ScatteringData scatteringData;");
                sb.AppendLine("ZERO_INITIALIZE(ScatteringData, scatteringData);");
                
                // Evaluate the data
                sb.AppendLine("EvaluateScatteringData({0}, {1}, {2}, {3}, {4}, {5}, {6}, scatteringData);",
                    GetSlotValue(kPositionWSInputSlotId, generationMode),
                    GetSlotValue(kNormalWSInputSlotId, generationMode),
                    GetSlotValue(kLowFrequencyNormalWSInputSlotId, generationMode),
                    GetSlotValue(kScreenPositionInputSlotId, generationMode),
                    GetSlotValue(kSSSMaskInputSlotId, generationMode),
                    GetSlotValue(kLowFrequencyHeightInputSlotId, generationMode),
                    GetSlotValue(kScatteringFoamInputSlotId, generationMode));

                // Output the data
                sb.AppendLine("$precision3 {0} = scatteringData.scatteringColor;",
                    GetVariableNameForSlot(kScatteringColorOutputSlotId));
                sb.AppendLine("$precision3 {0} = scatteringData.foamScatteringTint;",
                    GetVariableNameForSlot(kFoamScatteringTintOutputSlotId));
                sb.AppendLine("$precision3 {0} = scatteringData.refractionColor;",
                    GetVariableNameForSlot(kRefractionColorOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kScatteringColorOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kFoamScatteringTintOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kRefractionColorOutputSlotId));
            }
        }
    }
}
