using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;

namespace MixLab
{
    /// <summary>
    /// Decide al arrancar si se juega en VR o en PC y prepara la escena para ese modo.
    ///  · Meta Quest (APK): siempre VR.
    ///  · PC: modo escritorio (mouse y teclado). Con el visor por Link: LaboratorioMezclas.exe -vr
    ///  · En el Editor: VR si el visor ya está activo al darle Play.
    /// En VR se activa el XR Origin (manos, locomoción, rayos) y los componentes de XR Interaction
    /// Toolkit del bar (botellas, vaso, palancas, posavasos); se apaga el jugador de PC.
    /// </summary>
    public class MixVrRig : MonoBehaviour
    {
        [SerializeField] GameObject xrOrigin;
        [SerializeField] Camera xrCamera;
        [SerializeField] GameObject desktopPlayer;
        [SerializeField] GameObject desktopHud;
        [SerializeField] InputSystemUIInputModule desktopUi;
        [SerializeField] XRUIInputModule xrUi;
        [SerializeField] MixTutorial tutorial;
        [SerializeField] Behaviour[] vrBehaviours;
        [SerializeField] GameObject[] vrOnlyObjects;

        /// <summary>true mientras se juega con el visor.</summary>
        public static bool Active { get; private set; }
        public static event System.Action OnVrEnabled;

        public void Setup(GameObject origin, Camera cam, GameObject player, GameObject hud, InputSystemUIInputModule desktopModule,
                          XRUIInputModule xrModule, MixTutorial tut, Behaviour[] behaviours, GameObject[] objects)
        {
            xrOrigin = origin; xrCamera = cam; desktopPlayer = player; desktopHud = hud; desktopUi = desktopModule;
            xrUi = xrModule; tutorial = tut; vrBehaviours = behaviours; vrOnlyObjects = objects;
        }

        void Awake()
        {
            Active = false;
            // El rig empieza apagado: su cámara no debe competir con la del PC.
            if (xrOrigin) xrOrigin.SetActive(false);
            if (xrUi) xrUi.enabled = false;
        }

        void OnDestroy() { if (Active) Active = false; }

        IEnumerator Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool wantDesktop = args.Contains("-desktop") || args.Contains("-autodemo");
            bool wantVr = args.Contains("-vr");
            if (wantDesktop) yield break;

            if (Application.platform == RuntimePlatform.Android) { EnableVr(); yield break; }

            if (wantVr && !LoaderActive())
            {
                var manager = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
                if (manager != null)
                {
                    yield return manager.InitializeLoader();
                    if (manager.activeLoader != null) manager.StartSubsystems();
                }
            }
            // Le damos un momento al runtime de OpenXR para reportar el visor.
            for (float t = 0f; t < 1.5f && !HeadsetActive(); t += Time.unscaledDeltaTime) yield return null;
            if (HeadsetActive()) EnableVr();
            else if (wantVr) Debug.LogWarning("[MixVrRig] Se pidió -vr pero no se detectó ningún visor: se juega en PC.");
        }

        static bool LoaderActive()
        {
            var manager = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
            return manager != null && manager.activeLoader != null;
        }

        static bool HeadsetActive() => LoaderActive() && XRSettings.isDeviceActive;

        /// <summary>Pasa la escena a modo VR (también lo usan las pruebas automáticas, sin visor).</summary>
        public void EnableVr()
        {
            if (Active) return;
            Active = true;
            if (desktopPlayer) desktopPlayer.SetActive(false);
            if (desktopHud) desktopHud.SetActive(false);
            if (xrOrigin) xrOrigin.SetActive(true);
            if (desktopUi) desktopUi.enabled = false;
            if (xrUi) xrUi.enabled = true;
            if (vrBehaviours != null) foreach (var b in vrBehaviours) if (b) b.enabled = true;
            if (vrOnlyObjects != null) foreach (var o in vrOnlyObjects) if (o) o.SetActive(true);
            if (tutorial) tutorial.SetDesktopMode(false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (xrCamera)
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.renderMode == RenderMode.WorldSpace) c.worldCamera = xrCamera;
            if (EventSystem.current) EventSystem.current.UpdateModules();
            Debug.Log("[MixVrRig] Modo VR activo: manos, locomoción y rayos de XR Interaction Toolkit.");
            OnVrEnabled?.Invoke();
            if (xrOrigin) StartCoroutine(PlaceAtStation());
        }

        /// <summary>true cuando la cabeza ya quedó en el puesto de trabajo (el menú VR se coloca después).</summary>
        public static bool Placed { get; private set; }

        /// <summary>
        /// El Quest coloca al jugador según dónde esté parado en su habitación: podría aparecer dentro
        /// de la barra o de espaldas. Se espera a que el visor reporte la cabeza y se mueve el XR Origin
        /// para que la cámara quede en el puesto, mirando al cliente, conservando la estatura real.
        /// </summary>
        IEnumerator PlaceAtStation()
        {
            Placed = false;
            var origin = xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            Vector3 station = xrOrigin.transform.position;          // el builder deja el rig en el puesto
            if (origin) origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
            var cam = xrCamera ? xrCamera.transform : null;
            // Esperar a que el seguimiento esté activo (la cámara deja de estar en el origen)
            for (float t = 0f; t < 2f && cam && cam.localPosition.sqrMagnitude < 0.0001f; t += Time.unscaledDeltaTime) yield return null;
            yield return null;
            if (origin && cam)
            {
                origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
                origin.MoveCameraToWorldLocation(new Vector3(station.x, cam.position.y, station.z));
                Debug.Log($"[MixVrRig] Jugador colocado en el puesto (altura de ojos {cam.position.y:0.00} m)");
            }
            Placed = true;
        }
    }
}
