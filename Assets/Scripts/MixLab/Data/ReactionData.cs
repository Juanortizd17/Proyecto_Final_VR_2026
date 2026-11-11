using UnityEngine;

namespace MixLab
{
    /// <summary>Reacción del cliente a una mezcla: texto, puntaje y aspecto.</summary>
    [CreateAssetMenu(menuName = "Mix Lab/Reacción", fileName = "Reaccion")]
    public class ReactionData : ScriptableObject
    {
        public ReactionId id;
        public string displayName = "Reacción";
        [Tooltip("Pista que aparece en el libro al descubrirla")]
        public string hint = "";
        [TextArea] public string customerLine = "";
        public int points;
        [Tooltip("Deja al cliente fuera de combate (cuenta para perder el turno)")]
        public bool knockout;
        public Color skinColor = new Color(0.95f, 0.76f, 0.61f);
        public Color bookColor = Color.white;
        public float duration = 3.4f;
    }
}
