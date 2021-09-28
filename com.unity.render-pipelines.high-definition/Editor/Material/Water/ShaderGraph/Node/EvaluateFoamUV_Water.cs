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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateFoamUV_Water (Preview)")]
    class EvaluateFoamUV_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateFoamUV_Water()
        {
            name = "Evaluate Foam UV Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateFoamUV_Water");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kFoamUVOutputSlotId = 1;
        const string kFoamUVSlotName = "FoamUV";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            
            // Outputs
            AddSlot(new Vector2MaterialSlot(kFoamUVOutputSlotId, kFoamUVSlotName, kFoamUVSlotName, SlotType.Output, Vector2.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionWSInputSlotId,
                kFoamUVOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);
                sb.AppendLine("$precision2 {0} = EvaluateFoamUV({1});",
                  GetVariableNameForSlot(kFoamUVOutputSlotId),
                  positionWS
              );
            }
            else
            {
                sb.AppendLine("$precision2 {0} = 0.0;",
                    GetVariableNameForSlot(kFoamUVOutputSlotId)
                );
            }
        }
    }
}
