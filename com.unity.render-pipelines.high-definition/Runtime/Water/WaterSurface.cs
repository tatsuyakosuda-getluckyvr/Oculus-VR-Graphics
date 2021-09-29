using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public class WaterSurface : MonoBehaviour
    {
        public bool global = true;
        public Vector2 extent = new Vector2(100.0f, 100.0f);
        public bool highBandCound = true;
        public float oceanMinPatchSize = 10.0f;
        public float oceanMaxPatchSize = 500.0f;
        public Vector4 waveAmplitude = new Vector4(2.0f, 2.0f, 2.0f, 2.0f);
        public Vector4 choppiness = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        public Material material = null;
        public float maxAbsorptionDistance = 10.0f;
        public Color transparentColor = new Color(0.00f, 0.45f, 0.65f);
        public Color scatteringColor = new Color(0.0f, 0.4f, 0.75f);
        public float scatteringFactor = 1.0f;

        internal WaterSiumulationResources simulation = null;

        internal bool CheckResources(CommandBuffer cmd, int bandResolution, int bandCount)
        {
            // If the resources have not been allocated for this water surface, allocate them
            if (simulation == null)
            {
                simulation = new WaterSiumulationResources();
                simulation.AllocateSmmulationResources(bandResolution, bandCount);
                return false;
            }
            else if (!simulation.ValidResources(bandResolution, bandCount))
            {
                simulation.ReleaseSmmulationResources();
                simulation.AllocateSmmulationResources(bandResolution, bandCount);
                return false;
            }
            return true;
        }
    }
}
