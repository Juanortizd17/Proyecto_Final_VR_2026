using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QualityControlXR
{
    /// <summary>
    /// Herramienta agarrable (lupa, destornillador, martillo). Al tocar un producto
    /// con el trigger de la herramienta, lo inspecciona y da feedback háptico/visual.
    /// Requiere XRGrabInteractable + Collider (IsTrigger) en la punta.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class InspectionTool : MonoBehaviour
    {
        [SerializeField] DefectType detects = DefectType.Crack;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip foundClip;
        [SerializeField] AudioClip nothingClip;
        [SerializeField, Range(0f, 1f)] float hapticStrength = 0.5f;

        XRGrabInteractable grab;

        void Awake() => grab = GetComponent<XRGrabInteractable>();

        void OnTriggerEnter(Collider other)
        {
            if (!grab.isSelected) return; // solo funciona en la mano
            var product = other.GetComponentInParent<Product>();
            if (product == null) return;

            bool found = product.Inspect(detects);
            if (sfx != null) sfx.PlayOneShot(found ? foundClip : nothingClip);

            if (grab.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor input)
                input.SendHapticImpulse(found ? hapticStrength : hapticStrength * 0.3f, 0.15f);

            if (found) product.SendMessage("OnDefectRevealed", SendMessageOptions.DontRequireReceiver);
        }
    }
}
