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
            "PASO 1 · MOVERSE\nJoystick <b>izquierdo</b>: caminar  ·  <b>derecho</b>: girar.\nTambién puedes dar un paso real.",
            "PASO 2 · AGARRAR Y VERTER\nAcerca la mano a una botella y aprieta el botón lateral (<b>GRIP</b>).\n<b>Inclínala sobre el vaso</b> para verter. Vierte <b>dos</b> ingredientes.",
            "PASO 3 · AGITAR\nAgarra el vaso y <b>sacúdelo</b> de lado a lado,\no tira de la palanca <b>AGITAR</b>.",
            "PASO 4 · SERVIR\nLleva el vaso al <b>posavasos</b> del cliente y suéltalo encima.\nSi volteas el vaso, se vacía.",
            "¡LISTO PARA TRABAJAR!\nPrepara lo que pida el cliente antes de que pierda la paciencia.\nApunta con el rayo a <b>INICIAR TURNO</b> y aprieta el <b>gatillo</b>."
        };

        string[] Steps => desktopMode ? DesktopSteps : VrSteps;
        public int CurrentStep => step;
        public bool DesktopMode => desktopMode;

        /// <summary>Cambia los textos del entrenamiento entre PC y VR (lo llama MixVrRig).</summary>
        public void SetDesktopMode(bool desktop)
        {
            desktopMode = desktop;
            if (gm != null && gm.State != MixState.Onboarding) OnState(gm.State); else Show();
        }

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
                (desktopMode ? "El lavaplatos vacía el vaso" : "Voltear el vaso lo vacía");
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
