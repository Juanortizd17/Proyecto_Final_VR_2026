using System;
using System.Linq;
using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Regulador de calidad: mantiene el juego fluido sin perder el ambiente del bar.
    /// Tiene tres niveles (Alto, Medio, VR). Cada nivel conserva menos público de fondo y menos
    /// luces por píxel; el DJ, parte de los bailarines y los clientes de la barra nunca se quitan.
    /// En visores autónomos arranca en el nivel VR; en PC arranca en Alto y, si los cuadros por
    /// segundo caen por debajo del objetivo, baja de nivel solo. Se puede forzar con -quality N.
    /// </summary>
    public class QualityGovernor : MonoBehaviour
    {
        public static readonly string[] TierNames = { "Alto", "Medio", "VR" };

        [SerializeField] GameObject[] crowd;                  // ordenado por importancia: primero los que más aportan
        [SerializeField] int[] crowdPerTier = { 999, 16, 8 };
        [SerializeField] Light[] extraLights;                 // focos y acentos que se apagan al bajar de nivel
        [SerializeField] int[] extraLightsPerTier = { 999, 4, 2 };
        [SerializeField] int[] pixelLightsPerTier = { 12, 6, 3 };
        [SerializeField] float pcTargetFps = 50f;
        [SerializeField] float mobileTargetFps = 66f;

        bool adaptive = true;
        float windowTime, warmup = 5f;
        int windowFrames;

        public static QualityGovernor Instance { get; private set; }
        public int Tier { get; private set; }
        public string TierName => TierNames[Tier];
        public int CrowdTotal => crowd != null ? crowd.Length : 0;
        public int CrowdActive => crowd != null ? crowd.Count(c => c && c.activeSelf) : 0;

        public void Setup(GameObject[] optionalCrowd, Light[] optionalLights)
        {
            crowd = optionalCrowd; extraLights = optionalLights;
        }

        void Awake()
        {
            Instance = this;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-quality");
            int start = Application.isMobilePlatform ? 2 : 0;
            if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int forced))
            {
                start = Mathf.Clamp(forced, 0, 2);
                adaptive = false;
            }
            if (Application.isBatchMode) adaptive = false;      // en pruebas automáticas no se mide
            Apply(start);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Apply(int tier)
        {
            Tier = Mathf.Clamp(tier, 0, 2);
            if (crowd != null)
                for (int k = 0; k < crowd.Length; k++)
                    if (crowd[k]) crowd[k].SetActive(k < crowdPerTier[Tier]);
            if (extraLights != null)
                for (int k = 0; k < extraLights.Length; k++)
                    if (extraLights[k]) extraLights[k].enabled = k < extraLightsPerTier[Tier];
            int forcedLights = Array.IndexOf(Environment.GetCommandLineArgs(), "-pixellights");
            var cmd = Environment.GetCommandLineArgs();
            QualitySettings.pixelLightCount = forcedLights >= 0 && forcedLights + 1 < cmd.Length && int.TryParse(cmd[forcedLights + 1], out int pl) ? pl : pixelLightsPerTier[Tier];
            Debug.Log($"[QualityGovernor] Nivel {TierName}: {CrowdActive}/{CrowdTotal} personajes opcionales, {QualitySettings.pixelLightCount} luces por píxel");
        }

        void Update()
        {
            if (!adaptive || Tier >= 2 || Time.timeScale <= 0f) return;
            if (warmup > 0f) { warmup -= Time.unscaledDeltaTime; return; }
            windowTime += Time.unscaledDeltaTime; windowFrames++;
            if (windowTime < 3f) return;
            float fps = windowFrames / windowTime;
            windowTime = 0f; windowFrames = 0;
            float target = Application.isMobilePlatform ? mobileTargetFps : pcTargetFps;
            if (fps >= target) return;
            Debug.Log($"[QualityGovernor] {fps:0} FPS por debajo del objetivo ({target:0}): se baja un nivel");
            Apply(Tier + 1);
            warmup = 3f;
        }
    }
}
