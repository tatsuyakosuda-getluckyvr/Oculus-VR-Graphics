using System;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Universal
{
    class DebugRenderSetup : IDisposable
    {
        private readonly DebugHandler m_DebugHandler;
        private readonly ScriptableRenderContext m_Context;
        private readonly CommandBuffer m_CommandBuffer;
        private readonly int m_Index;

        private DebugMaterialSettings MaterialSettings => m_DebugHandler.DebugDisplaySettings.MaterialSettings;
        private DebugDisplaySettingsRendering RenderingSettings => m_DebugHandler.DebugDisplaySettings.RenderingSettings;
        private DebugDisplaySettingsLighting LightingSettings => m_DebugHandler.DebugDisplaySettings.LightingSettings;
        private DebugDisplaySettingsValidation ValidationSettings => m_DebugHandler.DebugDisplaySettings.ValidationSettings;

        private void Begin()
        {
            DebugSceneOverrideMode sceneOverrideMode = RenderingSettings.debugSceneOverrideMode;

            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Wireframe:
                {
                    m_Context.Submit();
                    GL.wireframe = true;
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        m_Context.Submit();
                        GL.wireframe = true;
                    }
                    break;
                }
            } // End of switch.

            m_DebugHandler.SetupShaderProperties(m_CommandBuffer, m_Index);

            m_Context.ExecuteCommandBuffer(m_CommandBuffer);
            m_CommandBuffer.Clear();
        }

        private void End()
        {
            DebugSceneOverrideMode sceneOverrideMode = RenderingSettings.debugSceneOverrideMode;

            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Wireframe:
                {
                    m_Context.Submit();
                    GL.wireframe = false;
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        m_Context.Submit();
                        GL.wireframe = false;
                    }
                    break;
                }

                default:
                {
                    break;
                }
            } // End of switch.
        }

        internal DebugRenderSetup(DebugHandler debugHandler, ScriptableRenderContext context, CommandBuffer commandBuffer, int index)
        {
            m_DebugHandler = debugHandler;
            m_Context = context;
            m_CommandBuffer = commandBuffer;
            m_Index = index;

            Begin();
        }

        internal DrawingSettings CreateDrawingSettings(ref RenderingData renderingData, DrawingSettings drawingSettings)
        {
            bool usesReplacementMaterial = (MaterialSettings.DebugVertexAttributeIndexData != DebugVertexAttributeMode.None);

            if (usesReplacementMaterial)
            {
                Material replacementMaterial = m_DebugHandler.ReplacementMaterial;
                DrawingSettings modifiedDrawingSettings = drawingSettings;

                modifiedDrawingSettings.overrideMaterial = replacementMaterial;
                modifiedDrawingSettings.overrideMaterialPassIndex = 0;
                return modifiedDrawingSettings;
            }

            // No overrides, return original
            return drawingSettings;
        }

        internal bool GetRenderStateBlock(out RenderStateBlock renderStateBlock)
        {
            DebugSceneOverrideMode sceneOverrideMode = RenderingSettings.debugSceneOverrideMode;

            // Create an empty render-state block and only enable the parts we wish to override...
            renderStateBlock = new RenderStateBlock();

            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Overdraw:
                {
                    RenderTargetBlendState additiveBlend = new RenderTargetBlendState(sourceColorBlendMode: BlendMode.One, destinationColorBlendMode: BlendMode.One);

                    // Additive-blend but leave z-write and culling as they are when we draw normally...
                    renderStateBlock.blendState = new BlendState {blendState0 = additiveBlend};
                    renderStateBlock.mask = RenderStateMask.Blend;
                    return true;
                }

                case DebugSceneOverrideMode.Wireframe:
                {
                    return true;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        // Ensure we render the wireframe in front of the solid triangles of the previous pass...
                        renderStateBlock.rasterState = new RasterState(offsetUnits: -1, offsetFactor: -1);
                        renderStateBlock.mask = RenderStateMask.Raster;
                    }

                    return true;
                }

                default:
                {
                    // We're not going to override anything...
                    return false;
                }
            } // End of switch.
        }

        public void Dispose()
        {
            End();
        }
    }
}
