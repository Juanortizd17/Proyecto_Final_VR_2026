using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Tablero principal: pedido actual, paciencia del cliente, tiempo, puntos, desmayos,
    /// alertas de eventos y panel final con retroalimentación.
    /// </summary>
    public class MixBoard : MonoBehaviour
    {
        [SerializeField] GameObject waitingPanel, gamePanel, resultsPanel;
        [SerializeField] Text orderTitle, orderDetail, timerText, scoreText, knockoutText, alertText, resultsText;
        [SerializeField] RectTransform patienceFill;
        [SerializeField] Image patienceImage;

        static readonly Color Good = new Color(0.45f, 0.9f, 0.5f);
        static readonly Color Warn = new Color(1f, 0.78f, 0.2f);
        static readonly Color Bad = new Color(1f, 0.4f, 0.35f);

        MixGameManager gm;
        float alertUntil;

        public void Setup(GameObject waiting, GameObject game, GameObject results, Text title, Text detail, Text timer,
                          Text score, Text knockouts, Text alert, Text resultsBody, RectTransform fill, Image fillImage)
        {
            waitingPanel = waiting; gamePanel = game; resultsPanel = results; orderTitle = title; orderDetail = detail;
            timerText = timer; scoreText = score; knockoutText = knockouts; alertText = alert; resultsText = resultsBody;
            patienceFill = fill; patienceImage = fillImage;
        }

        void Start()
        {
            gm = MixGameManager.Instance;
            gm.OnStateChanged += OnState;
            gm.OnOrderChanged += RefreshOrder;
            gm.OnScoreChanged += RefreshScore;
            gm.OnAlert += msg => ShowAlert(msg, Warn);
            gm.OnReaction += (r, isNew) =>
            {
                if (gm.State != MixState.Playing) return;
                string pts = r.id == ReactionId.Perfecto ? "" : $"  ({(r.points > 0 ? "+" : "")}{r.points})";
                ShowAlert((isNew ? "¡Nueva reacción! " : "") + r.displayName + pts, r.id == ReactionId.Perfecto ? Good : r.knockout ? Bad : Warn);
            };
            alertText.text = "";
            OnState(gm.State);
        }

        void Update()
        {
            if (alertText.text.Length > 0 && Time.time > alertUntil) alertText.text = "";
            if (gm == null || gm.State != MixState.Playing) return;
            int t = Mathf.CeilToInt(gm.TimeLeft);
            timerText.text = $"TIEMPO\n<b>{t / 60}:{t % 60:00}</b>";
            timerText.color = gm.TimeLeft <= 30f ? Warn : Color.white;
            float k = gm.PatienceRatio;
            patienceFill.localScale = new Vector3(k, 1f, 1f);
            patienceImage.color = k > 0.5f ? Good : k > 0.25f ? Warn : Bad;
        }

        void ShowAlert(string message, Color color)
        {
            alertText.text = message; alertText.color = color; alertUntil = Time.time + 4f;
        }

        void RefreshOrder()
        {
            var o = gm.CurrentOrder;
            if (o == null) return;
            orderTitle.text = o.displayName.ToUpper();
            // Una sola línea: la letra se encoge por pasos según el largo del nombre
            orderTitle.fontSize = Mathf.Clamp(Mathf.FloorToInt(1500f / Mathf.Max(8, o.displayName.Length) / 8f) * 8, 56, 104);
            string temp = o.needsTemperature ? $"\n<color=#FFC21A>+ temperatura {o.TemperatureLabel}</color>" : "";
            orderDetail.text = o.IngredientList + temp;
        }

        void RefreshScore()
        {
            scoreText.text = $"PUNTOS\n<size=104><b>{gm.Score}</b></size>";
            knockoutText.text = $"DESMAYOS\n<size=104><b>{gm.Knockouts}/{gm.MaxKnockouts}</b></size>";
        }

        void OnState(MixState s)
        {
            waitingPanel.SetActive(s == MixState.Onboarding);
            gamePanel.SetActive(s == MixState.Playing);
            resultsPanel.SetActive(s == MixState.Finished);
            if (s == MixState.Playing) { RefreshOrder(); RefreshScore(); }
            if (s != MixState.Finished) return;

            string title = gm.Won ? "¡TURNO COMPLETADO!" : "TURNO SUSPENDIDO";
            resultsText.text =
                $"<size=72><b>{title}</b></size>\n" +
                $"Puntaje: <size=72><b>{gm.Score}</b></size>{(gm.NewRecord ? "  <color=#FFD54A>¡NUEVO RÉCORD!</color>" : "")}     Récord: {gm.BestScore}\n" +
                $"Bebidas servidas: {gm.Served}     Pedidos perfectos: {gm.Perfects}     Desmayos: {gm.Knockouts}\n" +
                $"Reacciones descubiertas: {gm.DiscoveredCount}/{gm.Reactions.Count}\n" +
                $"<i>{Feedback()}</i>";
        }

        string Feedback()
        {
            if (!gm.Won) return "Demasiados clientes fuera de combate. Revisa las mezclas peligrosas en el libro.";
            if (gm.Served == 0) return "No serviste nada. ¡El cliente se quedó con sed!";
            if (gm.Perfects == gm.Served) return "Turno impecable: todos los pedidos perfectos.";
            if (gm.Perfects * 2 >= gm.Served) return "Buen pulso. Lee bien cada pedido antes de mezclar.";
            return "Mucho experimento y pocos pedidos correctos. ¡Pero el libro creció!";
        }

        // Botones
        public void OnRestartPressed() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

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
