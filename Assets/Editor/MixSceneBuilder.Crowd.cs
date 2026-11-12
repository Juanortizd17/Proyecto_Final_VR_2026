using System.Collections.Generic;
using MixLab;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SB = QualityControlXR.EditorTools.SceneBuilder;

namespace QualityControlXR.EditorTools
{
    /// <summary>Mobiliario de las zonas de clientes y el público del bar (Kenney Mini Characters).</summary>
    public static partial class MixSceneBuilder
    {
        static readonly string[] Models =
        {
            "character-female-a", "character-male-a", "character-female-b", "character-male-b", "character-female-c",
            "character-male-c", "character-female-d", "character-male-d", "character-female-e", "character-male-e",
        };
        static readonly string[] DanceMoves = { "walk", "emote-yes", "interact-right", "interact-left", "jump" };
        static readonly string[] DjMoves = { "interact-right", "interact-left", "emote-yes" };

        // Mesas con gente de pie alrededor (x, z)
        static readonly Vector2[] Tables =
        {
            new Vector2(5.9f, 1.7f), new Vector2(5.9f, -0.9f), new Vector2(4.6f, 2.75f),
            new Vector2(-4.4f, 2.75f), new Vector2(5.6f, 5.0f), new Vector2(5.6f, 7.6f), new Vector2(-5.6f, 5.0f), new Vector2(-5.6f, 7.6f),
        };
        // Sofás contra la pared oeste (z del centro)
        static readonly float[] Sofas = { 0.3f, -2.0f };

        static float SofaX => -HalfW + 0.7f;

        /// <summary>Público prescindible, ordenado por importancia (lo usa QualityGovernor para recortar).</summary>
        static readonly List<GameObject> optionalCrowd = new List<GameObject>();

        // ---------------------------------------------------------------------
        // Zona de barra, salas de estar y mesas
        // ---------------------------------------------------------------------

        static void BuildBarZone(Transform env)
        {
            foreach (float x in new[] { -2.2f, -1.1f, 1.1f, 2.25f })
                SB.Solid(SB.Fit("FurnitureKit", "stoolBar", env, new Vector3(x, 0f, SeatZ), 0.4f, 0f));
            SB.Fit("FurnitureKit", "rugRectangle", env, new Vector3(0f, 0.001f, 2.1f), 2.6f, 0f);

            // Salas de estar (oeste): el espaldar va contra la pared y la gente mira hacia el bar
            foreach (float z in Sofas)
            {
                SB.Solid(SB.Fit("FurnitureKit", "loungeSofa", env, new Vector3(SofaX, 0f, z), 1.7f, -90f));
                var coffee = SB.Solid(SB.Fit("FurnitureKit", "tableCoffee", env, new Vector3(SofaX + 1.05f, 0f, z), 0.8f, 90f));
                OnTop(coffee, "FoodKit", "cocktail", env, 0f, 0.15f, 0.13f, 0f);
                OnTop(coffee, "FoodKit", "glass-wine", env, 0.05f, -0.15f, 0.14f, 0f);
                SB.Fit("FurnitureKit", "rugRound", env, new Vector3(SofaX + 0.95f, 0.001f, z), 2.1f, 0f);
            }
            SB.Solid(SB.Fit("FurnitureKit", "lampRoundFloor", env, new Vector3(-HalfW + 0.45f, 0f, -0.85f), 0.35f, 0f));
            foreach (float s in new[] { -1f, 1f })
            {
                SB.Solid(SB.Fit("FurnitureKit", "pottedPlant", env, new Vector3(s * (HalfW - 0.5f), 0f, 3.1f), 0.6f, 0f));
                SB.Solid(SB.Fit("FurnitureKit", "pottedPlant", env, new Vector3(s * (HalfW - 0.5f), 0f, BackZ - 0.5f), 0.6f, 0f));
            }
            SB.Solid(SB.Fit("FurnitureKit", "pottedPlant", env, new Vector3(HalfW - 0.5f, 0f, SouthZ + 0.6f), 0.6f, 0f));

            foreach (var t in Tables)
            {
                var table = SB.Solid(SB.Fit("FurnitureKit", "tableRound", env, new Vector3(t.x, 0f, t.y), 0.85f, 0f));
                OnTop(table, "FoodKit", "cocktail", env, -0.15f, 0.08f, 0.13f, 0f);
                OnTop(table, "FoodKit", "glass-wine", env, 0.14f, -0.1f, 0.14f, 0f);
                if (t.y < 3.4f) SB.Fit("FurnitureKit", "rugRound", env, new Vector3(t.x, 0.001f, t.y), 1.8f, 0f);
            }
        }

        // ---------------------------------------------------------------------
        // Público
        // ---------------------------------------------------------------------

        static void BuildPatrons()
        {
            var root = new GameObject("--- CLIENTES DE FONDO ---").transform;
            optionalCrowd.Clear();
            int m = 0;
            string Next() => Models[m++ % Models.Length];

            // Imprescindibles: los dos de la barra y el DJ
            BuildPatron(root, "character-female-b", new Vector3(-2.2f, 0f, SeatZ), 165f, "sit", 0.4f);
            BuildPatron(root, "character-male-d", new Vector3(2.25f, 0f, SeatZ), 195f, "sit", 0.4f);
            SB.Fit("FoodKit", "cocktail", root, new Vector3(-2.2f, Top, 1.0f), 0.13f, 0f, allAxes: true);
            SB.Fit("FoodKit", "glass-wine", root, new Vector3(2.25f, Top, 1.02f), 0.14f, 0f, allAxes: true);
            BuildPatron(root, "character-male-d", new Vector3(0f, 0f, BackZ - 0.25f), 180f, "idle", 0.25f, DjMoves, 0.025f, 7f);

            // Sentados en los sofás, mirando hacia el bar
            var sofa = new List<GameObject>();
            foreach (float z in Sofas)
            {
                sofa.Add(BuildPatron(root, Next(), new Vector3(SofaX + 0.12f, 0f, z + 0.38f), 90f, "sit", 0.12f));
                sofa.Add(BuildPatron(root, Next(), new Vector3(SofaX + 0.12f, 0f, z - 0.38f), 90f, "sit", 0.12f));
            }
            var loner = BuildPatron(root, Next(), new Vector3(-HalfW + 1.3f, 0f, 1.85f), 150f, "idle", 0f);

            // De pie alrededor de las mesas, conversando
            var rng = new System.Random(5);
            var tables = new List<GameObject>();
            foreach (var t in Tables)
            {
                float tilt = (float)rng.NextDouble() * 50f - 25f;
                tables.Add(BuildPatron(root, Next(), new Vector3(t.x - 0.64f, 0f, t.y + 0.12f), 95f + tilt, "idle", 0f));
                tables.Add(BuildPatron(root, Next(), new Vector3(t.x + 0.64f, 0f, t.y - 0.12f), 275f + tilt, "idle", 0f));
            }

            // Pista de baile
            const float f = 0.03f;
            Vector2[] spots =
            {
                new Vector2(-2.2f, -1.3f), new Vector2(-1.1f, -1.5f), new Vector2(0.1f, -1.1f), new Vector2(1.3f, -1.4f), new Vector2(2.3f, -1.0f),
                new Vector2(-1.8f, -0.1f), new Vector2(-0.6f, 0.2f), new Vector2(0.8f, -0.2f), new Vector2(2.0f, 0.3f),
                new Vector2(-2.3f, 1.1f), new Vector2(-0.9f, 1.3f), new Vector2(0.5f, 1.0f), new Vector2(1.7f, 1.4f),
            };
            var dancers = new List<GameObject>();
            foreach (var s in spots)
                dancers.Add(BuildPatron(root, Next(), new Vector3(s.x, 0f, DanceCenter.z + s.y), 180f + (float)rng.NextDouble() * 120f - 60f, "idle", f, DanceMoves));
            // Un par bailando fuera de la pista
            var side = new List<GameObject>
            {
                BuildPatron(root, Next(), new Vector3(-3.9f, 0f, DanceCenter.z + 0.4f), 100f, "idle", 0f, DanceMoves),
                BuildPatron(root, Next(), new Vector3(3.9f, 0f, DanceCenter.z - 0.3f), 260f, "idle", 0f, DanceMoves),
            };

            // Orden de importancia: lo primero es lo último que se quita.
            // Nivel VR (8): seis bailarines repartidos y la pareja del sofá. Nivel Medio (16): más pista y dos mesas.
            foreach (int i in new[] { 1, 3, 6, 8, 10, 12 }) optionalCrowd.Add(dancers[i]);
            optionalCrowd.Add(sofa[0]); optionalCrowd.Add(sofa[1]);
            optionalCrowd.Add(tables[0]); optionalCrowd.Add(tables[1]); optionalCrowd.Add(tables[6]); optionalCrowd.Add(tables[7]);
            foreach (int i in new[] { 0, 5, 7, 11 }) optionalCrowd.Add(dancers[i]);
            foreach (int i in new[] { 2, 4, 9 }) optionalCrowd.Add(dancers[i]);
            optionalCrowd.Add(sofa[2]); optionalCrowd.Add(sofa[3]); optionalCrowd.Add(loner);
            optionalCrowd.AddRange(side);
            for (int i = 2; i < tables.Count; i++) if (i != 6 && i != 7) optionalCrowd.Add(tables[i]);
            optionalCrowd.RemoveAll(g => g == null);
        }

        static GameObject BuildPatron(Transform parent, string model, Vector3 pos, float yaw, string clip, float feetHeight,
                                      string[] dance = null, float bounce = 0.05f, float twist = 20f)
        {
            string path = $"Assets/ThirdParty/Kenney/MiniCharacters/{model}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { Debug.LogWarning("[MixSceneBuilder] Falta personaje: " + path); return null; }
            if (importer.animationType != ModelImporterAnimationType.Legacy)
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.SaveAndReimport();
            }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var root = new GameObject((dance != null ? "Dancer_" : "Patron_") + model);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var go = Object.Instantiate(asset, root.transform);
            go.name = model;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            var b = SB.Bounds(go);
            if (b.size.y > 0.001f) go.transform.localScale *= 1.15f / b.size.y;      // ~1.15 m de alto (estilo "mini")

            // Pose inicial dentro de la escena (y para medir dónde quedan los pies al sentarse)
            var anim = go.GetComponent<Animation>();
            AnimationClip found = null;
            if (anim != null) foreach (AnimationState s in anim) if (s.name == clip || s.name.EndsWith(clip)) found = s.clip;
            if (found != null) found.SampleAnimation(go, 0f);
            else Debug.LogWarning($"[MixSceneBuilder] {model}: sin animación '{clip}'");
            b = SB.Bounds(go);
            go.transform.position += Vector3.up * (feetHeight - b.min.y);

            // Rendimiento: no se animan fuera de cámara y no proyectan sombras
            if (anim != null) anim.cullingType = AnimationCullingType.BasedOnRenderers;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f); col.height = 1.5f; col.radius = 0.25f;
            if (dance != null) root.AddComponent<Dancer>().Setup(dance, bounce, twist);
            else root.AddComponent<Patron>().Setup(clip);
            return root;
        }
    }
}
