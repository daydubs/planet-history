using UnityEngine;

namespace LifeStory.Generation
{
    [System.Serializable]
    public class NoiseSettings
    {
        public float scale = 50f;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2f;
        public Vector2 offset = Vector2.zero;
    }
}