using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace QualityControlXR.Tests
{
    /// <summary>
    /// Pruebas de extremo a extremo sobre la escena real (sin visor):
    /// onboarding, banda física, clasificación, zona de pérdida, cierre y persistencia.
    /// </summary>
    public class GameplayTests
    {
        GameManager gm;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            LogAssert.ignoreFailingMessages = true;   // OpenXR no encuentra visor en CI
            SceneManager.LoadScene("QualityControl");
            yield return null;
            yield return null;
            gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager presente en la escena");
        }

        [Test]
        public void Product_OnlyMatchingToolRevealsDefect()
        {
            var go = new GameObject("p");
            var p = go.AddComponent<Product>();
            p.Configure(ProductType.Radio, DefectType.Hollow);
            Assert.IsFalse(p.Inspect(DefectType.Crack), "La lupa no revela una pieza hueca");
            Assert.IsFalse(p.Inspect(DefectType.LooseScrew), "El destornillador no revela una pieza hueca");
            Assert.IsTrue(p.Inspect(DefectType.Hollow), "El martillo revela la pieza hueca");
            Assert.IsTrue(p.DefectRevealed);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Onboarding_SkipShowsStartButton_AndStartTeleportsToWorkSpot()
        {
            Assert.AreEqual(GameState.Onboarding, gm.State);
            var tutorial = Object.FindFirstObjectByType<OnboardingTutorial>();
            tutorial.Skip();
            yield return null;
            Assert.IsTrue(GameObject.Find("OnboardingBoard/StartButton").activeInHierarchy, "Botón INICIAR visible");

            Object.FindFirstObjectByType<HudController>().OnStartPressed();
            yield return null;
            Assert.AreEqual(GameState.Playing, gm.State);
            var cam = Camera.main.transform.position;
            var spot = GameObject.Find("WorkSpot").transform.position;
            Assert.Less(Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(spot.x, spot.z)), 0.3f, "El jugador queda frente a la banda");
        }

        [UnityTest]
        public IEnumerator Belt_SpawnsAndCarriesProducts()
        {
            gm.StartRound();
            yield return new WaitForSeconds(1.6f);
            var p = Object.FindObjectsByType<Product>(FindObjectsSortMode.None).FirstOrDefault();
            Assert.IsNotNull(p, "La banda generó un producto");
            float x0 = p.transform.position.x;
            yield return new WaitForSeconds(3f);
            Assert.IsTrue(p != null, "El producto sigue en la escena");
            Assert.Greater(p.transform.position.x - x0, 0.3f, "La banda arrastra el producto hacia +X");
            Assert.Greater(p.transform.position.y, 0.8f, "El producto sigue encima de la banda");
        }

        [UnityTest]
        public IEnumerator Bins_ScoreCorrectAndWrongDecisions()
        {
            gm.StartRound();
            yield return new WaitForSeconds(1.6f);
            var source = Object.FindObjectsByType<Product>(FindObjectsSortMode.None).First();

            // Bueno -> su contenedor: acierto
            var good = Clone(source, ProductType.Radio, DefectType.None);
            yield return Drop(good, "Bin_Radio");
            Assert.AreEqual(1, gm.Correct, "Producto perfecto en su contenedor suma");
            Assert.AreEqual(100, gm.Score);

            // Defectuoso -> rechazo: acierto con reporte
            var bad = Clone(source, ProductType.Lamp, DefectType.Crack);
            yield return Drop(bad, "Bin_Rechazo");
            Assert.AreEqual(2, gm.Correct, "Producto defectuoso desechado suma");
            StringAssert.Contains("grieta", gm.Reports.Last());

            // Bueno -> rechazo: error
            var wrong = Clone(source, ProductType.Bear, DefectType.None);
            yield return Drop(wrong, "Bin_Rechazo");
            Assert.AreEqual(1, gm.Mistakes, "Desechar un producto bueno es un fallo");
            Assert.AreEqual(150, gm.Score);

            // Defectuoso -> contenedor de aprobados: error
            var wrong2 = Clone(source, ProductType.Speaker, DefectType.LooseScrew);
            yield return Drop(wrong2, "Bin_Speaker");
            Assert.AreEqual(2, gm.Mistakes, "Aprobar un producto defectuoso es un fallo");
        }

        [UnityTest]
        public IEnumerator LostZone_CountsMistake()
        {
            gm.StartRound();
            yield return new WaitForSeconds(1.6f);
            var source = Object.FindObjectsByType<Product>(FindObjectsSortMode.None).First();
            var lost = Clone(source, ProductType.Television, DefectType.None);
            var zone = GameObject.Find("LostZone_EndOfBelt").transform.position;
            yield return Teleport(lost, zone);
            Assert.AreEqual(1, gm.Mistakes, "Producto perdido al final de la banda es un fallo");
        }

        [UnityTest]
        public IEnumerator Finish_ShowsResults_AndPersistsHistory()
        {
            int before = ScoreRepository.SessionCount();
            gm.StartRound();
            yield return null;
            gm.RegisterCorrect("prueba");
            gm.Finish(true);
            yield return null;
            Assert.AreEqual(GameState.Finished, gm.State);
            Assert.IsTrue(gm.Won);
            Assert.IsTrue(GameObject.Find("MainBoard/ResultsPanel").activeInHierarchy, "Panel final visible");
            Assert.AreEqual(Mathf.Min(before + 1, 50), ScoreRepository.SessionCount(), "La sesión quedó guardada");
            Assert.IsTrue(File.Exists(Path.Combine(Application.persistentDataPath, "qc_history.json")));
            Assert.GreaterOrEqual(ScoreRepository.BestScore(), 100);
        }

        [UnityTest]
        public IEnumerator MaxMistakes_EndsRoundAsFailed()
        {
            gm.StartRound();
            yield return null;
            for (int i = 0; i < gm.MaxMistakes; i++) gm.RegisterMistake("prueba " + i);
            Assert.AreEqual(GameState.Finished, gm.State);
            Assert.IsFalse(gm.Won, "Llegar al máximo de fallos termina el turno como fallido");
        }

        // ---------- helpers ----------

        static Product Clone(Product source, ProductType t, DefectType d)
        {
            var go = Object.Instantiate(source.gameObject, new Vector3(0f, 6f, -6f), Quaternion.identity);
            go.GetComponent<Rigidbody>().isKinematic = true;
            var p = go.GetComponent<Product>();
            p.Configure(t, d);
            return p;
        }

        static IEnumerator Drop(Product p, string binName)
        {
            var zone = GameObject.Find(binName).transform.Find("DropZone").position;
            yield return Teleport(p, zone);
        }

        static IEnumerator Teleport(Product p, Vector3 pos)
        {
            var rb = p.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.position = pos;
            p.transform.position = pos;
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.3f);
        }
    }
}
