using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MixLab;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MixLab.Tests
{
    /// <summary>
    /// Simulación de una sesión completa en VR sin visor: se activa el modo VR y se juegan
    /// varios pedidos con los mismos gestos que haría la mano (inclinar botellas, arrastrar la
    /// palanca, sacudir el vaso, soltarlo en el posavasos), más una mezcla que se deja estallar.
    /// Falla si aparece cualquier error o excepción en el registro.
    /// </summary>
    public class VrSessionTests
    {
        readonly List<string> errors = new List<string>();

        void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            // Mensajes del entorno de pruebas sin visor, ajenos al juego
            if (msg.Contains("XR") && msg.Contains("display")) return;
            errors.Add($"{type}: {msg}\n{stack}");
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;
            MixSaveRepository.FileName = "mix_save_tests.json";
            MixSaveRepository.Delete();
            errors.Clear();
            Application.logMessageReceived += OnLog;
            SceneManager.LoadScene("MixLab");
            yield return null; yield return null;
        }

        [TearDown]
        public void Teardown()
        {
            Application.logMessageReceived -= OnLog;
            Time.timeScale = 1f;
            MixSaveRepository.Delete();
            MixSaveRepository.FileName = "mix_save.json";
        }

        static Bottle B(string id) => Object.FindObjectsByType<Bottle>(FindObjectsSortMode.None).First(b => b.Ingredient.name == id);

        IEnumerator Pour(MixGlass glass, Bottle b)
        {
            var pour = b.GetComponent<VrPourable>();
            pour.SimulateHeld = true;
            b.transform.rotation = Quaternion.identity;   // tomarla derecha (como al agarrarla)
            yield return null;
            var rot = Quaternion.AngleAxis(125f, Vector3.right);
            Vector3 mouth = glass.transform.position + Vector3.up * 0.24f;
            b.transform.SetPositionAndRotation(mouth - (rot * Vector3.up) * b.MouthHeight, rot);
            int before = glass.Count;
            for (float t = 0; t < 3f && glass.Count == before; t += Time.deltaTime) yield return null;
            pour.SimulateHeld = false;
            yield return b.ReturnHome();
            Assert.AreEqual(before + 1, glass.Count, $"Se vierte {b.Ingredient.name}");
        }

        IEnumerator WaitIdle(MixGameManager gm)
        {
            for (float t = 0; t < 20f && gm.Busy; t += Time.deltaTime) yield return null;
            Assert.IsFalse(gm.Busy, "El servicio termina");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vr_FullSessionRunsWithoutErrors()
        {
            Time.timeScale = 3f;
            var gm = MixGameManager.Instance;
            var glass = Object.FindFirstObjectByType<MixGlass>();
            var rig = Object.FindFirstObjectByType<MixVrRig>();
            rig.EnableVr();
            yield return null;
            Assert.IsFalse(MainMenu.IsOpen, "En pruebas el menú no bloquea");
            var vg = glass.GetComponent<VrGlass>();
            var socket = Object.FindFirstObjectByType<CoasterSocket>();
            var temp = Object.FindFirstObjectByType<TemperatureLever>();
            var tempHand = temp.GetComponent<VrLever>();

            gm.StartShift();
            yield return null;

            // 1) Cinco pedidos reales, hechos con las manos
            for (int order = 0; order < 5; order++)
            {
                var recipe = gm.CurrentOrder;
                Assert.IsNotNull(recipe);
                foreach (var ing in recipe.ingredients) yield return Pour(glass, B(ing.name));
                if (recipe.needsTemperature)
                {
                    Vector3 p = temp.transform.position + Vector3.up * 0.15f;
                    tempHand.BeginDrag(p);
                    tempHand.Drag(p + temp.transform.right * (recipe.temperature == Temperature.Frio ? -0.06f : recipe.temperature == Temperature.Caliente ? 0.06f : 0f));
                    tempHand.EndDrag();
                    Assert.AreEqual(recipe.temperature, temp.Current, "La palanca queda en la temperatura del pedido");
                }
                if (glass.Count >= 2)
                {
                    Vector3 g0 = glass.transform.position;
                    for (int i = 0; i < 12 && !glass.Shaken; i++) vg.TrackShake(g0 + Vector3.right * (i % 2 == 0 ? 0.04f : -0.04f), 0.02f);
                    Assert.IsTrue(glass.Shaken);
                }
                int scoreBefore = gm.Score;
                Assert.IsTrue(socket.Serve(), "El posavasos acepta el vaso");
                yield return WaitIdle(gm);
                Assert.AreEqual(ReactionId.Perfecto, gm.LastReaction.id, $"Pedido {recipe.displayName} perfecto");
                Assert.Greater(gm.Score, scoreBefore);
                Assert.IsTrue(glass.GetComponent<XRGrabInteractable>().enabled, "El vaso se puede volver a agarrar");
                if (gm.State != MixState.Playing) break;
            }

            // 2) Mezcla inestable estabilizada con hielo
            if (gm.State == MixState.Playing)
            {
                yield return Pour(glass, B("Soda"));
                yield return Pour(glass, B("Efervescente"));
                Assert.AreEqual(MixClass.Inestable, MixAnalyzer.Instance.Current);
                yield return Pour(glass, B("Hielo"));
                yield return null;
                Assert.AreEqual(MixClass.Estable, MixAnalyzer.Instance.Current, "El hielo la estabiliza");
                socket.Serve();
                yield return WaitIdle(gm);
            }

            // 3) Mezcla inestable que se deja estallar en la mano
            if (gm.State == MixState.Playing)
            {
                yield return Pour(glass, B("Soda"));
                yield return Pour(glass, B("Efervescente"));
                vg.SimulateHeld = true;
                for (float t = 0; t < 15f && !glass.Shattered; t += Time.deltaTime) yield return null;
                vg.SimulateHeld = false;
                Assert.IsTrue(glass.Shattered || glass.Count == 0, "El vaso estalla");
                for (float t = 0; t < 5f && (glass.Busy || glass.Shattered); t += Time.deltaTime) yield return null;
                Assert.IsFalse(glass.Shattered, "Aparece un vaso nuevo");
                Assert.AreEqual(0, glass.Count);
            }

            // 4) Terminar el turno y volver a empezar
            gm.Finish(true);
            yield return null;
            Assert.AreEqual(MixState.Finished, gm.State);

            Assert.IsEmpty(errors, "Errores durante la sesión VR:\n" + string.Join("\n", errors.Take(10)));
        }
    }
}
