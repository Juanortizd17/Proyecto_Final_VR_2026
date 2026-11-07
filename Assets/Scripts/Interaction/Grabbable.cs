using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QualityControlXR
{
    /// <summary>
    /// Objeto que se puede agarrar, independiente de la plataforma.
    /// En PC lo sostiene el DesktopInteractor; en VR (más adelante) lo sostiene un
    /// XRGrabInteractable. El resto del juego solo pregunta <see cref="IsHeld"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class Grabbable : MonoBehaviour
    {
        [SerializeField] string displayName;

        XRGrabInteractable xr;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public bool HeldByDesktop { get; private set; }
        public bool IsHeld => HeldByDesktop || (xr != null && xr.isSelected);

        public event Action<Grabbable> OnGrabbed;
        public event Action<Grabbable> OnReleased;

        public void SetName(string n) => displayName = n;

        void Awake()
        {
            xr = GetComponent<XRGrabInteractable>();
            if (xr != null)
            {
                xr.selectEntered.AddListener(_ => OnGrabbed?.Invoke(this));
                xr.selectExited.AddListener(_ => OnReleased?.Invoke(this));
            }
        }

        public void SetDesktopHeld(bool held)
        {
            if (HeldByDesktop == held) return;
            HeldByDesktop = held;
            if (held) OnGrabbed?.Invoke(this); else OnReleased?.Invoke(this);
        }

        public static bool IsHeldObject(Component c)
        {
            var g = c ? c.GetComponentInParent<Grabbable>() : null;
            return g != null && g.IsHeld;
        }
    }
}
