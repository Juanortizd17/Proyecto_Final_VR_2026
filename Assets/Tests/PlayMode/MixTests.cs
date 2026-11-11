using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MixLab;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace QualityControlXR.Tests
{
    /// <summary>
    /// Pruebas del Laboratorio de Mezclas sobre la escena real: reglas de reacción,
    /// flujo verter-agitar-servir, desmayos, guardado del libro y entrenamiento.
    /// Usan un archivo de guardado propio para no tocar la partida del jugador.
    /// </summary>
    public class MixTests
    {
        MixGameManager gm;
        MixGlass glass;
        MixInteractor hand;
        ShakeLever shake;
        TemperatureLever lever;
        Dictionary<string, Bottle> bottles;

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
            Assert.IsNotNull(gm, "MixGameManager presente");
            glass = Object.FindFirstObjectByType<MixGlass>();
            hand = Object.FindFirstObjectByType<MixInteractor>();
            shake = Object.FindFirstObjectByType<ShakeLever>();
            lever = Object.FindFirstObjectByType<TemperatureLever>();
            bottles = Object.FindObjectsByType<Bottle>(FindObjectsSortMode.None).ToDictionary(b => b.Ingredient.name);
            Assert.AreEqual(16, bottles.Count, "16 ingredientes en el estante");
            Time.timeScale = 4f;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            MixSaveRepository.Delete();
            MixSaveRepository.FileName = "mix_save.json";
        }

        IngredientData I(string id) => bottles[id].Ingredient;
        RecipeData Recipe(string id) => gm.Recipes.First(r => r.name == id);
        ReactionId Eval(Temperature t, RecipeData order, params string[] ids) =>
            ReactionRules.Evaluate(ids.Select(I).ToList(), t, order);

        [Test]
        public void Rules_EachCombinationGivesItsReaction()
        {
            var none = Temperature.Ambiente;
            Assert.AreEqual(ReactionId.Explosion, Eval(none, null, "Soda", "Efervescente"));
            Assert.AreEqual(ReactionId.Desmayo, Eval(none, null, "SalsaDiablo", "Ron"));
            Assert.AreEqual(ReactionId.Borracho, Eval(none, null, "Tequila", "Ron"));
            Assert.AreEqual(ReactionId.Llamarada, Eval(Temperature.Caliente, null, "Tequila"));
            Assert.AreEqual(ReactionId.Nausea, Eval(none, null, "Leche", "Limon"));
            Assert.AreEqual(ReactionId.Infierno, Eval(none, null, "SalsaDiablo", "Agua"));
            Assert.AreEqual(ReactionId.Picante, Eval(none, null, "Aji", "Agua"));
            Assert.AreEqual(ReactionId.Hiper, Eval(none, null, "Cafe", "Cafe"));
            Assert.AreEqual(ReactionId.Hiper, Eval(none, null, "Cafe", "Cola"));
            Assert.AreEqual(ReactionId.Azucar, Eval(none, null, "Azucar", "Azucar", "Azucar"));
            Assert.AreEqual(ReactionId.Explosion, Eval(none, null, "Cerveza", "Efervescente"));
            Assert.AreEqual(30, gm.Recipes.Count, "30 bebidas en la carta");
            Assert.AreEqual(17, gm.Reactions.Count, "17 reacciones en el libro");
            Assert.AreEqual(ReactionId.Bailarin, Eval(none, null, "Cafe", "Ron"));
            Assert.AreEqual(ReactionId.Hipo, Eval(none, null, "Soda", "Soda"));
            Assert.AreEqual(ReactionId.Agrio, Eval(none, null, "Limon", "Limon"));
            Assert.AreEqual(ReactionId.Dormido, Eval(none, null, "Leche", "Leche"));
            Assert.AreEqual(ReactionId.Dormido, Eval(Temperature.Caliente, null, "Leche", "Agua"));
            Assert.AreEqual(ReactionId.Congelado, Eval(none, null, "Hielo", "Menta"));
            Assert.AreEqual(ReactionId.Congelado, Eval(Temperature.Frio, null, "Agua", "Menta"));
            Assert.AreEqual(ReactionId.Alegre, Eval(none, null, "Ron", "Agua"));
            Assert.AreEqual(ReactionId.SinChiste, Eval(none, null, "Agua"));
        }

        [Test]
        public void Rules_RecipesNeedExactIngredientsAndTemperature()
        {
            Assert.AreEqual(ReactionId.Perfecto, Eval(Temperature.Ambiente, Recipe("LimonadaHelada"), "Hielo", "Agua", "Limon"));
            Assert.AreEqual(ReactionId.SinChiste, Eval(Temperature.Ambiente, Recipe("LimonadaHelada"), "Agua", "Limon"), "Falta el hielo");
            Assert.AreEqual(ReactionId.Perfecto, Eval(Temperature.Caliente, Recipe("CafeConLeche"), "Cafe", "Leche"));
            Assert.AreEqual(ReactionId.SinChiste, Eval(Temperature.Ambiente, Recipe("CafeConLeche"), "Cafe", "Leche"), "Sin calentar no cuenta");
            Assert.AreEqual(ReactionId.Perfecto, Eval(Temperature.Ambiente, Recipe("Michelada"), "Cerveza", "Limon", "Aji"), "El pedido gana sobre el picante leve");
            Assert.AreEqual(ReactionId.Perfecto, Eval(Temperature.Frio, Recipe("AguaMenta"), "Agua", "Menta"));
            foreach (var r in gm.Recipes)
            {
                var t = r.needsTemperature ? r.temperature : Temperature.Ambiente;
                Assert.AreEqual(ReactionId.Perfecto, ReactionRules.Evaluate(r.ingredients, t, r), $"La receta {r.displayName} se puede preparar");
            }
        }

        [UnityTest]
        public IEnumerator Flow_PourShakeServe_PerfectOrderScores()
        {
            gm.StartShift();
            gm.ForceOrder(Recipe("LimonadaHelada"));
            foreach (var id in new[] { "Agua", "Limon", "Hielo" })
            {
                Assert.IsTrue(bottles[id].Pour(), "La botella vierte: " + id);
                yield return new WaitForSeconds(1.6f);
            }
            Assert.AreEqual(3, glass.Count, "Tres ingredientes en el vaso");
            shake.Pull();
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(glass.Shaken, "Mezcla agitada");

            hand.TakeGlass();
            Assert.IsTrue(hand.HoldingGlass);
            hand.ReleaseGlass();
            Assert.IsTrue(gm.Serve());
            yield return WaitWhileBusy();

            Assert.AreEqual(1, gm.Perfects);
            Assert.GreaterOrEqual(gm.Score, 100);
            Assert.AreEqual(0, glass.Count, "El vaso vuelve vacío");
            Assert.IsTrue(glass.AtHome, "El vaso vuelve a la estación");
            Assert.IsNotNull(gm.CurrentOrder, "Llega un nuevo pedido");
            Assert.AreEqual(Temperature.Ambiente, lever.Current, "La palanca se reinicia");
        }

        [UnityTest]
        public IEnumerator Flow_ThreeKnockoutsSuspendTheShift()
        {
            gm.StartShift();
            for (int i = 0; i < 3; i++)
            {
                glass.Add(I("SalsaDiablo")); glass.Add(I("Ron"));
                Assert.IsTrue(gm.Serve());
                yield return WaitWhileBusy();
            }
            Assert.AreEqual(3, gm.Knockouts);
            Assert.AreEqual(MixState.Finished, gm.State);
            Assert.IsFalse(gm.Won, "Tres desmayos suspenden el turno");
            Assert.IsTrue(GameObject.Find("MainBoard/ResultsPanel").activeInHierarchy, "Panel final visible");
            Assert.AreEqual(1, MixSaveRepository.Load().sessions.Count, "El turno quedó en el historial");
        }

        [UnityTest]
        public IEnumerator Book_DiscoveredReactionsPersist()
        {
            Assert.IsFalse(gm.IsDiscovered(ReactionId.Explosion));
            glass.Add(I("Soda")); glass.Add(I("Efervescente"));
            gm.Serve();
            yield return WaitWhileBusy();
            Assert.IsTrue(gm.IsDiscovered(ReactionId.Explosion), "Se descubre también en el entrenamiento");
            CollectionAssert.Contains(MixSaveRepository.Load().discovered, "Explosion", "Queda guardada en el JSON");
            Assert.AreEqual(0, gm.Score, "En el entrenamiento no hay puntaje");
        }

        [UnityTest]
        public IEnumerator Tutorial_CompletesWithRealActions()
        {
            var tutorial = Object.FindFirstObjectByType<MixTutorial>();
            var player = Object.FindFirstObjectByType<DesktopPlayer>();
            yield return new WaitForSeconds(1.4f);
            player.SimulateMove(Vector2.left);
            yield return new WaitForSeconds(1.2f);
            player.StopSimulation();
            Assert.AreEqual(1, tutorial.CurrentStep, "Paso 1: moverse");

            bottles["Agua"].Pour();
            yield return new WaitForSeconds(1.6f);
            bottles["Limon"].Pour();
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(2, tutorial.CurrentStep, "Paso 2: verter dos ingredientes");

            shake.Pull();
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(3, tutorial.CurrentStep, "Paso 3: agitar");

            hand.TakeGlass(); hand.ReleaseGlass(); gm.Serve();
            yield return WaitWhileBusy();
            Assert.AreEqual(4, tutorial.CurrentStep, "Paso 4: servir");
            Assert.IsTrue(GameObject.Find("TutorialBoard/StartButton").activeInHierarchy, "Aparece INICIAR TURNO");
        }

        [UnityTest]
        public IEnumerator Player_IsBlockedByTheCounter()
        {
            var player = Object.FindFirstObjectByType<DesktopPlayer>();
            player.SimulateMove(Vector2.up);
            yield return new WaitForSeconds(2f);
            player.StopSimulation();
            Assert.Less(player.transform.position.z, 0.5f, "La barra detiene al jugador");
            Assert.Less(player.transform.position.y, 0.15f, "Sigue en el piso");
        }

        IEnumerator WaitWhileBusy()
        {
            float limit = Time.realtimeSinceStartup + 20f;
            yield return null;
            while (gm.Busy && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsFalse(gm.Busy, "El cliente termina de reaccionar");
        }
    }
}
