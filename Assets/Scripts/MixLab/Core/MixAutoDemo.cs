using System;
using System.Collections;
using System.IO;
using System.Linq;
using QualityControlXR;
using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Modo demostración (solo con el argumento -autodemo): juega un turno corto usando las
    /// mismas acciones que el jugador, guarda capturas de cada reacción y cierra la app.
    ///   ControlCalidadXR.exe -autodemo [-shotdir C:\ruta] [-roundSeconds 80]
    /// </summary>
    public class MixAutoDemo : MonoBehaviour
    {
        // Mezclas "equivocadas" para mostrar reacciones (nombres de assets de ingredientes).
        static readonly string[][] FunMixes =
        {
            new[] { "Leche", "Limon" },
            new[] { "SalsaDiablo", "Agua" },
            new[] { "Tequila", "Ron" },
            new[] { "Soda", "Efervescente" },
            new[] { "Leche", "Leche" },
            new[] { "Cafe", "Ron" },
            new[] { "Soda", "Cola" },
            new[] { "Limon", "Limon" },
            new[] { "SalsaDiablo", "Ron" },
            new[] { "Hielo", "Menta" },
            new[] { "Cafe", "Cafe" },
            new[] { "Azucar", "Azucar", "Azucar" },
        };

        string shotDir;
        int shot;

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-autodemo")) { enabled = false; return; }
            Application.runInBackground = true;   // sigue aunque la ventana pierda el foco
            shotDir = Arg(args, "-shotdir") ?? Path.Combine(Application.persistentDataPath, "autodemo");
            Directory.CreateDirectory(shotDir);
            float round = float.TryParse(Arg(args, "-roundSeconds"), out var r) ? r : 80f;
            StartCoroutine(Run(round));
        }

        IEnumerator Run(float round)
        {
            var gm = MixGameManager.Instance;
            var hand = FindFirstObjectByType<MixInteractor>();
            var player = FindFirstObjectByType<DesktopPlayer>();
            var lever = FindFirstObjectByType<TemperatureLever>();
            var shake = FindFirstObjectByType<ShakeLever>();
            var bottles = FindObjectsByType<Bottle>(FindObjectsSortMode.None);
            gm.OverrideShiftDuration(round);
            if (player) player.SimulateMove(Vector2.zero);   // la demo no lee el mouse ni el teclado reales

            yield return new WaitForSeconds(2f);
            yield return Shot("inicio");
            if (player) player.TeleportTo(player.transform.position, -40f, 22f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("estante_izquierdo");
            if (player) player.TeleportTo(player.transform.position, 40f, 22f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("estante_derecho");
            if (player) player.TeleportTo(player.transform.position, 0f, 4f);

            FindFirstObjectByType<MixTutorial>()?.Skip();
            yield return new WaitForSeconds(0.4f);
            gm.StartShift();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("pedido");

            int n = 0;
            while (gm.State == MixState.Playing)
            {
                while (gm.Busy) yield return null;
                if (gm.State != MixState.Playing) break;

                bool perfect = n % 2 == 0;
                string[] names = perfect
                    ? gm.CurrentOrder.ingredients.Select(i => i.name).ToArray()
                    : FunMixes[(n / 2) % FunMixes.Length];
                if (perfect && gm.CurrentOrder.needsTemperature) lever.Set(gm.CurrentOrder.temperature);

                foreach (var name in names)
                {
                    var b = bottles.FirstOrDefault(x => x.Ingredient.name == name);
                    if (b == null) continue;
                    b.Pour();
                    yield return new WaitForSeconds(1.5f);
                }
                if (n == 0) yield return Shot("vaso_por_capas");
                if (n % 2 == 1 && MusicPlayer.Instance != null) MusicPlayer.Instance.NextGenre(1);   // prueba la rockola
                shake.Pull();
                yield return new WaitForSeconds(1.1f);
                hand.TakeGlass();
                yield return new WaitForSeconds(0.5f);
                hand.ReleaseGlass();
                gm.Serve();
                yield return new WaitForSeconds(2.6f);
                if (gm.LastReaction) yield return Shot("reaccion_" + gm.LastReaction.id);
                n++;
            }

            yield return new WaitForSeconds(1.5f);
            yield return Shot("panel_final");
            Debug.Log($"[MixAutoDemo] Fin: puntaje {gm.Score}, servidas {gm.Served}, perfectas {gm.Perfects}, desmayos {gm.Knockouts}. Capturas en {shotDir}");
            FindFirstObjectByType<PerfProbe>()?.Report();
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(shotDir, $"{shot:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            shot++;
            Debug.Log("[MixAutoDemo] Captura: " + path);
        }

        static string Arg(string[] args, string key)
        {
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
