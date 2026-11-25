using System.Collections.Generic;
using System.Linq;

namespace MixLab
{
    /// <summary>
    /// "Química" del juego: decide la reacción a partir de las propiedades sumadas de la mezcla,
    /// la temperatura de la estación y el pedido actual. Lógica pura, fácil de probar.
    /// Las reglas se evalúan en orden de prioridad: las peligrosas primero.
    ///
    /// Clasificar: el analizador marca cada mezcla como estable, inestable o tóxica.
    /// Estabilizar: el frío (hielo o palanca en FRÍO) neutraliza las mezclas inestables.
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

        // ---------- Mezclas inestables y cómo se estabilizan ----------

        /// <summary>Gas + efervescente: explota (en el vaso si se deja esperar, o en la cara del cliente).</summary>
        public static bool IsExplosive(MixProps p) => p.gas > 0 && p.fizz > 0;

        /// <summary>Alcohol fuerte calentado: se prende.</summary>
        public static bool IsFlammable(MixProps p, Temperature temp) => temp == Temperature.Caliente && p.alcohol >= 2;

        static bool Chilled(MixProps p, Temperature temp) => p.cold > 0 || temp == Temperature.Frio;

        /// <summary>La mezcla explotará si no se enfría.</summary>
        public static bool WillExplode(IReadOnlyList<IngredientData> mix, Temperature temp)
        {
            var p = Sum(mix);
            return IsExplosive(p) && !Chilled(p, temp);
        }

        /// <summary>Era una mezcla peligrosa y el frío la neutralizó (da un bono al servirla).</summary>
        public static bool IsStabilized(IReadOnlyList<IngredientData> mix, Temperature temp)
        {
            var p = Sum(mix);
            return (IsExplosive(p) && Chilled(p, temp)) || (IsFlammable(p, temp) && p.cold > 0);
        }

        public static MixClass Classify(IReadOnlyList<IngredientData> mix, Temperature temp)
        {
            if (mix == null || mix.Count == 0) return MixClass.Vacia;
            var p = Sum(mix);
            if (IsExplosive(p) && !Chilled(p, temp)) return MixClass.Inestable;
            if (IsFlammable(p, temp) && p.cold == 0) return MixClass.Inestable;
            if ((p.spicy >= 3 && p.alcohol > 0) || (p.dairy > 0 && p.acid > 0) || p.alcohol >= 3) return MixClass.Toxica;
            return MixClass.Estable;
        }

        // ---------- Reacción del cliente ----------

        public static ReactionId Evaluate(IReadOnlyList<IngredientData> mix, Temperature temp, RecipeData order)
        {
            var p = Sum(mix);
            bool cold = Chilled(p, temp);
            bool hot = temp == Temperature.Caliente;

            if (IsExplosive(p) && !cold) return ReactionId.Explosion;       // gas + efervescente sin enfriar
            if (p.spicy >= 3 && p.alcohol > 0) return ReactionId.Desmayo;   // combinación fulminante
            if (IsFlammable(p, temp) && p.cold == 0) return ReactionId.Llamarada;   // alcohol fuerte al fuego, sin hielo
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
