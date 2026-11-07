using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace QualityControlXR
{
    /// <summary>
    /// Tablero en World Space: tiempo, puntos, fallos, último reporte, alertas de la banda,
    /// y panel final con retroalimentación, reportes y récord guardado.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] Text timerText;
        [SerializeField] Text scoreText;
        [SerializeField] Text mistakesText;
        [SerializeField] Text reportText;
        [SerializeField] Text alertText;
        [SerializeField] GameObject gamePanel;
        [SerializeField] GameObject onboardingPanel;
        [SerializeField] GameObject resultsPanel;
        [SerializeField] Text resultsText;
        [SerializeField] ConveyorBelt belt;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip warningClip;

        static readonly Color Good = new Color(0.35f, 0.9f, 0.45f);
        static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        static readonly Color Warn = new Color(1f, 0.8f, 0.2f);

        GameManager gm;
        float alertUntil;

        public void Setup(Text timer, Text score, Text mistakes, Text report, Text alert,
                          GameObject game, GameObject onboarding, GameObject results, Text resultsBody,
                          ConveyorBelt conveyor, AudioSource source, AudioClip warning)
        {
            timerText = timer; scoreText = score; mistakesText = mistakes; reportText = report; alertText = alert;
            gamePanel = game; onboardingPanel = onboarding; resultsPanel = results; resultsText = resultsBody;
            belt = conveyor; sfx = source; warningClip = warning;
        }

        void Start()
        {
            gm = GameManager.Instance;
            gm.OnStateChanged += OnState;
            gm.OnScoreChanged += s => scoreText.text = $"PUNTOS\n{s}";
            gm.OnMistake += m => mistakesText.text = $"FALLOS\n{m}/{gm.MaxMistakes}";
            gm.OnReport += (r, ok) => { reportText.text = r; reportText.color = ok ? Good : Bad; };
            gm.OnLowTime += () => Alert("¡Quedan 30 segundos!", Warn);
            if (belt) belt.OnBeltEvent += msg => Alert(msg, Warn);
            alertText.text = "";
            OnState(gm.State);
        }

        void Update()
        {
            if (alertText.text.Length > 0 && Time.time > alertUntil) alertText.text = "";
            if (gm == null || gm.State != GameState.Playing) return;
            int t = Mathf.CeilToInt(gm.TimeLeft);
            timerText.text = $"TIEMPO\n{t / 60:0}:{t % 60:00}";
            timerText.color = gm.TimeLeft <= 30f ? Warn : Color.white;
        }

        void Alert(string msg, Color c)
        {
            alertText.text = msg; alertText.color = c; alertUntil = Time.time + 4f;
            if (sfx && warningClip) sfx.PlayOneShot(warningClip);
        }

        void OnState(GameState s)
        {
            onboardingPanel.SetActive(s == GameState.Onboarding);
            gamePanel.SetActive(s == GameState.Playing);
            resultsPanel.SetActive(s == GameState.Finished);
            if (s != GameState.Finished) return;

            string title = gm.Won ? "¡TURNO COMPLETADO!" : "TURNO FALLIDO";
            string last = string.Join("\n", gm.Reports.Reverse().Take(3));
            resultsText.text =
                $"<size=56><b>{title}</b></size>\n" +
                $"Puntaje final: <b>{gm.Score}</b>{(gm.NewRecord ? "  <color=#FFD54A>¡NUEVO RÉCORD!</color>" : "")}\n" +
                $"Récord histórico: {gm.BestScore}    Aciertos: {gm.Correct}    Fallos: {gm.Mistakes}\n" +
                $"<i>{Feedback()}</i>\n<size=28><color=#B8C2D0>{last}</color></size>";
        }

        string Feedback()
        {
            if (gm.Correct == 0) return "No clasificaste ningún producto. Usa las herramientas y decide.";
            if (gm.Mistakes == 0) return "Inspección impecable: calidad garantizada.";
            if (gm.Correct >= gm.Mistakes * 3) return "Muy buen ojo. Revisa con las 3 herramientas antes de decidir.";
            return "Cuidado: inspecciona cada producto con lupa, destornillador y martillo.";
        }

        [Header("Puesto de trabajo")]
        [SerializeField] Transform playerRig;
        [SerializeField] Transform workSpot;

        public void SetTeleport(Transform rig, Transform spot) { playerRig = rig; workSpot = spot; }

        /// <summary>Lleva al jugador frente a la banda manteniendo la cabeza sobre el punto.</summary>
        void MoveToWorkSpot()
        {
            if (!playerRig || !workSpot) return;
            var cam = Camera.main ? Camera.main.transform : null;
            Vector3 headOffset = cam ? cam.position - playerRig.position : Vector3.zero;
            headOffset.y = 0f;
            float yawDelta = workSpot.eulerAngles.y - (cam ? cam.eulerAngles.y : playerRig.eulerAngles.y);
            playerRig.RotateAround(cam ? cam.position : playerRig.position, Vector3.up, yawDelta);
            headOffset = cam ? cam.position - playerRig.position : Vector3.zero;
            headOffset.y = 0f;
            playerRig.position = new Vector3(workSpot.position.x, playerRig.position.y, workSpot.position.z) - headOffset;
            Physics.SyncTransforms();   // el CharacterController del rig toma la nueva posición
        }

        // Botones de UI XR
        public void OnStartPressed() { MoveToWorkSpot(); gm.StartRound(); }
        public void OnRestartPressed() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
