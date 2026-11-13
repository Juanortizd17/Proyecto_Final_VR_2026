using System.Collections;
using System.IO;
using System.Linq;
using MixLab;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MixLab.Tests
{
    /// <summary>
    /// Pruebas de la capa VR sin visor: se activa el modo VR a mano y se simulan los movimientos
    /// de la mano (inclinar la botella, sacudir y voltear el vaso, arrastrar palancas, encajar en el
    /// posavasos). También verifican el historial guardado y el CSV.
    /// </summary>
    public class VrTests
    {
        MixGameManager gm;
        MixGlass glass;
        MixVrRig rig;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            LogAssert.ignoreFailingMessages = true;
            MixSaveRepository.FileName = "mix_save_tests.json";
            MixSaveRepository.Delete();
            SceneManager.LoadScene("MixLab");
            yield return null;
            yield return null;
            gm = MixGameManager.Instance;
            glass = Object.FindFirstObjectByType<MixGlass>();
            rig = Object.FindFirstObjectByType<MixVrRig>();
            Assert.IsNotNull(rig, "La escena trae el MixVrRig");
            rig.EnableVr();
            yield return null;
            Time.timeScale = 2f;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            MixSaveRepository.Delete();
            MixSaveRepository.FileName = "mix_save.json";
        }

        Bottle B(string id) => Object.FindObjectsByType<Bottle>(FindObjectsSortMode.None).First(b => b.Ingredient.name == id);

        /// <summary>Coloca la botella inclinada con la boca justo encima del vaso.</summary>
        void TiltOverGlass(Bottle b, float degrees)
        {
            var rot = Quaternion.AngleAxis(degrees, Vector3.right);
            Vector3 up = rot * Vector3.up;
            Vector3 mouth = glass.transform.position + Vector3.up * 0.24f;
            b.transform.SetPositionAndRotation(mouth - up * b.MouthHeight, rot);
        }

        [Test]
        public void Vr_ModeTurnsOnRigAndInteractables()
        {
            Assert.IsTrue(MixVrRig.Active);
            Assert.IsNull(Object.FindFirstObjectByType<DesktopPlayer>(), "El jugador de PC se apaga");
            Assert.IsNotNull(GameObject.Find("XR Origin (XR Rig)"), "El XR Origin queda activo");
            var grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
            Assert.AreEqual(20, grabs.Count(g => g.enabled), "19 ingredientes + el vaso se pueden agarrar");
            Assert.AreEqual(2, Object.FindObjectsByType<XRSimpleInteractable>(FindObjectsSortMode.None).Count(s => s.enabled), "2 palancas");
            Assert.IsTrue(Object.FindFirstObjectByType<CoasterSocket>().enabled, "Socket del posavasos activo");
            var tutorial = Object.FindFirstObjectByType<MixTutorial>();
            Assert.IsFalse(tutorial.DesktopMode);
            StringAssert.Contains("Joystick", tutorial.GetComponentsInChildren<Text>().First(t => t.name == "Instruction").text);
        }

        [UnityTest]
        public IEnumerator Vr_TiltingABottleOverTheGlassPoursOneDose()
        {
            var agua = B("Agua");
            var pour = agua.GetComponent<VrPourable>();
            pour.SimulateHeld = true;
            TiltOverGlass(agua, 125f);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(1, glass.Count, "Una dosis por inclinación");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, glass.Count, "Mantenerla inclinada no sirve otra dosis");

            agua.transform.rotation = Quaternion.identity;      // enderezar…
            yield return null; yield return null;
            TiltOverGlass(agua, 125f);                          // …y volver a inclinar
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(2, glass.Count);

            // Inclinada pero lejos del vaso: no cae nada
            agua.transform.rotation = Quaternion.identity; yield return null;
            agua.transform.SetPositionAndRotation(glass.transform.position + new Vector3(0.5f, 0.3f, 0f), Quaternion.AngleAxis(125f, Vector3.right));
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(2, glass.Count);
            pour.SimulateHeld = false;
        }

        [UnityTest]
        public IEnumerator Vr_PieceContainerDropsIntoTheGlass()
        {
            var hielo = B("Hielo");
            var pour = hielo.GetComponent<VrPourable>();
            pour.SimulateHeld = true;
            TiltOverGlass(hielo, 95f);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(1, glass.Count);
            Assert.AreEqual("Hielo", glass.Contents[0].name);
            pour.SimulateHeld = false;
        }

        [Test]
        public void Vr_ShakingTheGlassMixesIt()
        {
            var vg = glass.GetComponent<VrGlass>();
            glass.Add(B("Ron").Ingredient); glass.Add(B("Cola").Ingredient);
            Assert.IsFalse(glass.Shaken);
            Vector3 p = glass.transform.position;
            for (int i = 0; i < 10 && !glass.Shaken; i++)
                vg.TrackShake(p + Vector3.right * (i % 2 == 0 ? 0.04f : -0.04f), 0.02f);
            Assert.IsTrue(glass.Shaken, "Sacudir de lado a lado agita la mezcla");
        }

        [UnityTest]
        public IEnumerator Vr_TurningTheGlassUpsideDownEmptiesIt()
        {
            var vg = glass.GetComponent<VrGlass>();
            glass.Add(B("Leche").Ingredient);
            vg.SimulateHeld = true;
            glass.transform.rotation = Quaternion.AngleAxis(170f, Vector3.right);
            yield return null; yield return null;
            Assert.AreEqual(0, glass.Count);
            vg.SimulateHeld = false;
        }

        [UnityTest]
        public IEnumerator Vr_LeversAreDraggedByHand()
        {
            var temp = Object.FindFirstObjectByType<TemperatureLever>();
            var vl = temp.GetComponent<VrLever>();
            Vector3 p = temp.transform.position + Vector3.up * 0.15f;
            Vector3 right = temp.transform.right;
            vl.BeginDrag(p);
            vl.Drag(p - right * 0.06f);
            Assert.AreEqual(Temperature.Frio, temp.Current, "Izquierda = FRÍO");
            vl.Drag(p + right * 0.06f);
            Assert.AreEqual(Temperature.Caliente, temp.Current, "Derecha = CALIENTE");
            vl.Drag(p);
            Assert.AreEqual(Temperature.Ambiente, temp.Current, "Centro = AMBIENTE");
            vl.EndDrag();

            var shakeLever = Object.FindFirstObjectByType<ShakeLever>().GetComponent<VrLever>();
            glass.Add(B("Naranja").Ingredient); glass.Add(B("Soda").Ingredient);
            shakeLever.BeginDrag(p);
            shakeLever.Drag(p + Vector3.back * 0.08f);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(glass.Shaken, "Tirar de la palanca agita");
        }

        [UnityTest]
        public IEnumerator Vr_CoasterSocketServesTheDrink()
        {
            var socket = Object.FindFirstObjectByType<CoasterSocket>();
            glass.Add(B("Agua").Ingredient);
            Assert.IsTrue(socket.Serve());
            Assert.IsTrue(gm.Busy, "El cliente toma la bebida");
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(glass.GetComponent<XRGrabInteractable>().enabled, "Mientras el cliente la tiene, no se puede agarrar");
            while (gm.Busy) yield return null;
            Assert.IsNotNull(gm.LastReaction);
            yield return null;
            Assert.IsTrue(glass.GetComponent<XRGrabInteractable>().enabled, "El vaso vuelve y se puede agarrar otra vez");
        }

        [UnityTest]
        public IEnumerator Data_HistoryAndCsvAreSaved()
        {
            gm.StartShift();
            yield return null;
            gm.Finish(true);
            yield return null;
            Assert.AreEqual(1, gm.Sessions.Count, "El turno queda en el historial");
            Assert.IsTrue(File.Exists(MixSaveRepository.CsvPath), "Se exporta el CSV");
            var lines = File.ReadAllLines(MixSaveRepository.CsvPath);
            StringAssert.StartsWith("turno;fecha;puntaje", lines[0].TrimStart('﻿'));
            Assert.AreEqual(2, lines.Length);
            var board = Object.FindFirstObjectByType<HistoryBoard>();
            var texts = board.GetComponentsInChildren<Text>().Select(t => t.text).ToList();
            Assert.IsTrue(texts.Any(t => t.Contains("Récord")), "El tablero muestra el récord");
            Assert.IsTrue(texts.Any(t => t.Contains("#1")), "El tablero lista el turno");
            File.Delete(MixSaveRepository.CsvPath);
        }
    }
}
