using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MixLab
{
    /// <summary>
    /// VR: el vaso mezclador se agarra con la mano (XR Grab Interactable).
    ///  · Sacudirlo de un lado a otro agita la mezcla.
    ///  · Voltearlo boca abajo lo vacía.
    ///  · Dejarlo en el posavasos del cliente (socket) lo sirve.
    ///  · Soltarlo en cualquier otro sitio lo devuelve a la estación.
    /// Mientras la mezcla es inestable, vibra en la mano.
    /// </summary>
    [RequireComponent(typeof(MixGlass))]
    public class VrGlass : MonoBehaviour
    {
        [SerializeField] XRGrabInteractable grab;
        [SerializeField] float shakeSpeed = 0.8f;      // m/s mínimos para contar un vaivén
        [SerializeField] int shakesNeeded = 5;         // cambios de dirección para dar la mezcla por agitada
        [SerializeField] float shakeWindow = 0.7f;     // si pasa este tiempo sin vaivén, se reinicia la cuenta
        [SerializeField] float spillDot = 0.45f;       // boca abajo (~117°) para vaciar

        MixGlass glass;
        IXRSelectInteractor holder;
        Vector3 lastPos, lastDir;
        float lastShakeTime, hapticTimer;
        int shakes;

        public XRGrabInteractable Grab => grab;
        /// <summary>Lo usan las pruebas automáticas para simular la mano sin visor.</summary>
        public bool SimulateHeld { get; set; }
        public bool HeldByHand => SimulateHeld || (grab != null && grab.isSelected && holder != null && holder is not XRSocketInteractor);

        public void Setup(XRGrabInteractable g) => grab = g;

        void Awake()
        {
            glass = GetComponent<MixGlass>();
            if (grab == null) grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            if (grab == null) return;
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            if (grab == null) return;
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }

        void OnGrab(SelectEnterEventArgs a)
        {
            if (a.interactorObject is XRSocketInteractor) return;
            holder = a.interactorObject;
            glass.SetVrHeld(true);
            lastPos = transform.position; shakes = 0;
        }

        void OnRelease(SelectExitEventArgs a)
        {
            if (a.interactorObject is XRSocketInteractor) return;
            holder = null;
            StartCoroutine(ReturnUnlessServed());
        }

        // Se espera un instante: si el socket del posavasos lo tomó, el servicio ya arrancó (glass.Busy).
        System.Collections.IEnumerator ReturnUnlessServed()
        {
            yield return new WaitForSeconds(0.12f);
            bool serving = MixGameManager.Instance && MixGameManager.Instance.Busy;
            if (!glass.Busy && !glass.Shattered && !serving && !grab.isSelected) yield return glass.ReturnHome(0.35f);
        }

        void Update()
        {
            // Mientras el cliente lo tiene o el vaso estalla, la mano no puede sujetarlo.
            bool serving = MixGameManager.Instance && MixGameManager.Instance.Busy;
            bool allowed = !glass.Busy && !glass.Shattered && !serving;
            if (grab && grab.enabled != allowed && MixVrRig.Active) grab.enabled = allowed;
            if (!HeldByHand) return;

            if (Vector3.Dot(transform.up, Vector3.down) > spillDot && glass.Count > 0) glass.Empty();
            TrackShake(transform.position, Time.deltaTime);

            if (glass.Unstable || MixAnalyzer.Instance && MixAnalyzer.Instance.Fuse > 0f)
            {
                hapticTimer -= Time.deltaTime;
                if (hapticTimer <= 0f) { hapticTimer = 0.5f; Haptic(0.25f, 0.08f); }
            }
        }

        /// <summary>Cuenta los vaivenes de la mano; también lo usan las pruebas automáticas.</summary>
        public void TrackShake(Vector3 pos, float dt)
        {
            if (dt <= 0f) return;
            Vector3 v = (pos - lastPos) / dt;
            lastPos = pos;
            if (Time.time - lastShakeTime > shakeWindow) shakes = 0;
            if (v.magnitude < shakeSpeed) return;
            Vector3 dir = v.normalized;
            if (lastDir != Vector3.zero && Vector3.Dot(dir, lastDir) < -0.3f)
            {
                shakes++;
                lastShakeTime = Time.time;
                if (shakes >= shakesNeeded && glass.MarkShaken()) { Haptic(0.7f, 0.25f); shakes = 0; }
            }
            lastDir = dir;
        }

        void Haptic(float amplitude, float duration)
        {
            if (holder is XRBaseInputInteractor input) input.SendHapticImpulse(amplitude, duration);
        }
    }
}
