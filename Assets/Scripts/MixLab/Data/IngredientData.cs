using System;
using UnityEngine;

namespace MixLab
{
    /// <summary>Propiedades químicas que aporta un ingrediente (se suman al mezclar).</summary>
    [Serializable]
    public struct MixProps
    {
        public int water, cold, acid, spicy, alcohol, gas, fizz, dairy, mint, caffeine, fruit, sweet;

        public static MixProps operator +(MixProps a, MixProps b) => new MixProps
        {
            water = a.water + b.water, cold = a.cold + b.cold, acid = a.acid + b.acid,
            spicy = a.spicy + b.spicy, alcohol = a.alcohol + b.alcohol, gas = a.gas + b.gas,
            fizz = a.fizz + b.fizz, dairy = a.dairy + b.dairy, mint = a.mint + b.mint,
            caffeine = a.caffeine + b.caffeine, fruit = a.fruit + b.fruit, sweet = a.sweet + b.sweet
        };
    }

    /// <summary>Ingrediente del estante. Se edita como asset en Assets/Data/Mix/Ingredients.</summary>
    [CreateAssetMenu(menuName = "Mix Lab/Ingrediente", fileName = "Ingrediente")]
    public class IngredientData : ScriptableObject
    {
        public string displayName = "Ingrediente";
        public string tag = "neutral";
        public Color color = Color.white;
        public MixProps props;
    }
}
