using System.Collections.Generic;
using MixLab;
using UnityEditor;
using UnityEngine;

namespace QualityControlXR.EditorTools
{
    /// <summary>Optimización: regulador de calidad, medidor de rendimiento y agrupado de objetos estáticos.</summary>
    public static partial class MixSceneBuilder
    {
        static void BuildPerformance(GameObject managers)
        {
            var go = new GameObject("Rendimiento (QualityGovernor)");
            go.transform.SetParent(managers.transform);

            // Luces prescindibles, por importancia: focos de la pista en diagonal, luego lámparas laterales y acentos
            var lights = new List<Light>();
            if (discoSpots != null)
                foreach (int i in new[] { 0, 3, 1, 2 }) if (i < discoSpots.Length) lights.Add(discoSpots[i]);
            foreach (string name in new[] { "Side Lamp W", "Side Lamp E", "Corner Lamp W", "Corner Lamp E", "Accent Teal", "Accent Pink", "BackBar Lamp" })
            {
                var l = GameObject.Find(name);
                if (l) lights.Add(l.GetComponent<Light>());
            }
            var gov = go.AddComponent<QualityGovernor>();
            gov.Setup(optionalCrowd.ToArray(), lights.ToArray());
            go.AddComponent<PerfProbe>();
            EditorUtility.SetDirty(gov);
        }

        /// <summary>
        /// Marca como estáticos los objetos de decoración que nunca se mueven, para que Unity los
        /// agrupe en pocos lotes de dibujado (las 39 botellas de la repisa pasan a ser unos pocos).
        /// Quedan fuera los que se animan o cambian de color por código.
        /// </summary>
        static void MarkStaticDecor()
        {
            var skip = new HashSet<Renderer>(pulseNeons);
            if (discoTiles != null) foreach (var r in discoTiles) skip.Add(r);
            int count = 0;
            foreach (string rootName in new[] { "--- SALA ---", "--- PISTA ---", "--- BARRA ---" })
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if (skip.Contains(r)) continue;
                    var t = r.transform;
                    if (mirrorBall != null && t.IsChildOf(mirrorBall)) continue;
                    if (t.GetComponentInParent<Light>() != null) continue;                    // carcasas de los focos móviles
                    if (eqBars != null && System.Array.IndexOf(eqBars, t) >= 0) continue;
                    var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.BatchingStatic);
                    count++;
                }
            }
            Debug.Log($"[MixSceneBuilder] {count} objetos de decoración marcados para agrupado estático");
        }
    }
}
