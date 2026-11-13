using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace MixLab.EditorTools
{
    /// <summary>
    /// Herramientas comunes del generador de escena: lienzos y textos de UI (con rayos para PC y VR),
    /// primitivas, modelos de Kenney ajustados a medida, materiales, audio y carpetas.
    /// Las usa <see cref="MixSceneBuilder"/>.
    /// </summary>
    public static class BuildKit
    {
        const string Kenney = "Assets/ThirdParty/Kenney";

        internal static Font font;
        internal static Camera rigCamera;

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
            if (asset == null) { Debug.LogWarning("[BuildKit] Falta modelo: " + path); return new GameObject("Missing_" + fbx); }
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
            if (guid == null) Debug.LogWarning("[BuildKit] Sin audio para " + prefix);
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
    }
}
