using UnityEngine;

namespace MixLab
{
    /// <summary>Base de la estación: sitio donde descansa el vaso.</summary>
    public class StationPad : MixInteractable
    {
        public override string Prompt(MixInteractor hand) => hand.HoldingGlass ? "Dejar el vaso en la estación" : null;
        public override void Interact(MixInteractor hand) => hand.ReturnGlass();
    }
}
