using UnityEngine;
using UnityEngine.UI;

namespace QualityControlXR
{
    /// <summary>
    /// Entrenamiento guiado en 5 pasos: moverse, agarrar, inspeccionar con una herramienta,
    /// colocar en el soporte y pulsar INICIAR. Cada paso se valida con eventos reales del juego.
    /// Tiene textos para PC (mouse y teclado) y para VR (controles del visor).
    /// </summary>
    public class OnboardingTutorial : MonoBehaviour
    {
        [SerializeField] Text instructionText;
        [SerializeField] Text progressText;
        [SerializeField] Transform playerRig;
        [SerializeField] Grabbable practiceObject;
        [SerializeField] PracticeSocket practiceSocket;
        [SerializeField] GameObject startButton;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip stepClip;
        [SerializeField] bool desktopMode = true;
        [SerializeField] float moveDistance = 0.4f;

        int step;
        Vector3 headStart;
        bool startRecorded;
        float startTime;

        static readonly string[] DesktopSteps =
        {
            "PASO 1 · MOVERSE\nCamina con <b>W A S D</b> y mira con el <b>mouse</b>.\nAcércate a la mesa de práctica.",
            "PASO 2 · AGARRAR\nApunta a la <b>caja naranja</b> con la mira y haz <b>CLIC IZQUIERDO</b>.",
            "PASO 3 · INSPECCIONAR\nApunta a la <b>lupa</b> y haz <b>CLIC IZQUIERDO</b> para tomarla.\nLuego haz <b>CLIC DERECHO</b> para revisar la caja.",
            "PASO 4 · COLOCAR\nApunta al <b>soporte verde</b> y haz <b>CLIC IZQUIERDO</b> para dejar la caja.",
            "¡ENTRENAMIENTO COMPLETO!\nApunta a <b>INICIAR TURNO</b> y haz <b>CLIC IZQUIERDO</b>."
        };

        static readonly string[] VrSteps =
        {
            "PASO 1 · MOVERSE\nUsa el joystick IZQUIERDO para caminar y el DERECHO para girar.\nAcércate a la mesa de práctica.",
            "PASO 2 · AGARRAR\nAcerca la mano a la caja y mantén el botón lateral (GRIP).",
            "PASO 3 · INSPECCIONAR\nToma una herramienta y tócala contra la caja.",
            "PASO 4 · COLOCAR\nSuelta la caja sobre el soporte verde.",
            "¡ENTRENAMIENTO COMPLETO!\nApunta al botón INICIAR TURNO y presiona el GATILLO."
        };

        string[] Steps => desktopMode ? DesktopSteps : VrSteps;

        /// <summary>Se mide la cabeza (cámara): cuenta caminar, joystick o simulador por igual.</summary>
        Transform Head => Camera.main ? Camera.main.transform : playerRig;

        public void Setup(Text instr, Text progress, Transform rig, Grabbable obj, PracticeSocket socket,
                          GameObject start, AudioSource source, AudioClip clip, bool desktop)
        {
            instructionText = instr; progressText = progress; playerRig = rig; practiceObject = obj;
            practiceSocket = socket; startButton = start; sfx = source; stepClip = clip; desktopMode = desktop;
        }

        void Start()
        {
            startTime = Time.time;
            startButton.SetActive(false);
            if (practiceObject) practiceObject.OnGrabbed += _ => Complete(1);
            if (practiceSocket) practiceSocket.OnPlaced += () => Complete(3);
            Show();
        }

        void Update()
        {
            if (step != 0 || Head == null) return;
            if (!startRecorded)
            {
                if (Time.time - startTime < 1f) return;
                headStart = Head.position; startRecorded = true;
                return;
            }
            if (Vector3.Distance(Flat(Head.position), Flat(headStart)) > moveDistance) Complete(0);
        }

        /// <summary>Llamado por la caja de práctica cuando una herramienta la revisa.</summary>
        public void OnPracticeInspected() => Complete(2);

        /// <summary>Botón "Saltar tutorial".</summary>
        public void Skip() { step = Steps.Length - 1; Show(); }

        public int CurrentStep => step;

        void Complete(int s)
        {
            if (step != s) return;
            step++;
            if (sfx && stepClip) sfx.PlayOneShot(stepClip);
            Show();
        }

        void Show()
        {
            var steps = Steps;
            instructionText.text = steps[Mathf.Min(step, steps.Length - 1)];
            if (progressText) progressText.text = $"{Mathf.Min(step, steps.Length - 1)}/{steps.Length - 1}";
            startButton.SetActive(step >= steps.Length - 1);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
