//// RiftLine.cs - Système de séparation continental graduelle
//// Sépare le supercontinent en 2-4 continents distincts basé sur température noyau
//// Utilise les volcans comme zones de faiblesse naturelles pour lignes de rift organiques

//using UnityEngine;
//using System.Collections.Generic;

//namespace LifeStory.Geology
//{
//    /// <summary>
//    /// Ligne de rift avec points volcaniques
//    /// </summary>
//    [System.Serializable]
//    public class RiftLine
//    {
//        public List<Vector2Int> points;              // Points de la ligne de rift
//        public List<Vector2Int> volcanicAnchors;     // Volcans qui ancrent la ligne
//        public float intensity;                      // Intensité du creusement
//        public bool isActive;                        // Ligne en cours de formation
//        public float currentDepth;                   // Profondeur actuelle
//        public float targetDepth;                    // Profondeur cible
//        public int riftID;                          // ID unique de la ligne
//    }
//}