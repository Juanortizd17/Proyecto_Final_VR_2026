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
            Restore();
            SetHeld(false);
            AtHome = true;
        }

        // ---------- Mezcla inestable: tiembla y, si nadie la estabiliza, el vaso estalla ----------

        [SerializeField] ParticleSystem burstFx;
        [SerializeField] Material shardMaterial;
        Renderer[] visuals;
        bool rattling, shattered;

        /// <summary>Lo activa el analizador mientras la mezcla esté a punto de explotar.</summary>
        public bool Unstable { get; set; }
        public bool Shattered => shattered;

        public void SetBurst(ParticleSystem fx, Material shards) { burstFx = fx; shardMaterial = shards; }

        void Update()
        {
            bool rattle = Unstable && AtHome && !Busy && !shattered;
            if (rattle)
            {
                float t = Time.time;
                transform.position = HomePosition + new Vector3(Mathf.Sin(t * 47f) * 0.004f, Mathf.Abs(Mathf.Sin(t * 31f)) * 0.006f, Mathf.Cos(t * 53f) * 0.004f);
                transform.rotation = HomeRotation * Quaternion.Euler(Mathf.Sin(t * 39f) * 4f, 0f, Mathf.Cos(t * 43f) * 4f);
            }
            else if (rattling && AtHome && !Busy) transform.SetPositionAndRotation(HomePosition, HomeRotation);
            rattling = rattle;
        }

        /// <summary>Rompe el vaso donde esté: salpica el líquido, vuelan los vidrios y el vaso desaparece hasta Restore().</summary>
        public void Burst()
        {
            if (shattered) return;
            shattered = true;
            Color liquid = contents.Count > 0 ? BlendColor() : new Color(0.8f, 0.95f, 1f);
            Vector3 center = transform.position + Vector3.up * 0.07f;
            if (burstFx)
            {
                burstFx.transform.position = center;
                var main = burstFx.main;
                main.startColor = new ParticleSystem.MinMaxGradient(liquid, Color.Lerp(liquid, Color.white, 0.5f));
                burstFx.Play();
            }
            if (Application.isPlaying)
                for (int i = 0; i < 12; i++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "GlassShard";
                    shard.layer = 2;      // no estorba a la mira
                    shard.transform.position = center + UnityEngine.Random.insideUnitSphere * 0.04f;
                    shard.transform.rotation = UnityEngine.Random.rotation;
                    shard.transform.localScale = new Vector3(UnityEngine.Random.Range(0.015f, 0.04f), 0.004f, UnityEngine.Random.Range(0.015f, 0.035f));
                    if (shardMaterial) shard.GetComponent<Renderer>().sharedMaterial = shardMaterial;
                    var rb = shard.AddComponent<Rigidbody>();
                    rb.mass = 0.02f;
                    Vector3 dir = UnityEngine.Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 0.8f + 0.3f;
                    rb.linearVelocity = dir * UnityEngine.Random.Range(1.4f, 3.2f);
                    rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 25f;
                    Destroy(shard, 2.5f);
                }
            if (visuals == null) visuals = GetComponentsInChildren<Renderer>(true);
            foreach (var r in visuals) if (r) r.enabled = false;
        }

        /// <summary>Un vaso nuevo en la estación.</summary>
        public void Restore()
        {
            if (!shattered) return;
            shattered = false;
            if (visuals != null) foreach (var r in visuals) if (r) r.enabled = true;
        }

        /// <summary>La mezcla inestable estalla en la estación (o en la mano) y luego aparece un vaso limpio.</summary>
        public IEnumerator ExplodeAtStation()
        {
            EnsureHome();
            Busy = true;
            Unstable = false;
            if (colliders == null) colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders) if (c) c.enabled = false;
            Burst();
            contents.Clear();
            Shaken = false;
            Refresh();
            OnChanged?.Invoke();
            yield return new WaitForSeconds(1.3f);
            transform.SetPositionAndRotation(HomePosition, HomeRotation);
            Restore();
            foreach (var c in colliders) if (c) c.enabled = true;
            AtHome = true;
            // El vaso nuevo "aparece" creciendo
            Vector3 full = transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                transform.localScale = full * Mathf.SmoothStep(0.2f, 1f, t);
                yield return null;
            }
            transform.localScale = full;
            Busy = false;
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
