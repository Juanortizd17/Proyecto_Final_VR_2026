using UnityEngine;

namespace QualityControlXR
{
    public enum ProductType { Radio, Lamp, Toy, Clock, Phone }

    /// <summary>
    /// Producto de la banda. Cada defecto solo se revela con la herramienta adecuada:
    /// lupa -> grietas/rayones, destornillador -> tornillos flojos, martillo -> golpe hueco.
    /// </summary>
    public enum DefectType { None, Crack, LooseScrew, Hollow }

    public class Product : MonoBehaviour
    {
        [SerializeField] ProductType type;
        [SerializeField] DefectType defect = DefectType.None;

        public ProductType Type => type;
        public DefectType Defect => defect;
        public bool IsDefective => defect != DefectType.None;
        public bool Inspected { get; private set; }
        public bool DefectRevealed { get; private set; }

        public void Configure(ProductType t, DefectType d) { type = t; defect = d; }

        /// <summary>Llamado por las herramientas cuando inspeccionan el producto.</summary>
        public bool Inspect(DefectType detectableBy)
        {
            Inspected = true;
            if (defect != DefectType.None && defect == detectableBy) DefectRevealed = true;
            return DefectRevealed;
        }
    }
}
