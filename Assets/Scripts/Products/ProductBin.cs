using UnityEngine;

namespace QualityControlXR
{
    public enum BinKind { Reject, Accept }

    /// <summary>
    /// Contenedor (trigger). Reject = desechar defectuosos y generar reporte.
    /// Accept = sitio del producto perfecto; solo acepta su acceptedType.
    /// Solo cuenta productos ya soltados (no los que pasan en la mano).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ProductBin : MonoBehaviour
    {
        [SerializeField] BinKind kind = BinKind.Accept;
        [SerializeField] ProductType acceptedType;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip okClip;
        [SerializeField] AudioClip failClip;

        public string DisplayLabel => kind == BinKind.Reject ? "RECHAZO" : Es(acceptedType).ToUpper();

        public void Setup(BinKind k, ProductType t, AudioSource source, AudioClip ok, AudioClip fail)
        {
            kind = k; acceptedType = t; sfx = source; okClip = ok; failClip = fail;
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            var p = other.GetComponentInParent<Product>();
            if (p == null || p.Processed || GameManager.Instance == null) return;
            if (Grabbable.IsHeldObject(p)) return;   // aún en la mano: no cuenta

            p.Processed = true;
            bool correct = kind == BinKind.Reject
                ? p.IsDefective
                : (!p.IsDefective && p.Type == acceptedType);

            string report = kind == BinKind.Reject
                ? $"Reporte: {Es(p.Type)} desechado - {Es(p.Defect)}"
                : $"{Es(p.Type)} aprobado y almacenado";

            if (correct) GameManager.Instance.RegisterCorrect(report);
            else GameManager.Instance.RegisterMistake(kind == BinKind.Reject
                ? $"Error: {Es(p.Type)} estaba en buen estado"
                : p.IsDefective ? $"Error: {Es(p.Type)} tenía {Es(p.Defect)}" : $"Error: {Es(p.Type)} en el sitio equivocado");

            if (sfx != null) sfx.PlayOneShot(correct ? okClip : failClip);
            Destroy(p.gameObject, 0.2f);
        }

        public static string Es(ProductType t) => t switch
        {
            ProductType.Radio => "Radio",
            ProductType.Lamp => "Lámpara",
            ProductType.Speaker => "Parlante",
            ProductType.Television => "Televisor",
            _ => "Oso de peluche"
        };

        public static string Es(DefectType d) => d switch
        {
            DefectType.Crack => "grieta",
            DefectType.LooseScrew => "tornillo flojo",
            DefectType.Hollow => "pieza hueca",
            _ => "sin defecto"
        };
    }
}
