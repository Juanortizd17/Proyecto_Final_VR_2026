using System.Collections.Generic;
using MixLab;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using SB = QualityControlXR.EditorTools.SceneBuilder;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Ambientación del bar-discoteca: sala ampliada con pista de baile, cabina de DJ, luces de
    /// discoteca, barra trasera con botellas, letreros de neón, mesas y clientes de fondo.
    /// </summary>
    public static partial class MixSceneBuilder
    {
        const float HalfW = 5.2f;            // media anchura de la sala
        const float SouthZ = -3.1f;          // pared detrás del jugador (barra trasera)
        const float BackZ = 7.2f;            // pared del fondo (cabina del DJ)
        const float CeilY = 3.45f;
        const int TileCols = 6, TileRows = 4;
        static readonly Vector3 DanceCenter = new Vector3(0f, 0f, 4.8f);
        static readonly Color Teal = new Color(0.37f, 0.9f, 0.82f);
        static readonly Color Pink = new Color(1f, 0.35f, 0.6f);
        static readonly Color Amber = new Color(1f, 0.72f, 0.25f);
        static readonly Color Violet = new Color(0.66f, 0.45f, 1f);

        static Renderer[] discoTiles;
        static Light[] discoSpots, discoFills;
        static Transform mirrorBall;
        static Light ballLight;
        static Transform[] eqBars;
        static readonly List<Renderer> pulseNeons = new List<Renderer>();

        // =====================================================================
        // Sala
        // =====================================================================

        static Light[] BuildRoom()
        {
            pulseNeons.Clear();
            var env = new GameObject("--- SALA ---").transform;
            var wood = SB.Mat("MixFloor", new Color(0.34f, 0.22f, 0.15f));
            var danceBase = SB.Mat("DanceBase", new Color(0.07f, 0.06f, 0.1f));
            var wall = SB.Mat("MixWall", new Color(0.24f, 0.18f, 0.4f));
            var panel = SB.Mat("MixWainscot", new Color(0.27f, 0.16f, 0.12f));
            var dark = SB.Mat("MixCeiling", new Color(0.1f, 0.08f, 0.17f));
            var trim = SB.Mat("CounterTop", Copper);
            var teal = SB.Mat("NeonTeal", Teal, emissive: true);
            var pink = SB.Mat("NeonPink", Pink, emissive: true);
            var amber = SB.Mat("NeonAmber", Amber, emissive: true);
            var violet = SB.Mat("NeonViolet", Violet, emissive: true);

            float depth = BackZ - SouthZ, midZ = (BackZ + SouthZ) * 0.5f, width = HalfW * 2f;
            const float split = 3.25f;       // donde termina el piso de madera y empieza la pista
            SB.Prim(PrimitiveType.Cube, "Floor_Bar", env, new Vector3(0f, -0.05f, (SouthZ + split) * 0.5f), new Vector3(width, 0.1f, split - SouthZ), wood);
            SB.Prim(PrimitiveType.Cube, "Floor_Dance", env, new Vector3(0f, -0.05f, (split + BackZ) * 0.5f), new Vector3(width, 0.1f, BackZ - split), danceBase);
            SB.Prim(PrimitiveType.Cube, "Ceiling", env, new Vector3(0f, CeilY, midZ), new Vector3(width + 0.2f, 0.1f, depth + 0.2f), dark, false);
            SB.Prim(PrimitiveType.Cube, "Wall_N", env, new Vector3(0f, 1.7f, BackZ + 0.1f), new Vector3(width + 0.2f, 3.4f, 0.2f), wall);
            SB.Prim(PrimitiveType.Cube, "Wall_S", env, new Vector3(0f, 1.7f, SouthZ), new Vector3(width + 0.2f, 3.4f, 0.2f), wall);
            SB.Prim(PrimitiveType.Cube, "Wall_E", env, new Vector3(HalfW + 0.1f, 1.7f, midZ), new Vector3(0.2f, 3.4f, depth + 0.2f), wall);
            SB.Prim(PrimitiveType.Cube, "Wall_W", env, new Vector3(-HalfW - 0.1f, 1.7f, midZ), new Vector3(0.2f, 3.4f, depth + 0.2f), wall);

            // Zócalo de madera con remate de cobre
            foreach (float s in new[] { -1f, 1f })
            {
                SB.Prim(PrimitiveType.Cube, "Wainscot", env, new Vector3(s * (HalfW - 0.03f), 0.5f, midZ), new Vector3(0.06f, 1.0f, depth), panel, false);
                SB.Prim(PrimitiveType.Cube, "WainscotTrim", env, new Vector3(s * (HalfW - 0.04f), 1.02f, midZ), new Vector3(0.09f, 0.04f, depth), trim, false);
            }
            SB.Prim(PrimitiveType.Cube, "Wainscot_N", env, new Vector3(0f, 0.5f, BackZ - 0.03f), new Vector3(width, 1.0f, 0.06f), panel, false);
            SB.Prim(PrimitiveType.Cube, "WainscotTrim_N", env, new Vector3(0f, 1.02f, BackZ - 0.04f), new Vector3(width, 0.04f, 0.09f), trim, false);

            // Neones perimetrales (laten con la música)
            Neon(env, "Neon_N_Top", new Vector3(0f, 3.3f, BackZ - 0.03f), new Vector3(width - 0.4f, 0.05f, 0.04f), teal);
            Neon(env, "Neon_N_Low", new Vector3(0f, 1.1f, BackZ - 0.09f), new Vector3(width - 0.4f, 0.04f, 0.04f), pink);
            Neon(env, "Neon_W", new Vector3(-HalfW + 0.03f, 3.3f, midZ), new Vector3(0.04f, 0.05f, depth - 0.4f), pink);
            Neon(env, "Neon_E", new Vector3(HalfW - 0.03f, 3.3f, midZ), new Vector3(0.04f, 0.05f, depth - 0.4f), teal);
            Neon(env, "Neon_W_Low", new Vector3(-HalfW + 0.09f, 1.1f, midZ), new Vector3(0.04f, 0.04f, depth - 0.4f), violet);
            Neon(env, "Neon_E_Low", new Vector3(HalfW - 0.09f, 1.1f, midZ), new Vector3(0.04f, 0.04f, depth - 0.4f), amber);
            Neon(env, "Neon_Step", new Vector3(0f, 0.012f, split), new Vector3(width - 0.2f, 0.02f, 0.05f), pink);

            // Luces generales: sala clara, nunca a oscuras
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.47f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.37f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.25f);
            RenderSettings.fog = false;

            var lights = new List<Light>();
            var sun = new GameObject("Fill Light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 0.45f; sun.shadows = LightShadows.None;
            sun.color = new Color(0.92f, 0.86f, 1f);
            sun.transform.rotation = Quaternion.Euler(50f, 20f, 0f);
            sun.transform.SetParent(env);
            lights.Add(sun);

            foreach (float x in new[] { -1.6f, 0f, 1.6f })
            {
                var l = PointLight(env, "Bar Lamp", new Vector3(x, 2.55f, 0.75f), new Color(1f, 0.86f, 0.68f), 1.25f, 6f);
                l.shadows = x == 0f ? LightShadows.Soft : LightShadows.None;
                lights.Add(l);
                SB.Fit("FurnitureKit", "lampSquareCeiling", env, new Vector3(x, 2.75f, 0.75f), 0.4f, 0f).name = "CeilingLamp";
            }
            lights.Add(PointLight(env, "Lounge Lamp W", new Vector3(-4.1f, 2.3f, 0.6f), new Color(1f, 0.8f, 0.62f), 1.0f, 5.5f));
            lights.Add(PointLight(env, "Lounge Lamp E", new Vector3(4.1f, 2.3f, 0.4f), new Color(1f, 0.8f, 0.62f), 1.0f, 5.5f));
            lights.Add(PointLight(env, "Accent Teal", new Vector3(-4.2f, 1.6f, 2.6f), Teal, 0.8f, 4.5f));
            lights.Add(PointLight(env, "Accent Pink", new Vector3(4.2f, 1.6f, 2.6f), Pink, 0.8f, 4.5f));
            lights.Add(PointLight(env, "BackBar Lamp", new Vector3(0.3f, 2.4f, -2.1f), new Color(1f, 0.82f, 0.6f), 0.9f, 4.5f));

            BuildBarZone(env);
            BuildBackBar(env, teal, pink, amber);
            BuildDanceZone(env, lights, teal, pink, amber, violet);
            BuildSigns(env);
            BuildScreenFrames(env, pink, teal);
            return lights.ToArray();
        }

        static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color; l.shadows = LightShadows.None;
            l.transform.position = pos;
            l.transform.SetParent(parent);
            return l;
        }

        static GameObject Neon(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool pulse = true)
        {
            var go = SB.Prim(PrimitiveType.Cube, name, parent, pos, scale, mat, false);
            if (pulse) pulseNeons.Add(go.GetComponent<Renderer>());
            return go;
        }

        /// <summary>Pone un modelo sobre otro (encima de su punto más alto).</summary>
        static GameObject OnTop(GameObject support, string kit, string model, Transform parent, float dx, float dz, float size, float yaw)
        {
            var b = SB.Bounds(support);
            return SB.Fit(kit, model, parent, new Vector3(b.center.x + dx, b.max.y, b.center.z + dz), size, yaw, allAxes: true);
        }

        // ---------------------------------------------------------------------
        // Zona de barra, sala de estar y mesas altas
        // ---------------------------------------------------------------------

        static void BuildBarZone(Transform env)
        {
            foreach (float x in new[] { -2.2f, -1.1f, 1.1f, 2.25f })
                SB.Solid(SB.Fit("FurnitureKit", "stoolBar", env, new Vector3(x, 0f, SeatZ), 0.4f, 0f));
            SB.Fit("FurnitureKit", "rugRectangle", env, new Vector3(0f, 0.001f, 2.1f), 2.6f, 0f);

            // Sala de estar (oeste)
            SB.Solid(SB.Fit("FurnitureKit", "loungeSofa", env, new Vector3(-4.5f, 0f, 0.3f), 1.7f, 90f));
            var coffee = SB.Solid(SB.Fit("FurnitureKit", "tableCoffee", env, new Vector3(-3.65f, 0f, 0.3f), 0.8f, 90f));
            OnTop(coffee, "FoodKit", "cocktail", env, 0f, 0.15f, 0.13f, 0f);
            OnTop(coffee, "FoodKit", "glass-wine", env, 0.05f, -0.15f, 0.14f, 0f);
            SB.Fit("FurnitureKit", "rugRound", env, new Vector3(-3.7f, 0.001f, 0.3f), 1.9f, 0f);
            SB.Solid(SB.Fit("FurnitureKit", "lampRoundFloor", env, new Vector3(-4.75f, 0f, -0.95f), 0.35f, 0f));
            SB.Solid(SB.Fit("FurnitureKit", "pottedPlant", env, new Vector3(-4.7f, 0f, 2.75f), 0.6f, 0f));
            SB.Solid(SB.Fit("FurnitureKit", "pottedPlant", env, new Vector3(4.7f, 0f, 2.75f), 0.6f, 0f));
            SB.Solid(SB.Fit("FurnitureKit", "pottedPlant", env, new Vector3(4.7f, 0f, -2.55f), 0.6f, 0f));

            // Mesas (este)
            foreach (float z in new[] { 1.5f, -0.7f })
            {
                var table = SB.Solid(SB.Fit("FurnitureKit", "tableRound", env, new Vector3(4.1f, 0f, z), 0.85f, 0f));
                OnTop(table, "FoodKit", "cocktail", env, -0.15f, 0.08f, 0.13f, 0f);
                OnTop(table, "FoodKit", "glass-wine", env, 0.14f, -0.1f, 0.14f, 0f);
                SB.Fit("FurnitureKit", "rugRound", env, new Vector3(4.1f, 0.001f, z), 1.7f, 0f);
            }
        }

        // ---------------------------------------------------------------------
        // Barra trasera: muebles, repisas iluminadas y botellas
        // ---------------------------------------------------------------------

        static void BuildBackBar(Transform env, Material teal, Material pink, Material amber)
        {
            var root = new GameObject("BackBar").transform;
            root.SetParent(env);
            SB.Solid(SB.Fit("FurnitureKit", "kitchenFridge", root, new Vector3(-2.65f, 0f, -2.55f), 0.9f, 0f));
            string[] row = { "kitchenCabinet", "kitchenSink", "kitchenCabinetDrawer", "kitchenCabinet", "kitchenCabinetDrawer" };
            GameObject coffeeBase = null;
            for (int i = 0; i < row.Length; i++)
            {
                var c = SB.Solid(SB.Fit("FurnitureKit", row[i], root, new Vector3(-1.5f + 0.9f * i, 0f, -2.6f), 0.9f, 0f));
                if (i == 3) coffeeBase = c;
            }
            if (coffeeBase != null) OnTop(coffeeBase, "FurnitureKit", "kitchenCoffeeMachine", root, 0f, 0f, 0.36f, 0f);
            SB.Solid(SB.Fit("FurnitureKit", "bookcaseClosedWide", root, new Vector3(3.75f, 0f, -2.7f), 1.2f, 0f));

            var back = SB.Mat("BackBarPanel", new Color(0.09f, 0.07f, 0.15f));
            var plank = SB.Mat("MixWainscot", new Color(0.27f, 0.16f, 0.12f));
            const float cx = 0.3f, w = 4.2f;
            SB.Prim(PrimitiveType.Cube, "Panel", root, new Vector3(cx, 1.95f, -2.97f), new Vector3(w + 0.3f, 1.55f, 0.04f), back, false);
            var strips = new[] { pink, teal, amber };
            Color[] bodies =
            {
                new Color(0.18f, 0.5f, 0.3f), new Color(0.72f, 0.42f, 0.16f), new Color(0.85f, 0.88f, 0.92f),
                new Color(0.16f, 0.3f, 0.62f), new Color(0.6f, 0.12f, 0.18f), new Color(0.93f, 0.8f, 0.4f),
            };
            Color[] labels = { Ink, new Color(0.1f, 0.1f, 0.14f), Pink, Amber, Ink, new Color(0.55f, 0.1f, 0.12f) };
            var rng = new System.Random(11);
            for (int s = 0; s < 3; s++)
            {
                float y = 1.4f + 0.42f * s;
                SB.Prim(PrimitiveType.Cube, "Shelf" + s, root, new Vector3(cx, y, -2.85f), new Vector3(w, 0.04f, 0.26f), plank, false);
                Neon(root, "ShelfLight" + s, new Vector3(cx, y - 0.03f, -2.73f), new Vector3(w, 0.02f, 0.02f), strips[s]);
                for (int k = 0; k < 13; k++)
                {
                    int kind = (k * 5 + s * 2) % bodies.Length;
                    var g = Group($"DecoBottle_{s}_{k}", root, new Vector3(cx - w * 0.5f + 0.18f + k * 0.32f, y + 0.02f, -2.86f));
                    ProcBottle(g, "Deco" + kind, bodies[kind], labels[kind], kind % 2 == 0 ? Copper : new Color(0.12f, 0.12f, 0.14f),
                        0.036f + 0.01f * (float)rng.NextDouble(), 0.24f + 0.08f * (float)rng.NextDouble());
                }
            }
        }

        // ---------------------------------------------------------------------
        // Pista de baile, cabina del DJ y luces de discoteca
        // ---------------------------------------------------------------------

        static void BuildDanceZone(Transform env, List<Light> lights, Material teal, Material pink, Material amber, Material violet)
        {
            var root = new GameObject("--- PISTA ---").transform;
            var metal = SB.Mat("TrussMetal", new Color(0.12f, 0.12f, 0.15f));

            // Baldosas luminosas
            var tileMat = SB.Mat("DanceTile", new Color(0.5f, 0.5f, 0.55f), emissive: true);
            var tiles = new List<Renderer>();
            const float size = 0.7f;
            float x0 = -(TileCols - 1) * size * 0.5f, z0 = DanceCenter.z - (TileRows - 1) * size * 0.5f;
            SB.Prim(PrimitiveType.Cube, "TileBed", root, new Vector3(0f, 0.004f, DanceCenter.z), new Vector3(TileCols * size + 0.1f, 0.01f, TileRows * size + 0.1f), metal, false);
            for (int r = 0; r < TileRows; r++)
                for (int c = 0; c < TileCols; c++)
                {
                    var t = SB.Prim(PrimitiveType.Cube, $"Tile_{r}_{c}", root, new Vector3(x0 + c * size, 0.015f, z0 + r * size), new Vector3(size - 0.04f, 0.03f, size - 0.04f), tileMat, false);
                    tiles.Add(t.GetComponent<Renderer>());
                }
            discoTiles = tiles.ToArray();

            // Estructura del techo, bola de espejos y focos
            float hx = TileCols * size * 0.5f + 0.2f, hz = TileRows * size * 0.5f + 0.2f;
            foreach (float s in new[] { -1f, 1f })
            {
                SB.Prim(PrimitiveType.Cube, "Truss_X", root, new Vector3(0f, 3.28f, DanceCenter.z + s * hz), new Vector3(hx * 2f + 0.1f, 0.08f, 0.08f), metal, false);
                SB.Prim(PrimitiveType.Cube, "Truss_Z", root, new Vector3(s * hx, 3.28f, DanceCenter.z), new Vector3(0.08f, 0.08f, hz * 2f + 0.1f), metal, false);
            }
            var ballMat = SB.Mat("MirrorBall", new Color(0.82f, 0.84f, 0.9f), emissive: true);
            ballMat.SetFloat("_Metallic", 0.75f); ballMat.SetFloat("_Glossiness", 0.9f);
            ballMat.SetColor("_EmissionColor", new Color(0.22f, 0.22f, 0.27f));
            SB.Prim(PrimitiveType.Cube, "BallRod", root, new Vector3(0f, 3.25f, DanceCenter.z), new Vector3(0.025f, 0.3f, 0.025f), metal, false);
            mirrorBall = SB.Prim(PrimitiveType.Sphere, "MirrorBall", root, new Vector3(0f, 2.92f, DanceCenter.z), Vector3.one * 0.44f, ballMat, false).transform;
            // Facetas: anillos oscuros que hacen visible el giro
            for (int i = 0; i < 4; i++)
            {
                var ring = SB.Prim(PrimitiveType.Cylinder, "Facet" + i, mirrorBall, Vector3.zero, new Vector3(1.02f, 0.012f, 1.02f), metal, false, local: true);
                ring.transform.localRotation = Quaternion.Euler(i < 2 ? 0f : 90f, i * 45f, i == 1 ? 90f : 0f);
            }

            ballLight = new GameObject("MirrorBall Sparkles").AddComponent<Light>();
            ballLight.type = LightType.Spot; ballLight.spotAngle = 150f; ballLight.range = 9f; ballLight.intensity = 2.4f;
            ballLight.color = new Color(1f, 0.97f, 0.9f); ballLight.shadows = LightShadows.None; ballLight.renderMode = LightRenderMode.ForcePixel;
            ballLight.transform.SetParent(root);
            ballLight.transform.SetPositionAndRotation(new Vector3(0f, 2.9f, DanceCenter.z), Quaternion.Euler(90f, 0f, 0f));

            var spots = new List<Light>();
            Color[] spotColors = { Pink, Teal, Amber, Violet };
            int n = 0;
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    var pos = new Vector3(sx * hx, 3.18f, DanceCenter.z + sz * hz);
                    var l = new GameObject("Disco Spot " + n).AddComponent<Light>();
                    l.type = LightType.Spot; l.spotAngle = 44f; l.range = 7.5f; l.intensity = 3.4f; l.color = spotColors[n];
                    l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
                    l.transform.SetParent(root);
                    l.transform.position = pos;
                    l.transform.LookAt(new Vector3(-sx * 0.7f, 0f, DanceCenter.z - sz * 0.5f));
                    var can = SB.Prim(PrimitiveType.Cylinder, "SpotCan", l.transform, new Vector3(0f, 0f, -0.04f), new Vector3(0.14f, 0.09f, 0.14f), metal, false, local: true);
                    can.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    spots.Add(l);
                    n++;
                }
            discoSpots = spots.ToArray();

            // Relleno de la pista (cambia de tono, pero siempre encendido)
            var fillA = PointLight(root, "Dance Fill A", new Vector3(-2.4f, 2.6f, DanceCenter.z + 0.2f), Violet, 1.15f, 7f);
            var fillB = PointLight(root, "Dance Fill B", new Vector3(2.4f, 2.6f, DanceCenter.z + 0.2f), Pink, 1.15f, 7f);
            discoFills = new[] { fillA, fillB };
            lights.Add(fillA); lights.Add(fillB);

            // Cabina del DJ
            var riserMat = SB.Mat("DjRiser", new Color(0.13f, 0.1f, 0.2f));
            SB.Prim(PrimitiveType.Cube, "DjRiser", root, new Vector3(0f, 0.125f, 6.75f), new Vector3(3.6f, 0.25f, 0.9f), riserMat);
            Neon(root, "DjRiserNeon", new Vector3(0f, 0.2f, 6.29f), new Vector3(3.6f, 0.04f, 0.02f), pink);
            var desk = SB.Solid(SB.Fit("FurnitureKit", "desk", root, new Vector3(0f, 0.25f, 6.55f), 1.5f, 0f));
            OnTop(desk, "FurnitureKit", "laptop", root, -0.35f, 0f, 0.34f, 0f);
            OnTop(desk, "FurnitureKit", "radio", root, 0.3f, 0f, 0.34f, 0f);
            foreach (float s in new[] { -1f, 1f })
            {
                SB.Solid(SB.Fit("FurnitureKit", "speaker", root, new Vector3(s * 2.45f, 0f, 6.75f), 0.42f, 180f));
                SB.Solid(SB.Fit("FurnitureKit", "speakerSmall", root, new Vector3(s * 3.1f, 0f, 6.8f), 0.4f, 180f));
                SB.Solid(SB.Fit("FurnitureKit", "speaker", root, new Vector3(s * 4.7f, 0f, 3.75f), 0.36f, s > 0 ? -90f : 90f));
            }

            // Ecualizador de pared (se mueve con el sonido real de la pista)
            var eq = Group("Equalizer", root, new Vector3(0f, 1.3f, BackZ - 0.12f));
            var bars = new List<Transform>();
            var eqMats = new[] { teal, violet, pink, amber };
            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                var bar = L(PrimitiveType.Cube, "Bar" + i, eq, new Vector3(-1.65f + i * 0.22f, 0.55f, 0f), new Vector3(0.15f, 1.1f, 0.04f), eqMats[i * eqMats.Length / count]);
                bars.Add(bar.transform);
            }
            eqBars = bars.ToArray();
        }

        // ---------------------------------------------------------------------
        // Letreros de neón y pantallas colgantes
        // ---------------------------------------------------------------------

        static void BuildSigns(Transform env)
        {
            var root = new GameObject("Signs").transform;
            root.SetParent(env);
            Quaternion north = Quaternion.identity, south = Quaternion.Euler(0f, 180f, 0f);
            Quaternion east = Quaternion.Euler(0f, 90f, 0f), west = Quaternion.Euler(0f, -90f, 0f);
            NeonSign(root, "Sign_MixLab", new Vector3(0f, 2.92f, BackZ - 0.06f), north, "MIX LAB", Pink, "NeonTeal", 2.6f, 0.58f, 150);
            NeonSign(root, "Sign_Bar", new Vector3(0.3f, 2.98f, SouthZ + 0.16f), south, "★  BAR  ★", Amber, "NeonPink", 1.7f, 0.42f, 120);
            NeonSign(root, "Sign_HappyHour", new Vector3(HalfW - 0.06f, 2.25f, 0.4f), east, "HAPPY HOUR\n<size=70>2 × 1 hasta las 9</size>", Teal, "NeonAmber", 1.7f, 0.85f, 105);
            NeonSign(root, "Sign_Salud", new Vector3(-HalfW + 0.06f, 2.3f, 0.3f), west, "¡SALUD!", Pink, "NeonViolet", 1.5f, 0.6f, 150);
            NeonSign(root, "Sign_House", new Vector3(HalfW - 0.06f, 2.3f, 5.0f), east, "HOUSE NIGHT\n<size=70>DJ en vivo</size>", Violet, "NeonPink", 1.8f, 0.85f, 105);
            NeonSign(root, "Sign_Baila", new Vector3(-HalfW + 0.06f, 2.3f, 5.0f), west, "BAILA\n<size=70>que la barra invita</size>", Amber, "NeonTeal", 1.8f, 0.85f, 120);
            NeonSign(root, "Sign_ScreenBack", new Vector3(0f, 2.52f, WallZ + 0.012f), south, "LABORATORIO\nDE MEZCLAS", Teal, "NeonPink", 2.5f, 1.1f, 150);
        }

        /// <summary>Letrero de neón: texto brillante dentro de un marco de tubos luminosos.</summary>
        static void NeonSign(Transform parent, string name, Vector3 pos, Quaternion rot, string text, Color color, string frameMat,
                             float w, float h, int fontSize)
        {
            const float scale = 0.002f;
            var size = new Vector2(w / scale, h / scale);
            var canvas = SB.MakeCanvas(name, pos, rot, size, scale);
            canvas.transform.SetParent(parent, true);
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            foreach (var extra in canvas.GetComponents<MonoBehaviour>())
                if (extra != null && extra.GetType().Name == "TrackedDeviceGraphicRaycaster") Object.DestroyImmediate(extra);
            SB.Panel(canvas.transform, "Bg", new Color(0.05f, 0.04f, 0.1f, 0.85f)).raycastTarget = false;
            var label = SB.Label(canvas.transform, "Text", "<b>" + text + "</b>", fontSize, TextAnchor.MiddleCenter, color,
                Vector2.zero, size, absolute: true);
            label.raycastTarget = false; label.lineSpacing = 0.9f;

            var frame = new GameObject(name + "_Frame").transform;
            frame.SetParent(parent);
            frame.SetPositionAndRotation(pos, rot);
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{frameMat}.mat");
            foreach (float s in new[] { -1f, 1f })
            {
                pulseNeons.Add(L(PrimitiveType.Cube, "H", frame, new Vector3(0f, s * (h * 0.5f + 0.02f), 0f), new Vector3(w + 0.07f, 0.03f, 0.03f), mat).GetComponent<Renderer>());
                pulseNeons.Add(L(PrimitiveType.Cube, "V", frame, new Vector3(s * (w * 0.5f + 0.02f), 0f, 0f), new Vector3(0.03f, h + 0.07f, 0.03f), mat).GetComponent<Renderer>());
            }
        }

        /// <summary>Los tableros del juego cuelgan del techo como pantallas, con la pista detrás.</summary>
        static void BuildScreenFrames(Transform env, Material pink, Material teal)
        {
            var root = new GameObject("ScreenFrames").transform;
            root.SetParent(env);
            var frame = SB.Mat("ScreenFrame", new Color(0.06f, 0.05f, 0.1f));
            // (centro x, centro y, ancho, alto) de MainBoard, ReactionBook y TutorialBoard
            var screens = new[] { new Vector4(0f, 2.52f, 2.72f, 1.312f), new Vector4(-2.4f, 2.32f, 1.61f, 1.75f), new Vector4(2.4f, 2.4f, 1.61f, 1.4f) };
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                float z = WallZ - 0.02f, top = s.y + s.w * 0.5f + 0.05f;
                SB.Prim(PrimitiveType.Cube, "Screen" + i, root, new Vector3(s.x, s.y, z), new Vector3(s.z + 0.1f, s.w + 0.1f, 0.05f), frame, false);
                Neon(root, "ScreenGlowTop" + i, new Vector3(s.x, top + 0.015f, z), new Vector3(s.z + 0.1f, 0.03f, 0.07f), i == 0 ? pink : teal, false);
                Neon(root, "ScreenGlowBottom" + i, new Vector3(s.x, s.y - s.w * 0.5f - 0.065f, z), new Vector3(s.z + 0.1f, 0.03f, 0.07f), i == 0 ? pink : teal, false);
                foreach (float side in new[] { -1f, 1f })
                    SB.Prim(PrimitiveType.Cube, "ScreenRod" + i, root, new Vector3(s.x + side * s.z * 0.35f, (top + CeilY) * 0.5f, z),
                        new Vector3(0.03f, Mathf.Max(0.02f, CeilY - top), 0.03f), frame, false);
            }
        }

        // =====================================================================
        // Música y director de luces
        // =====================================================================

        static void BuildDisco(GameObject managers)
        {
            var go = new GameObject("Disco (música y luces)");
            go.transform.SetParent(managers.transform);
            var music = SB.AddAudio(go, 0f);
            music.clip = MixMusicGenerator.EnsureClip();
            music.loop = true; music.playOnAwake = true; music.volume = 0.3f; music.priority = 0;
            var disco = go.AddComponent<DiscoLights>();
            disco.Setup(music, MixMusicGenerator.Bpm, discoTiles, TileCols, discoSpots, discoFills, mirrorBall, ballLight, eqBars, pulseNeons.ToArray());
            EditorUtility.SetDirty(disco);
        }

        // =====================================================================
        // Clientes de fondo (Kenney Mini Characters, animaciones Legacy)
        // =====================================================================

        static readonly string[] DanceMoves = { "walk", "emote-yes", "interact-right", "interact-left", "jump" };
        static readonly string[] DjMoves = { "interact-right", "interact-left", "emote-yes" };

        static void BuildPatrons()
        {
            var root = new GameObject("--- CLIENTES DE FONDO ---").transform;
            // En los extremos de la barra (lejos del estante, para no tapar las etiquetas)
            BuildPatron(root, "character-female-b", new Vector3(-2.2f, 0f, SeatZ), 165f, "sit", 0.4f);
            BuildPatron(root, "character-male-d", new Vector3(2.25f, 0f, SeatZ), 195f, "sit", 0.4f);
            SB.Fit("FoodKit", "cocktail", root, new Vector3(-2.2f, Top, 1.0f), 0.13f, 0f, allAxes: true);
            SB.Fit("FoodKit", "glass-wine", root, new Vector3(2.25f, Top, 1.02f), 0.14f, 0f, allAxes: true);
            // Sala de estar
            BuildPatron(root, "character-female-a", new Vector3(-4.45f, 0f, 0.65f), 90f, "sit", 0.12f);
            BuildPatron(root, "character-male-a", new Vector3(-4.45f, 0f, -0.05f), 90f, "sit", 0.12f);
            BuildPatron(root, "character-male-e", new Vector3(-4.2f, 0f, 2.2f), 130f, "idle", 0f);
            // Mesas del lado este
            BuildPatron(root, "character-male-b", new Vector3(3.45f, 0f, 1.65f), 100f, "idle", 0f);
            BuildPatron(root, "character-female-e", new Vector3(4.7f, 0f, 1.35f), 280f, "idle", 0f);
            BuildPatron(root, "character-female-c", new Vector3(3.5f, 0f, -0.45f), 110f, "idle", 0f);
            BuildPatron(root, "character-male-c", new Vector3(4.7f, 0f, -0.95f), 290f, "idle", 0f);
            // Pista de baile
            const float f = 0.03f;
            BuildPatron(root, "character-female-d", new Vector3(-1.45f, 0f, 4.0f), 150f, "idle", f, DanceMoves);
            BuildPatron(root, "character-male-a", new Vector3(-0.5f, 0f, 4.45f), 215f, "idle", f, DanceMoves);
            BuildPatron(root, "character-female-b", new Vector3(0.75f, 0f, 3.95f), 190f, "idle", f, DanceMoves);
            BuildPatron(root, "character-male-e", new Vector3(1.65f, 0f, 4.75f), 235f, "idle", f, DanceMoves);
            BuildPatron(root, "character-female-e", new Vector3(-1.75f, 0f, 5.35f), 120f, "idle", f, DanceMoves);
            BuildPatron(root, "character-male-b", new Vector3(-0.2f, 0f, 5.55f), 175f, "idle", f, DanceMoves);
            BuildPatron(root, "character-female-c", new Vector3(1.0f, 0f, 5.65f), 200f, "idle", f, DanceMoves);
            // DJ
            BuildPatron(root, "character-male-d", new Vector3(0f, 0f, 6.95f), 180f, "idle", 0.25f, DjMoves, 0.025f, 7f);
        }

        static void BuildPatron(Transform parent, string model, Vector3 pos, float yaw, string clip, float feetHeight,
                                string[] dance = null, float bounce = 0.05f, float twist = 20f)
        {
            string path = $"Assets/ThirdParty/Kenney/MiniCharacters/{model}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { Debug.LogWarning("[MixSceneBuilder] Falta personaje: " + path); return; }
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

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f); col.height = 1.5f; col.radius = 0.25f;
            if (dance != null) root.AddComponent<Dancer>().Setup(dance, bounce, twist);
            else root.AddComponent<Patron>().Setup(clip);
        }
    }
}
