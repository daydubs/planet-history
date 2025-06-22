using UnityEngine;
using LifeStory.Generation;
using LifeStory.Ocean;
using System.Collections;
using LifeStory.Core;

namespace LifeStory.Ocean
{
    public class OceanSphere : MonoBehaviour
    {
        [Header("Ocean Configuration")]
        [SerializeField] private bool enableOceanSphere = true;
        [SerializeField] private float baseOceanLevel = 0.4f;
        [SerializeField] private Color oceanColor = new Color(0.1f, 0.3f, 0.8f, 0.7f);

        [Header("Ocean Sphere Configuration")]
        [SerializeField] private bool useBlenderSphere = true;
        [SerializeField] private Mesh blenderSphereMesh; // Assignez votre sphère Blender ici

        [Header("Ocean Material")]
        [SerializeField] private Material oceanDepthMaterial; // Assignez votre OceanDepthMaterial ici

        [Header("Progressive Ocean Formation")]
        [SerializeField] private bool enableProgressiveOcean = true;
        [SerializeField] private float minOceanLevel = 0.3f;
        [SerializeField] private float maxOceanLevel = 0.5f;

        [Header("Visual Settings")]
        [SerializeField] private bool useTransparency = true;
        [SerializeField] private float transparency = 0.7f;
        [SerializeField] private bool enableBackfaceCulling = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Références
        private PlanetGenerator planetGenerator;
        private LifeStory.Core.GameManager gameManager;
        private GameObject oceanSphereObject;
        private Material oceanMaterial;
        private MeshRenderer oceanRenderer;

        // État
        private bool isInitialized = false;
        private float currentOceanRadius = 0f;
        private float currentOceanLevel = 0f;
        private float lastWaterLevel = -1f;

        public static OceanSphere Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // S'abonner aux changements de HydricOceanSystem
            //HydricOceanSystem.OnOceanDiameterChanged += OnOceanDiameterChanged;

            // Initialisation
            StartCoroutine(InitializeOceanSphere());
        }

        private IEnumerator InitializeOceanSphere()
        {
            LogDebug("🌊 === INITIALISATION OCEAN SPHERE ===");

            // Attendre que les systèmes soient prêts
            yield return new WaitForSeconds(0.1f);

            // Trouver les références
            if (planetGenerator == null)
                planetGenerator = PlanetGenerator.Instance; 
           

            if (gameManager == null)
                gameManager = GameManager.Instance;

            LogDebug($"   PlanetGenerator: {(planetGenerator != null ? "✓" : "❌ NULL")}");
            LogDebug($"   GameManager: {(gameManager != null ? "✓" : "❌ NULL")}");

            if (!enableOceanSphere)
            {
                LogDebug("⚠️ OceanSphere désactivé");
                yield break;
            }

            // Calculer le rayon océan initial
            currentOceanRadius = GetCurrentOceanRadius();
            LogDebug($"   Rayon océan initial: {currentOceanRadius:F2}");

            // Créer la sphère océan (Blender ou Unity)
            CreateOceanSphere();

            // Créer et appliquer le matériau
            CreateOceanMaterial();
            oceanRenderer.material = oceanMaterial;

            // Configuration rendu
            if (enableBackfaceCulling)
            {
                oceanRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                oceanRenderer.receiveShadows = false;
            }

            isInitialized = true;
            yield return new WaitForSeconds(0.5f); // Petit délai de sécurité
            HydricOceanSystem.OnOceanDiameterChanged += OnOceanDiameterChanged;
            LogDebug("🔗 Écoute HydricOceanSystem activée");
            LogDebug("✅ OceanSphere initialisé avec succès");
        }

        private void CreateOceanSphere()
        {
            LogDebug("🌊 Création sphère océan...");

            if (useBlenderSphere && blenderSphereMesh != null)
            {
                CreateBlenderOceanSphere();
                LogDebug("✅ Blender sphere créée");
            }
            else
            {
                CreateUnityOceanSphere();
                LogDebug("✅ Unity sphere créée");
            }
        }

        private void CreateBlenderOceanSphere()
        {
            LogDebug("🎯 Création sphère océan avec mesh Blender...");

            // Créer GameObject avec MeshFilter et MeshRenderer
            oceanSphereObject = new GameObject("OceanSphere_Blender");
            oceanSphereObject.transform.SetParent(transform);
            oceanSphereObject.transform.localPosition = Vector3.zero;
            oceanSphereObject.transform.localScale = Vector3.one; // ✅ Toujours (1,1,1)
            

            // Ajouter composants
            MeshFilter meshFilter = oceanSphereObject.AddComponent<MeshFilter>();
            oceanRenderer = oceanSphereObject.AddComponent<MeshRenderer>();

            // Copier et adapter le mesh Blender
            Mesh oceanMesh = Instantiate(blenderSphereMesh);
            oceanMesh.name = "OceanSphere_BlenderMesh";

            // Appliquer correction d'orientation (même que PlanetGenerator)
            CorrectSphereOrientation(oceanMesh);

            // Dimensionner selon le rayon océan actuel
            ///ScaleOceanMeshVertices(oceanMesh, currentOceanRadius);
            float scale = currentOceanRadius / 1.0f;
            oceanSphereObject.transform.localScale = Vector3.one * scale;

            // Appliquer le mesh
            meshFilter.mesh = oceanMesh;

            LogDebug($"✅ Sphère océan Blender créée - Vertices: {oceanMesh.vertices.Length}");
        }

        private float GetBlenderMeshActualRadius()
        {
            if (blenderSphereMesh == null) return 1f;

            // Calculer le rayon réel du mesh
            Vector3[] vertices = blenderSphereMesh.vertices;
            float maxRadius = 0f;

            foreach (Vector3 vertex in vertices)
            {
                float distance = vertex.magnitude;
                if (distance > maxRadius)
                    maxRadius = distance;
            }

            LogDebug($"🔍 Rayon réel mesh Blender: {maxRadius:F3}");
            return maxRadius;
        }

        private void CreateUnityOceanSphere()
        {
            LogDebug("🔄 Création sphère océan Unity primitive (fallback)...");

            // Sphère primitive Unity
            oceanSphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            oceanSphereObject.name = "OceanSphere_Unity";
            oceanSphereObject.transform.SetParent(transform);
            oceanSphereObject.transform.localPosition = Vector3.zero;

            // Unity sphere a un rayon de 0.5, donc scale = rayon_voulu / 0.5
            float scale = currentOceanRadius / 0.5f;
            oceanSphereObject.transform.localScale = Vector3.one * scale;

            oceanRenderer = oceanSphereObject.GetComponent<MeshRenderer>();

            // Supprimer le collider
            Collider oceanCollider = oceanSphereObject.GetComponent<Collider>();
            if (oceanCollider != null)
            {
                DestroyImmediate(oceanCollider);
            }

            LogDebug($"✅ Sphère océan Unity créée - Scale: {scale:F2}");
        }

        private void CorrectSphereOrientation(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;

            // Rotation de 90° sur l'axe X pour redresser la sphère (même que PlanetGenerator)
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rotation * vertices[i];
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            LogDebug("🔄 Orientation sphère océan corrigée");
        }

        private void ScaleOceanMeshVertices(Mesh mesh, float targetRadius)
        {
            // ✅ SIMPLE - Transform.scale au lieu de vertices
            float scale = targetRadius / 1.0f; // Mesh Blender a rayon 1
            oceanSphereObject.transform.localScale = Vector3.one * scale;

            LogDebug($"🔧 Scale océan: {scale:F2} pour rayon {targetRadius:F2}");
        }

        private void CreateOceanMaterial()
        {
            LogDebug("🎨 Création matériau océan...");

            if (oceanDepthMaterial != null)
            {
                // Utiliser le matériau assigné dans l'inspector
                oceanMaterial = new Material(oceanDepthMaterial);
                oceanMaterial.name = "OceanMaterial_Instance";
                LogDebug("✅ Matériau océan créé depuis OceanDepthMaterial");
            }
            else
            {
                // Fallback vers URP/Lit si matériau non assigné
                LogDebug("⚠️ OceanDepthMaterial non assigné, utilisation URP/Lit");
                oceanMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                oceanMaterial.name = "OceanMaterial_Fallback";

                // Configuration transparence pour fallback
                oceanMaterial.SetInt("_Surface", 1); // Transparent
                oceanMaterial.SetFloat("_Blend", 0); // Alpha
                Color finalColor = oceanColor;
                finalColor.a = transparency;
                oceanMaterial.color = finalColor;
            }

            // Configuration initiale des propriétés shader
            UpdateOceanShaderProperties();
        }

        private void UpdateOceanShaderProperties()
        {
            if (oceanMaterial == null) return;

            // Propriétés de base du shader
            if (oceanMaterial.HasProperty("_PlanetCenter"))
            {
                oceanMaterial.SetVector("_PlanetCenter", transform.position);
            }

            if (oceanMaterial.HasProperty("_PlanetRadius"))
            {
                float planetRadius = planetGenerator?.PlanetRadius ?? 10f;
                oceanMaterial.SetFloat("_PlanetRadius", planetRadius);
            }

            // Récupérer le rayon depuis HydricOceanSystem
            if (oceanMaterial.HasProperty("_OceanRadius"))
            {
                oceanMaterial.SetFloat("_OceanRadius", currentOceanRadius); // ← Utilise la valeur en cache
            }

            if (oceanMaterial.HasProperty("_MaxOceanRadius"))
            {
                oceanMaterial.SetFloat("_MaxOceanRadius", 11.5f); // 23/2
            }

            // Calculer intensité événement
            if (oceanMaterial.HasProperty("_EventIntensity"))
            {
                float normalRadius = 10.135f; // 20.27 / 2
                float maxRadius = 11.5f;      // 23 / 2
                float currentRadius = GetCurrentOceanRadius();
                float eventIntensity = Mathf.InverseLerp(normalRadius, maxRadius, currentRadius);
                oceanMaterial.SetFloat("_EventIntensity", eventIntensity);
            }

            LogDebug($"🔄 Propriétés shader mises à jour - Rayon océan: {GetCurrentOceanRadius():F2}");
        }

        private float GetCurrentOceanRadius()
        {
            // Convertir diamètre HydricOceanSystem vers rayon
            HydricOceanSystem hydricSystem = FindAnyObjectByType<HydricOceanSystem>();
            if (hydricSystem != null && hydricSystem.IsInitialized)
            {
                float diameter = hydricSystem.CurrentOceanDiameter;
                if (diameter > 0) // ✅ Vérifier que le diamètre est valide
                {
                    LogDebug($"🔍 DEBUG HydricOceanSystem: Trouvé={hydricSystem != null}, Initialisé={hydricSystem?.IsInitialized}, Diamètre={hydricSystem?.CurrentOceanDiameter}");
                    return diameter / 2f;
                }
            }

            // ✅ FALLBACK RÉALISTE au lieu de 10.5f
            LogDebug($"🔍 DEBUG HydricOceanSystem: Trouvé={hydricSystem != null}, Initialisé={hydricSystem?.IsInitialized}, Diamètre={hydricSystem?.CurrentOceanDiameter}");
            return 9.8f; // Rayon normal océan (20.27/2)
        }

        private void OnOceanDiameterChanged(float newDiameter)
        {
            if (!isInitialized || oceanMaterial == null) return;

            LogDebug($"🔄 HydricOceanSystem signale diamètre: {newDiameter:F2}");

            // Mettre à jour la taille de la sphère
            float newRadius = newDiameter / 2f;
            UpdateOceanSphereSize(newRadius);

            // Mettre à jour les propriétés du shader
            UpdateOceanShaderProperties();
            LogDebug($"🔍 ÉVÉNEMENT reçu immédiatement après init - Diamètre: {newDiameter}");
        }

        private void UpdateOceanSphereSize(float newRadius)
        {
            if (oceanSphereObject == null) return;

            // Toujours garder transform à (1,1,1)
            oceanSphereObject.transform.localScale = Vector3.one;

            // Tout le redimensionnement sur les vertices
            MeshFilter meshFilter = oceanSphereObject.GetComponent<MeshFilter>();
            if (meshFilter?.mesh != null)
            {
                ScaleOceanMeshVertices(meshFilter.mesh, newRadius);
            }

            currentOceanRadius = newRadius;
            LogDebug($"🔄 Taille sphère océan mise à jour - Nouveau rayon: {newRadius:F2}");
        }

        // === MÉTHODES UTILITAIRES ===
        public bool IsPositionUnderwater(Vector3 worldPosition)
        {
            if (!isInitialized) return false;

            float distanceFromCenter = worldPosition.magnitude;
            return distanceFromCenter < currentOceanRadius;
        }

        public bool IsVolcanoEmerging(Vector3 volcanoPosition, float volcanoHeight)
        {
            if (!isInitialized) return false;

            float volcanoTop = volcanoPosition.magnitude + volcanoHeight;
            return volcanoTop > currentOceanRadius;
        }

        // === GETTERS PUBLICS ===
        public bool IsInitialized => isInitialized;
        public float CurrentOceanRadius => currentOceanRadius;
        public float CurrentOceanLevel => currentOceanLevel;
        public GameObject OceanSphereObject => oceanSphereObject;
        public bool HasOcean => currentOceanLevel > 0.001f;

        // === MÉTHODES DEBUG ===
        [ContextMenu("Toggle Ocean Visibility")]
        public void ToggleOceanVisibility()
        {
            if (oceanSphereObject != null)
            {
                bool isVisible = oceanSphereObject.activeSelf;
                oceanSphereObject.SetActive(!isVisible);
                LogDebug($"🌊 Océan {(!isVisible ? "visible" : "masqué")}");
            }
        }

        [ContextMenu("Test Ocean Transparency")]
        public void TestOceanTransparency()
        {
            if (oceanMaterial != null)
            {
                transparency = transparency > 0.5f ? 0.3f : 0.8f;

                if (oceanMaterial.HasProperty("_ShallowColor"))
                {
                    Color shallowColor = oceanMaterial.GetColor("_ShallowColor");
                    shallowColor.a = transparency;
                    oceanMaterial.SetColor("_ShallowColor", shallowColor);
                }

                if (oceanMaterial.HasProperty("_DeepColor"))
                {
                    Color deepColor = oceanMaterial.GetColor("_DeepColor");
                    deepColor.a = transparency;
                    oceanMaterial.SetColor("_DeepColor", deepColor);
                }

                LogDebug($"🎨 Transparence océan: {transparency:P0}");
            }
        }

        [ContextMenu("Force Update Ocean Properties")]
        public void ForceUpdateOceanProperties()
        {
            if (isInitialized)
            {
                UpdateOceanShaderProperties();
                LogDebug("🔧 Propriétés océan mises à jour manuellement");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[OceanSphere] {message}");
            }
        }

        // === CLEANUP ===
        private void OnDestroy()
        {
            // Se désabonner de l'événement
            HydricOceanSystem.OnOceanDiameterChanged -= OnOceanDiameterChanged;

            if (oceanSphereObject != null)
            {
                DestroyImmediate(oceanSphereObject);
            }

            if (oceanMaterial != null)
            {
                DestroyImmediate(oceanMaterial);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // === GUI DEBUG ===
        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUI.Box(new Rect(10, 350, 300, 120), "");
            GUI.Label(new Rect(20, 365, 280, 20), "=== OCEAN SPHERE ===");

            if (isInitialized)
            {
                GUI.Label(new Rect(20, 385, 280, 20), $"Rayon océan: {currentOceanRadius:F1}");
                GUI.Label(new Rect(20, 405, 280, 20), $"Type sphère: {(useBlenderSphere ? "Blender" : "Unity")}");
                GUI.Label(new Rect(20, 425, 280, 20), $"Matériau: {(oceanDepthMaterial != null ? "Depth Shader" : "Fallback")}");
                GUI.Label(new Rect(20, 445, 280, 20), $"HydricSystem: {(FindAnyObjectByType<HydricOceanSystem>() != null ? "✓" : "❌")}");
            }
            else
            {
                GUI.Label(new Rect(20, 385, 280, 20), "Initialisation en cours...");
            }
        }
    }
}