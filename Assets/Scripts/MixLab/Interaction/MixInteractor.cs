using System.Collections.Generic;
using QualityControlXR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// "Mano" del jugador en PC: apunta con la mira del centro de la pantalla.
    ///  · Clic izquierdo: verter un ingrediente, mover palancas, agarrar el vaso, servir, botones.
    ///  · R: devolver el vaso a la estación.
    /// </summary>
    public class MixInteractor : MonoBehaviour, IAimHud
    {
        [SerializeField] Transform holdPoint;
        [SerializeField] MixGlass glass;
        [SerializeField] TemperatureLever lever;
        [SerializeField] float reach = 3.2f;
        [SerializeField] float aimAssistRadius = 0.05f;
        [SerializeField] float uiReach = 8f;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip pickClip;
        [SerializeField] AudioClip clickClip;

        Camera cam;
        DesktopPlayer player;
        Button hoveredButton;

        public bool HoldingGlass { get; private set; }
        public MixGlass Glass => glass;
        public string Prompt { get; private set; } = "";
        public bool HasTarget { get; private set; }

        public string StatusText =>
            $"<b>Vaso:</b> <color=#FFC21A>{(glass ? glass.Describe() : "-")}</color>\n" +
            $"<size=17><b>Temperatura:</b> {(lever ? lever.CurrentName : "-")}   ·   máximo 4 ingredientes</size>";

        public string HelpText =>
            "<b><color=#FFC21A>CONTROLES</color></b>\n" +
            "WASD  caminar   ·   Mouse  mirar\n" +
            "Clic izq sobre un ingrediente:  verter\n" +
            "Clic izq sobre una palanca:  usarla\n" +
            "Clic izq sobre el vaso:  agarrarlo\n" +
            "Con el vaso: apunta al cliente para servir\n" +
            "R  dejar el vaso   ·   Esc  pausa\n" +
            "M  música sí/no   ·   H  ocultar ayuda";

        public void Setup(Transform hold, MixGlass g, TemperatureLever l, AudioSource source, AudioClip pick, AudioClip click)
        {
            holdPoint = hold; glass = g; lever = l; sfx = source; pickClip = pick; clickClip = click;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            player = GetComponentInParent<DesktopPlayer>();
        }

        void Update()
        {
            Prompt = ""; HasTarget = false;
            if (player != null && player.Paused) { SetHoverButton(null); return; }

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool lmb = mouse != null && mouse.leftButton.wasPressedThisFrame;
            var lines = new List<string>();

            const int mask = Physics.DefaultRaycastLayers;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool hit = Physics.Raycast(ray, out var h, reach, mask, QueryTriggerInteraction.Ignore);

            // ---- Botones de los tableros ----
            var button = AimedButton(out float buttonDistance);
            if (button != null && (!hit || buttonDistance <= h.distance + 0.05f))
            {
                SetHoverButton(button);
                lines.Add(Key("Clic izq") + ButtonLabel(button));
                HasTarget = true;
                if (lmb) { Play(clickClip); button.onClick.Invoke(); }
                Prompt = string.Join("\n", lines);
                return;
            }
            SetHoverButton(null);

            // ---- Objetos interactivos (con asistencia de puntería para cosas delgadas) ----
            MixInteractable target = hit ? h.collider.GetComponentInParent<MixInteractable>() : null;
            if (target == null && Physics.SphereCast(ray, aimAssistRadius, out var hs, reach, mask, QueryTriggerInteraction.Ignore))
            {
                var near = hs.collider.GetComponentInParent<MixInteractable>();
                if (near != null && (!hit || hs.distance <= h.distance + 0.3f)) target = near;
            }

            string text = target != null ? target.Prompt(this) : null;
            if (!string.IsNullOrEmpty(text))
            {
                bool can = target.CanInteract(this);
                lines.Add(can ? Key("Clic izq") + text : $"<color=#C9BFE6>{text}</color>");
                HasTarget = can;
                if (can && lmb) { Play(clickClip); target.Interact(this); }
            }

            if (HoldingGlass)
            {
                lines.Add(Key("R") + "Dejar el vaso");
                if (kb != null && kb.rKey.wasPressedThisFrame) ReturnGlass();
            }
            Prompt = string.Join("\n", lines);
        }

        void LateUpdate()
        {
            if (!HoldingGlass || glass == null || holdPoint == null) return;
            var t = glass.transform;
            t.position = Vector3.Lerp(t.position, holdPoint.position, 1f - Mathf.Exp(-18f * Time.deltaTime));
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, holdPoint.eulerAngles.y, 0f), 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        // ---------- Acciones (también usadas por las pruebas y el modo demo) ----------

        public void TakeGlass()
        {
            if (HoldingGlass || glass == null || glass.Busy || !glass.AtHome) return;
            HoldingGlass = true;
            glass.SetHeld(true);
            Play(pickClip);
        }

        public void ReturnGlass()
        {
            if (!HoldingGlass) return;
            HoldingGlass = false;
            StartCoroutine(glass.ReturnHome());
            Play(pickClip);
        }

        /// <summary>Suelta el vaso sin devolverlo (lo toma el flujo de servir).</summary>
        public void ReleaseGlass() => HoldingGlass = false;

        // ---------- Helpers ----------

        static string Key(string k) => $"<color=#FFC21A><b>{k}</b></color>  ";

        void Play(AudioClip c) { if (sfx && c) sfx.PlayOneShot(c, 0.6f); }

        void SetHoverButton(Button b)
        {
            if (hoveredButton == b) return;
            if (EventSystem.current == null) { hoveredButton = b; return; }
            var ped = new PointerEventData(EventSystem.current);
            if (hoveredButton) ExecuteEvents.Execute(hoveredButton.gameObject, ped, ExecuteEvents.pointerExitHandler);
            hoveredButton = b;
            if (hoveredButton) ExecuteEvents.Execute(hoveredButton.gameObject, ped, ExecuteEvents.pointerEnterHandler);
        }

        Button AimedButton(out float distance)
        {
            distance = float.MaxValue;
            if (EventSystem.current == null) return null;
            var ped = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width / 2f, Screen.height / 2f) };
            var results = new List<RaycastResult>();
            Button best = null;
            foreach (var gr in FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
            {
                var canvas = gr.GetComponent<Canvas>();
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace || !gr.isActiveAndEnabled) continue;
                results.Clear();
                gr.Raycast(ped, results);
                foreach (var r in results)
                {
                    var b = r.gameObject.GetComponentInParent<Button>();
                    if (b == null || !b.IsInteractable()) continue;
                    float d = Vector3.Distance(cam.transform.position, r.worldPosition);
                    if (d < distance && d <= uiReach) { distance = d; best = b; }
                }
            }
            return best;
        }

        static string ButtonLabel(Button b)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t == null) return "Pulsar";
            string s = System.Text.RegularExpressions.Regex.Replace(t.text, "<.*?>", "").ToLower();
            return s.Length == 0 ? "Pulsar" : char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
