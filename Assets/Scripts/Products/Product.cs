using UnityEngine;

namespace QualityControlXR
{
    public enum ProductType { Radio, Lamp, Speaker, Television, Bear }

    /// <summary>
    /// Cada defecto solo se revela con la herramienta adecuada:
    /// lupa -> grieta (Crack), destornillador -> tornillo flojo (LooseScrew), martillo -> hueco (Hollow).
    /// </summary>
    public enum DefectType { None, Crack, LooseScrew, Hollow }

    /// <summary>
    /// Producto de la banda. Lleva la cuenta de qué herramientas lo revisaron y
    /// muestra un indicador: rojo = defecto encontrado, verde = revisado con las 3 herramientas y OK.
    /// </summary>
    public class Product : MonoBehaviour
    {
        [SerializeField] ProductType type;
        [SerializeField] DefectType defect = DefectType.None;
        [SerializeField] GameObject defectMarker;   // se activa al revelar el defecto
        [SerializeField] GameObject okMarker;       // se activa tras revisar con las 3 herramientas sin defecto

        int inspectedMask;
        const int AllTools = (1 << 1) | (1 << 2) | (1 << 3);

        public ProductType Type => type;
        public DefectType Defect => defect;
        public bool IsDefective => defect != DefectType.None;
        public bool DefectRevealed { get; private set; }
        public bool FullyInspected => inspectedMask == AllTools;
        public bool Processed { get; set; }   // ya fue entregado a un contenedor

        public void Configure(ProductType t, DefectType d)
        {
            type = t; defect = d;
            inspectedMask = 0; DefectRevealed = false;
            if (defectMarker) defectMarker.SetActive(false);
            if (okMarker) okMarker.SetActive(false);
        }

        public void SetMarkers(GameObject bad, GameObject ok) { defectMarker = bad; okMarker = ok; }

        /// <summary>Llamado por InspectionTool. Devuelve true si esta herramienta reveló el defecto.</summary>
        public bool Inspect(DefectType detectableBy)
        {
            inspectedMask |= 1 << (int)detectableBy;
            bool foundNow = !DefectRevealed && defect != DefectType.None && defect == detectableBy;
            if (foundNow)
            {
                DefectRevealed = true;
                if (defectMarker) defectMarker.SetActive(true);
                if (okMarker) okMarker.SetActive(false);
            }
            else if (!IsDefective && FullyInspected && okMarker && !okMarker.activeSelf)
            {
                okMarker.SetActive(true);
            }
            return foundNow;
        }
    }
}
