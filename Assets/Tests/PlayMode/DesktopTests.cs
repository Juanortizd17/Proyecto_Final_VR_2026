using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace QualityControlXR.Tests
{
    /// <summary>
    /// Pruebas del modo PC: colisiones del jugador, agarre, inspección con herramienta,
    /// depósito en contenedores y entrenamiento completo con mouse y teclado.
    /// </summary>
    public class DesktopTests
    {
        GameManager gm;
        DesktopPlayer player;
        DesktopInteractor hand;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("QualityControl");
            yield return null;
            yield return null;
            gm = GameManager.Instance;
            player = Object.FindFirstObjectByType<DesktopPlayer>();
            hand = Object.FindFirstObjectByType<DesktopInteractor>();
            Assert.IsNotNull(player, "La escena tiene jugador de PC");
            Assert.IsNotNull(hand, "La cámara tiene DesktopInteractor");
        }

        [UnityTest]
        public IEnumerator Player_CannotWalkThroughWall()
        {
            player.TeleportTo(new Vector3(0f, 0f, 3.6f), 0f, 0f);      // mirando a la pared norte (z = 5)
            yield return null;
            player.SimulateMove(Vector2.up);
            yield return new WaitForSeconds(2.5f);
            player.StopSimulation();
            Debug.Log($"[Test] Pared: z final {player.transform.position.z:F2}");
            Assert.Greater(player.transform.position.z, 4.2f, "El jugador avanza hasta la pared");
            Assert.Less(player.transform.position.z, 4.9f - 0.2f, "La pared detiene al jugador");
        }

        [UnityTest]
        public IEnumerator Player_DoesNotClimbBins_AndStaysOnFloor()
        {
            player.TeleportTo(new Vector3(0.3f, 0f, 0.25f), 90f, 0f);   // mirando al contenedor de radios
            player.SimulateMove(Vector2.up);
            yield return new WaitForSeconds(2f);
            player.StopSimulation();
            yield return new WaitForSeconds(0.8f);
            Debug.Log($"[Test] Contenedor: pos final {player.transform.position}");
            Assert.Less(player.transform.position.y, 0.15f, "El jugador no queda encima ni flotando");
            Assert.Greater(player.transform.position.x, 0.5f, "El jugador avanza hasta el contenedor");
            Assert.Less(player.transform.position.x, 1.2f - 0.25f, "El contenedor bloquea el paso");
        }

        [UnityTest]
        public IEnumerator Player_CannotWalkThroughDecoration()
        {
            // Robot decorativo (antes se atravesaba): caminamos directo hacia el centro de su colisión.
            var robot = GameObject.Find("robot-arm-a").GetComponent<BoxCollider>();
            Assert.IsNotNull(robot, "El robot tiene BoxCollider");
            var c = robot.bounds.center;
            Debug.Log($"[Test] Robot bounds {robot.bounds}");
            // Se acerca por el oeste (por el sur está la máquina de la banda).
            player.TeleportTo(new Vector3(c.x - robot.bounds.extents.x - 1.2f, 0f, c.z), 90f, 0f);
            yield return null;
            float x0 = player.transform.position.x;
            player.SimulateMove(Vector2.up);
            yield return new WaitForSeconds(2.5f);
            player.StopSimulation();
            Debug.Log($"[Test] Robot: x {x0:F2} -> {player.transform.position.x:F2}");
            Assert.Greater(player.transform.position.x, x0 + 0.5f, "El jugador avanza hacia el robot");
            Assert.Less(player.transform.position.x, c.x, "La decoración tiene colisión: no se atraviesa");
        }

        [UnityTest]
        public IEnumerator Desktop_GrabInspectAndDeposit()
        {
            gm.StartRound();
            yield return new WaitForSeconds(1.6f);
            var product = Object.FindObjectsByType<Product>(FindObjectsSortMode.None).First();
            product.Configure(product.Type, DefectType.Crack);

            hand.Grab(product.GetComponent<Grabbable>());
            yield return null;
            Assert.IsTrue(product.GetComponent<Grabbable>().IsHeld, "Producto en la mano");

            var lupa = Object.FindObjectsByType<InspectionTool>(FindObjectsSortMode.None)
                .First(t => t.name == "Tool_Lupa");
            hand.Equip(lupa);
            hand.UseToolOn(product);
            Assert.IsTrue(product.DefectRevealed, "La lupa revela la grieta");

            var reject = GameObject.Find("Bin_Rechazo").transform.Find("DropZone").position;
            hand.DepositInto(reject);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, gm.Correct, "Desechar el defectuoso suma un acierto");
            Assert.IsNull(hand.Held);
        }

        [UnityTest]
        public IEnumerator Desktop_OnboardingCanBeCompletedWithMouse()
        {
            var tutorial = Object.FindFirstObjectByType<OnboardingTutorial>();
            yield return new WaitForSeconds(1.3f);                        // registra la posición inicial
            var p0 = player.transform.position;
            foreach (var hit in Physics.SphereCastAll(p0 + Vector3.up * 0.9f, 0.3f, Vector3.forward, 2f, ~0, QueryTriggerInteraction.Ignore))
                Debug.Log($"[Test] Obstáculo al frente: {hit.collider.name} (padre {hit.collider.transform.parent?.name}) a {hit.distance:F2} m, bounds {hit.collider.bounds}");
            player.SimulateMove(Vector2.up);
            yield return new WaitForSeconds(0.8f);
            player.StopSimulation();
            yield return null;
            Debug.Log($"[Test] Onboarding: de {p0} a {player.transform.position}, cámara {Camera.main?.transform.position}");
            Assert.AreEqual(1, tutorial.CurrentStep, "Paso 1: moverse");

            var box = GameObject.Find("PracticeBox").GetComponent<Grabbable>();
            hand.Grab(box);
            yield return null;
            Assert.AreEqual(2, tutorial.CurrentStep, "Paso 2: agarrar");

            hand.Equip(GameObject.Find("PracticeTool_Lupa").GetComponent<InspectionTool>());
            hand.UseToolOn(box.GetComponent<PracticeTarget>());
            Assert.AreEqual(3, tutorial.CurrentStep, "Paso 3: inspeccionar");

            var socket = Object.FindFirstObjectByType<PracticeSocket>();
            hand.DepositInto(socket.SnapPosition + Vector3.up * 0.02f);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(4, tutorial.CurrentStep, "Paso 4: colocar en el soporte");
            Assert.IsTrue(GameObject.Find("OnboardingBoard/StartButton").activeInHierarchy, "Aparece INICIAR TURNO");
        }
    }
}
