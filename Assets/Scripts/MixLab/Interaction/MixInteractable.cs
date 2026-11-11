using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Algo con lo que el jugador puede interactuar apuntando con la mira (PC)
    /// o, más adelante, con la mano en VR.
    /// </summary>
    public abstract class MixInteractable : MonoBehaviour
    {
        /// <summary>Texto de la acción ("Verter Limón"). null o vacío = no mostrar nada.</summary>
        public abstract string Prompt(MixInteractor hand);

        /// <summary>false = el texto es solo informativo (no se puede hacer clic ahora).</summary>
        public virtual bool CanInteract(MixInteractor hand) => true;

        public abstract void Interact(MixInteractor hand);
    }
}
