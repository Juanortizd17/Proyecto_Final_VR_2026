using System.IO;
using MixLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace QualityControlXR.EditorTools
{
    /// <summary>Renderiza vistas del Laboratorio de Mezclas a docs/screenshots/mix (revisión visual).</summary>
    public static class MixSnapshot
    {
        const string OutDir = "docs/screenshots/mix";

        [MenuItem("Tools/Mix Lab/Capture Screenshots")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(MixSceneBuilder.ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutDir);

            var customer = Object.FindFirstObjectByType<Customer>();
            customer.ResetPose();
            customer.Say("Quiero una limonada helada, por favor.");
            var instr = GameObject.Find("TutorialBoard")?.transform.Find("Instruction")?.GetComponent<Text>();
            if (instr) instr.text = "PASO 2 · VERTER\nApunta a un ingrediente del estante y haz <b>CLIC IZQUIERDO</b>.\nVierte <b>dos</b> ingredientes en el vaso.";
            Canvas.ForceUpdateCanvases();

            Shot("01_jugador", new Vector3(0f, 1.65f, -0.05f), new Vector3(0f, 1.5f, 1.6f), 72f);
            Shot("02_general", new Vector3(3.3f, 2.7f, -2.5f), new Vector3(0f, 1.2f, 1.2f), 62f);
            Shot("03_estante_izquierdo", new Vector3(-0.35f, 1.6f, 0.05f), new Vector3(-1.0f, 1.12f, 0.92f), 62f);
            Shot("04_estante_derecho", new Vector3(0.35f, 1.6f, 0.05f), new Vector3(1.05f, 1.12f, 0.92f), 62f);
            Shot("05_estacion", new Vector3(0f, 1.55f, 0.15f), new Vector3(0f, 1.05f, 0.8f), 60f);
            Shot("06_pista", new Vector3(-4.8f, 2.0f, 3.0f), new Vector3(0.6f, 0.9f, 6.4f), 68f);
            Shot("07_barra_trasera", new Vector3(0.2f, 1.65f, -0.2f), new Vector3(0.3f, 1.7f, -3f), 72f);
            Shot("08_vista_bar", new Vector3(6.4f, 2.5f, -2.7f), new Vector3(-0.6f, 1.0f, 2.8f), 72f);
            Shot("09_desde_pista", new Vector3(0.4f, 1.8f, 8.7f), new Vector3(0f, 1.3f, 1.5f), 72f);
            Shot("10_meson_L", new Vector3(0.2f, 1.65f, -0.15f), new Vector3(2.3f, 1.15f, -0.3f), 62f);
            Shot("11_rockola", new Vector3(-0.3f, 1.65f, -0.1f), new Vector3(-2.85f, 1.4f, -0.75f), 62f);
            Shot("12_sofas", new Vector3(-3.4f, 1.7f, -0.9f), new Vector3(-6.3f, 0.6f, -0.7f), 68f);

            foreach (var id in new[] { ReactionId.Infierno, ReactionId.Nausea, ReactionId.Desmayo, ReactionId.Llamarada, ReactionId.Hiper, ReactionId.Picante, ReactionId.Borracho, ReactionId.Congelado, ReactionId.Dormido, ReactionId.Perfecto })
            {
                var data = AssetDatabase.LoadAssetAtPath<ReactionData>($"Assets/Data/Mix/Reactions/{id}.asset");
                customer.ShowReaction(data);
                Canvas.ForceUpdateCanvases();
                Shot("reaccion_" + id, new Vector3(0.05f, 1.62f, 0.55f), new Vector3(0.1f, 1.52f, 1.5f), 50f);
            }
            customer.ResetPose();
            Debug.Log("[MixSnapshot] Capturas guardadas en " + Path.GetFullPath(OutDir));
        }

        static void Shot(string name, Vector3 pos, Vector3 lookAt, float fov)
        {
            var go = new GameObject("SnapshotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.02f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.09f);

            const int w = 1600, h = 900;
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
    }

    /// <summary>Pipeline del Laboratorio de Mezclas en batchmode.</summary>
    public static class MixPipeline
    {
        /// <summary>Configura PC, genera la escena y saca capturas.</summary>
        public static void BuildScene()
        {
            ProjectSetup.ConfigureDesktop();
            MixSceneBuilder.Build();
            MixSnapshot.Capture();
        }

        /// <summary>Todo lo anterior y además compila el .exe de Windows.</summary>
        public static void BuildAll()
        {
            ProjectSetup.ConfigureDesktop();
            MixSceneBuilder.Build();
            BuildQuest.BuildWindows();
        }
    }
}
