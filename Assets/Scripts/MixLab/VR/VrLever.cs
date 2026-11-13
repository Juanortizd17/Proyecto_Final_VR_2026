using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MixLab
{
    /// <summary>
    /// VR: palanca de la estación que se agarra con la mano (XR Simple Interactable).
    ///  · Temperatura: se arrastra a la izquierda (FRÍO), al centro (AMBIENTE) o a la derecha (CALIENTE).
    ///  · Agitar: se tira de ella en cualquier dirección.
    /// Un toque corto sin arrastrar también funciona (cambia de posición o agita).
    /// </summary>
    public class VrLever : MonoBehaviour
    {
        [SerializeField] XRSimpleInteractable interactable;
        [SerializeField] TemperatureLever temperature;
        [SerializeField] ShakeLever shake;
        [SerializeField] float sideStep = 0.04f;     // metros hacia un lado para FRÍO / CALIENTE
        [SerializeField] float pullDistance = 0.06f; // metros de tirón para agitar

        Transform hand;
        IXRSelectInteractor holder;
        Vector3 grabStart;
        float grabTime, maxMove;
        bool pulled;

        public void Setup(XRSimpleInteractable i, TemperatureLever t, ShakeLever s) { interactable = i; temperature = t; shake = s; }

        void Awake() { if (interactable == null) interactable = GetComponent<XRSimpleInteractable>(); }

        void OnEnable()
        {
            if (interactable == null) return;
            interactable.selectEntered.AddListener(OnGrab);
            interactable.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            if (interactable == null) return;
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
        }

        void OnGrab(SelectEnterEventArgs a)
        {
            holder = a.interactorObject;
            hand = a.interactorObject.GetAttachTransform(interactable);
            BeginDrag(hand.position);
        }

        void OnRelease(SelectExitEventArgs a)
        {
            EndDrag();
            hand = null; holder = null;
        }

        void Update()
        {
            if (hand != null) Drag(hand.position);
        }

        // ---------- Lógica de arrastre (pública para las pruebas automáticas) ----------

        public void BeginDrag(Vector3 handPos)
        {
            grabStart = handPos; grabTime = Time.time; maxMove = 0f; pulled = false;
        }

        public void Drag(Vector3 handPos)
        {
            Vector3 delta = handPos - grabStart;
            maxMove = Mathf.Max(maxMove, delta.magnitude);
            if (temperature)
            {
                float side = Vector3.Dot(delta, transform.right);
                var target = side <= -sideStep ? Temperature.Frio : side >= sideStep ? Temperature.Caliente
                           : Mathf.Abs(side) < sideStep * 0.4f ? Temperature.Ambiente : temperature.Current;
                if (target != temperature.Current) { temperature.Set(target); Haptic(0.35f, 0.06f); }
            }
            else if (shake && !pulled && delta.magnitude >= pullDistance)
            {
                pulled = true;
                shake.Pull();
                Haptic(0.5f, 0.15f);
            }
        }

        public void EndDrag()
        {
            // Toque corto: funciona como un botón.
            bool tap = maxMove < 0.02f && Time.time - grabTime < 0.8f;
            if (!tap) return;
            if (temperature) temperature.Interact(null);
            else if (shake && !pulled) shake.Pull();
        }

        void Haptic(float amplitude, float duration)
        {
            if (holder is XRBaseInputInteractor input) input.SendHapticImpulse(amplitude, duration);
        }
    }
}
