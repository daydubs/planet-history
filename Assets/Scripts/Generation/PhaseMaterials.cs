using UnityEngine;
using LifeStory.Core;

namespace LifeStory.Generation
{
    [System.Serializable]
    public class PhaseMaterials
    {
        [Header("Phase Identification")]
        public GamePhase gamePhase;
        public string phaseName;

        [Header("Terrain Materials")]
        public Material oceanMaterial;      // Océan (ou terrain sec en géologique)
        public Material shoreMaterial;      // Rivage/plage
        public Material plainMaterial;      // Plaines
        public Material hillMaterial;       // Collines
        public Material mountainMaterial;   // Montagnes
        public Material tundraMaterial;     // Toundra
        public Material iceMaterial;        // Glace

        [Header("Phase Settings")]
        [Range(0f, 1f)]
        public float globalSaturation = 1f;     // Saturation générale des couleurs
        [Range(0f, 2f)]
        public float globalContrast = 1f;       // Contraste général
        public Color globalTint = Color.white;   // Teinte générale de la phase

        /// <summary>
        /// Assigne automatiquement les matériaux créés par nom
        /// </summary>
        public void AssignMaterialsByName()
        {
            // Chercher les matériaux dans Assets/Materials par nom
            string baseName = phaseName;

            oceanMaterial = FindMaterialByName($"{baseName}_Ocean");
            shoreMaterial = FindMaterialByName($"{baseName}_Shore");
            plainMaterial = FindMaterialByName($"{baseName}_Plains");
            hillMaterial = FindMaterialByName($"{baseName}_Hills");
            mountainMaterial = FindMaterialByName($"{baseName}_Mountains");
            tundraMaterial = FindMaterialByName($"{baseName}_Tundra");
            iceMaterial = FindMaterialByName($"{baseName}_Ice");

            ////Debug.Log($"Assigned materials for phase {phaseName}");
        }

        private Material FindMaterialByName(string materialName)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{materialName} t:Material");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                ////Debug.Log($"Found material: {materialName} at {path}");
                return mat;
            }
            else
            {
                //////Debug.LogWarning($"Material not found: {materialName}");
                return null;
            }
#else
            // En runtime, utiliser Resources.Load si nécessaire
            return Resources.Load<Material>($"Materials/{materialName}");
#endif
        }

        /// <summary>
        /// Vérifie si tous les matériaux sont assignés
        /// </summary>
        public bool AreAllMaterialsAssigned()
        {
            return oceanMaterial != null &&
                   shoreMaterial != null &&
                   plainMaterial != null &&
                   hillMaterial != null &&
                   mountainMaterial != null &&
                   tundraMaterial != null &&
                   iceMaterial != null;
        }

        /// <summary>
        /// Compte le nombre de matériaux manquants
        /// </summary>
        public int GetMissingMaterialCount()
        {
            int count = 0;
            if (oceanMaterial == null) count++;
            if (shoreMaterial == null) count++;
            if (plainMaterial == null) count++;
            if (hillMaterial == null) count++;
            if (mountainMaterial == null) count++;
            if (tundraMaterial == null) count++;
            if (iceMaterial == null) count++;
            return count;
        }
    }
}