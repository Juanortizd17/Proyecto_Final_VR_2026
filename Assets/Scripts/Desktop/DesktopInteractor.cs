using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace QualityControlXR
{
    /// <summary>
    /// Interacción en PC desde el centro de la pantalla (mira):
    ///  · Clic izquierdo: agarrar/soltar producto, tomar herramienta, depositar en contenedor, pulsar botones.
    ///  · Clic derecho: usar la herramienta sobre el producto apuntado o el que se tiene en la mano.
    ///  · Q: lanzar el producto · 1/2/3: lupa/destornillador/martillo · R: devolver la herramienta.
    /// </summary>
    public class DesktopInteractor : MonoBehaviour, IAimHud
    {
        [SerializeField] Transform productHold;
        [SerializeField] Transform toolHold;
        [SerializeField] float reach = 3.0f;
        [SerializeField] float aimAssistRadius = 0.08f;   // tolerancia para objetos delgados (lupa, destornillador)
        [SerializeField] float uiReach = 7f;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip pickClip;
        [SerializeField] AudioClip dropClip;
        [SerializeField] AudioClip clickClip;

        static readonly Color HoverTint = new Color(1.45f, 1.4f, 1.15f, 1f);
        const int IgnoreRaycastLayer = 2;

        Camera cam;
        DesktopPlayer player;
        CharacterController body;
        Grabbable held;
        InspectionTool equipped;
        Grabbable hovered;
        Button hoveredButton;
        readonly Dictionary<GameObject, int> savedLayers = new Dictionary<GameObject, int>();
        MaterialPropertyBlock mpb;
        float swing;

        public Grabbable Held => held;
        public InspectionTool Equipped => equipped;
        public string Prompt { get; private set; } = "";
        public bool HasTarget { get; private set; }

        public string StatusText =>
            $"<b>Herramienta:</b> <color=#FFC21A>{(equipped ? equipped.ToolName : "ninguna")}</color>     <b>Mano:</b> {(held ? held.DisplayName : "vacía")}\n" +
            "<size=17>[1] Lupa   [2] Destornillador   [3] Martillo   [R] Guardar</size>";

        public string HelpText =>
            "<b><color=#FFC21A>CONTROLES</color></b>\n" +
            "WASD  caminar   ·   Shift  correr\n" +
            "Mouse  mirar\n" +
            "Clic izq  agarrar / soltar / botones\n" +
            "Clic der  usar herramienta\n" +
            "1 2 3  elegir herramienta   ·   R  guardar\n" +
            "Q  lanzar producto\n" +
            "Esc  pausa   ·   H  ocultar ayuda";

        public void Setup(Transform productPoint, Transform toolPoint, AudioSource source, AudioClip pick, AudioClip drop, AudioClip click)
        {
            productHold = productPoint; toolHold = toolPoint; sfx = source; pickClip = pick; dropClip = drop; clickClip = click;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            player = GetComponentInParent<DesktopPlayer>();
            body = GetComponentInParent<CharacterController>();
            mpb = new MaterialPropertyBlock();
        }

        // =====================================================================
        // Bucle principal
        // =====================================================================

        void Update()
        {
            if (player != null && player.Paused) { SetHover(null); SetHoverButton(null); return; }

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool lmb = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool rmb = mouse != null && mouse.rightButton.wasPressedThisFrame;

            // Capa "Ignore Raycast" (jugador y lo que tiene en la mano) queda fuera de la mira.
            const int mask = Physics.DefaultRaycastLayers;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool hit = Physics.Raycast(ray, out var h, reach, mask, QueryTriggerInteraction.Ignore);
            Physics.Raycast(ray, out var ht, reach, mask, QueryTriggerInteraction.Collide);

            // Asistencia de puntería: si el rayo no toca nada agarrable, probamos un "rayo grueso".
            if (!hit || h.collider.GetComponentInParent<Grabbable>() == null)
            {
                if (Physics.SphereCast(ray, aimAssistRadius, out var hs, reach, mask, QueryTriggerInteraction.Ignore) &&
                    hs.collider.GetComponentInParent<Grabbable>() != null &&
                    (!hit || hs.distance <= h.distance + 0.3f))
                { h = hs; hit = true; }
            }

            var lines = new List<string>();

            // ---- Botones de los tableros (UI en el mundo) ----
            var button = AimedButton(out float buttonDistance);
            if (button != null && (!hit || buttonDistance <= h.distance + 0.05f))
            {
                SetHover(null);
                SetHoverButton(button);
                lines.Add(Key("Clic izq") + Cap(ButtonLabel(button)));
                if (lmb) { Play(clickClip); button.onClick.Invoke(); }
                Finish(lines);
                return;
            }
            SetHoverButton(null);

            Grabbable target = hit ? h.collider.GetComponentInParent<Grabbable>() : null;
            if (target == held || (equipped && target == equipped.GetComponent<Grabbable>())) target = null;
            var aimedProduct = hit ? h.collider.GetComponentInParent<Product>() : null;
            var aimedPractice = hit ? h.collider.GetComponentInParent<PracticeTarget>() : null;
            var aimedBin = BinFrom(hit ? h.collider : null) ?? BinFrom(ht.collider);
            var aimedSocket = ht.collider ? ht.collider.GetComponentInParent<PracticeSocket>() : null;
            if (aimedSocket == null && hit) aimedSocket = SocketNear(h.point);

            SetHover(target);

            // ---- Mano izquierda: productos / caja de práctica ----
            if (held != null)
            {
                bool isPractice = held.GetComponent<PracticeTarget>() != null;
                if (aimedBin != null && !isPractice)
                {
                    lines.Add(Key("Clic izq") + "Depositar en " + aimedBin.Label);
                    if (lmb) DepositInto(aimedBin.DropPoint);
                }
                else if (aimedSocket != null && isPractice && !aimedSocket.Occupied)
                {
                    lines.Add(Key("Clic izq") + "Colocar en el soporte");
                    if (lmb) DepositInto(aimedSocket.SnapPosition + Vector3.up * 0.02f);
                }
                else
                {
                    lines.Add(Key("Clic izq") + "Soltar     " + Key("Q") + "Lanzar");
                    if (lmb) Drop(0.6f);
                }
                if (kb != null && kb.qKey.wasPressedThisFrame) Throw();
            }
            else if (target != null)
            {
                var tool = target.GetComponent<InspectionTool>();
                lines.Add(Key("Clic izq") + (tool ? "Tomar " + tool.ToolName : "Agarrar " + target.DisplayName));
                if (lmb) { if (tool) Equip(tool); else Grab(target); }
            }

            // ---- Mano derecha: herramienta ----
            if (equipped != null)
            {
                Component useTarget = null;
                if (aimedProduct != null && aimedProduct.GetComponent<Grabbable>() != held) useTarget = aimedProduct;
                else if (aimedPractice != null && aimedPractice.GetComponent<Grabbable>() != held) useTarget = aimedPractice;
                else if (held != null)
                {
                    var hp = held.GetComponent<Product>();
                    if (hp != null) useTarget = hp;
                    else
                    {
                        var hpt = held.GetComponent<PracticeTarget>();
                        if (hpt != null) useTarget = hpt;
                    }
                }

                if (useTarget != null)
                {
                    lines.Add(Key("Clic der") + "Revisar con " + equipped.ToolName);
                    if (rmb) UseToolOn(useTarget);
                }
                if (kb != null && kb.rKey.wasPressedThisFrame) Unequip();
            }

            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) EquipByType(DefectType.Crack);
                if (kb.digit2Key.wasPressedThisFrame) EquipByType(DefectType.LooseScrew);
                if (kb.digit3Key.wasPressedThisFrame) EquipByType(DefectType.Hollow);
            }

            Finish(lines);
        }

        static string Key(string k) => $"<color=#FFC21A><b>{k}</b></color>  ";
        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        void Finish(List<string> lines)
        {
            HasTarget = lines.Count > 0;
            Prompt = string.Join("\n", lines);
        }

        void LateUpdate()
        {
            if (held != null && productHold != null)
            {
                var t = held.transform;
                t.position = Vector3.Lerp(t.position, productHold.position, 1f - Mathf.Exp(-18f * Time.deltaTime));
                t.rotation = Quaternion.Slerp(t.rotation, productHold.rotation * Quaternion.Euler(0f, 180f, 0f), 1f - Mathf.Exp(-12f * Time.deltaTime));
            }
            if (equipped != null && toolHold != null)
            {
                swing = Mathf.MoveTowards(swing, 0f, Time.deltaTime * 5f);
                float s = Mathf.Sin(swing * Mathf.PI);
                var t = equipped.transform;
                t.position = toolHold.position + toolHold.forward * (0.12f * s);
                t.rotation = toolHold.rotation * Quaternion.Euler(70f + 35f * s, 0f, 0f);
            }
        }

        // =====================================================================
        // Acciones (públicas también para pruebas automáticas)
        // =====================================================================

        public void Grab(Grabbable g)
        {
            if (g == null) return;
            if (held != null) Drop(0f);
            held = g;
            MakeHeld(g, true);
            Play(pickClip);
        }

        public void Drop(float forwardSpeed)
        {
            if (held == null) return;
            var g = held; held = null;
            MakeHeld(g, false);
            var rb = g.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = cam.transform.forward * forwardSpeed;
            Play(dropClip);
        }

        public void Throw()
        {
            if (held == null) return;
            var g = held;
            Drop(0f);
            var rb = g.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = cam.transform.forward * 5.5f + Vector3.up * 1.2f;
        }

        /// <summary>Lanza el objeto en mano en un arco suave hasta un punto (contenedor o soporte).</summary>
        public void DepositInto(Vector3 point)
        {
            if (held == null) return;
            var g = held; held = null;
            RestoreLayers(g.gameObject);
            SetColliders(g.gameObject, true);
            SetIgnoreBody(g.gameObject, false);
            g.SetDesktopHeld(false);
            StartCoroutine(Fly(g, point));
            Play(dropClip);
        }

        IEnumerator Fly(Grabbable g, Vector3 target)
        {
            var rb = g.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
            Vector3 start = g.transform.position;
            for (float t = 0f; t < 1f && g != null; t += Time.deltaTime / 0.4f)
            {
                g.transform.position = Vector3.Lerp(start, target, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.25f);
                yield return null;
            }
            if (g == null) yield break;
            if (IsSnapped(g)) yield break;      // el soporte ya la encajó: se queda fija
            g.transform.position = target;
            if (rb) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; }
        }

        static bool IsSnapped(Grabbable g)
        {
            if (g.GetComponent<PracticeTarget>() == null) return false;
            var s = FindObjectsByType<PracticeSocket>(FindObjectsSortMode.None);
            return s.Any(x => x.Occupied && Vector3.Distance(x.SnapPosition, g.transform.position) < 0.05f);
        }

        public void Equip(InspectionTool tool)
        {
            if (tool == null || tool == equipped) return;
            Unequip();
            equipped = tool;
            var g = tool.GetComponent<Grabbable>();
            if (g) MakeHeld(g, true);
            Play(pickClip);
        }

        public void Unequip()
        {
            if (equipped == null) return;
            var tool = equipped; equipped = null;
            var g = tool.GetComponent<Grabbable>();
            if (g) MakeHeld(g, false);
            var home = tool.GetComponent<ReturnHome>();
            if (home) home.GoHome();
        }

        void EquipByType(DefectType type)
        {
            var tools = FindObjectsByType<InspectionTool>(FindObjectsSortMode.None).Where(t => t.Detects == type).ToArray();
            if (tools.Length == 0) return;
            var nearest = tools.OrderBy(t => (t.transform.position - transform.position).sqrMagnitude).First();
            Equip(nearest);
        }

        public void UseToolOn(Component target)
        {
            if (equipped == null || target == null || !equipped.Ready) return;
            swing = 1f;
            if (target is Product p) equipped.UseOn(p);
            else if (target is PracticeTarget pt) equipped.UseOn(pt);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        void MakeHeld(Grabbable g, bool isHeld)
        {
            var rb = g.GetComponent<Rigidbody>();
            if (isHeld)
            {
                // En la mano no colisiona: no tapa la mira ni empuja productos fuera de la banda.
                SaveLayers(g.gameObject);
                SetLayer(g.gameObject, IgnoreRaycastLayer);
                SetIgnoreBody(g.gameObject, true);
                SetColliders(g.gameObject, false);
                if (rb) { rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.None; }
                if (hovered == g) SetHover(null);
            }
            else
            {
                RestoreLayers(g.gameObject);
                SetColliders(g.gameObject, true);
                if (rb) { rb.isKinematic = false; rb.interpolation = RigidbodyInterpolation.Interpolate; }
                StartCoroutine(ReenableBodyCollision(g.gameObject));
            }
            g.SetDesktopHeld(isHeld);
        }

        IEnumerator ReenableBodyCollision(GameObject go)
        {
            yield return new WaitForSeconds(0.35f);
            if (go != null) SetIgnoreBody(go, false);
        }

        void SetIgnoreBody(GameObject go, bool ignore)
        {
            if (body == null) return;
            foreach (var c in go.GetComponentsInChildren<Collider>()) if (c) Physics.IgnoreCollision(body, c, ignore);
        }

        static void SetColliders(GameObject go, bool enabled)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = enabled;
        }

        void SaveLayers(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) savedLayers[t.gameObject] = t.gameObject.layer;
        }

        void RestoreLayers(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (savedLayers.TryGetValue(t.gameObject, out int l)) { t.gameObject.layer = l; savedLayers.Remove(t.gameObject); }
        }

        static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        void SetHover(Grabbable g)
        {
            if (hovered == g) return;
            if (hovered != null) Tint(hovered, false);
            hovered = g;
            if (hovered != null) Tint(hovered, true);
        }

        void Tint(Grabbable g, bool on)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name.Contains("Marker")) continue;
                if (on) { r.GetPropertyBlock(mpb); mpb.SetColor("_Color", HoverTint); r.SetPropertyBlock(mpb); }
                else r.SetPropertyBlock(null);
            }
        }

        void SetHoverButton(Button b)
        {
            if (hoveredButton == b) return;
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
            if (t == null) return "pulsar";
            return System.Text.RegularExpressions.Regex.Replace(t.text, "<.*?>", "").ToLower();
        }

        static AimBin BinFrom(Collider c)
        {
            if (c == null) return null;
            var bin = c.GetComponentInParent<ProductBin>();
            if (bin == null)
            {
                // Apuntar a las paredes del contenedor también cuenta
                var root = c.transform.parent;
                bin = root ? root.GetComponentInChildren<ProductBin>() : null;
            }
            return bin ? new AimBin(bin) : null;
        }

        static PracticeSocket SocketNear(Vector3 point)
        {
            foreach (var s in FindObjectsByType<PracticeSocket>(FindObjectsSortMode.None))
                if (Vector3.Distance(s.SnapPosition, point) < 0.3f) return s;
            return null;
        }

        void Play(AudioClip c) { if (sfx && c) sfx.PlayOneShot(c, 0.6f); }

        class AimBin
        {
            public readonly string Label;
            public readonly Vector3 DropPoint;
            public AimBin(ProductBin b) { Label = b.DisplayLabel; DropPoint = b.transform.position + Vector3.up * 0.05f; }
        }
    }
}
