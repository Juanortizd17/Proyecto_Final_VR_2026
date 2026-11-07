using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Renderiza vistas de la escena a docs/screenshots/*.png (QA visual y material para la sustentación).
    /// Pone la escena en un "estado de demostración" en memoria (no guarda cambios).
    /// </summary>
    public static class SceneSnapshot
    {
        const string OutDir = "docs/screenshots";

        [MenuItem("Tools/Quality Control XR/Capture Screenshots")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);
            StageDemo();
            Directory.CreateDirectory(OutDir);

            Shot("01_vista_general", new Vector3(3.6f, 3.2f, -4.8f), new Vector3(0f, 0.8f, -0.2f), 60f);
            Shot("02_puesto_de_trabajo", new Vector3(0f, 1.62f, -0.75f), new Vector3(0f, 1.2f, 1.3f), 75f);
            Shot("03_herramientas", new Vector3(-0.25f, 1.45f, -0.25f), new Vector3(-1.2f, 0.82f, 0.15f), 65f);
            Shot("04_contenedores", new Vector3(-0.3f, 1.55f, -0.4f), new Vector3(1.2f, 0.85f, -1.05f), 70f);
            Shot("05_onboarding", new Vector3(0f, 1.6f, -4.5f), new Vector3(0f, 1.2f, -2.8f), 70f);
            Shot("06_banda_detalle", new Vector3(-1.2f, 1.4f, 0.1f), new Vector3(0.2f, 0.9f, 0.95f), 65f);
            Debug.Log("[Snapshot] Capturas guardadas en " + Path.GetFullPath(OutDir));
        }

        static void StageDemo()
        {
            // Tablero principal mostrando una partida en curso
            var board = GameObject.Find("MainBoard");
            if (board != null)
            {
                SetActive(board.transform, "WaitingPanel", false);
                SetActive(board.transform, "GamePanel", true);
                SetText(board.transform, "GamePanel/Timer", "TIEMPO\n2:14");
                SetText(board.transform, "GamePanel/Score", "PUNTOS\n450");
                SetText(board.transform, "GamePanel/Mistakes", "FALLOS\n1/5");
                SetText(board.transform, "GamePanel/Alert", "¡ALERTA! La banda se aceleró");
                SetText(board.transform, "GamePanel/Report", "Reporte: Radio desechado - tornillo flojo");
            }
            var onboarding = GameObject.Find("OnboardingBoard");
            if (onboarding != null)
                SetText(onboarding.transform, "Instruction",
                    "PASO 2 · AGARRAR\nAcerca la mano a la caja y mantén el botón lateral (GRIP).");

            // Productos de ejemplo sobre la banda: uno con defecto revelado y uno aprobado
            var spawn = GameObject.Find("SpawnPoint");
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Products" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            if (spawn != null && prefabs.Length > 0)
            {
                Vector3 p0 = spawn.transform.position;
                for (int i = 0; i < prefabs.Length; i++)
                {
                    var go = (GameObject)Object.Instantiate(prefabs[i], new Vector3(p0.x + 0.55f + i * 0.85f, 0.9f, p0.z), Quaternion.Euler(0f, 180f, 0f));
                    var prod = go.GetComponent<Product>();
                    if (i == 1) { prod.Configure(prod.Type, DefectType.LooseScrew); prod.Inspect(DefectType.LooseScrew); }
                    if (i == 3)
                    {
                        prod.Configure(prod.Type, DefectType.None);
                        prod.Inspect(DefectType.Crack); prod.Inspect(DefectType.LooseScrew); prod.Inspect(DefectType.Hollow);
                    }
                }
            }
            Canvas.ForceUpdateCanvases();
        }

        static void Shot(string name, Vector3 pos, Vector3 lookAt, float fov)
        {
            var go = new GameObject("SnapshotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.02f;
            cam.clearFlags = CameraClearFlags.Skybox;

            const int w = 1920, h = 1080;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }

        static void SetActive(Transform root, string path, bool on)
        {
            var t = root.Find(path);
            if (t) t.gameObject.SetActive(on);
        }

        static void SetText(Transform root, string path, string value)
        {
            var t = root.Find(path);
            var txt = t ? t.GetComponent<Text>() : null;
            if (txt) txt.text = value;
        }
    }

    /// <summary>Pipeline completo en batchmode: configurar Quest + generar escena + capturas.</summary>
    public static class Pipeline
    {
        public static void RunAll()
        {
            ProjectSetup.ConfigureQuest();
            SceneBuilder.Build();
            SceneSnapshot.Capture();
        }

        /// <summary>Plataforma Windows: regenera la escena y compila el .exe.</summary>
        public static void WindowsAll()
        {
            SceneBuilder.Build();
            BuildQuest.BuildWindows();
        }
    }
}
