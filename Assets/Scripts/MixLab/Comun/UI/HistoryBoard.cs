using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Tablero de historial: muestra los datos guardados entre partidas (mix_save.json):
    /// récord, últimos turnos, promedio y reacciones descubiertas. También recuerda dónde
    /// queda el CSV exportado. Se actualiza al terminar cada turno.
    /// </summary>
    public class HistoryBoard : MonoBehaviour
    {
        [SerializeField] Text summary;
        [SerializeField] Text[] rows;
        [SerializeField] Text footer;

        MixGameManager gm;
        int baseFont;

        public void Setup(Text summaryText, Text[] rowTexts, Text footerText) { summary = summaryText; rows = rowTexts; footer = footerText; }

        void Start()
        {
            gm = MixGameManager.Instance;
            if (gm == null || rows == null || rows.Length == 0) return;
            baseFont = rows[0].fontSize;
            gm.OnStateChanged += s => { if (s == MixState.Finished) Refresh(); };
            gm.OnDiscoveredChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            var sessions = gm.Sessions;
            int n = sessions.Count;
            float avg = n == 0 ? 0f : (float)sessions.Average(s => s.score);
            int won = sessions.Count(s => s.completed);
            summary.text = n == 0
                ? "Todavía no hay turnos guardados.\n<size=38>Juega uno y aquí verás tu progreso.</size>"
                : $"Récord: <color=#7FE08A><b>{gm.BestScore}</b></color>     Turnos: <b>{n}</b>     Promedio: <b>{avg:0}</b>\n" +
                  $"<size=38>Completados: {won} de {n}     Reacciones descubiertas: {gm.DiscoveredCount}/{gm.Reactions.Count}</size>";

            // Últimos turnos, el más reciente arriba
            for (int i = 0; i < rows.Length; i++)
            {
                int idx = n - 1 - i;
                if (idx < 0) { rows[i].text = ""; continue; }
                var s = sessions[idx];
                string date = s.date != null && s.date.Length > 5 ? s.date.Substring(5) : s.date;
                string mark = s.completed ? "<color=#7FE08A>completo</color>" : "<color=#FF8073>suspendido</color>";
                string best = s.score == gm.BestScore && s.score > 0 ? " <color=#FFC21A>(récord)</color>" : "";
                rows[i].fontSize = baseFont;
                rows[i].text = $"<color=#B6A8D6>#{idx + 1}</color>  {date}   <b>{s.score} pts</b>{best}   {s.perfects}/{s.served} perfectos   {mark}";
                ShrinkToFit(rows[i]);
            }
            if (footer) footer.text = "Datos guardados en mix_save.json  ·  historial exportado a CSV";
        }

        /// <summary>Reduce la letra hasta que el texto quepa a lo ancho (nada se sale del tablero).</summary>
        public static void ShrinkToFit(Text t, int minSize = 22)
        {
            float max = t.rectTransform.rect.width - 10f;
            while (t.fontSize > minSize && t.preferredWidth > max) t.fontSize -= 2;
        }
    }
}
