using UnityEngine;

namespace QualityControlXR
{
    public enum BinKind { Reject, Accept }

    /// <summary>
    /// Zona de destino (trigger). Reject = desechar defectuosos y reportar.
    /// Accept = sitio de producto perfecto; solo acepta su acceptedType.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ProductBin : MonoBehaviour
    {
        [SerializeField] BinKind kind = BinKind.Accept;
        [SerializeField] ProductType acceptedType;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip okClip;
        [SerializeField] AudioClip failClip;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<Product>();
            if (p == null || GameManager.Instance == null) return;

            bool correct = kind == BinKind.Reject
                ? p.IsDefective
                : (!p.IsDefective && p.Type == acceptedType);

            string report = kind == BinKind.Reject
                ? $"Reporte: {p.Type} desechado ({p.Defect})"
                : $"{p.Type} colocado en {acceptedType}";

            if (correct) GameManager.Instance.RegisterCorrect(report);
            else GameManager.Instance.RegisterMistake($"Error: {p.Type} ({p.Defect}) en {kind}/{acceptedType}");

            if (sfx != null) sfx.PlayOneShot(correct ? okClip : failClip);
            Destroy(p.gameObject, 0.1f);
        }
    }
}
