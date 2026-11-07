using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QualityControlXR
{
    /// <summary>
    /// Modo demostración: solo se activa al abrir la app con el argumento -autodemo.
    /// Salta el tutorial, juega un turno corto clasificando productos (con algunos errores
    /// intencionales), guarda capturas de pantalla y cierra la app.
    /// Útil para revisar el juego en PC sin visor y como video de respaldo.
    ///   ControlCalidadXR.exe -autodemo [-shotdir C:\ruta] [-roundSeconds 75]
    /// </summary>
    public class AutoDemo : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float mistakeRate = 0.2f;
        [SerializeField] float decisionEvery = 2.5f;

        string shotDir;
        int shotIndex;

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-autodemo")) { enabled = false; return; }
            shotDir = Arg(args, "-shotdir") ?? Path.Combine(Application.persistentDataPath, "autodemo");
            Directory.CreateDirectory(shotDir);
            float round = float.TryParse(Arg(args, "-roundSeconds"), out var r) ? r : 75f;
            StartCoroutine(Run(round));
        }

        IEnumerator Run(float round)
        {
            var gm = GameManager.Instance;
            var hud = FindFirstObjectByType<HudController>();
            var belt = FindFirstObjectByType<ConveyorBelt>();
            gm.OverrideRoundDuration(round);
            if (belt) belt.OverrideFirstEvent(Mathf.Min(25f, round * 0.4f));

            yield return new WaitForSeconds(3f);          // deja aparecer el simulador / visor
            yield return Shot("01_inicio_onboarding");

            FindFirstObjectByType<OnboardingTutorial>()?.Skip();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("02_tutorial_completo");

            hud.OnStartPressed();
            yield return new WaitForSeconds(4f);
            yield return Shot("03_turno_iniciado");

            float nextShot = Time.time + 8f;
            while (gm.State == GameState.Playing)
            {
                ClassifyOne(gm);
                yield return new WaitForSeconds(decisionEvery);
                if (Time.time >= nextShot && gm.State == GameState.Playing)
                {
                    yield return Shot($"04_jugando_{shotIndex:00}");
                    nextShot = Time.time + 10f;
                }
            }

            yield return new WaitForSeconds(1.5f);
            yield return Shot("05_panel_final");
            Debug.Log($"[AutoDemo] Fin: puntaje {gm.Score}, aciertos {gm.Correct}, fallos {gm.Mistakes}. Capturas en {shotDir}");
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        void ClassifyOne(GameManager gm)
        {
            var p = FindObjectsByType<Product>(FindObjectsSortMode.None)
                .Where(x => !x.Processed && x.transform.position.y > 0.7f)
                .OrderByDescending(x => x.transform.position.x)   // el más avanzado en la banda
                .FirstOrDefault();
            if (p == null) return;

            // Inspección con las tres herramientas (como haría el operario)
            p.Inspect(DefectType.Crack); p.Inspect(DefectType.LooseScrew); p.Inspect(DefectType.Hollow);

            bool doMistake = UnityEngine.Random.value < mistakeRate;
            bool toReject = p.IsDefective != doMistake;
            string bin = toReject ? "Bin_Rechazo" : "Bin_" + p.Type;
            var zone = GameObject.Find(bin)?.transform.Find("DropZone");
            if (zone == null) return;
            StartCoroutine(Fly(p, zone.position));
        }

        static IEnumerator Fly(Product p, Vector3 target)
        {
            var rb = p.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            Vector3 start = p.transform.position;
            for (float t = 0f; t < 1f && p != null; t += Time.deltaTime / 0.6f)
            {
                var pos = Vector3.Lerp(start, target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.4f;
                p.transform.position = pos;
                yield return null;
            }
            if (p == null) yield break;
            p.transform.position = target;
            rb.isKinematic = false;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(shotDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            shotIndex++;
            Debug.Log("[AutoDemo] Captura: " + path);
        }

        static string Arg(string[] args, string key)
        {
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
