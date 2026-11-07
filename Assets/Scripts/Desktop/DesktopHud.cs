using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QualityControlXR
{
    /// <summary>
    /// Interfaz en pantalla del modo PC (se construye sola al iniciar):
    /// mira central, indicaciones contextuales, herramienta activa, ayuda de controles (H)
    /// y menú de pausa (Esc) con sensibilidad del mouse.
    /// </summary>
    public class DesktopHud : MonoBehaviour
    {
        static readonly Color Panel = new Color(0.06f, 0.07f, 0.09f, 0.82f);
        static readonly Color Accent = new Color(1f, 0.78f, 0.1f);

        DesktopPlayer player;
        DesktopInteractor interactor;
        Font font;
        Image crosshair;
        Text prompt, toolText, helpText, sensValue;
        GameObject helpPanel, pausePanel;
        Slider sensSlider;

        void Start()
        {
            player = FindFirstObjectByType<DesktopPlayer>();
            interactor = FindFirstObjectByType<DesktopInteractor>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) SetPaused(!pausePanel.activeSelf);
            if (kb != null && kb.hKey.wasPressedThisFrame) helpPanel.SetActive(!helpPanel.activeSelf);
            if (pausePanel.activeSelf || interactor == null) return;

            prompt.text = interactor.Prompt;
            crosshair.color = interactor.HasTarget ? Accent : new Color(1f, 1f, 1f, 0.85f);
            crosshair.rectTransform.sizeDelta = Vector2.one * (interactor.HasTarget ? 12f : 7f);

            string tool = interactor.Equipped ? interactor.Equipped.ToolName : "ninguna";
            string hand = interactor.Held ? interactor.Held.DisplayName : "vacía";
            toolText.text = $"<b>Herramienta:</b> <color=#FFC21A>{tool}</color>     <b>Mano:</b> {hand}\n" +
                            "<size=17>[1] Lupa   [2] Destornillador   [3] Martillo   [R] Guardar</size>";
        }

        public void SetPaused(bool paused)
        {
            pausePanel.SetActive(paused);
            if (player) player.Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            DesktopPlayer.SetCursorLocked(!paused);
        }

        void OnDestroy() => Time.timeScale = 1f;

        // =====================================================================
        // Construcción de la UI
        // =====================================================================

        void Build()
        {
            var canvasGo = new GameObject("DesktopHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            // Mira
            crosshair = MakeImage(root, "Crosshair", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 7f);
            var outline = crosshair.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f); outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Indicación contextual bajo la mira
            prompt = MakeText(root, "Prompt", "", 24, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 90f));
            AddShadow(prompt);

            // Herramienta activa (abajo a la derecha)
            var toolBg = MakeImage(root, "ToolPanel", Panel, new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(620f, 74f), new Vector2(1f, 0f));
            toolText = MakeText(toolBg.transform, "Text", "", 21, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590f, 70f));

            // Ayuda de controles (abajo a la izquierda)
            var help = MakeImage(root, "HelpPanel", Panel, new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(420f, 210f), new Vector2(0f, 0f));
            helpPanel = help.gameObject;
            helpText = MakeText(help.transform, "Text",
                "<b><color=#FFC21A>CONTROLES</color></b>\n" +
                "WASD  caminar   ·   Shift  correr\n" +
                "Mouse  mirar\n" +
                "Clic izq  agarrar / soltar / botones\n" +
                "Clic der  usar herramienta\n" +
                "1 2 3  elegir herramienta   ·   R  guardar\n" +
                "Q  lanzar producto\n" +
                "Esc  pausa   ·   H  ocultar ayuda",
                19, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(390f, 190f));

            // Menú de pausa
            var pause = MakeImage(root, "PausePanel", new Color(0f, 0f, 0f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
            pausePanel = pause.gameObject;
            var box = MakeImage(pause.transform, "Box", Panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 470f));
            var title = MakeText(box.transform, "Title", "PAUSA", 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(500f, 70f));
            title.color = Accent; title.fontStyle = FontStyle.Bold;
            MakeButton(box.transform, "Continuar", new Vector2(0f, 95f), new Color(0.12f, 0.55f, 0.3f), () => SetPaused(false));
            MakeButton(box.transform, "Reiniciar turno", new Vector2(0f, 25f), new Color(0.22f, 0.3f, 0.45f), () =>
            {
                SetPaused(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
            MakeButton(box.transform, "Salir", new Vector2(0f, -45f), new Color(0.55f, 0.15f, 0.15f), () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            MakeText(box.transform, "SensLabel", "Sensibilidad del mouse", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -115f), new Vector2(500f, 34f));
            sensSlider = MakeSlider(box.transform, new Vector2(-30f, -160f));
            sensValue = MakeText(box.transform, "SensValue", "", 22, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(215f, -160f), new Vector2(80f, 34f));
            sensSlider.minValue = 0.02f; sensSlider.maxValue = 0.5f;
            sensSlider.value = player ? player.Sensitivity : DesktopPlayer.DefaultSensitivity;
            sensValue.text = (sensSlider.value * 10f).ToString("0.0");
            sensSlider.onValueChanged.AddListener(v => { if (player) player.Sensitivity = v; sensValue.text = (v * 10f).ToString("0.0"); });
            pausePanel.SetActive(false);
        }

        Image MakeImage(Transform parent, string name, Color c, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = c; img.raycastTarget = name == "PausePanel" || name == "Box";
            return img;
        }

        Text MakeText(Transform parent, string name, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void AddShadow(Text t)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f); o.effectDistance = new Vector2(2f, -2f);
        }

        void MakeButton(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var img = MakeImage(parent, "Button_" + label, color, new Vector2(0.5f, 0.5f), pos, new Vector2(380f, 56f));
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            b.colors = cb;
            b.onClick.AddListener(onClick);
            var t = MakeText(img.transform, "Label", "<b>" + label + "</b>", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 56f));
        }

        Slider MakeSlider(Transform parent, Vector2 pos)
        {
            var bg = MakeImage(parent, "Slider", new Color(0.25f, 0.27f, 0.3f), new Vector2(0.5f, 0.5f), pos, new Vector2(380f, 14f));
            bg.raycastTarget = true;
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(bg.transform, false);
            fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one; fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = MakeImage(fillArea, "Fill", Accent, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(bg.transform, false);
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one; handleArea.offsetMin = handleArea.offsetMax = Vector2.zero;
            var handle = MakeImage(handleArea, "Handle", Color.white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(26f, 30f));
            handle.raycastTarget = true;
            var s = bg.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            return s;
        }
    }
}
