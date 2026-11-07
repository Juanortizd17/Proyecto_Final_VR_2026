using UnityEngine;

namespace QualityControlXR
{
    /// <summary>
    /// Trigger al final de la banda y en el piso: un producto que llega aquí sin
    /// clasificar cuenta como fallo (sistema reactivo a errores del usuario).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LostProductZone : MonoBehaviour
    {
        [SerializeField] string reason = "se escapó de la banda";
        [SerializeField] AudioSource sfx;

        public void Setup(string why, AudioSource source) { reason = why; sfx = source; }

        void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<Product>();
            if (p == null || p.Processed) return;
            p.Processed = true;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                GameManager.Instance.RegisterMistake($"{ProductBin.Es(p.Type)} {reason}");
            if (sfx) sfx.Play();
            Destroy(p.gameObject, 0.5f);
        }
    }
}
