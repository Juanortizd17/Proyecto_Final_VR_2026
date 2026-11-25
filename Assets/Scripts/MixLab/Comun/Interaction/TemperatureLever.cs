using System;
using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Palanca de temperatura de la estación: Frío / Ambiente / Caliente.
    /// Algunas recetas la exigen y algunas reacciones dependen de ella.
    /// </summary>
    public class TemperatureLever : MixInteractable
    {
        [SerializeField] Transform handle;
        [SerializeField] Renderer indicator;
        [SerializeField] Text label;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip clickClip;

        static readonly Color ColdColor = new Color(0.35f, 0.75f, 1f);
        static readonly Color RoomColor = new Color(0.75f, 0.75f, 0.8f);
        static readonly Color HotColor = new Color(1f, 0.45f, 0.15f);

        MaterialPropertyBlock mpb;

        public Temperature Current { get; private set; } = Temperature.Ambiente;
        public event Action<Temperature> OnChanged;

        public void Setup(Transform h, Renderer ind, Text text, AudioSource source, AudioClip click)
        {
            handle = h; indicator = ind; label = text; sfx = source; clickClip = click;
        }

        void Start() => Apply();

        public static string Name(Temperature t) =>
            t == Temperature.Frio ? "Frío" : t == Temperature.Caliente ? "Caliente" : "Ambiente";

        public string CurrentName => Name(Current);

        public override string Prompt(MixInteractor hand)
        {
            var next = Next(Current);
            return $"Temperatura: {CurrentName} → {Name(next)}";
        }

        public override void Interact(MixInteractor hand) => Set(Next(Current));

        static Temperature Next(Temperature t) =>
            t == Temperature.Ambiente ? Temperature.Caliente : t == Temperature.Caliente ? Temperature.Frio : Temperature.Ambiente;

        public void Set(Temperature t)
        {
            if (Current == t) return;
            Current = t;
            Apply();
            if (sfx && clickClip) sfx.PlayOneShot(clickClip, 0.7f);
            OnChanged?.Invoke(t);
        }

        public void ResetLever() => Set(Temperature.Ambiente);

        void Apply()
        {
            float angle = Current == Temperature.Frio ? -38f : Current == Temperature.Caliente ? 38f : 0f;
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, -angle);
            Color c = Current == Temperature.Frio ? ColdColor : Current == Temperature.Caliente ? HotColor : RoomColor;
            if (indicator)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                indicator.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", c);
                mpb.SetColor("_EmissionColor", c * 1.4f);
                indicator.SetPropertyBlock(mpb);
            }
            if (label)
            {
                label.text = "TEMP\n<b>" + CurrentName.ToUpper() + "</b>";
                label.color = c;
            }
        }
    }
}
