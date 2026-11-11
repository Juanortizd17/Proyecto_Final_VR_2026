using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Vaso mezclador: guarda hasta 4 ingredientes, se agita y se sirve.
    /// El líquido se ve por capas (una por ingrediente) y se mezcla en un solo color al agitar.
    /// </summary>
    public class MixGlass : MixInteractable
    {
        public const int Capacity = 4;

        [SerializeField] Renderer[] layers;
        [SerializeField] Transform iceVisual;      // cubos que flotan cuando la mezcla lleva hielo
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip pourClip;
        [SerializeField] AudioClip shakeClip;
        [SerializeField] AudioClip emptyClip;

        readonly List<IngredientData> contents = new List<IngredientData>();
        MaterialPropertyBlock mpb;
        Collider[] colliders;
        bool homeSet;

        public IReadOnlyList<IngredientData> Contents => contents;
        public int Count => contents.Count;
        public bool IsFull => contents.Count >= Capacity;
        public bool Shaken { get; private set; }
        public bool Busy { get; private set; }
        public bool AtHome { get; private set; } = true;
        public Vector3 HomePosition { get; private set; }
        public Quaternion HomeRotation { get; private set; }

        /// <summary>Se dispara al verter, agitar o vaciar.</summary>
        public event Action OnChanged;

        public void Setup(Renderer[] liquidLayers, AudioSource source, AudioClip pour, AudioClip shake, AudioClip empty)
        {
            layers = liquidLayers; sfx = source; pourClip = pour; shakeClip = shake; emptyClip = empty;
        }

        public void SetIceVisual(Transform ice) => iceVisual = ice;

        void Awake()
        {
            EnsureHome();
            colliders = GetComponentsInChildren<Collider>();
            Refresh();
        }

        void EnsureHome()
        {
            if (homeSet) return;
            HomePosition = transform.position; HomeRotation = transform.rotation; homeSet = true;
        }

        // ---------- Contenido ----------

        public bool Add(IngredientData ingredient)
        {
            if (ingredient == null || IsFull) return false;
            contents.Add(ingredient);
            Shaken = false;
            Refresh();
            Play(pourClip);
            OnChanged?.Invoke();
            return true;
        }

        public void Empty()
        {
            if (contents.Count == 0) return;
            contents.Clear();
            Shaken = false;
            Refresh();
            Play(emptyClip);
            OnChanged?.Invoke();
        }

        public bool CanShake => Count >= 2 && !Shaken && !Busy && AtHome;

        public void Shake()
        {
            if (CanShake) StartCoroutine(ShakeRoutine());
        }

        IEnumerator ShakeRoutine()
        {
            Busy = true;
            Play(shakeClip);
            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float s = Mathf.Sin(t * 40f);
                transform.position = p0 + Vector3.up * (0.05f + 0.03f * Mathf.Abs(s)) + Vector3.right * (0.02f * s);
                transform.rotation = r0 * Quaternion.Euler(0f, 0f, 18f * s);
                yield return null;
            }
            transform.SetPositionAndRotation(p0, r0);
            Shaken = true;
            Refresh();
            Busy = false;
            OnChanged?.Invoke();
        }

        public Color BlendColor()
        {
            if (contents.Count == 0) return Color.clear;
            Color c = Color.black;
            foreach (var i in contents) c += i.color;
            c /= contents.Count; c.a = 1f;
            return c;
        }

        public string Describe()
        {
            if (contents.Count == 0) return "vacío";
            return string.Join(" + ", contents.Select(i => i.displayName)) + (Shaken ? " (agitado)" : "");
        }

        void Refresh()
        {
            if (layers == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color blend = BlendColor();
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                bool on = i < contents.Count;
                layers[i].gameObject.SetActive(on);
                if (!on) continue;
                layers[i].GetPropertyBlock(mpb);
                mpb.SetColor("_Color", Shaken ? blend : contents[i].color);
                layers[i].SetPropertyBlock(mpb);
            }
            if (iceVisual != null)
            {
                bool hasIce = contents.Any(c => c.props.cold > 0);
                iceVisual.gameObject.SetActive(hasIce);
                iceVisual.localPosition = new Vector3(0f, 0.012f + contents.Count * 0.03f, 0f);
            }
        }

        // ---------- Movimiento ----------

        /// <summary>En la mano o viajando: sin colisiones (no tapa la mira ni empuja nada).</summary>
        public void SetHeld(bool held)
        {
            EnsureHome();
            if (held) AtHome = false;
            if (colliders == null) colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders) if (c) c.enabled = !held;
        }

        public IEnumerator FlyTo(Vector3 pos, Quaternion rot, float seconds)
        {
            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                float k = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(p0, pos, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.08f);
                transform.rotation = Quaternion.Slerp(r0, rot, k);
                yield return null;
            }
            transform.SetPositionAndRotation(pos, rot);
        }

        public IEnumerator ReturnHome(float seconds = 0.3f)
        {
            EnsureHome();
            yield return FlyTo(HomePosition, HomeRotation, seconds);
            SetHeld(false);
            AtHome = true;
        }

        void Play(AudioClip clip) { if (sfx && clip) sfx.PlayOneShot(clip, 0.8f); }

        // ---------- Interacción ----------

        public override string Prompt(MixInteractor hand)
        {
            if (hand.HoldingGlass || Busy || !AtHome) return null;
            return Count == 0 ? "Vaso vacío: vierte ingredientes del estante" : "Agarrar el vaso";
        }

        public override bool CanInteract(MixInteractor hand) => Count > 0 && !Busy && AtHome;

        public override void Interact(MixInteractor hand) => hand.TakeGlass();
    }
}
