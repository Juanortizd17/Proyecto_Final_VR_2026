using UnityEngine;

namespace MixLab
{
    /// <summary>Lavaplatos: vacía el vaso (para corregir una mezcla antes de servirla).</summary>
    public class SinkSpot : MixInteractable
    {
        [SerializeField] MixGlass glass;

        public void Setup(MixGlass g) => glass = g;

        public override string Prompt(MixInteractor hand) =>
            glass.Count == 0 || glass.Busy ? null : "Vaciar el vaso en el lavaplatos";

        public override void Interact(MixInteractor hand)
        {
            if (hand.HoldingGlass) hand.ReturnGlass();
            glass.Empty();
        }
    }
}
