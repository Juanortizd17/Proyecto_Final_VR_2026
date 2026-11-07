using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace QualityControlXR
{
    /// <summary>
    /// Modo escritorio: si al iniciar no hay un visor activo (por ejemplo, el Quest no está
    /// conectado por Link), instancia el XR Device Simulator para jugar con mouse y teclado.
    /// Con el visor conectado no hace nada y la experiencia corre en VR.
    /// Funciona igual en el Editor (Play) y en el .exe de Windows.
    /// </summary>
    public class DesktopFallback : MonoBehaviour
    {
        [SerializeField] GameObject simulatorPrefab;
        [SerializeField] float waitForHeadsetSeconds = 1.5f;

        public static bool SimulatorActive { get; private set; }

        public void Setup(GameObject prefab) => simulatorPrefab = prefab;

        IEnumerator Start()
        {
            // Damos tiempo a que OpenXR intente inicializar el visor.
            yield return new WaitForSeconds(waitForHeadsetSeconds);
            if (HeadsetActive() || simulatorPrefab == null || SimulatorActive) yield break;
            // El simulador se controla con teclado; sin teclado (o en batchmode/CI) su UI falla.
            if (Application.isBatchMode || UnityEngine.InputSystem.Keyboard.current == null)
            {
                Debug.Log("[DesktopFallback] Sin visor ni teclado: no se activa el simulador.");
                yield break;
            }

            Debug.Log("[DesktopFallback] No se detectó visor: se activa el simulador (mouse y teclado).");
            var sim = Instantiate(simulatorPrefab);
            DontDestroyOnLoad(sim);   // sobrevive al botón REINICIAR (recarga de escena)
            SimulatorActive = true;
        }

        static bool HeadsetActive()
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            return manager != null && manager.activeLoader != null && XRSettings.isDeviceActive;
        }
    }
}
