using UnityEngine;
using UnityEngine.InputSystem;

namespace MixLab
{
    /// <summary>
    /// Ambiente de discoteca sincronizado con la música: pista de baile de baldosas luminosas,
    /// focos de colores que barren la pista, bola de espejos con destellos, ecualizador en la
    /// pared y luces de relleno que cambian de tono. También reacciona al juego: destello verde
    /// con un pedido perfecto y rojo cuando la mezcla sale mal. Tecla M: silenciar la música.
    /// </summary>
    public class DiscoLights : MonoBehaviour
    {
        [SerializeField] AudioSource music;
        [SerializeField] float bpm = 124f;
        [SerializeField] float musicVolume = 0.3f;
        [SerializeField] Renderer[] tiles;
        [SerializeField] int tileColumns = 6;
        [SerializeField] Light[] spots;
        [SerializeField] Light[] fills;
        [SerializeField] Transform mirrorBall;
        [SerializeField] Light ballLight;
        [SerializeField] Transform[] eqBars;
        [SerializeField] Renderer[] pulseNeons;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        MaterialPropertyBlock mpb;
        Quaternion[] spotBase;
        Vector3[] eqBase;
        float[] eqLevel;
        readonly float[] spectrum = new float[256];
        Color flashColor;
        float flashUntil;
        bool muted;
        MixGameManager gm;

        /// <summary>Tiempo musical en pulsos (negras). Lo usan los bailarines para ir al ritmo.</summary>
        public static float Beat { get; private set; }
        public bool Muted => muted;

        public void Setup(AudioSource source, float tempo, Renderer[] floorTiles, int columns, Light[] spotLights, Light[] fillLights,
                          Transform ball, Light ballSpot, Transform[] bars, Renderer[] neons)
        {
            music = source; bpm = tempo; tiles = floorTiles; tileColumns = columns; spots = spotLights; fills = fillLights;
            mirrorBall = ball; ballLight = ballSpot; eqBars = bars; pulseNeons = neons;
        }

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 12);
            spotBase = new Quaternion[spots != null ? spots.Length : 0];
            for (int i = 0; i < spotBase.Length; i++) spotBase[i] = spots[i].transform.localRotation;
            eqBase = new Vector3[eqBars != null ? eqBars.Length : 0];
            eqLevel = new float[eqBase.Length];
            for (int i = 0; i < eqBase.Length; i++) eqBase[i] = eqBars[i].localScale;
            if (ballLight) ballLight.cookie = BuildCookie();
            if (music) { music.volume = musicVolume; if (!music.isPlaying) music.Play(); }
        }

        void Start()
        {
            gm = MixGameManager.Instance;
            if (gm != null) gm.OnReaction += OnReaction;
        }

        void OnDestroy()
        {
            if (gm != null) gm.OnReaction -= OnReaction;
        }

        void OnReaction(ReactionData data, bool isNew)
        {
            if (data.id == ReactionId.SinChiste) return;
            flashColor = data.points > 0 ? new Color(0.25f, 1f, 0.45f) : data.knockout ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.2f, 0.25f);
            flashUntil = Time.time + 1.3f;
        }

        /// <summary>Puntos de luz de la bola de espejos (se proyectan con un foco giratorio).</summary>
        static Texture2D BuildCookie()
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            var rng = new System.Random(7);
            for (int d = 0; d < 90; d++)
            {
                // Dentro del círculo (el foco recorta las esquinas)
                double a = rng.NextDouble() * Mathf.PI * 2, rad = 0.12 + 0.36 * System.Math.Sqrt(rng.NextDouble());
                int cx = (int)(n * (0.5 + rad * System.Math.Cos(a))), cy = (int)(n * (0.5 + rad * System.Math.Sin(a)));
                int r = rng.Next(3, 6);
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                    {
                        int X = cx + x, Y = cy + y;
                        if (X < 2 || Y < 2 || X >= n - 2 || Y >= n - 2 || x * x + y * y > r * r) continue;
                        px[Y * n + X] = new Color32(255, 255, 255, 255);
                    }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame) ToggleMusic();

            bool playing = music && music.isPlaying;
            Beat = (playing ? music.time : Time.time) * bpm / 60f;
            float phase = Beat - Mathf.Floor(Beat);
            float pulse = Mathf.Exp(-phase * 3.5f);                 // golpe en cada negra
            int beatIndex = Mathf.FloorToInt(Beat);
            bool flash = Time.time < flashUntil;

            // Pista de baile
            if (tiles != null)
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i] == null) continue;
                    int col = i % tileColumns, row = i / tileColumns;
                    Color c;
                    if (flash) c = flashColor * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 14f + col)));
                    else
                    {
                        int pattern = (beatIndex / 8) % 3;
                        int k = pattern == 0 ? (col + row + beatIndex)
                              : pattern == 1 ? (Mathf.Abs(col * 2 - tileColumns + 1) / 2 + Mathf.Abs(row - 1) + beatIndex)
                              : Hash(i, beatIndex);
                        float hue = Mathf.Repeat(k * 0.17f + beatIndex * 0.03f, 1f);
                        bool lit = pattern == 2 ? (Hash(i * 3, beatIndex) & 1) == 0 : (k & 1) == 0;
                        c = Color.HSVToRGB(hue, 0.85f, 1f) * (lit ? 0.55f + 0.6f * pulse : 0.16f);
                    }
                    tiles[i].GetPropertyBlock(mpb);
                    mpb.SetColor(ColorId, c * 0.6f);
                    mpb.SetColor(EmissionId, c * 1.5f);
                    tiles[i].SetPropertyBlock(mpb);
                }

            // Focos que barren la pista
            for (int i = 0; i < spotBase.Length; i++)
            {
                if (spots[i] == null) continue;
                float a = Beat * 0.5f + i * 1.7f;
                spots[i].transform.localRotation = spotBase[i] * Quaternion.Euler(Mathf.Sin(a) * 22f, Mathf.Cos(a * 0.7f) * 30f, 0f);
                spots[i].color = flash ? flashColor : Color.HSVToRGB(Mathf.Repeat(Beat * 0.03f + i * 0.25f, 1f), 0.8f, 1f);
            }

            // Luces de relleno: cambian de tono despacio (nunca se apagan)
            if (fills != null)
                for (int i = 0; i < fills.Length; i++)
                    if (fills[i]) fills[i].color = Color.HSVToRGB(Mathf.Repeat(Beat * 0.012f + i * 0.45f + 0.75f, 1f), 0.45f, 1f);

            if (mirrorBall) mirrorBall.Rotate(0f, 24f * Time.deltaTime, 0f, Space.World);
            if (ballLight) ballLight.transform.Rotate(0f, 0f, 24f * Time.deltaTime, Space.Self);

            // Ecualizador
            if (eqBase.Length > 0)
            {
                bool live = playing && !muted;
                if (live) music.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
                for (int i = 0; i < eqBase.Length; i++)
                {
                    float target;
                    if (live)
                    {
                        int from = Mathf.FloorToInt(Mathf.Pow(i / (float)eqBase.Length, 2f) * 120f) + 1;
                        int to = Mathf.FloorToInt(Mathf.Pow((i + 1) / (float)eqBase.Length, 2f) * 120f) + 2;
                        float max = 0f;
                        for (int s = from; s < to && s < spectrum.Length; s++) max = Mathf.Max(max, spectrum[s]);
                        target = Mathf.Clamp01(Mathf.Sqrt(max / Mathf.Max(0.02f, music.volume)) * 2.2f * (1f + i * 0.25f));
                    }
                    else target = 0.25f + 0.2f * Mathf.Abs(Mathf.Sin(Beat * 1.5f + i * 0.9f)) + 0.5f * pulse * ((i & 1) == 0 ? 1f : 0.5f);
                    eqLevel[i] = target > eqLevel[i] ? target : Mathf.Lerp(eqLevel[i], target, 1f - Mathf.Exp(-7f * Time.deltaTime));
                    var scale = eqBase[i]; scale.y = eqBase[i].y * (0.08f + eqLevel[i]);
                    eqBars[i].localScale = scale;
                    var p = eqBars[i].localPosition; p.y = scale.y * 0.5f; eqBars[i].localPosition = p;
                }
            }

            // Neones que laten con el bombo
            if (pulseNeons != null)
                foreach (var r in pulseNeons)
                {
                    if (r == null) continue;
                    Color baseColor = r.sharedMaterial.color;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(ColorId, baseColor);
                    mpb.SetColor(EmissionId, baseColor * (1.1f + 1.2f * pulse));
                    r.SetPropertyBlock(mpb);
                }
        }

        public void ToggleMusic()
        {
            muted = !muted;
            if (music) music.mute = muted;
        }

        static int Hash(int a, int b)
        {
            unchecked
            {
                int h = a * 73856093 ^ b * 19349663;
                h ^= h >> 13; h *= 1274126177;
                return (h ^ (h >> 16)) & 0x7fffffff;
            }
        }
    }
}
