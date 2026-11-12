using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Analizador de la estación: clasifica la mezcla del vaso (estable, inestable o tóxica) y lo
    /// avisa con texto y color antes de servir. Si la mezcla es explosiva arranca una cuenta
    /// regresiva: el jugador debe estabilizarla con frío (hielo o palanca en FRÍO) o vaciarla;
    /// si no, el vaso estalla en la estación.
    /// </summary>
    public class MixAnalyzer : MonoBehaviour
    {
        [SerializeField] MixGlass glass;
        [SerializeField] TemperatureLever lever;
        [SerializeField] Text label;
        [SerializeField] Image background;
        [SerializeField] Renderer lamp;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip beep;
        [SerializeField] float fuseSeconds = 9f;

        static readonly Color Idle = new Color(0.07f, 0.05f, 0.13f, 0.94f);
        static readonly Color Green = new Color(0.1f, 0.42f, 0.2f, 0.96f);
        static readonly Color Amber = new Color(0.72f, 0.42f, 0.02f, 0.96f);
        static readonly Color Red = new Color(0.62f, 0.08f, 0.1f, 0.96f);

        MaterialPropertyBlock mpb;
        MixGameManager gm;
        MixInteractor hand;
        float fuse;
        int lastBeep = -1;
        bool wasExplosive;
        MixClass lastClass;

        public static MixAnalyzer Instance { get; private set; }
        public MixClass Current { get; private set; }
        /// <summary>Segundos que faltan para que estalle (0 si no hay peligro).</summary>
        public float Fuse => wasExplosive ? Mathf.Max(0f, fuse) : 0f;
        public string Summary { get; private set; } = "";

        public void Setup(MixGlass g, TemperatureLever l, Text text, Image bg, Renderer light, AudioSource source, AudioClip beepClip)
        {
            glass = g; lever = l; label = text; background = bg; lamp = light; sfx = source; beep = beepClip;
        }

        void Awake() { Instance = this; mpb = new MaterialPropertyBlock(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            gm = MixGameManager.Instance;
            hand = FindFirstObjectByType<MixInteractor>();
        }

        void Update()
        {
            if (glass == null || lever == null) return;
            var temp = lever.Current;
            Current = glass.Shattered ? MixClass.Vacia : ReactionRules.Classify(glass.Contents, temp);
            bool serving = gm != null && gm.Busy;
            bool explosive = !glass.Shattered && !glass.Busy && ReactionRules.WillExplode(glass.Contents, temp);

            // Cuenta regresiva de la mezcla explosiva (se detiene mientras el cliente la tiene)
            if (explosive && !wasExplosive) { fuse = fuseSeconds; lastBeep = -1; }
            wasExplosive = explosive;
            glass.Unstable = explosive && !serving;
            if (explosive && !serving)
            {
                fuse -= Time.deltaTime;
                int whole = Mathf.CeilToInt(fuse);
                if (whole != lastBeep) { lastBeep = whole; if (sfx && beep) sfx.PlayOneShot(beep, 0.7f); }
                if (fuse <= 0f)
                {
                    wasExplosive = false;
                    if (hand != null && hand.HoldingGlass) hand.ReleaseGlass();
                    StartCoroutine(glass.ExplodeAtStation());
                    if (gm != null) gm.GlassExploded();
                }
            }

            // Aviso en el tablero cuando la mezcla pasa a ser peligrosa
            if (Current != lastClass)
            {
                lastClass = Current;
                if (gm != null && !serving)
                {
                    if (Current == MixClass.Inestable) gm.Announce(explosive ? "¡MEZCLA INESTABLE! Enfríala o va a explotar" : "¡MEZCLA INFLAMABLE! Baja la temperatura o ponle hielo");
                    else if (Current == MixClass.Toxica) gm.Announce("Mezcla TÓXICA: mejor vacíala en el lavaplatos");
                }
            }

            Color bg; string text;
            switch (Current)
            {
                case MixClass.Estable:
                    bool saved = ReactionRules.IsStabilized(glass.Contents, temp);
                    bg = Green; text = saved ? "MEZCLA <b>ESTABILIZADA</b>\n<size=24>el frío la neutralizó</size>" : "MEZCLA <b>ESTABLE</b>\n<size=24>se puede servir</size>";
                    Summary = saved ? "<color=#7FE08A>ESTABILIZADA</color>" : "<color=#7FE08A>ESTABLE</color>";
                    break;
                case MixClass.Inestable:
                    bool blink = Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;
                    bg = blink ? Amber : Amber * 0.6f; bg.a = 0.96f;
                    text = explosive
                        ? $"<b>INESTABLE</b> · explota en {Mathf.CeilToInt(Mathf.Max(0f, fuse))}\n<size=24>estabilízala con FRÍO o hielo</size>"
                        : "<b>INESTABLE</b> · inflamable\n<size=24>baja la temperatura o ponle hielo</size>";
                    Summary = explosive ? $"<color=#FFC21A>INESTABLE · explota en {Mathf.CeilToInt(Mathf.Max(0f, fuse))}</color>" : "<color=#FFC21A>INESTABLE · inflamable</color>";
                    break;
                case MixClass.Toxica:
                    bg = Red; text = "MEZCLA <b>TÓXICA</b>\n<size=24>vacíala en el lavaplatos</size>";
                    Summary = "<color=#FF8073>TÓXICA</color>";
                    break;
                default:
                    bg = Idle; text = "ANALIZADOR\n<size=24>vaso vacío</size>";
                    Summary = "-";
                    break;
            }
            if (label && label.text != text) label.text = text;
            if (background) background.color = bg;
            if (lamp)
            {
                Color c = new Color(bg.r, bg.g, bg.b, 1f) * (Current == MixClass.Vacia ? 0.6f : 2.2f);
                lamp.GetPropertyBlock(mpb); mpb.SetColor("_Color", c); mpb.SetColor("_EmissionColor", c); lamp.SetPropertyBlock(mpb);
            }
        }
    }
}
