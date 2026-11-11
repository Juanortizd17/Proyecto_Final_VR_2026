using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Libro de reacciones en la pared: empieza vacío y cada reacción aparece cuando el jugador
    /// la descubre (se guardan en JSON entre partidas). Letra grande para leerse en VR.
    /// </summary>
    public class ReactionBookBoard : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text[] rows;
        [SerializeField] Text footer;

        MixGameManager gm;

        public void Setup(Text header, Text[] rowTexts, Text footerText) { title = header; rows = rowTexts; footer = footerText; }

        void Start()
        {
            gm = MixGameManager.Instance;
            gm.OnDiscoveredChanged += Refresh;
            Refresh();
        }

        void Refresh()
        {
            var list = gm.Reactions;
            int found = gm.DiscoveredCount, row = 0;
            title.text = $"<b>LIBRO DE REACCIONES</b>   {found}/{list.Count}";

            // Con pocas reacciones la letra es más grande; se va ajustando a medida que el libro se llena
            float step = Mathf.Clamp(1040f / Mathf.Max(1, found), 61f, 82f);
            int font = Mathf.FloorToInt(step * 0.8f), hint = Mathf.FloorToInt(step * 0.5f);
            for (int i = 0; i < rows.Length; i++)
            {
                var rt = rows[i].rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 1140f - (i + 1) * step);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, step);
                rows[i].fontSize = font;
            }
            foreach (var r in list)
            {
                if (row >= rows.Length || !gm.IsDiscovered(r.id)) continue;
                string hex = ColorUtility.ToHtmlStringRGB(r.bookColor);
                rows[row++].text = $"<color=#{hex}>●</color> <b>{r.displayName}</b>  <size={hint}><color=#B6A8D6>{r.hint}</color></size>";
            }
            for (; row < rows.Length; row++) rows[row].text = "";
            if (footer)
                footer.text = found == 0 ? "Todavía no has descubierto ninguna.\nPrueba mezclas raras a ver qué pasa…"
                    : found >= list.Count ? "<color=#7FE08A>¡Libro completo!</color>"
                    : $"Faltan {list.Count - found} por descubrir";
        }
    }
}
