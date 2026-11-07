using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace QualityControlXR
{
    /// <summary>
    /// Onboarding VR guiado en 5 pasos: moverse, agarrar, inspeccionar con una herramienta,
    /// colocar en un socket y pulsar INICIAR. Cada paso se valida con eventos XR reales.
    /// </summary>
    public class OnboardingTutorial : MonoBehaviour
    {
        [SerializeField] Text instructionText;
        [SerializeField] Text progressText;
        [SerializeField] Transform playerRig;
        [SerializeField] XRGrabInteractable practiceObject;
        [SerializeField] XRGrabInteractable practiceTool;
        [SerializeField] XRSocketInteractor practiceSocket;
        [SerializeField] GameObject startButton;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip stepClip;
        [SerializeField] float moveDistance = 0.6f;

        int step;
        Vector3 rigStart;

        readonly string[] steps =
        {
            "PASO 1 · MOVERSE\nUsa el joystick IZQUIERDO para caminar y el DERECHO para girar.\nAcércate a la mesa de práctica.",
            "PASO 2 · AGARRAR\nAcerca la mano a la caja y mantén el botón lateral (GRIP).",
            "PASO 3 · INSPECCIONAR\nToma una herramienta y tócala contra la caja.",
            "PASO 4 · COLOCAR\nSuelta la caja sobre el soporte verde.",
            "¡ENTRENAMIENTO COMPLETO!\nApunta al botón INICIAR TURNO y presiona el GATILLO."
        };

        public void Setup(Text instr, Text progress, Transform rig, XRGrabInteractable obj,
                          XRGrabInteractable tool, XRSocketInteractor socket, GameObject start,
                          AudioSource source, AudioClip clip)
        {
            instructionText = instr; progressText = progress; playerRig = rig; practiceObject = obj;
            practiceTool = tool; practiceSocket = socket; startButton = start; sfx = source; stepClip = clip;
        }

        void Start()
        {
            if (playerRig) rigStart = playerRig.position;
            startButton.SetActive(false);
            practiceObject.selectEntered.AddListener(_ => Complete(1));
            practiceSocket.selectEntered.AddListener(_ => Complete(3));
            Show();
        }

        void Update()
        {
            if (step == 0 && playerRig &&
                Vector3.Distance(Flat(playerRig.position), Flat(rigStart)) > moveDistance)
                Complete(0);
        }

        /// <summary>Llamado por la caja de práctica cuando una herramienta la toca.</summary>
        public void OnPracticeInspected() => Complete(2);

        /// <summary>Permite saltar el tutorial (botón "Saltar").</summary>
        public void Skip() { step = steps.Length - 1; Show(); }

        void Complete(int s)
        {
            if (step != s) return;
            step++;
            if (sfx && stepClip) sfx.PlayOneShot(stepClip);
            Show();
        }

        void Show()
        {
            instructionText.text = steps[Mathf.Min(step, steps.Length - 1)];
            if (progressText) progressText.text = $"{Mathf.Min(step, steps.Length - 1)}/{steps.Length - 1}";
            startButton.SetActive(step >= steps.Length - 1);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
