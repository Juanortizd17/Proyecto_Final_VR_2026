using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Entrenamiento guiado: moverse, verter, agitar y servir. Cada paso se valida con
    /// eventos reales del juego. Al terminar aparece el botón INICIAR TURNO.
    /// </summary>
    public class MixTutorial : MonoBehaviour
    {
        [SerializeField] Text instructionText;
        [SerializeField] Text progressText;
        [SerializeField] GameObject startButton;
        [SerializeField] GameObject skipButton;
        [SerializeField] MixGlass glass;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip stepClip;
        [SerializeField] bool desktopMode = true;
        [SerializeField] float moveDistance = 0.35f;

        int step;
        Vector3 headStart;
        bool startRecorded;
        float startTime;
        MixGameManager gm;

        static readonly string[] DesktopSteps =
        {
            "PASO 1 · MOVERSE\nCamina con <b>W A S D</b> y mira con el <b>mouse</b>.",
            "PASO 2 · VERTER\nApunta a un ingrediente del estante y haz <b>CLIC IZQUIERDO</b>.\nVierte <b>dos</b> ingredientes en el vaso.",
            "PASO 3 · AGITAR\nApunta a la palanca <b>AGITAR</b> y haz <b>CLIC IZQUIERDO</b>.",
            "PASO 4 · SERVIR\nHaz clic en el <b>vaso</b> para agarrarlo.\nLuego apunta al cliente y haz clic para servirle.",
            "¡LISTO PARA TRABAJAR!\nPrepara lo que pida el cliente antes de que pierda la paciencia.\nApunta a <b>INICIAR TURNO</b> y haz clic."
        };

        static readonly string[] VrSteps =
        {
            "PASO 1 · MOVERSE\nUsa el joystick izquierdo para caminar y el derecho para girar.",
            "PASO 2 · VERTER\nToma un ingrediente con el GRIP e inclínalo sobre el vaso.\nVierte dos ingredientes.",
            "PASO 3 · AGITAR\nBaja la palanca AGITAR.",
            "PASO 4 · SERVIR\nAgarra el vaso y déjalo sobre el posavasos del cliente.",
            "¡LISTO PARA TRABAJAR!\nPrepara lo que pida el cliente antes de que pierda la paciencia.\nPulsa INICIAR TURNO."
        };

        string[] Steps => desktopMode ? DesktopSteps : VrSteps;
        public int CurrentStep => step;

        public void Setup(Text instruction, Text progress, GameObject start, GameObject skip, MixGlass g,
                          AudioSource source, AudioClip clip, bool desktop)
        {
            instructionText = instruction; progressText = progress; startButton = start; skipButton = skip;
            glass = g; sfx = source; stepClip = clip; desktopMode = desktop;
        }

        void Start()
        {
            gm = MixGameManager.Instance;
            startTime = Time.time;
            glass.OnChanged += OnGlassChanged;
            gm.OnReaction += (r, isNew) => Complete(3);
            gm.OnStateChanged += OnState;
            Show();
        }

        void OnState(MixState s)
        {
            if (s == MixState.Onboarding) return;
            // Durante el turno el tablero queda como recordatorio de reglas.
            startButton.SetActive(false);
            if (skipButton) skipButton.SetActive(false);
            if (progressText) progressText.text = "";
            instructionText.text =
                "<b>REGLAS DEL TURNO</b>\n" +
                "Pedido perfecto: +100 y bono por rapidez\n" +
                "Mezcla equivocada: el cliente reacciona\n" +
                "3 desmayos y se suspende el turno\n" +
                "El lavaplatos vacía el vaso";
        }

        void OnGlassChanged()
        {
            if (step == 1 && glass.Count >= 2) Complete(1);
            if (step == 2 && glass.Shaken) Complete(2);
        }

        void Update()
        {
            if (step != 0) return;
            var head = Camera.main ? Camera.main.transform : null;
            if (head == null) return;
            if (!startRecorded)
            {
                if (Time.time - startTime < 1f) return;
                headStart = head.position; startRecorded = true;
                return;
            }
            Vector3 d = head.position - headStart; d.y = 0f;
            if (d.magnitude > moveDistance) Complete(0);
        }

        /// <summary>Botón "Saltar tutorial".</summary>
        public void Skip() { step = Steps.Length - 1; Show(); }

        /// <summary>Botón "INICIAR TURNO".</summary>
        public void StartShift() => gm.StartShift();

        void Complete(int s)
        {
            if (step != s) return;
            step++;
            if (sfx && stepClip) sfx.PlayOneShot(stepClip);
            Show();
            // Si ya había cumplido el siguiente paso (p. ej. vertió 2 antes de moverse), se encadena.
            OnGlassChanged();
        }

        void Show()
        {
            if (gm != null && gm.State != MixState.Onboarding) return;
            var steps = Steps;
            int last = steps.Length - 1;
            instructionText.text = steps[Mathf.Min(step, last)];
            if (progressText) progressText.text = $"{Mathf.Min(step, last)}/{last}";
            startButton.SetActive(step >= last);
            if (skipButton) skipButton.SetActive(step < last);
        }
    }
}
