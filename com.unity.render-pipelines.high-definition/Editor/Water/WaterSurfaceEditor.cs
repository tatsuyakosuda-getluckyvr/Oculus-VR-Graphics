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
        }

        void SanitizeVector4(SerializedProperty property)
        {
            Vector4 vec4 = property.vector4Value;
            vec4.x = Mathf.Max(0, vec4.x);
            vec4.y = Mathf.Max(0, vec4.y);
            vec4.z = Mathf.Max(0, vec4.z);
            vec4.w = Mathf.Max(0, vec4.w);
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
                SanitizeVector4(m_WaveAmplitude);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Choppiness);
            if (EditorGUI.EndChangeCheck())
                SanitizeVector4(m_Choppiness);

            EditorGUILayout.PropertyField(m_Material);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
