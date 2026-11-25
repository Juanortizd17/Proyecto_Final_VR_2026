using UnityEngine;
using System.Collections;

namespace MixLab
{
    /// <summary>Palanca que agita la mezcla del vaso.</summary>
    public class ShakeLever : MixInteractable
    {
        [SerializeField] MixGlass glass;
        [SerializeField] Transform handle;

        public void Setup(MixGlass g, Transform h) { glass = g; handle = h; }

        public override string Prompt(MixInteractor hand)
        {
            if (hand.HoldingGlass || !glass.AtHome) return "Deja el vaso en la estación para agitar";
            if (glass.Busy) return null;
            if (glass.Count < 2) return "Agitar: necesitas al menos 2 ingredientes";
            if (glass.Shaken) return "La mezcla ya está agitada";
            return "Agitar la mezcla";
        }

        public override bool CanInteract(MixInteractor hand) => !hand.HoldingGlass && glass.CanShake;

        public override void Interact(MixInteractor hand) => Pull();

        public void Pull()
        {
            if (!glass.CanShake) return;
            glass.Shake();
            if (handle) StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            Quaternion r0 = handle.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
            {
                handle.localRotation = r0 * Quaternion.Euler(Mathf.Sin(t * Mathf.PI) * 70f, 0f, 0f);
                yield return null;
            }
            handle.localRotation = r0;
        }
    }
}
