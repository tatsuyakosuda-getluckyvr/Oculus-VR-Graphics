using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    static class WaterSurfaceMenuItems
    {
        [MenuItem("GameObject/3D Object/Water Surface", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateWaterSurface(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("WaterSurface", menuCommand.context);
            var waterSurface = go.AddComponent<WaterSurface>();
        }
    }
}
