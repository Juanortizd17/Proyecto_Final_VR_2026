using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using SB = MixLab.EditorTools.BuildKit;

namespace MixLab.EditorTools
{
    /// <summary>
    /// Genera el juego "Laboratorio de Mezclas" (opción 3 del enunciado) de forma reproducible:
    /// datos en ScriptableObjects, sala, barra, estante de ingredientes, estación de mezcla,
    /// cliente con reacciones, clientes de fondo animados, tableros y jugador de PC.
    /// Menú: Tools > Mix Lab > Build Scene.
    /// </summary>
    public static partial class MixSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MixLab.unity";
        const string DataDir = "Assets/Data/Mix";
        const float Top = 1.025f;            // altura de la superficie de la barra
        const float WallZ = 3.0f;            // pared del fondo (detrás del cliente)
        const float SeatZ = 1.5f;            // línea de taburetes
        static readonly Vector3 PlayerStart = new Vector3(0f, 0f, -0.05f);
        static readonly Vector3 GlassHome = new Vector3(0f, Top + 0.012f, 0.72f);
        static readonly Vector3 CustomerPos = new Vector3(0f, 0f, SeatZ);
        static readonly Color Skin = new Color(0.95f, 0.76f, 0.61f);
        static readonly Color Copper = new Color(0.88f, 0.54f, 0.29f);
        static readonly Color Ink = new Color(0.96f, 0.94f, 0.89f);
        static readonly Color Muted = new Color(0.71f, 0.66f, 0.84f);
        static readonly Color PanelBg = new Color(0.07f, 0.05f, 0.13f, 0.94f);

        static readonly string[] LeftShelf = { "Agua", "Hielo", "Limon", "Naranja", "Menta", "Leche", "Cafe", "Azucar" };
        static readonly string[] RightShelf = { "Soda", "Cola", "Cerveza", "Ron", "Tequila", "Aji", "SalsaDiablo", "Efervescente" };
        static readonly string[] SideShelf = { "Coco", "Pina", "Vino" };      // brazo de la L, a la derecha del jugador

        static Material particleMat;

        // =====================================================================
        // Entrada
        // =====================================================================

        [MenuItem("Tools/Mix Lab/Build Scene")]
        public static void Build()
        {
            SB.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var d in new[] { "Assets/Materials", "Assets/Scenes", DataDir + "/Ingredients", DataDir + "/Recipes", DataDir + "/Reactions" })
                SB.EnsureFolder(d);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            if (particleMat == null)
            {
                particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MixParticle.mat");
                if (particleMat == null)
                {
                    particleMat = new Material(Shader.Find("Sprites/Default"));
                    AssetDatabase.CreateAsset(particleMat, "Assets/Materials/MixParticle.mat");
                }
            }

            var ing = CreateIngredients();
            var recipes = CreateRecipes(ing);
            var reactions = CreateReactions();

            var managers = new GameObject("--- JUEGO ---");
            var lights = BuildRoom();

            // Jugador (la cámara debe existir antes de crear los tableros)
            var player = BuildPlayer(out var hand, out var holdPoint, out var handAudio);
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(managers.transform);

            BuildCounter();
            var glass = BuildStation(out var lever);
            hand.Setup(holdPoint, glass, lever, handAudio, SB.Clip("select"), SB.Clip("click"));
            BuildShelf(ing, glass);
            var customer = BuildCustomer();
            BuildPatrons();

            var explosion = Fx("FX_Explosion", null, new Vector3(0f, Top + 0.1f, 1f), Color.white, new Color(0.7f, 1f, 0.6f),
                0f, 0.8f, 2.6f, 0.07f, 1f, ParticleSystemShapeType.Sphere, 0.05f, 45);

            var gmGo = new GameObject("MixGameManager");
            gmGo.transform.SetParent(managers.transform);
            var gm = gmGo.AddComponent<MixGameManager>();
            var gmAudio = SB.AddAudio(gmGo, 0f);
            gm.Setup(recipes, reactions, glass, customer, lever, explosion, lights, gmAudio,
                SB.Clip("slime"), SB.Clip("confirmation"), SB.Clip("error"), SB.Clip("explosionCrunch"),
                SB.Clip("impactBell_heavy"), SB.Clip("glitch"), SB.Clip("maximize"));

            BuildJukebox();
            BuildDisco(managers);
            BuildPerformance(managers);
            MarkStaticDecor();

            var board = BuildMainBoard();
            BuildBook(reactions.Length);
            BuildTutorial(glass, gmAudio);
            BuildHistory();

            var hud = new GameObject("DesktopHUD"); hud.AddComponent<DesktopHud>(); hud.transform.SetParent(managers.transform);
            BuildVr(managers.transform, player, hud, es.GetComponent<EventSystem>(), glass, lever);
            new GameObject("MixAutoDemo (solo con -autodemo)").AddComponent<MixAutoDemo>().transform.SetParent(managers.transform);

            EditorUtility.SetDirty(gm); EditorUtility.SetDirty(board);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MixSceneBuilder] Escena generada: {ScenePath} ({ing.Count} ingredientes, {recipes.Length} recetas, {reactions.Length} reacciones)");
        }

        // =====================================================================
        // Datos (ScriptableObjects)
        // =====================================================================

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static Dictionary<string, IngredientData> CreateIngredients()
        {
            var d = new Dictionary<string, IngredientData>();
            void Add(string id, string name, string tag, Color c, MixProps p)
            {
                var a = Asset<IngredientData>($"{DataDir}/Ingredients/{id}.asset");
                a.displayName = name; a.tag = tag; a.color = c; a.props = p;
                EditorUtility.SetDirty(a);
                d[id] = a;
            }
            Add("Agua", "Agua", "neutral", new Color(0.44f, 0.75f, 1f), new MixProps { water = 1 });
            Add("Hielo", "Hielo", "frío", new Color(0.84f, 0.95f, 1f), new MixProps { cold = 1 });
            Add("Limon", "Limón", "ácido", new Color(0.91f, 0.94f, 0.37f), new MixProps { acid = 1 });
            Add("Naranja", "Naranja", "frutal", new Color(1f, 0.6f, 0.15f), new MixProps { fruit = 1 });
            Add("Menta", "Menta", "fresca", new Color(0.27f, 0.81f, 0.53f), new MixProps { mint = 1 });
            Add("Leche", "Leche", "lácteo", new Color(0.98f, 0.96f, 0.91f), new MixProps { dairy = 1 });
            Add("Cafe", "Café", "cafeína", new Color(0.35f, 0.2f, 0.13f), new MixProps { caffeine = 1 });
            Add("Azucar", "Azúcar", "dulce", new Color(1f, 0.97f, 0.9f), new MixProps { sweet = 1 });
            Add("Soda", "Soda", "gas", new Color(0.75f, 0.91f, 0.82f), new MixProps { gas = 1 });
            Add("Cola", "Cola", "gas + cafeína", new Color(0.27f, 0.13f, 0.09f), new MixProps { gas = 1, caffeine = 1 });
            Add("Cerveza", "Cerveza", "alcohol + gas", new Color(0.95f, 0.74f, 0.22f), new MixProps { alcohol = 1, gas = 1 });
            Add("Ron", "Ron", "alcohol", new Color(0.78f, 0.52f, 0.23f), new MixProps { alcohol = 1 });
            Add("Tequila", "Tequila", "alcohol ×2", new Color(0.95f, 0.87f, 0.61f), new MixProps { alcohol = 2 });
            Add("Aji", "Ají", "picante", new Color(1f, 0.42f, 0.24f), new MixProps { spicy = 1 });
            Add("SalsaDiablo", "Salsa del Diablo", "picante ×3", new Color(0.7f, 0.07f, 0.11f), new MixProps { spicy = 3 });
            Add("Efervescente", "Efervescente", "reactiva", new Color(1f, 1f, 1f), new MixProps { fizz = 1 });
            Add("Coco", "Crema de coco", "frutal + dulce", new Color(0.97f, 0.95f, 0.88f), new MixProps { fruit = 1, sweet = 1 });
            Add("Pina", "Piña", "frutal", new Color(1f, 0.85f, 0.25f), new MixProps { fruit = 1 });
            Add("Vino", "Vino tinto", "alcohol + frutal", new Color(0.5f, 0.08f, 0.2f), new MixProps { alcohol = 1, fruit = 1 });
            return d;
        }

        static RecipeData[] CreateRecipes(Dictionary<string, IngredientData> ing)
        {
            var list = new List<RecipeData>();
            void Add(string id, string name, string request, Temperature? temp, params string[] items)
            {
                var a = Asset<RecipeData>($"{DataDir}/Recipes/{id}.asset");
                a.displayName = name; a.requestLine = request;
                a.needsTemperature = temp.HasValue; a.temperature = temp ?? Temperature.Ambiente;
                a.ingredients = items.Select(i => ing[i]).ToArray();
                EditorUtility.SetDirty(a);
                list.Add(a);
            }
            Temperature? hot = Temperature.Caliente, cold = Temperature.Frio;
            Add("LimonadaHelada", "Limonada helada", "una limonada helada", null, "Agua", "Limon", "Hielo");
            Add("Mojito", "Mojito", "un mojito", null, "Ron", "Menta", "Limon", "Soda");
            Add("CafeConLeche", "Café con leche", "un café con leche bien caliente", hot, "Cafe", "Leche");
            Add("Michelada", "Michelada", "una michelada", null, "Cerveza", "Limon", "Aji");
            Add("SodaLimon", "Soda con limón", "una soda con limón", null, "Soda", "Limon");
            Add("AguaMenta", "Agua de menta", "un agua de menta bien fría", cold, "Agua", "Menta");
            Add("Carajillo", "Carajillo", "un carajillo caliente", hot, "Cafe", "Ron");
            Add("CubaLibre", "Cuba libre", "un cuba libre", null, "Ron", "Cola", "Hielo");
            Add("TequilaSunrise", "Tequila sunrise", "un tequila sunrise", null, "Tequila", "Naranja", "Hielo");
            Add("Naranjada", "Naranjada", "una naranjada", null, "Naranja", "Agua", "Azucar");
            Add("CafeHelado", "Café helado", "un café helado", null, "Cafe", "Leche", "Hielo", "Azucar");
            Add("LecheMerengada", "Leche merengada", "una leche merengada bien fría", cold, "Leche", "Azucar");
            Add("Refajo", "Refajo", "un refajo", null, "Cerveza", "Cola");
            Add("Margarita", "Margarita", "una margarita", null, "Tequila", "Limon", "Hielo");
            Add("Aguapanela", "Aguapanela con limón", "una aguapanela con limón bien caliente", hot, "Agua", "Azucar", "Limon");
            Add("Ponche", "Ponche caliente", "un ponche caliente", hot, "Ron", "Naranja", "Azucar");
            Add("ColaHielo", "Cola con hielo", "una cola con hielo", null, "Cola", "Hielo");
            Add("MentaFrappe", "Frappé de menta", "un frappé de menta", null, "Menta", "Leche", "Hielo", "Azucar");
            Add("Sangrita", "Sangrita", "una sangrita", null, "Naranja", "Limon", "Aji");
            Add("EspressoDoble", "Espresso doble", "un espresso doble caliente", hot, "Cafe", "Cafe");
            Add("Tinto", "Tinto", "un tinto bien caliente", hot, "Cafe", "Agua");
            Add("Capuchino", "Capuchino dulce", "un capuchino dulce caliente", hot, "Cafe", "Leche", "Azucar");
            Add("CafeIrlandes", "Café irlandés", "un café irlandés caliente", hot, "Cafe", "Ron", "Leche", "Azucar");
            Add("Canelazo", "Canelazo", "un canelazo bien caliente", hot, "Ron", "Agua", "Azucar", "Limon");
            Add("LimonadaMenta", "Limonada de menta", "una limonada de menta", null, "Agua", "Limon", "Menta", "Hielo");
            Add("Clara", "Clara con limón", "una clara con limón", null, "Cerveza", "Soda", "Limon");
            Add("Chelada", "Chelada", "una chelada", null, "Cerveza", "Limon", "Hielo");
            Add("Paloma", "Paloma", "una paloma", null, "Tequila", "Soda", "Limon", "Hielo");
            Add("RonNaranja", "Ron con naranja", "un ron con naranja", null, "Ron", "Naranja", "Hielo");
            Add("NaranjaEspumosa", "Naranja espumosa", "una naranja espumosa", null, "Naranja", "Soda", "Hielo");
            Add("PinaColada", "Piña colada", "una piña colada", null, "Ron", "Coco", "Pina", "Hielo");
            Add("LimonadaCoco", "Limonada de coco", "una limonada de coco", null, "Coco", "Limon", "Agua", "Hielo");
            Add("CocoLoco", "Coco loco", "un coco loco", null, "Tequila", "Coco", "Hielo");
            Add("JugoPina", "Jugo de piña", "un jugo de piña", null, "Pina", "Agua", "Azucar");
            Add("TintoVerano", "Tinto de verano", "un tinto de verano", null, "Vino", "Soda", "Limon", "Hielo");
            Add("Sangria", "Sangría", "una sangría", null, "Vino", "Naranja", "Pina", "Azucar");
            Add("Calimocho", "Calimocho", "un calimocho", null, "Vino", "Cola");
            Add("VinoCaliente", "Vino caliente", "un vino caliente", hot, "Vino", "Naranja", "Azucar");
            return list.ToArray();
        }

        static ReactionData[] CreateReactions()
        {
            var list = new List<ReactionData>();
            void Add(ReactionId id, string name, string hint, string line, int pts, bool ko, Color skin, Color book, float dur)
            {
                var a = Asset<ReactionData>($"{DataDir}/Reactions/{id}.asset");
                a.id = id; a.displayName = name; a.hint = hint; a.customerLine = line; a.points = pts; a.knockout = ko;
                a.skinColor = skin; a.bookColor = book; a.duration = dur;
                EditorUtility.SetDirty(a);
                list.Add(a);
            }
            Add(ReactionId.Perfecto, "Pedido perfecto", "Exactamente lo que pidió", "¡Exactamente lo que pedí! Diez de diez.", 100, false, Skin, new Color(0.5f, 0.88f, 0.54f), 2.6f);
            Add(ReactionId.Picante, "Picante", "Un toque de ají", "¡Fff… fff…! Me arde la lengua… ¡se me duermen los labios!", -20, false, new Color(1f, 0.56f, 0.45f), new Color(1f, 0.54f, 0.36f), 3.2f);
            Add(ReactionId.Infierno, "Picante infernal", "Salsa del Diablo", "¡¡AAAGH!! ¡ME QUEMA LA GARGANTA! ¡¡AGUA, AGUA!!", -50, false, new Color(1f, 0.3f, 0.25f), new Color(1f, 0.23f, 0.19f), 3.6f);
            Add(ReactionId.Explosion, "Explosión efervescente", "Gas + efervescente, sin frío", "¡*Cof, cof*! ¡Se me metió por la nariz! ¡Arde!", -50, false, new Color(0.96f, 0.72f, 0.56f), new Color(0.85f, 0.95f, 0.77f), 3.4f);
            Add(ReactionId.Borracho, "Mareo total", "Demasiado alcohol", "Hip… todo da vueltas… creo que voy a… *buurp*", -50, false, new Color(0.97f, 0.69f, 0.63f), new Color(1f, 0.62f, 0.69f), 3.8f);
            Add(ReactionId.Alegre, "Alegrito", "Un poco de alcohol", "Ooh, esto está bueno… hic.", 10, false, new Color(0.97f, 0.72f, 0.64f), new Color(1f, 0.75f, 0.8f), 3f);
            Add(ReactionId.Nausea, "Náusea verde", "Leche + limón", "Ay no… ay no… la leche cortada… ¡BUAAAGH!", -50, false, new Color(0.66f, 0.85f, 0.42f), new Color(0.66f, 0.85f, 0.42f), 3.6f);
            Add(ReactionId.Congelado, "Cerebro congelado", "Menta + frío", "B-b-brrr… s-se me congeló el ce-cerebro.", -20, false, new Color(0.66f, 0.85f, 0.97f), new Color(0.62f, 0.83f, 0.96f), 3.2f);
            Add(ReactionId.Hiper, "Sobredosis de café", "Doble cafeína", "¡¡EL CORAZÓN SE ME VA A SALIR!! ¡¿LO OYES?! ¡¿LO OYES?!", -20, false, Skin, new Color(0.62f, 0.42f, 0.28f), 3.2f);
            Add(ReactionId.Azucar, "Subidón de azúcar", "Triple azúcar", "¡¡WIIII!! ¡Quiero correr una maratón!", -20, false, Skin, new Color(1f, 0.7f, 0.85f), 3.2f);
            Add(ReactionId.Bailarin, "Fiebre de baile", "Cafeína + alcohol", "¡Súbele a esa música! ¡Esta es mi canción!", 10, false, new Color(0.98f, 0.72f, 0.62f), new Color(0.75f, 0.5f, 1f), 3.8f);
            Add(ReactionId.Hipo, "Ataque de hipo", "Doble gas", "¡Hip!… perdón… ¡HIP!… ¿tienes un vaso de… ¡hip!?", -20, false, new Color(0.98f, 0.74f, 0.66f), new Color(0.55f, 0.85f, 1f), 3.4f);
            Add(ReactionId.Agrio, "Cara de limón", "Doble ácido", "¡Mmmgh! Se me arrugó hasta el apellido.", -20, false, new Color(0.95f, 0.88f, 0.5f), new Color(0.93f, 0.92f, 0.35f), 3.2f);
            Add(ReactionId.Dormido, "Sueño profundo", "Leche tibia o doble leche", "Zzz… cinco minuticos más, mamá… zzz…", -20, false, new Color(0.93f, 0.78f, 0.7f), new Color(0.6f, 0.62f, 0.9f), 3.8f);
            Add(ReactionId.Llamarada, "Llamarada", "Alcohol fuerte + caliente, sin hielo", "¡¡ME QUEMO, ME QUEMO!! … *cof, cof* … ¿y mis cejas?", -50, false, new Color(0.8f, 0.71f, 0.67f), new Color(1f, 0.6f, 0.1f), 4.6f);
            Add(ReactionId.Desmayo, "Desmayo fulminante", "Salsa del Diablo + alcohol", "Ghhk… ghh… (ojos en blanco)", -100, true, new Color(0.92f, 0.86f, 0.78f), new Color(1f, 0.84f, 0.29f), 4.6f);
            Add(ReactionId.SinChiste, "Sin chiste", "Nada especial", "Mmm… sabe a nada. ¿Esto era lo que pedí?", 0, false, Skin, Muted, 2.6f);
            return list.ToArray();
        }

        // =====================================================================
        // Jugador
        // =====================================================================

        static GameObject BuildPlayer(out MixInteractor hand, out Transform holdPoint, out AudioSource handAudio)
        {
            var player = new GameObject("Player (PC)");
            player.layer = 2;
            player.transform.SetPositionAndRotation(PlayerStart, Quaternion.identity);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.25f; cc.slopeLimit = 45f; cc.skinWidth = 0.03f; cc.minMoveDistance = 0f;

            var camGo = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.03f; cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.04f, 0.09f);
            SB.rigCamera = cam;

            holdPoint = new GameObject("GlassHold").transform;
            holdPoint.SetParent(camGo.transform, false);
            holdPoint.localPosition = new Vector3(0.27f, -0.3f, 0.62f);

            var steps = SB.AddAudio(player, 0f);
            var footsteps = AssetDatabase.FindAssets("footstep_wood t:AudioClip", new[] { "Assets/ThirdParty/Kenney/Audio" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            player.AddComponent<DesktopPlayer>().Setup(camGo.transform, steps, footsteps);

            handAudio = SB.AddAudio(camGo, 0f);
            hand = camGo.AddComponent<MixInteractor>();
            return player;
        }

        // =====================================================================
        // Barra y estación
        // =====================================================================

        static void BuildCounter()
        {
            var root = new GameObject("--- BARRA ---").transform;
            var body = SB.Mat("CounterBody", new Color(0.1f, 0.24f, 0.28f));
            var top = SB.Mat("CounterTop", Copper);
            var teal = SB.Mat("NeonTeal", new Color(0.37f, 0.9f, 0.82f), emissive: true);
            SB.Prim(PrimitiveType.Cube, "CounterBody", root, new Vector3(0f, 0.5f, 0.8f), new Vector3(4.9f, 1.0f, 0.5f), body);
            SB.Prim(PrimitiveType.Cube, "CounterTop", root, new Vector3(0f, 1.0f, 0.8f), new Vector3(5.1f, 0.05f, 0.64f), top);
            SB.Prim(PrimitiveType.Cube, "CounterNeon", root, new Vector3(0f, 0.93f, 1.055f), new Vector3(4.9f, 0.03f, 0.02f), teal, false);
            SB.Prim(PrimitiveType.Cube, "FootRail", root, new Vector3(0f, 0.2f, 1.12f), new Vector3(4.9f, 0.04f, 0.04f), top, false);

            // Elevadores del estante
            var riser = SB.Mat("ShelfRiser", new Color(0.16f, 0.12f, 0.26f));
            SB.Prim(PrimitiveType.Cube, "Riser_L", root, new Vector3(-1.09f, Top + 0.02f, 0.92f), new Vector3(1.38f, 0.04f, 0.2f), riser);
            SB.Prim(PrimitiveType.Cube, "Riser_R", root, new Vector3(1.09f, Top + 0.02f, 0.92f), new Vector3(1.38f, 0.04f, 0.2f), riser);

            SB.Fit("FurnitureKit", "kitchenBlender", root, new Vector3(-2.2f, Top, 0.8f), 0.3f, 0f, allAxes: true);

            // Brazo de la L: sale de la esquina derecha hacia el jugador y lleva tres ingredientes más
            SB.Prim(PrimitiveType.Cube, "CounterArmBody", root, new Vector3(2.2f, 0.5f, -0.3f), new Vector3(0.5f, 1.0f, 1.7f), body);
            SB.Prim(PrimitiveType.Cube, "CounterArmTop", root, new Vector3(2.2f, 1.0f, -0.32f), new Vector3(0.64f, 0.05f, 1.84f), top);
            SB.Prim(PrimitiveType.Cube, "CounterArmNeon", root, new Vector3(2.455f, 0.93f, -0.3f), new Vector3(0.02f, 0.03f, 1.7f), teal, false);
            SB.Prim(PrimitiveType.Cube, "Riser_Arm", root, new Vector3(2.3f, Top + 0.02f, -0.3f), new Vector3(0.2f, 0.04f, 1.1f), riser);
        }

        static MixGlass BuildStation(out TemperatureLever lever)
        {
            var root = new GameObject("--- ESTACIÓN ---").transform;
            var dark = SB.Mat("StationDark", new Color(0.12f, 0.1f, 0.18f));
            var metal = SB.Mat("StationMetal", new Color(0.72f, 0.73f, 0.78f));
            var teal = SB.Mat("NeonTeal", new Color(0.37f, 0.9f, 0.82f), emissive: true);

            // Base del vaso
            var pad = SB.Prim(PrimitiveType.Cylinder, "StationPad", root, new Vector3(0f, Top + 0.004f, 0.72f), new Vector3(0.2f, 0.004f, 0.2f), dark, false);
            var padCol = pad.AddComponent<BoxCollider>(); padCol.size = new Vector3(1f, 4f, 1f);
            pad.AddComponent<StationPad>();
            SB.Prim(PrimitiveType.Cylinder, "PadRing", root, new Vector3(0f, Top + 0.002f, 0.72f), new Vector3(0.23f, 0.002f, 0.23f), teal, false);

            // Vaso
            var glassGo = new GameObject("MixGlass");
            glassGo.transform.SetParent(root);
            glassGo.transform.position = GlassHome;
            var glassMat = SB.Mat("GlassClear", new Color(0.8f, 0.95f, 1f, 0.22f), transparent: true);
            var liquid = SB.Mat("Liquid", Color.white);
            SB.Prim(PrimitiveType.Cylinder, "Outer", glassGo.transform, new Vector3(0f, 0.07f, 0f), new Vector3(0.085f, 0.07f, 0.085f), glassMat, false, local: true);
            var layers = new Renderer[MixGlass.Capacity];
            for (int i = 0; i < layers.Length; i++)
            {
                var l = SB.Prim(PrimitiveType.Cylinder, "Layer" + i, glassGo.transform, new Vector3(0f, 0.022f + i * 0.03f, 0f),
                    new Vector3(0.07f, 0.015f, 0.07f), liquid, false, local: true);
                layers[i] = l.GetComponent<Renderer>();
                l.SetActive(false);
            }
            // Hielo flotando (se muestra cuando la mezcla lleva hielo)
            var ice = Group("IceCubes", glassGo.transform, Vector3.zero);
            var iceMat = SB.Mat("IceCube", new Color(0.85f, 0.96f, 1f));
            L(PrimitiveType.Cube, "A", ice, new Vector3(-0.012f, 0f, 0.008f), Vector3.one * 0.03f, iceMat).transform.localRotation = Quaternion.Euler(20f, 30f, 10f);
            L(PrimitiveType.Cube, "B", ice, new Vector3(0.014f, 0.004f, -0.01f), Vector3.one * 0.028f, iceMat).transform.localRotation = Quaternion.Euler(-15f, 60f, 25f);
            ice.gameObject.SetActive(false);

            var gc = glassGo.AddComponent<BoxCollider>(); gc.center = new Vector3(0f, 0.075f, 0f); gc.size = new Vector3(0.11f, 0.15f, 0.11f);
            var glass = glassGo.AddComponent<MixGlass>();
            glass.Setup(layers, SB.AddAudio(glassGo, 1f), SB.Clip("drop"), SB.Clip("glass"), SB.Clip("impactSoft_medium"));
            glass.SetIceVisual(ice);
            var burst = Fx("FX_GlassBurst", null, GlassHome, Color.white, Color.white, 0f, 0.7f, 2.8f, 0.06f, 1.4f, ParticleSystemShapeType.Sphere, 0.04f, 40);
            glass.SetBurst(burst, glassMat);

            // Analizador: clasifica la mezcla (estable, inestable, tóxica) y avisa antes de servir
            var anaText = SmallSign("AnalyzerLabel", new Vector3(0f, Top + 0.03f, 0.545f), "ANALIZADOR", new Vector2(560, 130), 36);
            var anaLamp = SB.Prim(PrimitiveType.Sphere, "AnalyzerLamp", root, new Vector3(0f, Top + 0.02f, 0.615f), Vector3.one * 0.035f,
                SB.Mat("AnalyzerLamp", new Color(0.5f, 0.5f, 0.55f), emissive: true), false);
            var analyzer = anaText.transform.parent.gameObject.AddComponent<MixAnalyzer>();
            anaText.transform.parent.SetParent(root, true);
            analyzer.Setup(glass, null, anaText, anaText.transform.parent.Find("Bg").GetComponent<Image>(), anaLamp.GetComponent<Renderer>(),
                SB.AddAudio(anaLamp, 1f), SB.Clip("switch"));

            // Palanca de temperatura (izquierda)
            var leverGo = new GameObject("TemperatureLever");
            leverGo.transform.SetParent(root);
            leverGo.transform.position = new Vector3(-0.27f, Top, 0.74f);
            L(PrimitiveType.Cube, "Base", leverGo.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.15f, 0.06f, 0.11f), dark);
            var handle = Group("Handle", leverGo.transform, new Vector3(0f, 0.06f, 0f));
            L(PrimitiveType.Cylinder, "Stick", handle, new Vector3(0f, 0.055f, 0f), new Vector3(0.018f, 0.055f, 0.018f), metal);
            var knob = L(PrimitiveType.Sphere, "Knob", handle, new Vector3(0f, 0.125f, 0f), Vector3.one * 0.045f,
                SB.Mat("LeverKnob", new Color(0.75f, 0.75f, 0.8f), emissive: true));
            var lc = leverGo.AddComponent<BoxCollider>(); lc.center = new Vector3(0f, 0.1f, 0f); lc.size = new Vector3(0.17f, 0.22f, 0.13f);
            var tempLabel = SmallSign("TempLabel", new Vector3(-0.27f, Top + 0.035f, 0.655f), "TEMP\n<b>AMBIENTE</b>", new Vector2(260, 120), 34);
            tempLabel.transform.parent.SetParent(leverGo.transform, true);
            lever = leverGo.AddComponent<TemperatureLever>();
            lever.Setup(handle, knob.GetComponent<Renderer>(), tempLabel, SB.AddAudio(leverGo, 1f), SB.Clip("switch"));
            analyzer.Setup(glass, lever, anaText, anaText.transform.parent.Find("Bg").GetComponent<Image>(), anaLamp.GetComponent<Renderer>(),
                anaLamp.GetComponent<AudioSource>(), SB.Clip("switch"));
            EditorUtility.SetDirty(analyzer);

            // Palanca de agitar (derecha)
            var shakeGo = new GameObject("ShakeLever");
            shakeGo.transform.SetParent(root);
            shakeGo.transform.position = new Vector3(0.27f, Top, 0.74f);
            L(PrimitiveType.Cube, "Base", shakeGo.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.15f, 0.06f, 0.11f), dark);
            var sHandle = Group("Handle", shakeGo.transform, new Vector3(0f, 0.06f, 0f));
            L(PrimitiveType.Cylinder, "Stick", sHandle, new Vector3(0f, 0.055f, 0f), new Vector3(0.018f, 0.055f, 0.018f), metal);
            L(PrimitiveType.Sphere, "Knob", sHandle, new Vector3(0f, 0.125f, 0f), Vector3.one * 0.045f, SB.Mat("CounterTop", Copper));
            var sc = shakeGo.AddComponent<BoxCollider>(); sc.center = new Vector3(0f, 0.1f, 0f); sc.size = new Vector3(0.17f, 0.22f, 0.13f);
            var shakeLabel = SmallSign("ShakeLabel", new Vector3(0.27f, Top + 0.035f, 0.655f), "<b>AGITAR</b>", new Vector2(260, 120), 44);
            shakeLabel.transform.parent.SetParent(shakeGo.transform, true);
            shakeGo.AddComponent<ShakeLever>().Setup(glass, sHandle);

            // Lavaplatos (extremo derecho de la barra)
            var sinkGo = new GameObject("Sink");
            sinkGo.transform.SetParent(root);
            sinkGo.transform.position = new Vector3(2.2f, Top, 0.8f);
            L(PrimitiveType.Cube, "Basin", sinkGo.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.04f, 0.34f), metal);
            L(PrimitiveType.Cube, "Hole", sinkGo.transform, new Vector3(0f, 0.042f, 0f), new Vector3(0.28f, 0.004f, 0.26f), dark);
            L(PrimitiveType.Cylinder, "Faucet", sinkGo.transform, new Vector3(0f, 0.11f, 0.14f), new Vector3(0.025f, 0.09f, 0.025f), metal);
            L(PrimitiveType.Cylinder, "Spout", sinkGo.transform, new Vector3(0f, 0.19f, 0.09f), new Vector3(0.022f, 0.05f, 0.022f), metal)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var sinkCol = sinkGo.AddComponent<BoxCollider>(); sinkCol.center = new Vector3(0f, 0.1f, 0.02f); sinkCol.size = new Vector3(0.38f, 0.22f, 0.36f);
            sinkGo.AddComponent<SinkSpot>().Setup(glass);
            var sinkLabel = SmallSign("SinkLabel", new Vector3(2.2f, Top + 0.035f, 0.6f), "<b>LAVAPLATOS</b>\nvaciar vaso", new Vector2(300, 120), 30);
            sinkLabel.transform.parent.SetParent(sinkGo.transform, true);
            return glass;
        }

        /// <summary>Letrero pequeño apoyado en la barra, inclinado hacia el jugador.</summary>
        static Text SmallSign(string name, Vector3 pos, string text, Vector2 sizePx, int fontSize)
        {
            var c = SB.MakeCanvas(name, pos, Quaternion.Euler(38f, 0f, 0f), sizePx, 0.0005f);
            Object.DestroyImmediate(c.GetComponent<GraphicRaycaster>());
            SB.Panel(c.transform, "Bg", PanelBg);
            return SB.Label(c.transform, "Text", text, fontSize, TextAnchor.MiddleCenter, Ink, new Vector2(8, 6), new Vector2(-8, -6));
        }

        // =====================================================================
        // Estante de ingredientes
        // =====================================================================

        static void BuildShelf(Dictionary<string, IngredientData> ing, MixGlass glass)
        {
            var root = new GameObject("--- INGREDIENTES ---").transform;
            for (int i = 0; i < LeftShelf.Length; i++)
            {
                BuildBottle(root, ing[LeftShelf[i]], LeftShelf[i], i, new Vector3(-0.48f - 0.17f * i, Top + 0.04f, 0.92f), glass);
                BuildBottle(root, ing[RightShelf[i]], RightShelf[i], i, new Vector3(0.48f + 0.17f * i, Top + 0.04f, 0.92f), glass);
            }
            for (int i = 0; i < SideShelf.Length; i++)
                BuildBottle(root, ing[SideShelf[i]], SideShelf[i], i, new Vector3(2.3f, Top + 0.04f, 0.1f - 0.4f * i), glass, 0.0009f);
        }

        /// <summary>Botella modelada por piezas: cuerpo, hombro, cuello, tapa y etiqueta.</summary>
        static void ProcBottle(Transform root, string id, Color body, Color label, Color cap, float radius, float height)
        {
            var bodyMat = SB.Mat("Bottle_" + id, body);
            var labelMat = SB.Mat("BottleLabel_" + id, label);
            var capMat = SB.Mat("BottleCap_" + id, cap);
            float d = radius * 2f, bodyH = height * 0.6f;
            L(PrimitiveType.Cylinder, "Body", root, new Vector3(0f, bodyH * 0.5f, 0f), new Vector3(d, bodyH * 0.5f, d), bodyMat);
            L(PrimitiveType.Sphere, "Shoulder", root, new Vector3(0f, bodyH, 0f), new Vector3(d, radius * 1.3f, d), bodyMat);
            L(PrimitiveType.Cylinder, "Neck", root, new Vector3(0f, bodyH + height * 0.17f, 0f), new Vector3(radius * 0.8f, height * 0.17f, radius * 0.8f), bodyMat);
            L(PrimitiveType.Cylinder, "Cap", root, new Vector3(0f, height * 0.97f, 0f), new Vector3(radius * 0.95f, height * 0.03f, radius * 0.95f), capMat);
            L(PrimitiveType.Cylinder, "Label", root, new Vector3(0f, bodyH * 0.52f, 0f), new Vector3(d * 1.04f, bodyH * 0.2f, d * 1.04f), labelMat);
        }

        static void BuildBottle(Transform parent, IngredientData data, string id, int slot, Vector3 pos, MixGlass glass, float labelScale = 0.00048f)
        {
            var go = new GameObject("Bottle_" + id);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var t = go.transform;
            PieceShape? pieces = null; int pieceCount = 1; string verb = "Verter";

            switch (id)
            {
                case "Agua": ProcBottle(t, id, new Color(0.55f, 0.82f, 1f), Color.white, new Color(0.2f, 0.45f, 0.9f), 0.037f, 0.25f); break;
                case "Menta": ProcBottle(t, id, new Color(0.16f, 0.62f, 0.38f), new Color(0.9f, 1f, 0.92f), new Color(0.1f, 0.3f, 0.2f), 0.034f, 0.23f); break;
                case "Cola": ProcBottle(t, id, new Color(0.2f, 0.1f, 0.07f), new Color(0.85f, 0.1f, 0.1f), new Color(0.85f, 0.1f, 0.1f), 0.04f, 0.21f); break;
                case "Cerveza": ProcBottle(t, id, new Color(0.38f, 0.2f, 0.07f), new Color(0.98f, 0.8f, 0.2f), new Color(0.95f, 0.75f, 0.2f), 0.032f, 0.25f); break;
                case "Vino": ProcBottle(t, id, new Color(0.16f, 0.2f, 0.12f), new Color(0.93f, 0.9f, 0.8f), new Color(0.55f, 0.08f, 0.2f), 0.036f, 0.28f); break;
                case "Coco": SB.Fit("FoodKit", "coconut-half", t, pos, 0.14f, 0f, allAxes: true); break;
                case "Pina":
                    SB.Fit("FoodKit", "pineapple", t, pos, 0.24f, 0f, allAxes: true);
                    pieces = PieceShape.Disc; verb = "Agregar"; break;
                case "Leche": SB.Fit("FoodKit", "carton", t, pos, 0.2f, 0f, allAxes: true); break;
                case "Cafe": SB.Fit("FoodKit", "cup-coffee", t, pos, 0.12f, 0f, allAxes: true); break;
                case "Soda": SB.Fit("FoodKit", "soda-bottle", t, pos, 0.24f, 0f, allAxes: true); break;
                case "Ron": SB.Fit("FoodKit", "wine-red", t, pos, 0.26f, 0f, allAxes: true); break;
                case "Tequila": SB.Fit("FoodKit", "wine-white", t, pos, 0.26f, 0f, allAxes: true); break;
                case "SalsaDiablo": SB.Fit("FoodKit", "bottle-ketchup", t, pos, 0.2f, 0f, allAxes: true); break;
                case "Limon":
                    SB.Fit("FoodKit", "lemon", t, pos, 0.1f, 0f, allAxes: true);
                    pieces = PieceShape.Disc; verb = "Agregar"; break;
                case "Naranja":
                    SB.Fit("FoodKit", "orange", t, pos, 0.11f, 0f, allAxes: true);
                    pieces = PieceShape.Disc; verb = "Agregar"; break;
                case "Aji":
                    SB.Fit("FoodKit", "pepper", t, pos, 0.13f, 0f, allAxes: true);
                    pieces = PieceShape.Stick; verb = "Agregar"; break;
                case "Azucar":
                    SB.Fit("FoodKit", "bowl", t, pos, 0.13f, 0f, allAxes: true);
                    var sugar = SB.Mat("TabletWhite", Color.white);
                    Vector3[] lumps = { new Vector3(-0.02f, 0.04f, 0f), new Vector3(0.02f, 0.042f, 0.015f), new Vector3(0f, 0.045f, -0.02f), new Vector3(0.003f, 0.065f, 0.002f) };
                    foreach (var c in lumps) L(PrimitiveType.Cube, "Lump", t, c, Vector3.one * 0.028f, sugar).transform.localRotation = Quaternion.Euler(c.x * 500f, c.z * 800f, 12f);
                    pieces = PieceShape.Cube; verb = "Agregar"; break;
                case "Hielo":
                    // Hielera metálica con cubos y pinzas
                    var metal = SB.Mat("StationMetal", new Color(0.72f, 0.73f, 0.78f));
                    L(PrimitiveType.Cylinder, "Bucket", t, new Vector3(0f, 0.055f, 0f), new Vector3(0.14f, 0.055f, 0.14f), metal);
                    L(PrimitiveType.Cylinder, "Rim", t, new Vector3(0f, 0.112f, 0f), new Vector3(0.15f, 0.004f, 0.15f), SB.Mat("StationDark", new Color(0.12f, 0.1f, 0.18f)));
                    var iceM = SB.Mat("IceCube", new Color(0.85f, 0.96f, 1f));
                    Vector3[] cubes = { new Vector3(-0.03f, 0.125f, 0f), new Vector3(0.03f, 0.128f, 0.02f), new Vector3(0f, 0.13f, -0.03f), new Vector3(0.005f, 0.155f, 0.005f), new Vector3(-0.02f, 0.15f, 0.03f) };
                    foreach (var c in cubes) L(PrimitiveType.Cube, "Ice", t, c, Vector3.one * 0.04f, iceM).transform.localRotation = Quaternion.Euler(c.x * 600f, c.z * 900f, 20f);
                    L(PrimitiveType.Cube, "TongA", t, new Vector3(0.06f, 0.15f, -0.035f), new Vector3(0.012f, 0.13f, 0.006f), metal, -22f);
                    L(PrimitiveType.Cube, "TongB", t, new Vector3(0.075f, 0.15f, -0.035f), new Vector3(0.012f, 0.13f, 0.006f), metal, -34f);
                    pieces = PieceShape.Cube; pieceCount = 3; verb = "Sacar"; break;
                case "Efervescente":
                    L(PrimitiveType.Cylinder, "Jar", t, new Vector3(0f, 0.06f, 0f), new Vector3(0.085f, 0.06f, 0.085f), SB.Mat("GlassClear", new Color(0.8f, 0.95f, 1f, 0.22f), transparent: true));
                    for (int k = 0; k < 4; k++)
                        L(PrimitiveType.Cylinder, "Tablet", t, new Vector3(0f, 0.012f + k * 0.022f, 0f), new Vector3(0.06f, 0.008f, 0.06f), SB.Mat("TabletWhite", Color.white));
                    L(PrimitiveType.Cylinder, "Lid", t, new Vector3(0f, 0.127f, 0f), new Vector3(0.09f, 0.008f, 0.09f), SB.Mat("CounterTop", Copper));
                    pieces = PieceShape.Disc; verb = "Soltar"; break;
            }

            var b = SB.Bounds(go);
            var col = go.AddComponent<BoxCollider>();
            col.center = b.center - pos;
            col.size = new Vector3(Mathf.Max(b.size.x, 0.1f), Mathf.Max(b.size.y, 0.1f), Mathf.Max(b.size.z, 0.1f));

            // Etiqueta flotante orientada hacia el puesto del jugador (alturas alternadas para que no se monten)
            Vector3 labelPos = new Vector3(pos.x, Top + 0.37f + (slot % 2) * 0.085f, pos.z);
            Vector3 eye = PlayerStart + Vector3.up * 1.65f;
            var canvas = SB.MakeCanvas("Label_" + id, labelPos, Quaternion.LookRotation(labelPos - eye), new Vector2(330, 130), labelScale);
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            canvas.transform.SetParent(parent, true);
            SB.Panel(canvas.transform, "Bg", PanelBg);
            string hex = ColorUtility.ToHtmlStringRGB(Color.Lerp(data.color, Color.white, 0.35f));
            SB.Label(canvas.transform, "Text", $"<color=#{hex}><b>{data.displayName}</b></color>\n<size=26>{data.tag}</size>", 40,
                TextAnchor.MiddleCenter, Muted, new Vector2(6, 4), new Vector2(-6, -4));

            var bottle = go.AddComponent<Bottle>();
            bottle.Setup(data, glass, canvas.gameObject, Mathf.Max(0.1f, b.size.y));
            if (pieces.HasValue) bottle.SetPieces(pieces.Value, pieceCount, verb);
        }

        // =====================================================================
        // Cliente principal
        // =====================================================================

        static GameObject L(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, float rotZ = 0f)
        {
            var go = SB.Prim(type, name, parent, localPos, scale, mat, false, local: true);
            if (rotZ != 0f) go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            return go;
        }

        static Transform Group(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        static Customer BuildCustomer()
        {
            var root = new GameObject("Customer");
            root.transform.SetPositionAndRotation(CustomerPos, Quaternion.Euler(0f, 180f, 0f));   // +Z local mira al jugador
            var skin = SB.Mat("CustomerSkin", Color.white);       // el color lo pone Customer con MaterialPropertyBlock
            var shirt = SB.Mat("CustomerShirt", new Color(0.24f, 0.44f, 0.72f));
            var pants = SB.Mat("CustomerPants", new Color(0.17f, 0.18f, 0.26f));
            var shoes = SB.Mat("CustomerShoes", new Color(0.35f, 0.2f, 0.12f));
            var hair = SB.Mat("CustomerHair", new Color(0.23f, 0.15f, 0.09f));
            var darkM = SB.Mat("FaceDark", new Color(0.13f, 0.09f, 0.16f));
            var white = SB.Mat("TabletWhite", Color.white);
            var cheekM = SB.Mat("CustomerCheek", Color.white);

            SB.Solid(SB.Fit("FurnitureKit", "stoolBar", null, CustomerPos, 0.4f, 0f)).name = "Stool_Customer";

            // Piernas (sentado): muslos hacia la barra, canillas hacia abajo, zapatos en el reposapiés
            foreach (float x in new[] { -0.1f, 0.1f })
            {
                var thigh = L(PrimitiveType.Capsule, "Thigh", root.transform, new Vector3(x, 0.8f, 0.15f), new Vector3(0.14f, 0.18f, 0.14f), pants);
                thigh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                L(PrimitiveType.Capsule, "Shin", root.transform, new Vector3(x, 0.58f, 0.31f), new Vector3(0.11f, 0.2f, 0.11f), pants);
                L(PrimitiveType.Cube, "Shoe", root.transform, new Vector3(x, 0.37f, 0.35f), new Vector3(0.1f, 0.07f, 0.2f), shoes);
            }

            var pivot = Group("Pivot", root.transform, new Vector3(0f, 0.82f, 0f));
            L(PrimitiveType.Capsule, "Body", pivot, new Vector3(0f, 0.26f, 0f), new Vector3(0.44f, 0.27f, 0.3f), shirt);
            L(PrimitiveType.Cylinder, "Skin_Neck", pivot, new Vector3(0f, 0.55f, 0f), new Vector3(0.11f, 0.04f, 0.11f), skin);
            // Brazos: Customer los coloca con cinemática inversa (hombro → codo → mano)
            foreach (var side in new[] { "L", "R" })
            {
                L(PrimitiveType.Capsule, $"Arm{side}_Upper", pivot, Vector3.zero, new Vector3(0.105f, 0.13f, 0.105f), shirt);
                L(PrimitiveType.Capsule, $"Arm{side}_Fore", pivot, Vector3.zero, new Vector3(0.09f, 0.13f, 0.09f), shirt);
                L(PrimitiveType.Sphere, $"Skin_Hand{side}", pivot, Vector3.zero, Vector3.one * 0.1f, skin);
            }

            var head = Group("Head", pivot, new Vector3(0f, 0.75f, 0.02f));
            L(PrimitiveType.Sphere, "Skin_Skull", head, Vector3.zero, Vector3.one * 0.36f, skin);
            L(PrimitiveType.Sphere, "Skin_EarL", head, new Vector3(-0.182f, 0f, 0f), Vector3.one * 0.085f, skin);
            L(PrimitiveType.Sphere, "Skin_EarR", head, new Vector3(0.182f, 0f, 0f), Vector3.one * 0.085f, skin);
            L(PrimitiveType.Sphere, "Skin_Nose", head, new Vector3(0f, -0.025f, 0.178f), new Vector3(0.045f, 0.04f, 0.04f), skin);

            var hairNormal = Group("HairNormal", head, Vector3.zero);
            // El pelo deja la frente libre para que se vean las cejas
            L(PrimitiveType.Sphere, "Cap", hairNormal, new Vector3(0f, 0.1f, -0.05f), new Vector3(0.385f, 0.22f, 0.36f), hair);
            L(PrimitiveType.Sphere, "Fringe", hairNormal, new Vector3(0.05f, 0.165f, 0.06f), new Vector3(0.2f, 0.07f, 0.14f), hair);
            var hairUp = Group("HairUp", head, Vector3.zero);
            L(PrimitiveType.Sphere, "Base", hairUp, new Vector3(0f, 0.09f, -0.03f), new Vector3(0.37f, 0.2f, 0.36f), hair);
            float[] sx = { -0.12f, -0.06f, 0f, 0.06f, 0.12f, -0.03f, 0.04f };
            for (int i = 0; i < sx.Length; i++)
                L(PrimitiveType.Capsule, "Spike", hairUp, new Vector3(sx[i], 0.2f + (i % 2) * 0.03f, i > 4 ? -0.08f : 0f), new Vector3(0.055f, 0.1f, 0.055f), hair, -sx[i] * 220f);

            // Otros peinados y accesorios: Customer elige uno según el cliente de turno
            var accent = SB.Mat("CustomerAccent", new Color(0.2f, 0.35f, 0.8f));
            var hairLong = Group("HairLong", head, Vector3.zero);
            L(PrimitiveType.Sphere, "Cap", hairLong, new Vector3(0f, 0.1f, -0.05f), new Vector3(0.39f, 0.22f, 0.37f), hair);
            L(PrimitiveType.Sphere, "Back", hairLong, new Vector3(0f, -0.04f, -0.1f), new Vector3(0.38f, 0.44f, 0.22f), hair);
            L(PrimitiveType.Sphere, "LockL", hairLong, new Vector3(-0.165f, -0.07f, -0.01f), new Vector3(0.08f, 0.28f, 0.15f), hair);
            L(PrimitiveType.Sphere, "LockR", hairLong, new Vector3(0.165f, -0.07f, -0.01f), new Vector3(0.08f, 0.28f, 0.15f), hair);
            L(PrimitiveType.Sphere, "Fringe", hairLong, new Vector3(0f, 0.16f, 0.06f), new Vector3(0.3f, 0.07f, 0.13f), hair);
            hairLong.gameObject.SetActive(false);
            var hairBun = Group("HairBun", head, Vector3.zero);
            L(PrimitiveType.Sphere, "Cap", hairBun, new Vector3(0f, 0.1f, -0.05f), new Vector3(0.385f, 0.22f, 0.36f), hair);
            L(PrimitiveType.Sphere, "Bun", hairBun, new Vector3(0f, 0.21f, -0.12f), Vector3.one * 0.15f, hair);
            hairBun.gameObject.SetActive(false);
            var hairCurly = Group("HairCurly", head, Vector3.zero);
            L(PrimitiveType.Sphere, "Afro", hairCurly, new Vector3(0f, 0.12f, -0.07f), new Vector3(0.46f, 0.36f, 0.42f), hair);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 5f;
                L(PrimitiveType.Sphere, "Curl", hairCurly, new Vector3(Mathf.Cos(a) * 0.2f, 0.12f + Mathf.Sin(a) * 0.14f, -0.07f), Vector3.one * 0.13f, hair);
            }
            hairCurly.gameObject.SetActive(false);
            var hairBald = Group("HairBald", head, Vector3.zero);
            L(PrimitiveType.Sphere, "SideL", hairBald, new Vector3(-0.155f, 0f, -0.05f), new Vector3(0.09f, 0.12f, 0.2f), hair);
            L(PrimitiveType.Sphere, "SideR", hairBald, new Vector3(0.155f, 0f, -0.05f), new Vector3(0.09f, 0.12f, 0.2f), hair);
            L(PrimitiveType.Sphere, "Back", hairBald, new Vector3(0f, 0f, -0.14f), new Vector3(0.26f, 0.14f, 0.1f), hair);
            hairBald.gameObject.SetActive(false);
            var capAcc = Group("Acc_Cap", head, Vector3.zero);
            L(PrimitiveType.Sphere, "Crown", capAcc, new Vector3(0f, 0.12f, -0.02f), new Vector3(0.395f, 0.22f, 0.385f), accent);
            L(PrimitiveType.Cube, "Visor", capAcc, new Vector3(0f, 0.135f, 0.18f), new Vector3(0.27f, 0.018f, 0.15f), accent);
            capAcc.gameObject.SetActive(false);
            var beard = Group("Acc_Beard", head, Vector3.zero);
            L(PrimitiveType.Sphere, "Beard", beard, new Vector3(0f, -0.16f, 0.06f), new Vector3(0.27f, 0.12f, 0.22f), hair);
            L(PrimitiveType.Sphere, "SideL", beard, new Vector3(-0.12f, -0.09f, 0.06f), new Vector3(0.07f, 0.12f, 0.1f), hair);
            L(PrimitiveType.Sphere, "SideR", beard, new Vector3(0.12f, -0.09f, 0.06f), new Vector3(0.07f, 0.12f, 0.1f), hair);
            beard.gameObject.SetActive(false);

            // Cejas
            var brows = Group("Brows", head, Vector3.zero);
            L(PrimitiveType.Cube, "BrowL", brows, new Vector3(-0.07f, 0.092f, 0.158f), new Vector3(0.075f, 0.018f, 0.02f), hair);
            L(PrimitiveType.Cube, "BrowR", brows, new Vector3(0.07f, 0.092f, 0.158f), new Vector3(0.075f, 0.018f, 0.02f), hair);

            // Ojos
            var eyesOpen = Group("EyesOpen", head, Vector3.zero);
            L(PrimitiveType.Sphere, "WhiteL", eyesOpen, new Vector3(-0.07f, 0.03f, 0.152f), new Vector3(0.085f, 0.095f, 0.05f), white);
            L(PrimitiveType.Sphere, "WhiteR", eyesOpen, new Vector3(0.07f, 0.03f, 0.152f), new Vector3(0.085f, 0.095f, 0.05f), white);
            L(PrimitiveType.Sphere, "PupilL", eyesOpen, new Vector3(-0.07f, 0.03f, 0.176f), new Vector3(0.04f, 0.046f, 0.02f), darkM);
            L(PrimitiveType.Sphere, "PupilR", eyesOpen, new Vector3(0.07f, 0.03f, 0.176f), new Vector3(0.04f, 0.046f, 0.02f), darkM);
            var lids = Group("Lids", head, Vector3.zero);
            L(PrimitiveType.Cube, "Skin_LidL", lids, new Vector3(-0.07f, 0.066f, 0.16f), new Vector3(0.092f, 0.04f, 0.05f), skin);
            L(PrimitiveType.Cube, "Skin_LidR", lids, new Vector3(0.07f, 0.066f, 0.16f), new Vector3(0.092f, 0.04f, 0.05f), skin);
            var closed = Group("EyesClosed", head, Vector3.zero);
            L(PrimitiveType.Cube, "L", closed, new Vector3(-0.07f, 0.03f, 0.172f), new Vector3(0.085f, 0.017f, 0.02f), darkM, 18f);
            L(PrimitiveType.Cube, "R", closed, new Vector3(0.07f, 0.03f, 0.172f), new Vector3(0.085f, 0.017f, 0.02f), darkM, -18f);
            var happy = Group("EyesHappy", head, Vector3.zero);
            foreach (float x in new[] { -0.07f, 0.07f })
            {
                L(PrimitiveType.Cube, "A", happy, new Vector3(x - 0.02f, 0.035f, 0.172f), new Vector3(0.055f, 0.016f, 0.02f), darkM, 35f);
                L(PrimitiveType.Cube, "B", happy, new Vector3(x + 0.02f, 0.035f, 0.172f), new Vector3(0.055f, 0.016f, 0.02f), darkM, -35f);
            }
            var xs = Group("EyesX", head, Vector3.zero);
            foreach (float x in new[] { -0.07f, 0.07f })
            {
                L(PrimitiveType.Cube, "A", xs, new Vector3(x, 0.03f, 0.172f), new Vector3(0.09f, 0.018f, 0.02f), darkM, 45f);
                L(PrimitiveType.Cube, "B", xs, new Vector3(x, 0.03f, 0.172f), new Vector3(0.09f, 0.018f, 0.02f), darkM, -45f);
            }
            var flat = Group("EyesFlat", head, Vector3.zero);
            L(PrimitiveType.Cube, "L", flat, new Vector3(-0.07f, 0.03f, 0.172f), new Vector3(0.08f, 0.017f, 0.02f), darkM);
            L(PrimitiveType.Cube, "R", flat, new Vector3(0.07f, 0.03f, 0.172f), new Vector3(0.08f, 0.017f, 0.02f), darkM);

            // Bocas
            var smile = Group("MouthSmile", head, Vector3.zero);
            L(PrimitiveType.Cube, "C", smile, new Vector3(0f, -0.093f, 0.16f), new Vector3(0.06f, 0.015f, 0.02f), darkM);
            L(PrimitiveType.Cube, "L", smile, new Vector3(-0.043f, -0.081f, 0.155f), new Vector3(0.04f, 0.015f, 0.02f), darkM, -35f);
            L(PrimitiveType.Cube, "R", smile, new Vector3(0.043f, -0.081f, 0.155f), new Vector3(0.04f, 0.015f, 0.02f), darkM, 35f);
            L(PrimitiveType.Sphere, "MouthOpen", head, new Vector3(0f, -0.092f, 0.155f), new Vector3(0.055f, 0.07f, 0.03f), darkM);
            var wavy = Group("MouthWavy", head, Vector3.zero);
            float[] wx = { -0.045f, -0.015f, 0.015f, 0.045f };
            for (int i = 0; i < wx.Length; i++)
                L(PrimitiveType.Cube, "W" + i, wavy, new Vector3(wx[i], -0.092f, 0.16f - Mathf.Abs(wx[i]) * 0.12f), new Vector3(0.036f, 0.015f, 0.02f), darkM, i % 2 == 0 ? 30f : -30f);
            L(PrimitiveType.Cube, "MouthFlat", head, new Vector3(0f, -0.092f, 0.161f), new Vector3(0.09f, 0.015f, 0.02f), darkM);
            L(PrimitiveType.Sphere, "Tongue", head, new Vector3(0.02f, -0.116f, 0.163f), new Vector3(0.045f, 0.06f, 0.03f), SB.Mat("Tongue", new Color(1f, 0.48f, 0.55f)));
            L(PrimitiveType.Cube, "Teeth", head, new Vector3(0f, -0.092f, 0.162f), new Vector3(0.1f, 0.04f, 0.02f), white);
            L(PrimitiveType.Sphere, "Flame", head, new Vector3(0f, -0.09f, 0.182f), new Vector3(0.05f, 0.07f, 0.04f), SB.Mat("Flame", new Color(1f, 0.6f, 0.1f), emissive: true));

            var cheeks = Group("Cheeks", head, Vector3.zero);
            L(PrimitiveType.Sphere, "L", cheeks, new Vector3(-0.115f, -0.045f, 0.125f), new Vector3(0.075f, 0.06f, 0.05f), cheekM);
            L(PrimitiveType.Sphere, "R", cheeks, new Vector3(0.115f, -0.045f, 0.125f), new Vector3(0.075f, 0.06f, 0.05f), cheekM);

            // Extras de reacciones
            var splash = Group("Splash", head, Vector3.zero);
            var splashM = SB.Mat("Splash", new Color(0.85f, 0.97f, 0.75f));
            Vector3[] blobs = { new Vector3(-0.09f, 0.1f, 0.14f), new Vector3(0.1f, -0.02f, 0.15f), new Vector3(0.02f, 0.12f, 0.15f), new Vector3(-0.12f, -0.07f, 0.11f), new Vector3(0.12f, 0.09f, 0.11f), new Vector3(-0.02f, -0.03f, 0.175f) };
            for (int i = 0; i < blobs.Length; i++)
                L(PrimitiveType.Sphere, "Blob", splash, blobs[i], Vector3.one * (0.04f + (i % 3) * 0.018f), splashM);
            var icicles = Group("Icicles", head, Vector3.zero);
            var iceM = SB.Mat("IceCube", new Color(0.85f, 0.96f, 1f));
            float[] ix = { -0.13f, -0.065f, 0f, 0.065f, 0.13f };
            for (int i = 0; i < ix.Length; i++)
                L(PrimitiveType.Sphere, "Icicle", icicles, new Vector3(ix[i], 0.2f - Mathf.Abs(ix[i]) * 0.4f, 0.02f), new Vector3(0.04f, 0.11f + (i % 2) * 0.03f, 0.04f), iceM);
            L(PrimitiveType.Sphere, "Frost", icicles, new Vector3(0f, 0.1f, -0.02f), new Vector3(0.39f, 0.22f, 0.38f), iceM);
            var stars = Group("Stars", head, new Vector3(0f, 0.3f, 0f));
            var starM = SB.Mat("Star", new Color(1f, 0.84f, 0.29f), emissive: true);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                L(PrimitiveType.Cube, "Star", stars, new Vector3(Mathf.Cos(a) * 0.17f, 0f, Mathf.Sin(a) * 0.17f), Vector3.one * 0.05f, starM)
                    .transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            }

            // Partículas
            Fx("FX_Steam", head, new Vector3(0f, 0.02f, 0f), Color.white, new Color(1f, 1f, 1f, 0.6f), 26f, 0.9f, 0.55f, 0.09f, -0.05f, ParticleSystemShapeType.Box, 0.2f, 0, new Vector3(0.46f, 0.02f, 0.02f));
            Fx("FX_Sweat", head, new Vector3(0f, 0.05f, 0.05f), new Color(0.55f, 0.84f, 1f), new Color(0.7f, 0.9f, 1f), 14f, 0.8f, 0.25f, 0.035f, 0.9f, ParticleSystemShapeType.Sphere, 0.19f);
            Fx("FX_Smoke", head, new Vector3(0f, 0.2f, 0f), new Color(0.2f, 0.2f, 0.22f), new Color(0.4f, 0.4f, 0.42f), 16f, 1.3f, 0.5f, 0.16f, -0.05f, ParticleSystemShapeType.Cone, 0.1f);
            Fx("FX_Hearts", head, new Vector3(0f, 0.22f, 0f), new Color(1f, 0.35f, 0.5f), new Color(1f, 0.75f, 0.3f), 12f, 1.2f, 0.6f, 0.06f, -0.02f, ParticleSystemShapeType.Cone, 0.16f);

            BuildSymptoms(head);

            var mouthPoint = Group("MouthPoint", root.transform, new Vector3(0f, 1.47f, 0.27f));

            // Posavasos (destino para servir)
            var coaster = SB.Prim(PrimitiveType.Cylinder, "Coaster", null, new Vector3(0f, Top + 0.004f, 1.02f), new Vector3(0.14f, 0.004f, 0.14f), SB.Mat("CounterBody", new Color(0.1f, 0.24f, 0.28f)), false);
            var cc = coaster.AddComponent<BoxCollider>(); cc.size = new Vector3(1f, 6f, 1f);
            coaster.AddComponent<ServeSpot>();
            var coasterPoint = Group("CoasterPoint", coaster.transform, Vector3.zero);
            coasterPoint.position = new Vector3(0f, Top + 0.012f, 1.02f);

            var bodyCol = root.AddComponent<CapsuleCollider>();
            bodyCol.center = new Vector3(0f, 1.25f, 0f); bodyCol.height = 1.1f; bodyCol.radius = 0.27f;
            root.AddComponent<ServeSpot>();

            // Globo de diálogo
            var bubble = SB.MakeCanvas("SpeechBubble", new Vector3(0.7f, 1.6f, SeatZ - 0.12f), Quaternion.Euler(0f, 14f, 0f), new Vector2(600, 215), 0.001f);
            Object.DestroyImmediate(bubble.GetComponent<GraphicRaycaster>());
            bubble.transform.SetParent(root.transform, true);
            SB.Panel(bubble.transform, "Bg", Ink);
            var bubbleText = SB.Label(bubble.transform, "Text", "", 36, TextAnchor.MiddleCenter, new Color(0.16f, 0.11f, 0.29f), new Vector2(22, 14), new Vector2(-22, -14));
            bubbleText.fontStyle = FontStyle.Bold;

            var customer = root.AddComponent<Customer>();
            customer.Setup("Ramiro", Skin, mouthPoint, coasterPoint, bubble.gameObject, bubbleText);
            customer.ResetPose();    // deja brazos y cara en su pose inicial dentro de la escena
            return customer;
        }

        static ParticleSystem Fx(string name, Transform parent, Vector3 localPos, Color a, Color b, float rate, float life, float speed,
                                 float size, float gravity, ParticleSystemShapeType shape, float radius, int burst = 0, Vector3? boxScale = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // emite hacia arriba
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = burst == 0; main.duration = 1f;
            main.startLifetime = life; main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var em = ps.emission;
            em.rateOverTime = burst == 0 ? rate : 0f;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var sh = ps.shape;
            sh.shapeType = shape; sh.radius = radius; sh.angle = 16f;
            if (boxScale.HasValue) sh.scale = boxScale.Value;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
            return ps;
        }

    }
}
