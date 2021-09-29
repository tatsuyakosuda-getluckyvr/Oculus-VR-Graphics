using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(WaterSurface))]
    sealed class WaterSurfaceEditor : Editor
    {
        SerializedProperty m_Global;
        SerializedProperty m_Extent;
        SerializedProperty m_HighBandCound;
        SerializedProperty m_OceanMaxPatchSize;
        SerializedProperty m_OceanMinPatchSize;
        SerializedProperty m_WaveAmplitude;
        SerializedProperty m_Choppiness;
        SerializedProperty m_Material;
        SerializedProperty m_MaxAbsorptionDistance;
        SerializedProperty m_TransparentColor;
        SerializedProperty m_ScatteringColor;
        SerializedProperty m_ScatteringFactor;

        void OnEnable()
        {
            var o = new PropertyFetcher<WaterSurface>(serializedObject);
            m_Global = o.Find(x => x.global);
            m_Extent = o.Find(x => x.extent);
            m_HighBandCound = o.Find(x => x.highBandCound);
            m_OceanMaxPatchSize = o.Find(x => x.oceanMaxPatchSize);
            m_OceanMinPatchSize = o.Find(x => x.oceanMinPatchSize);
            m_WaveAmplitude = o.Find(x => x.waveAmplitude);
            m_Choppiness = o.Find(x => x.choppiness);
            m_Material = o.Find(x => x.material);
            m_MaxAbsorptionDistance = o.Find(x => x.maxAbsorptionDistance);
            m_TransparentColor = o.Find(x => x.transparentColor);
            m_ScatteringColor = o.Find(x => x.scatteringColor);
            m_ScatteringFactor = o.Find(x => x.scatteringFactor);
        }

        void SanitizeVector4(SerializedProperty property, float minValue, float maxValue)
        {
            Vector4 vec4 = property.vector4Value;
            vec4.x = Mathf.Clamp(vec4.x, minValue, maxValue);
            vec4.y = Mathf.Clamp(vec4.y, minValue, maxValue);
            vec4.z = Mathf.Clamp(vec4.z, minValue, maxValue);
            vec4.w = Mathf.Clamp(vec4.w, minValue, maxValue);
            property.vector4Value = vec4;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWater ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Water Surfaces.", MessageType.Error, wide: true);
                return;
            }

            EditorGUILayout.PropertyField(m_Global);
            if (!m_Global.boolValue)
            {
                EditorGUILayout.PropertyField(m_Extent);
            }
            EditorGUILayout.PropertyField(m_HighBandCound);
            EditorGUILayout.PropertyField(m_OceanMaxPatchSize);
            EditorGUILayout.PropertyField(m_OceanMinPatchSize);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_WaveAmplitude);
            if (EditorGUI.EndChangeCheck())
                SanitizeVector4(m_WaveAmplitude, 0.0f, 10.0f);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Choppiness);
            if (EditorGUI.EndChangeCheck())
                SanitizeVector4(m_Choppiness, 0.0f, 1.0f);

            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(m_MaxAbsorptionDistance);
            m_MaxAbsorptionDistance.floatValue = Mathf.Clamp(m_MaxAbsorptionDistance.floatValue, 0.0f, 100.0f);
            EditorGUILayout.PropertyField(m_TransparentColor);
            EditorGUILayout.PropertyField(m_ScatteringColor);
            m_ScatteringFactor.floatValue = EditorGUILayout.Slider(m_ScatteringFactor.floatValue, 0.0f, 1.0f);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
