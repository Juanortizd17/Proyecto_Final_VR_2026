using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace QualityControlXR
{
    /// <summary>
    /// Herramienta (lupa, destornillador, martillo). Cada una revela un tipo de defecto.
    ///  · PC: el DesktopInteractor llama a <see cref="UseOn(Product)"/> con clic derecho.
    ///  · VR: la punta es un trigger; al tocar un producto con la herramienta en la mano lo inspecciona.
    /// </summary>
    public class InspectionTool : MonoBehaviour
    {
        [SerializeField] DefectType detects = DefectType.Crack;
        [SerializeField] string toolName = "Lupa";
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip useClip;
        [SerializeField] AudioClip foundClip;
        [SerializeField, Range(0f, 1f)] float hapticStrength = 0.7f;
        [SerializeField] float cooldown = 0.35f;

        XRGrabInteractable grab;
        float lastUse = -10f;

        public DefectType Detects => detects;
        public string ToolName => toolName;

        public void Setup(DefectType d, string displayName, AudioSource source, AudioClip use, AudioClip found)
        {
            detects = d; toolName = displayName; sfx = source; useClip = use; foundClip = found;
        }

        void Awake() => grab = GetComponent<XRGrabInteractable>();

        public bool Ready => Time.time - lastUse >= cooldown;

        /// <summary>Inspecciona un producto. Devuelve true si esta herramienta reveló su defecto.</summary>
        public bool UseOn(Product product)
        {
            if (product == null || !Ready) return false;
            lastUse = Time.time;
            bool found = product.Inspect(detects);
            Feedback(found);
            return found;
        }

        /// <summary>Uso sobre la caja del entrenamiento.</summary>
        public void UseOn(PracticeTarget target)
        {
            if (target == null || !Ready) return;
            lastUse = Time.time;
            target.Inspected();
            Feedback(true);
        }

        // ---------- VR: contacto de la punta ----------
        void OnTriggerEnter(Collider other)
        {
            if (grab == null || !grab.isSelected) return;
            var practice = other.GetComponentInParent<PracticeTarget>();
            if (practice != null) { UseOn(practice); return; }
            var product = other.GetComponentInParent<Product>();
            if (product != null) UseOn(product);
        }

        void Feedback(bool found)
        {
            if (sfx != null) sfx.PlayOneShot(found ? foundClip : useClip);
            if (grab != null && grab.firstInteractorSelecting is XRBaseInputInteractor input)
                input.SendHapticImpulse(found ? hapticStrength : hapticStrength * 0.25f, found ? 0.3f : 0.08f);
        }
    }
}
