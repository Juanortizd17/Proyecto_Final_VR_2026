using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Genera la escena completa "QualityControl.unity" de forma reproducible:
    /// nave industrial, banda transportadora, 5 productos (prefabs), 3 herramientas,
    /// 6 contenedores, zona de onboarding, tableros UI XR, audio y rig XR.
    /// Menú: Tools > Quality Control XR > Build Scene.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/QualityControl.unity";
        const string Kenney = "Assets/ThirdParty/Kenney";
        const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.0.8/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string SimulatorPath = "Assets/Samples/XR Interaction Toolkit/3.0.8/XR Device Simulator/XR Device Simulator.prefab";

        // Distribución (metros). El jugador trabaja mirando +Z hacia la banda.
        const float BeltTopY = 0.9f;
        const float BeltZ = 0.95f;
        const float BeltWidth = 0.8f;
        const float BeltHalfLength = 2.4f;
        static readonly Vector3 WorkSpot = new Vector3(0f, 0f, -0.15f);
        static readonly Vector3 PlayerStart = new Vector3(0f, 0f, -4.3f);

        internal static Font font;
        internal static Camera rigCamera;

        internal struct ProductDef { public ProductType type; public string fbx; public Color color; }
        internal static readonly ProductDef[] Products =
        {
            new ProductDef { type = ProductType.Radio, fbx = "radio", color = new Color(0.95f, 0.55f, 0.2f) },
            new ProductDef { type = ProductType.Lamp, fbx = "lampSquareTable", color = new Color(0.95f, 0.85f, 0.3f) },
            new ProductDef { type = ProductType.Speaker, fbx = "speakerSmall", color = new Color(0.4f, 0.6f, 1f) },
            new ProductDef { type = ProductType.Television, fbx = "televisionVintage", color = new Color(0.7f, 0.45f, 0.95f) },
            new ProductDef { type = ProductType.Bear, fbx = "bear", color = new Color(0.85f, 0.6f, 0.45f) },
        };

        /// <summary>
        /// true = juego de PC (primera persona, mouse y teclado, sin gafas).
        /// false = rig XR para Meta Quest (para la conversión a VR).
        /// </summary>
        public static bool DesktopMode = true;

        [MenuItem("Tools/Quality Control XR/Build Scene (PC)")]
        public static void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureFolder("Assets/Materials"); EnsureFolder("Assets/Prefabs"); EnsureFolder("Assets/Prefabs/Products");
            EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- Audio ----------
            var clipOk = Clip("confirmation"); var clipFail = Clip("error");
            var clipStep = Clip("select"); var clipWarn = Clip("glitch");
            var clipFound = Clip("impactBell_heavy"); var clipTap = Clip("impactPlate_light");
            var clipScrew = Clip("impactTin_medium"); var clipHammer = Clip("impactMetal_medium");
            var clipLens = Clip("glass"); var clipAlarm = Clip("error");

            // ---------- Núcleo ----------
            var managers = new GameObject("--- GAME ---");
            var gm = new GameObject("GameManager");
            gm.transform.SetParent(managers.transform);
            gm.AddComponent<GameManager>();
            var uiAudio = AddAudio(gm, 0f);

            BuildLighting();
            BuildEnvironment();

            // ---------- Jugador ----------
            GameObject rig;
            if (DesktopMode)
            {
                rig = BuildDesktopPlayer(Clip("select"), Clip("impactSoft_medium"), Clip("click"));
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(managers.transform);
            }
            else
            {
                var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
                rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                rig.transform.SetPositionAndRotation(PlayerStart, Quaternion.identity);
                rigCamera = rig.GetComponentInChildren<Camera>(true);
                ConfigureDeviceSimulator();
                var fallback = new GameObject("DesktopFallback");
                fallback.transform.SetParent(managers.transform);
                fallback.AddComponent<DesktopFallback>().Setup(AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath));
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
                es.transform.SetParent(managers.transform);
            }
            new GameObject("AutoDemo (solo con -autodemo)").AddComponent<AutoDemo>().transform.SetParent(managers.transform);

            var workSpot = new GameObject("WorkSpot").transform;
            workSpot.SetParent(managers.transform);
            workSpot.position = WorkSpot;

            // ---------- Materiales y física ----------
            var grip = new PhysicsMaterial("Grip")
            {
                dynamicFriction = 0.9f, staticFriction = 1f,
                frictionCombine = PhysicsMaterialCombine.Maximum, bounceCombine = PhysicsMaterialCombine.Minimum
            };
            CreateOrReplace(grip, "Assets/Materials/Grip.asset");

            // ---------- Productos (prefabs) ----------
            var productPrefabs = Products.Select(p => BuildProductPrefab(p, grip)).ToArray();

            // ---------- Banda ----------
            var belt = BuildConveyor(productPrefabs, grip, clipAlarm, clipFail);

            // ---------- Herramientas ----------
            BuildToolTable(new Vector3(-1.2f, 0f, -0.05f), clipTap, clipScrew, clipHammer, clipLens, clipFound);

            // ---------- Contenedores ----------
            var bins = new GameObject("--- CONTENEDORES ---").transform;
            var rejectMat = Mat("BinReject", new Color(0.85f, 0.15f, 0.12f));
            BuildBin(bins, "Bin_Rechazo", new Vector3(-1.25f, 0f, -1.05f), -90f, rejectMat,
                "DESECHAR\n<size=34>Reportar daño</size>", BinKind.Reject, ProductType.Radio, null, clipOk, clipFail);
            for (int i = 0; i < Products.Length; i++)
            {
                var d = Products[i];
                BuildBin(bins, "Bin_" + d.type, new Vector3(1.2f, 0f, 0.25f - i * 0.62f), 90f,
                    Mat("Bin" + d.type, Color.Lerp(d.color, Color.gray, 0.25f)),
                    ProductBin.Es(d.type).ToUpper() + "\n<size=34>Aprobado</size>", BinKind.Accept, d.type, d.fbx, clipOk, clipFail);
            }

            // ---------- Tablero principal ----------
            var hud = BuildMainBoard(belt, uiAudio, clipWarn);
            hud.SetTeleport(rig.transform, workSpot);

            // ---------- Onboarding ----------
            BuildOnboarding(rig.transform, hud, uiAudio, clipStep, clipTap, clipFound);

            if (DesktopMode) new GameObject("DesktopHUD").AddComponent<DesktopHud>().transform.SetParent(managers.transform);

            // ---------- Guardar ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Escena generada: " + ScenePath);
        }

        [MenuItem("Tools/Quality Control XR/Build Scene (VR - Meta Quest)")]
        public static void BuildVr()
        {
            DesktopMode = false;
            try { Build(); } finally { DesktopMode = true; }
        }

        // =====================================================================
        // Jugador de PC
        // =====================================================================

        internal static GameObject BuildDesktopPlayer(AudioClip pick, AudioClip drop, AudioClip click)
        {
            var player = new GameObject("Player (PC)");
            player.layer = 2;   // Ignore Raycast: la mira no se apunta a sí misma
            player.transform.SetPositionAndRotation(PlayerStart, Quaternion.identity);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.25f; cc.slopeLimit = 45f; cc.skinWidth = 0.03f; cc.minMoveDistance = 0f;

            var camGo = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.03f; cam.farClipPlane = 60f;
            rigCamera = cam;

            var productHold = new GameObject("ProductHold").transform;
            productHold.SetParent(camGo.transform, false);
            productHold.localPosition = new Vector3(-0.05f, -0.2f, 0.55f);
            var toolHold = new GameObject("ToolHold").transform;
            toolHold.SetParent(camGo.transform, false);
            toolHold.localPosition = new Vector3(0.3f, -0.28f, 0.42f);

            var steps = AddAudio(player, 0f);
            var footsteps = AssetDatabase.FindAssets("footstep_concrete t:AudioClip", new[] { Kenney + "/Audio" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            player.AddComponent<DesktopPlayer>().Setup(camGo.transform, steps, footsteps);

            var handAudio = AddAudio(camGo, 0f);
            camGo.AddComponent<DesktopInteractor>().Setup(productHold, toolHold, handAudio, pick, drop, click);
            return player;
        }

        // =====================================================================
        // Entorno
        // =====================================================================

        internal static void BuildLighting()
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 35f;

            // Lámparas industriales (puntuales, sin sombras para rendimiento en Quest)
            foreach (var p in new[] { new Vector3(0f, 3.2f, 0.6f), new Vector3(0f, 3.2f, -3f) })
            {
                var l = new GameObject("Lamp Light").AddComponent<Light>();
                l.type = LightType.Point; l.range = 7f; l.intensity = 0.9f;
                l.color = new Color(1f, 0.92f, 0.8f);
                l.transform.position = p;
            }
        }

        internal static void BuildEnvironment()
        {
            var env = new GameObject("--- ENTORNO ---").transform;
            var floorMat = Mat("Floor", new Color(0.28f, 0.29f, 0.31f));
            var wallMat = Mat("Wall", new Color(0.72f, 0.74f, 0.76f));
            var stripeMat = Mat("SafetyYellow", new Color(1f, 0.78f, 0.1f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor"; floor.transform.SetParent(env);
            floor.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            floor.transform.position = new Vector3(0f, 0f, -1f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            // Paredes de la nave (12 x 12 m, 4 m de alto)
            Prim(PrimitiveType.Cube, "Wall_N", env, new Vector3(0f, 2f, 5f), new Vector3(12f, 4f, 0.2f), wallMat);
            Prim(PrimitiveType.Cube, "Wall_S", env, new Vector3(0f, 2f, -7f), new Vector3(12f, 4f, 0.2f), wallMat);
            Prim(PrimitiveType.Cube, "Wall_E", env, new Vector3(6f, 2f, -1f), new Vector3(0.2f, 4f, 12f), wallMat);
            Prim(PrimitiveType.Cube, "Wall_W", env, new Vector3(-6f, 2f, -1f), new Vector3(0.2f, 4f, 12f), wallMat);

            // Líneas de seguridad en el piso alrededor del puesto
            Prim(PrimitiveType.Cube, "Line_Front", env, new Vector3(0f, 0.005f, 0.42f), new Vector3(3.2f, 0.01f, 0.06f), stripeMat, false);
            Prim(PrimitiveType.Cube, "Line_L", env, new Vector3(-1.6f, 0.005f, -0.9f), new Vector3(0.06f, 0.01f, 2.7f), stripeMat, false);
            Prim(PrimitiveType.Cube, "Line_R", env, new Vector3(1.6f, 0.005f, -0.9f), new Vector3(0.06f, 0.01f, 2.7f), stripeMat, false);

            // Decoración Kenney Factory Kit
            // Decoración sólida: el jugador ya no atraviesa robots, máquinas ni cajas.
            Solid(Fit("FactoryKit", "robot-arm-a", env, new Vector3(-3.4f, 0f, 1.6f), 1.2f, 135f));
            Solid(Fit("FactoryKit", "robot-arm-b", env, new Vector3(3.6f, 0f, 1.8f), 1.2f, -135f));
            Solid(Fit("FactoryKit", "machine-fortified", env, new Vector3(-4.6f, 0f, 3.6f), 1.6f, 0f));
            Solid(Fit("FactoryKit", "machine-window", env, new Vector3(4.4f, 0f, 3.6f), 1.6f, 0f));
            Solid(Fit("FactoryKit", "box-large", env, new Vector3(4.8f, 0f, -2.2f), 0.9f, 20f));
            Solid(Fit("FactoryKit", "box-long", env, new Vector3(4.9f, 0f, -3.4f), 0.9f, 80f));
            Solid(Fit("FactoryKit", "box-small", env, new Vector3(4.2f, 0f, -2.9f), 0.5f, 10f));
            Solid(Fit("FactoryKit", "warning-orange", env, new Vector3(-1.9f, 0f, 1.9f), 0.35f, 0f));
            Solid(Fit("FactoryKit", "warning-orange", env, new Vector3(1.9f, 0f, 1.9f), 0.35f, 0f));
            Solid(Fit("FactoryKit", "crane", env, new Vector3(-4.4f, 0f, -4.6f), 2.2f, 90f));
            Fit("FactoryKit", "pipe-large-long", env, new Vector3(-5.6f, 2.6f, -1f), 1.4f, 0f);
            Fit("FactoryKit", "screen-wide", env, new Vector3(0f, 2.9f, 4.85f), 2.4f, 180f);
        }

        // =====================================================================
        // Banda transportadora
        // =====================================================================

        internal static ConveyorBelt BuildConveyor(Product[] prefabs, PhysicsMaterial grip, AudioClip alarmClip, AudioClip lostClip)
        {
            var root = new GameObject("--- BANDA ---").transform;
            var metal = Mat("MetalDark", new Color(0.18f, 0.19f, 0.21f));
            var rail = Mat("RailYellow", new Color(1f, 0.75f, 0.1f));

            // Tramos visuales (Kenney) repetidos a lo largo de X
            float yaw = 0f;
            var probe = Fit("FactoryKit", "conveyor-stripe", root, Vector3.zero, BeltWidth, yaw);
            var pb = Bounds(probe);
            if (pb.size.z < pb.size.x * 0.9f)   // aseguramos que el ancho quede en Z
            {
                Object.DestroyImmediate(probe);
                yaw = 90f;
                probe = Fit("FactoryKit", "conveyor-stripe", root, Vector3.zero, BeltWidth, yaw);
                pb = Bounds(probe);
            }
            float segLen = pb.size.x, segH = pb.size.y;
            Object.DestroyImmediate(probe);
            int count = Mathf.CeilToInt(BeltHalfLength * 2f / segLen);
            float total = count * segLen;
            float startX = -total / 2f;
            float bottomY = BeltTopY - segH;
            Debug.Log($"[SceneBuilder] Banda: {count} tramos de {segLen:F2} m (alto {segH:F2}), yaw {yaw}");
            for (int i = 0; i < count; i++)
                Fit("FactoryKit", "conveyor-stripe", root,
                    new Vector3(startX + segLen * (i + 0.5f), bottomY, BeltZ), BeltWidth, yaw).name = "BeltSegment_" + i;

            // Estructura (bastidor + patas)
            Prim(PrimitiveType.Cube, "Frame", root, new Vector3(0f, bottomY - 0.04f, BeltZ),
                new Vector3(total, 0.08f, BeltWidth * 0.95f), metal);
            for (float x = startX + 0.15f; x <= -startX; x += 1.2f)
                foreach (float dz in new[] { -0.33f, 0.33f })
                    Prim(PrimitiveType.Cube, "Leg", root, new Vector3(x, (bottomY - 0.08f) / 2f, BeltZ + dz),
                        new Vector3(0.07f, bottomY - 0.08f, 0.07f), metal);

            // Barandas laterales (evitan que los productos caigan por los lados)
            foreach (float dz in new[] { -BeltWidth / 2f, BeltWidth / 2f })
                Prim(PrimitiveType.Cube, "Rail", root, new Vector3(0f, BeltTopY + 0.04f, BeltZ + dz),
                    new Vector3(total, 0.08f, 0.04f), rail);

            // Superficie física de la banda (Rigidbody cinemático que arrastra los productos)
            var surface = new GameObject("BeltSurface");
            surface.transform.SetParent(root);
            surface.transform.position = new Vector3(0f, BeltTopY - 0.02f, BeltZ);
            var col = surface.AddComponent<BoxCollider>();
            col.size = new Vector3(total, 0.04f, BeltWidth - 0.06f);
            col.sharedMaterial = grip;
            var rb = surface.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            var belt = surface.AddComponent<ConveyorBelt>();

            // Máquina de salida (de donde aparecen los productos) y punto de aparición
            Solid(Fit("FactoryKit", "machine", root, new Vector3(startX - 0.45f, 0f, BeltZ), 1.1f, 90f));
            var spawn = new GameObject("SpawnPoint").transform;
            spawn.SetParent(root);
            spawn.position = new Vector3(startX + 0.35f, BeltTopY + 0.22f, BeltZ);

            var alarm = AddAudio(surface, 1f);
            alarm.clip = alarmClip;
            belt.Setup(spawn, prefabs, alarm);

            // Zona de pérdida al final de la banda: producto no clasificado = fallo
            Solid(Fit("FactoryKit", "hopper-square", root, new Vector3(-startX + 0.45f, 0f, BeltZ), 0.95f, 0f));
            var lost = new GameObject("LostZone_EndOfBelt");
            lost.transform.SetParent(root);
            lost.transform.position = new Vector3(-startX + 0.45f, 0.7f, BeltZ);
            var lc = lost.AddComponent<BoxCollider>();
            lc.isTrigger = true; lc.size = new Vector3(0.8f, 1.4f, 1.2f);   // cubre también la tapa de la tolva
            var lostAudio = AddAudio(lost, 1f); lostAudio.clip = lostClip;
            lost.AddComponent<LostProductZone>().Setup("llegó al final sin ser inspeccionado", lostAudio);
            EditorUtility.SetDirty(belt);
            return belt;
        }

        // =====================================================================
        // Productos
        // =====================================================================

        internal static Product BuildProductPrefab(ProductDef def, PhysicsMaterial grip)
        {
            var root = new GameObject("Product_" + def.type);
            var model = Fit("FurnitureKit", def.fbx, root.transform, Vector3.zero, 0.26f, 0f, allAxes: true);
            model.name = "Model";
            var b = Bounds(model);

            var box = root.AddComponent<BoxCollider>();
            box.center = b.center; box.size = b.size;
            box.sharedMaterial = grip;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.useDynamicAttach = true;
            grab.throwOnDetach = true;
            root.AddComponent<Grabbable>().SetName(ProductBin.Es(def.type));

            var bad = Prim(PrimitiveType.Sphere, "DefectMarker", root.transform,
                new Vector3(b.center.x, b.max.y + 0.07f, b.center.z), Vector3.one * 0.07f,
                Mat("MarkerDefect", new Color(1f, 0.1f, 0.1f), emissive: true), false);
            var ok = Prim(PrimitiveType.Sphere, "OkMarker", root.transform,
                new Vector3(b.center.x, b.max.y + 0.07f, b.center.z), Vector3.one * 0.07f,
                Mat("MarkerOk", new Color(0.1f, 1f, 0.3f), emissive: true), false);

            var product = root.AddComponent<Product>();
            product.SetMarkers(bad, ok);
            product.Configure(def.type, DefectType.None);

            string path = $"Assets/Prefabs/Products/Product_{def.type}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Product>();
        }

        // =====================================================================
        // Herramientas
        // =====================================================================

        internal static void BuildToolTable(Vector3 pos, AudioClip tap, AudioClip screw, AudioClip hammer, AudioClip lens, AudioClip found)
        {
            var root = new GameObject("--- HERRAMIENTAS ---").transform;
            var wood = Mat("TableTop", new Color(0.45f, 0.32f, 0.2f));
            var metal = Mat("MetalDark", new Color(0.18f, 0.19f, 0.21f));
            // Mesa de 1.0 m de fondo, separada de la banda (antes se metía debajo de la baranda).
            float top = BuildTable(root, "ToolTable", pos, new Vector2(0.55f, 1.0f), 0.8f, wood, metal);

            // Las herramientas quedan acostadas hacia -X desde su raíz: se corre la raíz la mitad
            // de su largo para que queden centradas en la mesa, repartidas a lo ancho.
            BuildMagnifier(root, new Vector3(pos.x + 0.11f, top + 0.04f, pos.z + 0.3f), DefectType.Crack, lens, found);
            BuildScrewdriver(root, new Vector3(pos.x + 0.11f, top + 0.04f, pos.z), screw, found);
            BuildHammer(root, new Vector3(pos.x + 0.15f, top + 0.04f, pos.z - 0.3f), hammer, found);

            var sign = MakeCanvas("ToolSign", new Vector3(pos.x - 0.32f, 1.35f, pos.z), Quaternion.Euler(0f, -90f, 0f),
                new Vector2(620, 330), 0.001f);
            Panel(sign.transform, "Bg", new Color(0.08f, 0.09f, 0.11f, 0.92f));
            Label(sign.transform, "Text",
                "<b>HERRAMIENTAS</b>\n<color=#7FD8FF>Lupa</color> → grietas\n<color=#FFD54A>Destornillador</color> → tornillos flojos\n<color=#FF8A65>Martillo</color> → piezas huecas",
                38, TextAnchor.MiddleCenter, Color.white, new Vector2(20, 15), new Vector2(-20, -15));
        }

        internal static GameObject ToolRoot(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            return go;
        }

        internal static void FinishTool(GameObject go, DefectType detects, AudioClip use, AudioClip found, Vector3 tipPos, float tipRadius)
        {
            string toolName = detects switch
            {
                DefectType.Crack => "Lupa",
                DefectType.LooseScrew => "Destornillador",
                _ => "Martillo"
            };
            var tip = new GameObject("InspectionTip");
            tip.transform.SetParent(go.transform, false);
            tip.transform.localPosition = tipPos;
            var sc = tip.AddComponent<SphereCollider>();
            sc.isTrigger = true; sc.radius = tipRadius;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var grab = go.AddComponent<XRGrabInteractable>();
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            go.AddComponent<Grabbable>().SetName(toolName);
            var audio = AddAudio(go, 1f);
            go.AddComponent<InspectionTool>().Setup(detects, toolName, audio, use, found);
            go.AddComponent<ReturnHome>();

            // La herramienta se construye "de pie" (eje Y). Se acuesta sobre la mesa.
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        internal static GameObject BuildMagnifier(Transform parent, Vector3 pos, DefectType detects, AudioClip use, AudioClip found)
        {
            var go = ToolRoot(parent, "Tool_Lupa", pos);
            var handleMat = Mat("ToolHandleBlue", new Color(0.15f, 0.45f, 0.85f));
            Part(PrimitiveType.Cylinder, "Handle", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.032f, 0.06f, 0.032f), Quaternion.identity, handleMat);
            Part(PrimitiveType.Cylinder, "Rim", go.transform, new Vector3(0f, 0.175f, 0f), new Vector3(0.11f, 0.008f, 0.11f), Quaternion.Euler(90f, 0f, 0f), Mat("ToolMetal", new Color(0.75f, 0.76f, 0.8f)));
            Part(PrimitiveType.Cylinder, "Glass", go.transform, new Vector3(0f, 0.175f, 0f), new Vector3(0.095f, 0.01f, 0.095f), Quaternion.Euler(90f, 0f, 0f), Mat("Glass", new Color(0.6f, 0.9f, 1f, 0.35f), transparent: true), false);
            FinishTool(go, detects, use, found, new Vector3(0f, 0.175f, 0f), 0.065f);
            return go;
        }

        internal static void BuildScrewdriver(Transform parent, Vector3 pos, AudioClip use, AudioClip found)
        {
            var go = ToolRoot(parent, "Tool_Destornillador", pos);
            Part(PrimitiveType.Cylinder, "Handle", go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.036f, 0.05f, 0.036f), Quaternion.identity, Mat("ToolHandleYellow", new Color(1f, 0.8f, 0.1f)));
            Part(PrimitiveType.Cylinder, "Shaft", go.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.009f, 0.06f, 0.009f), Quaternion.identity, Mat("ToolMetal", new Color(0.75f, 0.76f, 0.8f)));
            Part(PrimitiveType.Cube, "Tip", go.transform, new Vector3(0f, 0.225f, 0f), new Vector3(0.012f, 0.012f, 0.004f), Quaternion.identity, Mat("ToolMetal", new Color(0.75f, 0.76f, 0.8f)));
            FinishTool(go, DefectType.LooseScrew, use, found, new Vector3(0f, 0.225f, 0f), 0.035f);
        }

        internal static void BuildHammer(Transform parent, Vector3 pos, AudioClip use, AudioClip found)
        {
            var go = ToolRoot(parent, "Tool_Martillo", pos);
            Part(PrimitiveType.Cylinder, "Handle", go.transform, new Vector3(0f, 0.14f, 0f), new Vector3(0.03f, 0.14f, 0.03f), Quaternion.identity, Mat("ToolHandleWood", new Color(0.55f, 0.35f, 0.18f)));
            Part(PrimitiveType.Cube, "Head", go.transform, new Vector3(0f, 0.29f, 0f), new Vector3(0.14f, 0.05f, 0.05f), Quaternion.identity, Mat("ToolHammerHead", new Color(0.35f, 0.36f, 0.4f)));
            FinishTool(go, DefectType.Hollow, use, found, new Vector3(0f, 0.29f, 0f), 0.06f);
        }

        // =====================================================================
        // Contenedores
        // =====================================================================

        internal static void BuildBin(Transform parent, string name, Vector3 pos, float yaw, Material color, string label,
                             BinKind kind, ProductType type, string previewFbx, AudioClip ok, AudioClip fail)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var dark = Mat("BinBase", new Color(0.2f, 0.21f, 0.23f));

            Prim(PrimitiveType.Cube, "Base", root, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f), dark, true, local: true);
            Prim(PrimitiveType.Cube, "Bottom", root, new Vector3(0f, 0.51f, 0f), new Vector3(0.5f, 0.02f, 0.5f), color, true, local: true);
            Prim(PrimitiveType.Cube, "Wall_F", root, new Vector3(0f, 0.62f, -0.24f), new Vector3(0.5f, 0.22f, 0.02f), color, true, local: true);
            Prim(PrimitiveType.Cube, "Wall_B", root, new Vector3(0f, 0.67f, 0.24f), new Vector3(0.5f, 0.32f, 0.02f), color, true, local: true);
            Prim(PrimitiveType.Cube, "Wall_L", root, new Vector3(-0.24f, 0.65f, 0f), new Vector3(0.02f, 0.28f, 0.5f), color, true, local: true);
            Prim(PrimitiveType.Cube, "Wall_R", root, new Vector3(0.24f, 0.65f, 0f), new Vector3(0.02f, 0.28f, 0.5f), color, true, local: true);

            var trigger = new GameObject("DropZone");
            trigger.transform.SetParent(root, false);
            trigger.transform.localPosition = new Vector3(0f, 0.66f, 0f);
            var tc = trigger.AddComponent<BoxCollider>();
            tc.isTrigger = true; tc.size = new Vector3(0.44f, 0.26f, 0.44f);
            var audio = AddAudio(trigger, 1f);
            trigger.AddComponent<ProductBin>().Setup(kind, type, audio, ok, fail);

            // Poste + letrero
            Prim(PrimitiveType.Cube, "Post", root, new Vector3(0f, 1.0f, 0.24f), new Vector3(0.03f, 0.35f, 0.03f), dark, false, local: true);
            var sign = MakeCanvas("Sign", Vector3.zero, Quaternion.identity, new Vector2(440, 140), 0.001f);
            sign.transform.SetParent(root, false);
            sign.transform.localPosition = new Vector3(0f, 1.27f, 0.22f);
            sign.transform.localRotation = Quaternion.identity;
            Panel(sign.transform, "Bg", kind == BinKind.Reject ? new Color(0.55f, 0.05f, 0.05f, 0.95f) : new Color(0.08f, 0.09f, 0.11f, 0.92f));
            Label(sign.transform, "Text", "<b>" + label + "</b>", 44, TextAnchor.MiddleCenter, Color.white, new Vector2(10, 6), new Vector2(-10, -6));

            if (!string.IsNullOrEmpty(previewFbx))
            {
                var holder = new GameObject("Preview").transform;
                holder.SetParent(root, false);
                holder.localPosition = new Vector3(0f, 0.95f, 0f);
                var m = Fit("FurnitureKit", previewFbx, holder, holder.position, 0.17f, 0f, allAxes: true);
                m.transform.SetParent(holder, true);
                holder.gameObject.AddComponent<Spin>();
            }
            else
            {
                Fit("FactoryKit", "warning-traffic", root, root.TransformPoint(new Vector3(0.35f, 0f, -0.1f)), 0.25f, yaw);
            }
        }

        // =====================================================================
        // UI
        // =====================================================================

        internal static HudController BuildMainBoard(ConveyorBelt belt, AudioSource uiAudio, AudioClip warn)
        {
            // Un poco más bajo e inclinado hacia el operario: se lee completo en VR y en pantalla de PC.
            var canvas = MakeCanvas("MainBoard", new Vector3(0f, 1.88f, 1.7f), Quaternion.Euler(-12f, 0f, 0f), new Vector2(1600, 720), 0.0012f);
            Panel(canvas.transform, "Bg", new Color(0.06f, 0.07f, 0.09f, 0.9f));
            Label(canvas.transform, "Header", "<b>LÍNEA DE INSPECCIÓN 01</b>  ·  UAO Immersive Solutions", 34, TextAnchor.UpperCenter,
                new Color(0.6f, 0.75f, 0.9f), new Vector2(20, 640), new Vector2(-20, -16), anchored: true);

            // Panel de juego
            var game = Child(canvas.transform, "GamePanel");
            var timer = Label(game, "Timer", "TIEMPO\n3:00", 64, TextAnchor.MiddleCenter, Color.white, new Vector2(40, 330), new Vector2(560, 640), absolute: true);
            var score = Label(game, "Score", "PUNTOS\n0", 64, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 0.55f), new Vector2(540, 330), new Vector2(1060, 640), absolute: true);
            var mistakes = Label(game, "Mistakes", "FALLOS\n0/5", 64, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.45f), new Vector2(1040, 330), new Vector2(1560, 640), absolute: true);
            var alert = Label(game, "Alert", "", 50, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.2f), new Vector2(40, 200), new Vector2(1560, 320), absolute: true);
            var report = Label(game, "Report", "Inspecciona cada producto y clasifícalo", 40, TextAnchor.MiddleCenter, Color.white, new Vector2(40, 40), new Vector2(1560, 190), absolute: true);

            // Panel de resultados
            var results = Child(canvas.transform, "ResultsPanel");
            var resultsText = Label(results, "Text", "", 40, TextAnchor.UpperCenter, Color.white, new Vector2(60, 150), new Vector2(1540, 640), absolute: true);

            // Panel de espera (durante el onboarding)
            var waiting = Child(canvas.transform, "WaitingPanel");
            Label(waiting, "Text", "Completa el <b>entrenamiento</b> detrás de ti para iniciar el turno", 48, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.9f, 1f), new Vector2(60, 100), new Vector2(1540, 600), absolute: true);

            var hud = canvas.gameObject.AddComponent<HudController>();
            MakeButton(results, "RestartButton", "REINICIAR", new Vector2(420, 40), new Vector2(780, 130), new Color(0.15f, 0.55f, 0.3f), hud.OnRestartPressed);
            MakeButton(results, "QuitButton", "SALIR", new Vector2(820, 40), new Vector2(1180, 130), new Color(0.55f, 0.15f, 0.15f), hud.OnQuitPressed);

            hud.Setup(timer, score, mistakes, report, alert, game.gameObject, waiting.gameObject, results.gameObject, resultsText, belt, uiAudio, warn);
            game.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            EditorUtility.SetDirty(hud);
            return hud;
        }

        internal static void BuildOnboarding(Transform rig, HudController hud, AudioSource uiAudio, AudioClip step, AudioClip tap, AudioClip found)
        {
            var root = new GameObject("--- ONBOARDING ---").transform;
            var wood = Mat("TableTop", new Color(0.45f, 0.32f, 0.2f));
            var metal = Mat("MetalDark", new Color(0.18f, 0.19f, 0.21f));
            var tablePos = new Vector3(0f, 0f, -3.25f);
            float top = BuildTable(root, "PracticeTable", tablePos, new Vector2(1.1f, 0.5f), 0.8f, wood, metal);

            // Caja de práctica
            var box = Prim(PrimitiveType.Cube, "PracticeBox", root, new Vector3(-0.35f, top + 0.07f, tablePos.z),
                Vector3.one * 0.12f, Mat("PracticeOrange", new Color(1f, 0.55f, 0.15f)));
            var brb = box.AddComponent<Rigidbody>(); brb.mass = 0.5f; brb.interpolation = RigidbodyInterpolation.Interpolate;
            var boxGrab = box.AddComponent<XRGrabInteractable>(); boxGrab.useDynamicAttach = true;
            var boxGrabbable = box.AddComponent<Grabbable>(); boxGrabbable.SetName("Caja de práctica");
            box.AddComponent<ReturnHome>();
            var practice = box.AddComponent<PracticeTarget>();

            // Herramienta de práctica
            var tool = BuildMagnifier(root, new Vector3(0.05f, top + 0.04f, tablePos.z), DefectType.Crack, tap, found);
            tool.name = "PracticeTool_Lupa";

            // Soporte (socket)
            var pedestal = Prim(PrimitiveType.Cylinder, "SocketPedestal", root, new Vector3(0.4f, top + 0.03f, tablePos.z),
                new Vector3(0.18f, 0.03f, 0.18f), Mat("SocketGreen", new Color(0.2f, 0.85f, 0.4f), emissive: true));
            var socketGo = new GameObject("PracticeSocket");
            socketGo.transform.SetParent(root);
            socketGo.transform.position = pedestal.transform.position + Vector3.up * 0.12f;
            var ssc = socketGo.AddComponent<SphereCollider>(); ssc.isTrigger = true; ssc.radius = 0.14f;
            var snap = new GameObject("SnapPoint").transform;
            snap.SetParent(socketGo.transform, false);
            snap.position = pedestal.transform.position + Vector3.up * 0.09f;
            var socket = socketGo.AddComponent<PracticeSocket>();
            socket.Setup(snap);

            // Tablero de instrucciones
            var canvas = MakeCanvas("OnboardingBoard", new Vector3(0f, 1.65f, -2.75f), Quaternion.identity, new Vector2(1150, 700), 0.001f);
            Panel(canvas.transform, "Bg", new Color(0.06f, 0.07f, 0.09f, 0.92f));
            Label(canvas.transform, "Title", "<b>CONTROL DE CALIDAD XR</b>", 56, TextAnchor.UpperCenter, new Color(1f, 0.78f, 0.1f),
                new Vector2(20, 600), new Vector2(1130, 685), absolute: true);
            Label(canvas.transform, "Subtitle", "Entrenamiento de operario · UAO Immersive Solutions", 30, TextAnchor.UpperCenter,
                new Color(0.6f, 0.75f, 0.9f), new Vector2(20, 555), new Vector2(1130, 605), absolute: true);
            var instr = Label(canvas.transform, "Instruction", "", 40, TextAnchor.MiddleCenter, Color.white,
                new Vector2(50, 200), new Vector2(1100, 545), absolute: true);
            var progress = Label(canvas.transform, "Progress", "0/4", 30, TextAnchor.LowerRight, new Color(0.6f, 0.65f, 0.7f),
                new Vector2(900, 20), new Vector2(1120, 70), absolute: true);

            var tutorial = canvas.gameObject.AddComponent<OnboardingTutorial>();
            var start = MakeButton(canvas.transform, "StartButton", "INICIAR TURNO", new Vector2(350, 70), new Vector2(800, 180),
                new Color(0.1f, 0.6f, 0.3f), hud.OnStartPressed);
            MakeButton(canvas.transform, "SkipButton", "Saltar tutorial", new Vector2(30, 20), new Vector2(280, 80),
                new Color(0.25f, 0.27f, 0.3f), tutorial.Skip, 28);

            tutorial.Setup(instr, progress, rig, boxGrabbable, socket, start.gameObject, uiAudio, step, DesktopMode);
            practice.Setup(tutorial);
            EditorUtility.SetDirty(tutorial);
            EditorUtility.SetDirty(practice);
            EditorUtility.SetDirty(socket);
        }

        // =====================================================================
        // Helpers de UI
        // =====================================================================

        internal static Canvas MakeCanvas(string name, Vector3 pos, Quaternion rot, Vector2 sizePx, float scale)
        {
            // GraphicRaycaster: la mira del PC pulsa botones · TrackedDeviceGraphicRaycaster: rayos de los controles VR.
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = rigCamera;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.SetPositionAndRotation(pos, rot);
            rt.localScale = Vector3.one * scale;
            return c;
        }

        internal static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        internal static Image Panel(Transform parent, string name, Color color)
        {
            var rt = Child(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Texto. Por defecto los dos vectores son márgenes (offsetMin/offsetMax) sobre el padre completo.
        /// Con absolute=true son esquinas en píxeles (abajo-izquierda, arriba-derecha) desde la esquina inferior izquierda.
        /// </summary>
        internal static Text Label(Transform parent, string name, string text, int size, TextAnchor align, Color color,
                          Vector2 a, Vector2 b, bool absolute = false, bool anchored = false)
        {
            var rt = Child(parent, name);
            if (absolute)
            {
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.anchoredPosition = a;
                rt.sizeDelta = b - a;
            }
            else if (anchored)
            {
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = a; rt.offsetMax = b;
            }
            else { rt.offsetMin = a; rt.offsetMax = b; }

            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        internal static Button MakeButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Color bg,
                                 UnityAction action, int fontSize = 40)
        {
            var rt = Child(parent, name);
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
            rt.anchoredPosition = min; rt.sizeDelta = max - min;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;
            UnityEventTools.AddPersistentListener(btn.onClick, action);
            Label(rt, "Label", "<b>" + label + "</b>", fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.zero);
            return btn;
        }

        // =====================================================================
        // Helpers de geometría / assets
        // =====================================================================

        internal static float BuildTable(Transform parent, string name, Vector3 pos, Vector2 size, float height, Material top, Material legs)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent);
            root.position = pos;
            Prim(PrimitiveType.Cube, "Top", root, pos + new Vector3(0f, height - 0.025f, 0f), new Vector3(size.x, 0.05f, size.y), top);
            float lx = size.x / 2f - 0.05f, lz = size.y / 2f - 0.05f;
            foreach (var o in new[] { new Vector2(lx, lz), new Vector2(-lx, lz), new Vector2(lx, -lz), new Vector2(-lx, -lz) })
                Prim(PrimitiveType.Cube, "Leg", root, pos + new Vector3(o.x, (height - 0.05f) / 2f, o.y), new Vector3(0.05f, height - 0.05f, 0.05f), legs);
            return pos.y + height;
        }

        internal static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat,
                               bool collider = true, bool local = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Pieza de herramienta en coordenadas locales con BoxCollider ajustado a su malla.</summary>
        internal static void Part(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion rot,
                         Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (collider)
            {
                var mb = go.GetComponent<MeshFilter>().sharedMesh.bounds;
                var bc = go.AddComponent<BoxCollider>();
                bc.center = mb.center; bc.size = mb.size;
            }
        }

        /// <summary>
        /// Instancia un FBX de Kenney y lo escala uniformemente para que su mayor dimensión horizontal
        /// (o de cualquier eje si allAxes) mida targetSize; lo apoya con la base centrada en pos.
        /// </summary>
        internal static GameObject Fit(string kit, string fbx, Transform parent, Vector3 pos, float targetSize, float yaw, bool allAxes = false)
        {
            string path = $"{Kenney}/{kit}/{fbx}.fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning("[SceneBuilder] Falta modelo: " + path); return new GameObject("Missing_" + fbx); }
            var go = (GameObject)Object.Instantiate(asset);
            go.name = fbx;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            var b = Bounds(go);
            float max = allAxes ? Mathf.Max(b.size.x, b.size.y, b.size.z) : Mathf.Max(b.size.x, b.size.z);
            if (max > 0.0001f) go.transform.localScale *= targetSize / max;
            b = Bounds(go);
            go.transform.position += pos - new Vector3(b.center.x, b.min.y, b.center.z);
            go.transform.SetParent(parent, true);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        /// <summary>Añade un BoxCollider que envuelve el modelo (para que el jugador no lo atraviese).</summary>
        internal static GameObject Solid(GameObject go)
        {
            // Se mide sin rotación para obtener la caja en el espacio local del modelo.
            var rot = go.transform.rotation;
            go.transform.rotation = Quaternion.identity;
            var b = Bounds(go);
            var s = go.transform.lossyScale;
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = new Vector3(b.size.x / Mathf.Abs(s.x), b.size.y / Mathf.Abs(s.y), b.size.z / Mathf.Abs(s.z));
            go.transform.rotation = rot;
            go.isStatic = true;
            return go;
        }

        internal static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        internal static Material Mat(string name, Color color, bool emissive = false, bool transparent = false)
        {
            string path = $"Assets/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew)
            {
                var shader = GraphicsSettings.currentRenderPipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                m = new Material(shader);
            }
            m.color = color;
            m.SetFloat("_Glossiness", 0.35f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.6f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (transparent && m.shader.name == "Standard")
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)BlendMode.One);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            return m;
        }

        internal static AudioClip Clip(string prefix)
        {
            var guid = AssetDatabase.FindAssets(prefix + " t:AudioClip", new[] { Kenney + "/Audio" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(prefix + "_"))
                .OrderBy(p => p).FirstOrDefault();
            if (guid == null) Debug.LogWarning("[SceneBuilder] Sin audio para " + prefix);
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(guid);
        }

        internal static AudioSource AddAudio(GameObject go, float spatial)
        {
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.spatialBlend = spatial;
            a.minDistance = 0.5f; a.maxDistance = 12f;
            a.rolloffMode = AudioRolloffMode.Linear;
            return a;
        }

        internal static void CreateOrReplace(Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Activa el XR Device Simulator automático (solo en el Editor) para probar sin visor.</summary>
        internal static void ConfigureDeviceSimulator()
        {
            var type = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings"))
                .FirstOrDefault(t => t != null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath);
            if (type == null || prefab == null) { Debug.LogWarning("[SceneBuilder] No se pudo configurar el simulador"); return; }
            const System.Reflection.BindingFlags S = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                                                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy;
            const System.Reflection.BindingFlags I = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                                     System.Reflection.BindingFlags.NonPublic;
            var inst = type.GetProperty("Instance", S)?.GetValue(null) as Object;
            if (inst == null) { Debug.LogWarning("[SceneBuilder] XRDeviceSimulatorSettings.Instance nulo"); return; }
            void Set(string prop, object v)
            {
                var p = type.GetProperty(prop, I);
                if (p != null && p.CanWrite) p.SetValue(inst, v);
                else Debug.LogWarning("[SceneBuilder] Propiedad no encontrada: " + prop);
            }
            // DesktopFallback decide en tiempo de ejecución: simulador solo si no hay visor.
            Set("automaticallyInstantiateSimulatorPrefab", false);
            Set("simulatorPrefab", prefab);
            EditorUtility.SetDirty(inst);
            Debug.Log("[SceneBuilder] Simulador delegado a DesktopFallback (se activa sin visor)");
        }
    }
}
