using System.Collections.Generic;
using System.Linq;

namespace MixLab
{
    /// <summary>
    /// "Química" del juego: decide la reacción a partir de las propiedades sumadas de la mezcla,
    /// la temperatura de la estación y el pedido actual. Lógica pura, fácil de probar.
    /// Las reglas se evalúan en orden de prioridad: las peligrosas primero.
    /// </summary>
    public static class ReactionRules
    {
        public static MixProps Sum(IEnumerable<IngredientData> mix)
        {
            var total = new MixProps();
            foreach (var i in mix) if (i != null) total += i.props;
            return total;
        }

        public static bool MatchesRecipe(IReadOnlyList<IngredientData> mix, Temperature temp, RecipeData recipe)
        {
            if (recipe == null || recipe.ingredients == null || mix.Count != recipe.ingredients.Length) return false;
            if (recipe.needsTemperature && recipe.temperature != temp) return false;
            var a = mix.Select(i => i.name).OrderBy(n => n);
            var b = recipe.ingredients.Select(i => i.name).OrderBy(n => n);
            return a.SequenceEqual(b);
        }

        public static ReactionId Evaluate(IReadOnlyList<IngredientData> mix, Temperature temp, RecipeData order)
        {
            var p = Sum(mix);
            bool cold = p.cold > 0 || temp == Temperature.Frio;
            bool hot = temp == Temperature.Caliente;

            if (p.gas > 0 && p.fizz > 0) return ReactionId.Explosion;       // gas + efervescente
            if (p.spicy >= 3 && p.alcohol > 0) return ReactionId.Desmayo;   // combinación fulminante
            if (hot && p.alcohol >= 2) return ReactionId.Llamarada;         // alcohol fuerte al fuego
            if (p.alcohol >= 3) return ReactionId.Borracho;
            if (p.dairy > 0 && p.acid > 0) return ReactionId.Nausea;        // leche cortada
            if (MatchesRecipe(mix, temp, order)) return ReactionId.Perfecto;
            if (p.spicy >= 3) return ReactionId.Infierno;
            if (p.spicy > 0) return ReactionId.Picante;
            if (p.caffeine > 0 && p.alcohol > 0) return ReactionId.Bailarin;   // cafeína + alcohol: fiebre de baile
            if (p.caffeine >= 2) return ReactionId.Hiper;
            if (p.sweet >= 3) return ReactionId.Azucar;                    // subidón de azúcar
            if (cold && p.mint > 0) return ReactionId.Congelado;
            if (p.acid >= 2) return ReactionId.Agrio;                      // doble ácido
            if (p.gas >= 2) return ReactionId.Hipo;                        // doble gas
            if (p.caffeine == 0 && (p.dairy >= 2 || (p.dairy > 0 && hot))) return ReactionId.Dormido;   // leche tibia: a dormir
            if (p.alcohol > 0) return ReactionId.Alegre;
            return ReactionId.SinChiste;
        }
    }
}
