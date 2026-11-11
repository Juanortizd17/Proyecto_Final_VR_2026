using UnityEngine;

namespace MixLab
{
    /// <summary>Cliente o posavasos: destino para servir la mezcla.</summary>
    public class ServeSpot : MixInteractable
    {
        public override string Prompt(MixInteractor hand)
        {
            var gm = MixGameManager.Instance;
            if (!hand.HoldingGlass || gm == null) return null;
            return gm.Busy ? null : $"Servir a {gm.CustomerName}";
        }

        public override bool CanInteract(MixInteractor hand) =>
            hand.HoldingGlass && MixGameManager.Instance != null && !MixGameManager.Instance.Busy;

        public override void Interact(MixInteractor hand)
        {
            hand.ReleaseGlass();
            MixGameManager.Instance.Serve();
        }
    }
}
