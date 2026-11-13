using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MixLab
{
    /// <summary>
    /// VR: posavasos del cliente. Es un XR Socket Interactor que solo acepta el vaso mezclador,
    /// y solo cuando tiene algo y el cliente está libre. Al encajar el vaso, se sirve la bebida.
    /// </summary>
    public class CoasterSocket : XRSocketInteractor
    {
        [SerializeField] MixGlass glass;

        public void SetGlass(MixGlass g) => glass = g;

        bool Accepts(IXRInteractable interactable)
        {
            var gm = MixGameManager.Instance;
            if (glass == null || gm == null || gm.Busy || gm.State == MixState.Finished) return false;
            if (glass.Count == 0 || glass.Busy || glass.Shattered) return false;
            return interactable is Component c && c.gameObject == glass.gameObject;
        }

        public override bool CanHover(IXRHoverInteractable interactable) => base.CanHover(interactable) && Accepts(interactable);

        public override bool CanSelect(IXRSelectInteractable interactable) => base.CanSelect(interactable) && Accepts(interactable);

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            Serve();
        }

        /// <summary>Sirve lo que hay en el vaso (también lo usan las pruebas automáticas).</summary>
        public bool Serve()
        {
            var gm = MixGameManager.Instance;
            return gm != null && gm.Serve();
        }
    }
}
