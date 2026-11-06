using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QualityControlXR
{
    /// <summary>
    /// Onboarding VR guiado: mover, agarrar, usar herramienta, soltar en un socket.
    /// Cada paso se completa con un evento XR; al terminar habilita el botón de inicio.
    /// </summary>
    public class OnboardingTutorial : MonoBehaviour
    {
        [SerializeField] Text instructionText;
        [SerializeField] XRGrabInteractable practiceObject;
        [SerializeField] GameObject startButton;

        int step;
        readonly string[] steps =
        {
            "Muévete con el joystick izquierdo y gira con el derecho.",
            "Acerca la mano al objeto y mantén el botón de agarre (grip).",
            "Suéltalo sobre el estante verde (socket).",
            "¡Listo! Pulsa INICIAR para comenzar el turno."
        };

        void Start()
        {
            startButton.SetActive(false);
            practiceObject.selectEntered.AddListener(_ => Advance(1));
            practiceObject.selectExited.AddListener(_ => { if (step == 2) Advance(2); });
            Show();
        }

        public void OnMoved() { if (step == 0) Advance(0); } // llamado por LocomotionHook
        public void OnSocketPlaced() { if (step == 2) Advance(2); }

        void Advance(int fromStep)
        {
            if (step != fromStep) return;
            step++;
            Show();
        }

        void Show()
        {
            instructionText.text = steps[Mathf.Min(step, steps.Length - 1)];
            startButton.SetActive(step >= steps.Length - 1);
        }
    }
}
