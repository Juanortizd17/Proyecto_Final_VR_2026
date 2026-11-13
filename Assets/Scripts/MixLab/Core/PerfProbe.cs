using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Medidor de rendimiento (solo con el argumento -perf): quita el límite de cuadros, registra
    /// el tiempo de cada cuadro y los contadores de dibujado, y al final escribe un resumen en el
    /// log y en perf_&lt;nivel&gt;.txt. Sirve para decidir con datos cuánto público y luces aguanta.
    ///   LaboratorioMezclas.exe -autodemo -perf -quality 0
    /// </summary>
    public class PerfProbe : MonoBehaviour
    {
        readonly List<float> frames = new List<float>(20000);
        ProfilerRecorder drawCalls, batches, setPass, triangles;
        double sumDraw, sumBatches, sumSetPass, sumTris;
        int samples;
        float skip = 4f;
        string outDir;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-perf") < 0) { enabled = false; return; }
            int i = Array.IndexOf(args, "-shotdir");
            outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Application.persistentDataPath;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        void Update()
        {
            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (skip > 0f) { skip -= Time.unscaledDeltaTime; return; }
            frames.Add(Time.unscaledDeltaTime);
            if (drawCalls.Valid && drawCalls.LastValue > 0)
            {
                sumDraw += drawCalls.LastValue; sumBatches += batches.LastValue; sumSetPass += setPass.LastValue; sumTris += triangles.LastValue;
                samples++;
            }
        }

        void OnDestroy()
        {
            if (drawCalls.Valid) drawCalls.Dispose();
            if (batches.Valid) batches.Dispose();
            if (setPass.Valid) setPass.Dispose();
            if (triangles.Valid) triangles.Dispose();
        }

        /// <summary>Resumen: FPS medio, el 1 % de cuadros más lentos y contadores de dibujado.</summary>
        public void Report()
        {
            if (!enabled || frames.Count < 10) return;
            float total = 0f;
            foreach (float f in frames) total += f;
            var sorted = new List<float>(frames);
            sorted.Sort();
            int n = sorted.Count;
            float worst1 = 0f; int count1 = Mathf.Max(1, n / 100);
            for (int k = n - count1; k < n; k++) worst1 += sorted[k];
            worst1 /= count1;
            var gov = QualityGovernor.Instance;
            string tier = gov ? gov.TierName : "-";
            string line =
                $"[PerfProbe] nivel={tier} personajes_opcionales={(gov ? gov.CrowdActive : 0)}/{(gov ? gov.CrowdTotal : 0)} " +
                $"fps_medio={n / total:0} fps_1%_bajo={1f / worst1:0} ms_mediana={sorted[n / 2] * 1000f:0.00} " +
                (samples > 0
                    ? $"draw_calls={sumDraw / samples:0} batches={sumBatches / samples:0} setpass={sumSetPass / samples:0} triangulos={sumTris / samples:0} "
                    : "contadores_de_dibujado=no_disponibles ") +
                $"luces_px={QualitySettings.pixelLightCount} resolucion={Screen.width}x{Screen.height} gpu=\"{SystemInfo.graphicsDeviceName}\"";
            Debug.Log(line);
            try { File.WriteAllText(Path.Combine(outDir, $"perf_{tier}.txt"), line + Environment.NewLine); }
            catch (Exception e) { Debug.LogWarning("[PerfProbe] " + e.Message); }
        }
    }
}
