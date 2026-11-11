using System.Linq;
using UnityEngine;

namespace MixLab
{
    /// <summary>Pedido que puede hacer el cliente: ingredientes exactos y, opcionalmente, temperatura.</summary>
    [CreateAssetMenu(menuName = "Mix Lab/Receta", fileName = "Receta")]
    public class RecipeData : ScriptableObject
    {
        public string displayName = "Receta";
        [Tooltip("Cómo lo pide el cliente, por ejemplo: una limonada helada")]
        public string requestLine = "algo rico";
        public IngredientData[] ingredients;
        public bool needsTemperature;
        public Temperature temperature = Temperature.Ambiente;

        public string IngredientList => string.Join(" · ", ingredients.Select(i => i.displayName));
        public string TemperatureLabel => needsTemperature ? (temperature == Temperature.Frio ? "FRÍO" : "CALIENTE") : "";
    }
}
