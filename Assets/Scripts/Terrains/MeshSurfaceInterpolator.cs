// MeshSurfaceInterpolator.cs - Solution d'interpolation de vertices pour positionnement précis
// Remplace ElevateToSummit() pour éviter les volcans en lévitation

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LifeStory.Terrain
{
    /// <summary>
    /// Système d'interpolation de surface mesh pour positionnement précis d'objets
    /// Alternative économique au raycast pour mesh Blender sans colliders
    /// </summary>
    public static class MeshSurfaceInterpolator
    {
        /// <summary>
        /// Structure pour optimiser les calculs de vertices proches
        /// </summary>
        private struct VertexInfo
        {
            public Vector3 position;
            public Vector3 normal;
            public float distance;
            public int index;
        }

        /// <summary>
        /// Trouve la position exacte sur la surface mesh selon une direction donnée
        /// </summary>
        /// <param name="mesh">Mesh de la planète</param>
        /// <param name="direction">Direction normalisée depuis le centre</param>
        /// <param name="searchRadius">Rayon de recherche en degrés (défaut: 5°)</param>
        /// <param name="minVertices">Nombre minimum de vertices pour interpolation (défaut: 3)</param>
        /// <returns>Position interpolée sur la surface, ou Vector3.zero si échec</returns>
        public static Vector3 GetSurfacePositionFromDirection(Mesh mesh, Vector3 direction,
                                                             float searchRadius = 5f, int minVertices = 3)
        {
            if (mesh == null || mesh.vertices == null || mesh.vertices.Length == 0)
            {
                Debug.LogError("❌ MeshSurfaceInterpolator: Mesh invalide");
                return Vector3.zero;
            }

            direction = direction.normalized;

            // 1. Trouver les vertices dans le cône de recherche
            List<VertexInfo> candidateVertices = FindVerticesInCone(mesh, direction, searchRadius);

            if (candidateVertices.Count < minVertices)
            {
                Debug.LogWarning($"⚠️ MeshSurfaceInterpolator: Seulement {candidateVertices.Count} vertices trouvés " +
                               $"(minimum: {minVertices}). Élargissement du rayon...");

                // Élargir progressivement le rayon de recherche
                candidateVertices = FindVerticesInCone(mesh, direction, searchRadius * 2f);

                if (candidateVertices.Count < minVertices)
                {
                    Debug.LogError($"❌ MeshSurfaceInterpolator: Interpolation impossible, vertices insuffisants");
                    return Vector3.zero;
                }
            }

            // 2. Interpoler la position selon les vertices les plus proches
            Vector3 interpolatedPosition = InterpolatePosition(candidateVertices, direction, minVertices);

            return interpolatedPosition;
        }

        /// <summary>
        /// Trouve tous les vertices dans un cône de recherche autour d'une direction
        /// </summary>
        private static List<VertexInfo> FindVerticesInCone(Mesh mesh, Vector3 targetDirection, float coneAngleDegrees)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            List<VertexInfo> candidateVertices = new List<VertexInfo>();

            float cosThreshold = Mathf.Cos(coneAngleDegrees * Mathf.Deg2Rad);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 vertexDirection = vertex.normalized;

                // Test d'appartenance au cône
                float dotProduct = Vector3.Dot(targetDirection, vertexDirection);

                if (dotProduct >= cosThreshold)
                {
                    float distance = Vector3.Distance(targetDirection, vertexDirection);

                    VertexInfo vertexInfo = new VertexInfo
                    {
                        position = vertex,
                        normal = normals?.Length > i ? normals[i] : vertexDirection,
                        distance = distance,
                        index = i
                    };

                    candidateVertices.Add(vertexInfo);
                }
            }

            // Trier par distance (plus proche = plus d'influence)
            candidateVertices.Sort((a, b) => a.distance.CompareTo(b.distance));

            return candidateVertices;
        }

        /// <summary>
        /// Interpole la position finale basée sur les vertices les plus proches
        /// Utilise une pondération par distance inverse
        /// </summary>
        private static Vector3 InterpolatePosition(List<VertexInfo> vertices, Vector3 targetDirection, int maxVertices)
        {
            // Utiliser seulement les N vertices les plus proches pour optimiser
            int vertexCount = Mathf.Min(vertices.Count, maxVertices);

            Vector3 weightedPosition = Vector3.zero;
            float totalWeight = 0f;

            for (int i = 0; i < vertexCount; i++)
            {
                VertexInfo vertex = vertices[i];

                // Pondération inverse de la distance (plus proche = plus d'influence)
                // Ajouter epsilon pour éviter division par zéro
                float weight = 1f / (vertex.distance + 0.001f);

                weightedPosition += vertex.position * weight;
                totalWeight += weight;
            }

            if (totalWeight > 0f)
            {
                Vector3 interpolatedPosition = weightedPosition / totalWeight;

                // Projeter sur la direction cible pour garantir la cohérence radiale
                float interpolatedRadius = interpolatedPosition.magnitude;
                Vector3 finalPosition = targetDirection * interpolatedRadius;

                return finalPosition;
            }

            Debug.LogError("❌ MeshSurfaceInterpolator: Échec calcul poids interpolation");
            return Vector3.zero;
        }

        /// <summary>
        /// Version optimisée pour multiple objets - cache les vertices par région
        /// </summary>
        public static Vector3[] GetMultipleSurfacePositions(Mesh mesh, Vector3[] directions,
                                                           float searchRadius = 5f, int minVertices = 3)
        {
            if (directions == null || directions.Length == 0)
                return new Vector3[0];

            Vector3[] results = new Vector3[directions.Length];

            for (int i = 0; i < directions.Length; i++)
            {
                results[i] = GetSurfacePositionFromDirection(mesh, directions[i], searchRadius, minVertices);
            }

            return results;
        }

        /// <summary>
        /// Méthode de diagnostic pour visualiser les vertices trouvés
        /// </summary>
        public static void DebugVisualizeConeSearch(Mesh mesh, Vector3 direction, float searchRadius = 5f)
        {
            List<VertexInfo> vertices = FindVerticesInCone(mesh, direction, searchRadius);

            Debug.Log($"🔍 DEBUG MeshSurfaceInterpolator:");
            Debug.Log($"   Direction cible: {direction}");
            Debug.Log($"   Rayon recherche: {searchRadius}°");
            Debug.Log($"   Vertices trouvés: {vertices.Count}");

            for (int i = 0; i < Mathf.Min(5, vertices.Count); i++)
            {
                VertexInfo vertex = vertices[i];
                Debug.Log($"   Vertex {i}: Pos={vertex.position}, Distance={vertex.distance:F4}");
            }
        }

        /// <summary>
        /// Validation de la qualité d'interpolation
        /// </summary>
        public static bool ValidateInterpolationQuality(Mesh mesh, Vector3 direction, Vector3 resultPosition,
                                                        float toleranceRadius = 0.1f)
        {
            if (resultPosition == Vector3.zero)
                return false;

            // Vérifier que le résultat est cohérent avec la direction
            Vector3 resultDirection = resultPosition.normalized;
            float directionError = Vector3.Angle(direction, resultDirection);

            return directionError <= toleranceRadius;
        }
    }
}