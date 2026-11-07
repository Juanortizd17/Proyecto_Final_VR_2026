using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace QualityControlXR
{
    /// <summary>
    /// Herramienta agarrable (lupa, destornillador, martillo). La punta es un trigger:
    /// al tocar un producto mientras está en la mano, lo inspecciona y da feedback
    /// sonoro + háptico (fuerte si encontró el defecto, suave si no).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class InspectionTool : MonoBehaviour
    {
        [SerializeField] DefectType detects = DefectType.Crack;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip useClip;
        [SerializeField] AudioClip foundClip;
        [SerializeField, Range(0f, 1f)] float hapticStrength = 0.7f;
        [SerializeField] float cooldown = 0.4f;

        XRGrabInteractable grab;
        float lastUse;

        public DefectType Detects => detects;

        public void Setup(DefectType d, AudioSource source, AudioClip use, AudioClip found)
        {
            detects = d; sfx = source; useClip = use; foundClip = found;
        }

        void Awake() => grab = GetComponent<XRGrabInteractable>();

        void OnTriggerEnter(Collider other)
        {
            if (!grab.isSelected || Time.time - lastUse < cooldown) return;

            var practice = other.GetComponentInParent<PracticeTarget>();
            if (practice != null) { lastUse = Time.time; practice.Inspected(); Feedback(true); return; }

            var product = other.GetComponentInParent<Product>();
            if (product == null) return;
            lastUse = Time.time;
            Feedback(product.Inspect(detects));
        }

        void Feedback(bool found)
        {
            if (sfx != null) sfx.PlayOneShot(found ? foundClip : useClip);
            if (grab.firstInteractorSelecting is XRBaseInputInteractor input)
                input.SendHapticImpulse(found ? hapticStrength : hapticStrength * 0.25f, found ? 0.3f : 0.08f);
        }
    }
}
