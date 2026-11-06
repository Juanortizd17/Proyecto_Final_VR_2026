using UnityEngine;
using UnityEngine.UI;

namespace QualityControlXR
{
    /// <summary>
    /// HUD en World Space: tiempo, puntaje, fallos, último reporte, y panel final
    /// con retroalimentación. Los botones llaman a StartRound / Restart (UI XR).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] Text timerText;
        [SerializeField] Text scoreText;
        [SerializeField] Text mistakesText;
        [SerializeField] Text reportText;
        [SerializeField] GameObject onboardingPanel;
        [SerializeField] GameObject resultsPanel;
        [SerializeField] Text resultsText;

        GameManager gm;

        void Start()
        {
            gm = GameManager.Instance;
            gm.OnStateChanged += OnState;
            gm.OnScoreChanged += s => scoreText.text = $"Puntos: {s}";
            gm.OnMistake += m => mistakesText.text = $"Fallos: {m}";
            gm.OnReport += r => reportText.text = r;
            OnState(gm.State);
        }

        void Update()
        {
            if (gm == null || gm.State != GameState.Playing) return;
            int t = Mathf.CeilToInt(gm.TimeLeft);
            timerText.text = $"{t / 60:00}:{t % 60:00}";
        }

        void OnState(GameState s)
        {
            onboardingPanel.SetActive(s == GameState.Onboarding);
            resultsPanel.SetActive(s == GameState.Finished);
            if (s == GameState.Finished)
                resultsText.text =
                    (gm.Won ? "¡Turno completado!\n" : "Turno terminado\n") +
                    $"Puntaje final: {gm.Score}\nAciertos: {gm.Correct}\nFallos: {gm.Mistakes}\n" +
                    Feedback();
        }

        string Feedback()
        {
            if (gm.Mistakes == 0) return "Inspección impecable.";
            if (gm.Correct > gm.Mistakes * 2) return "Buen ojo, pero revisa mejor cada producto.";
            return "Usa las tres herramientas antes de decidir.";
        }

        // Botones de UI XR
        public void OnStartPressed() => gm.StartRound();
        public void OnRestartPressed() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
