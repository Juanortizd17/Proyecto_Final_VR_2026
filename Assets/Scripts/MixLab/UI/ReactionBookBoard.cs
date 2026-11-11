using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Libro de reacciones en la pared: muestra las descubiertas (guardadas en JSON entre partidas)
    /// y deja las demás como incógnitas por encontrar.
    /// </summary>
    public class ReactionBookBoard : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text[] rows;

        MixGameManager gm;

        public void Setup(Text header, Text[] rowTexts) { title = header; rows = rowTexts; }

        void Start()
        {
            gm = MixGameManager.Instance;
            gm.OnDiscoveredChanged += Refresh;
            Refresh();
        }

        void Refresh()
        {
            var list = gm.Reactions;
            title.text = $"<b>LIBRO DE REACCIONES</b>   {gm.DiscoveredCount}/{list.Count}";
            for (int i = 0; i < rows.Length; i++)
            {
                if (i >= list.Count) { rows[i].text = ""; continue; }
                var r = list[i];
                if (gm.IsDiscovered(r.id))
                {
                    string hex = ColorUtility.ToHtmlStringRGB(r.bookColor);
                    rows[i].text = $"<color=#{hex}>●</color> <b>{r.displayName}</b>  <size=34><color=#B6A8D6>{r.hint}</color></size>";
                }
                else rows[i].text = "<color=#6E6490>?  Reacción sin descubrir</color>";
            }
        }
    }
}
