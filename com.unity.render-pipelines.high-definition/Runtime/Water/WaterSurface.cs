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
        public Vector4 choppiness = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        public Material material = null;

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
