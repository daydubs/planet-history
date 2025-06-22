using UnityEngine;

namespace LifeStory.Generation
{
    [System.Serializable]
    public class BiomeSettings
    {
        [Header("Altitude Thresholds")]
        public float oceanLevel = 0.3f;        // Niveau de l'océan
        public float shoreLevel = 0.35f;       // Plages
        public float plainLevel = 0.5f;        // Plaines
        public float hillLevel = 0.7f;         // Collines
        public float mountainLevel = 0.85f;    // Montagnes
        public float snowLevel = 0.95f;        // Neige éternelle

        [Header("Materials")]
        public Material oceanMaterial;
        public Material shoreMaterial;
        public Material plainMaterial;
        public Material hillMaterial;
        public Material mountainMaterial;
        public Material snowMaterial;
    }
}