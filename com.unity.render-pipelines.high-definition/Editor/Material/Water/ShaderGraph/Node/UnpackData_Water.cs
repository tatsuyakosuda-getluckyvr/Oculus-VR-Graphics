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
    [Title("Utility", "High Definition Render Pipeline", "Water", "UnpackData_Water (Preview)")]
    class UnpackData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public UnpackData_Water()
        {
            name = "Unpack Water Data (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("UnpackData_Water");

        const int kUV1InputSlotId = 0;
        const string kUV1InputSlotName = "uv1";

        const int kLowFrequencyHeightOutputSlotId = 1;
        const string kLowFrequencyHeightSlotName = "LowFrequencyHeight";

        const int kFoamFromHeightOutputSlotId = 2;
        const string kFoamFromHeightSlotName = "FoamFromHeight";

        const int kSSSMaskOutputSlotId = 3;
        const string kSSSMaskSlotName = "SSSMask";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new Vector4MaterialSlot(kUV1InputSlotId, kUV1InputSlotName, kUV1InputSlotName, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
            
            // Outputs
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightSlotName, kLowFrequencyHeightSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kFoamFromHeightOutputSlotId, kFoamFromHeightSlotName, kFoamFromHeightSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSSSMaskOutputSlotId, kSSSMaskSlotName, kSSSMaskSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Inputs
                kUV1InputSlotId,

                // Outputs
                kLowFrequencyHeightOutputSlotId,
                kFoamFromHeightOutputSlotId,
                kSSSMaskOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv1 = GetSlotValue(kUV1InputSlotId, generationMode);
            sb.AppendLine("$precision {0} = {1}.x; $precision {0} = {2}.y; $precision {0} = {3}.z;",
                kUV1InputSlotId,
                GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId),
                GetVariableNameForSlot(kFoamFromHeightOutputSlotId),
                GetVariableNameForSlot(kSSSMaskOutputSlotId)
            );
        }
    }
}
