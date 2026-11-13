using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Director del juego: pedidos del cliente, paciencia, tiempo del turno, puntaje,
    /// reacciones, eventos inesperados (hora pico y apagón) y guardado en JSON.
    /// Publica eventos para que los tableros y el tutorial reaccionen sin acoplarse.
    /// </summary>
    public class MixGameManager : MonoBehaviour
    {
        public static MixGameManager Instance { get; private set; }

        [Header("Datos (ScriptableObjects)")]
        [SerializeField] RecipeData[] recipes;
        [SerializeField] ReactionData[] reactions;

        [Header("Reglas")]
        [SerializeField] float shiftDuration = 240f;
        [SerializeField] float orderPatience = 50f;
        [SerializeField] int maxKnockouts = 3;
        [SerializeField] int perfectPoints = 100;
        [SerializeField] int patiencePenalty = 30;
        [SerializeField] float firstEventAt = 40f;
        [SerializeField] float eventEvery = 45f;

        [Header("Escena")]
        [SerializeField] MixGlass glass;
        [SerializeField] Customer customer;
        [SerializeField] TemperatureLever lever;
        [SerializeField] ParticleSystem explosionFx;
        [SerializeField] Light[] roomLights;

        [Header("Audio")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip drinkClip, perfectClip, badClip, explosionClip, knockoutClip, alertClip, newReactionClip;

        public MixState State { get; private set; } = MixState.Onboarding;
        public float TimeLeft { get; private set; }
        public float PatienceLeft { get; private set; }
        public float PatienceRatio => orderPatience <= 0f ? 0f : Mathf.Clamp01(PatienceLeft / orderPatience);
        public int Score { get; private set; }
        public int Knockouts { get; private set; }
        public int MaxKnockouts => maxKnockouts;
        public int Served { get; private set; }
        public int Perfects { get; private set; }
        public int BestScore => save.bestScore;
        public bool NewRecord { get; private set; }
        public bool Won { get; private set; }
        public bool Busy { get; private set; }
        const int stabilizeBonus = 20, explodePenalty = 30;
        public bool Blackout { get; private set; }
        public RecipeData CurrentOrder { get; private set; }
        public string CustomerName => customer ? customer.Name : "el cliente";
        public IReadOnlyList<ReactionData> Reactions => reactions;
        public IReadOnlyList<RecipeData> Recipes => recipes;
        public int DiscoveredCount => save.discovered.Count;
        /// <summary>Historial de turnos guardado en JSON (el más reciente al final).</summary>
        public IReadOnlyList<MixSession> Sessions => save.sessions;
        public ReactionData LastReaction { get; private set; }

        public event Action<MixState> OnStateChanged;
        public event Action OnOrderChanged;
        public event Action OnScoreChanged;
        public event Action<string> OnAlert;
        /// <summary>Reacción mostrada y si es la primera vez que se descubre.</summary>
        public event Action<ReactionData, bool> OnReaction;
        public event Action OnDiscoveredChanged;

        MixSave save;
        float patienceRate = 1f;
        float[] lightIntensities;
        Color ambientSky, ambientEquator, ambientGround;
        Bottle[] bottles;
        int lastOrderIndex = -1;

        public void Setup(RecipeData[] recipeList, ReactionData[] reactionList, MixGlass g, Customer c, TemperatureLever l,
                          ParticleSystem explosion, Light[] lights, AudioSource source,
                          AudioClip drink, AudioClip perfect, AudioClip bad, AudioClip boom, AudioClip ko, AudioClip alert, AudioClip found)
        {
            recipes = recipeList; reactions = reactionList; glass = g; customer = c; lever = l; explosionFx = explosion;
            roomLights = lights; sfx = source; drinkClip = drink; perfectClip = perfect; badClip = bad;
            explosionClip = boom; knockoutClip = ko; alertClip = alert; newReactionClip = found;
        }

        void Awake()
        {
            Instance = this;
            save = MixSaveRepository.Load();
            TimeLeft = shiftDuration;
            PatienceLeft = orderPatience;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            bottles = FindObjectsByType<Bottle>(FindObjectsSortMode.None);
            lightIntensities = roomLights != null ? roomLights.Select(l => l ? l.intensity : 0f).ToArray() : new float[0];
            ambientSky = RenderSettings.ambientSkyColor; ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
            if (customer) customer.Say("¡Hola! Soy " + CustomerName + ". Prepárame algo para practicar.");
        }

        void Update()
        {
            if (State != MixState.Playing) return;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) { TimeLeft = 0f; if (!Busy) Finish(true); return; }

            customer.Impatient = !Busy && PatienceRatio < 0.3f;
            if (!Busy)
            {
                PatienceLeft -= Time.deltaTime * patienceRate;
                if (PatienceLeft <= 0f) OrderTimedOut();
            }
        }

        // =====================================================================
        // Turno
        // =====================================================================

        public bool IsDiscovered(ReactionId id) => save.discovered.Contains(id.ToString());
        public ReactionData Get(ReactionId id) => reactions.FirstOrDefault(r => r != null && r.id == id);

        public void OverrideShiftDuration(float seconds)
        {
            shiftDuration = Mathf.Max(15f, seconds);
            firstEventAt = Mathf.Min(firstEventAt, shiftDuration * 0.35f);
            if (State != MixState.Playing) TimeLeft = shiftDuration;
        }

        public void StartShift()
        {
            if (State == MixState.Playing || Busy) return;
            Score = 0; Knockouts = 0; Served = 0; Perfects = 0; NewRecord = false; Won = false;
            TimeLeft = shiftDuration;
            glass.Empty();
            lever.ResetLever();
            SetState(MixState.Playing);
            OnScoreChanged?.Invoke();
            NextOrder();
            StartCoroutine(EventLoop());
        }

        void NextOrder()
        {
            if (recipes == null || recipes.Length == 0) return;
            int i;
            do { i = UnityEngine.Random.Range(0, recipes.Length); } while (recipes.Length > 1 && i == lastOrderIndex);
            lastOrderIndex = i;
            CurrentOrder = recipes[i];
            PatienceLeft = orderPatience;
            customer.Say("Quiero " + CurrentOrder.requestLine + ", por favor.");
            OnOrderChanged?.Invoke();
        }

        /// <summary>Fija el pedido actual (pruebas automáticas y modo demostración).</summary>
        public void ForceOrder(RecipeData recipe)
        {
            CurrentOrder = recipe;
            PatienceLeft = orderPatience;
            OnOrderChanged?.Invoke();
        }

        void OrderTimedOut()
        {
            Score = Mathf.Max(0, Score - patiencePenalty);
            OnScoreChanged?.Invoke();
            Alert(CustomerName + " se cansó de esperar (-" + patiencePenalty + ")");
            NextOrder();
        }

        public void Finish(bool survived)
        {
            if (State != MixState.Playing) return;
            StopAllCoroutines();
            Busy = false;
            SetBlackout(false);
            patienceRate = 1f;
            Won = survived && Knockouts < maxKnockouts;
            NewRecord = Score > save.bestScore;
            if (NewRecord) save.bestScore = Score;
            save.sessions.Add(new MixSession
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                score = Score, served = Served, perfects = Perfects, knockouts = Knockouts, completed = Won
            });
            MixSaveRepository.Save(save);
            customer.Impatient = false;
            customer.ReleaseGlass();
            customer.ResetPose();
            customer.Say(Won ? "¡Buen turno! Nos vemos mañana." : "Creo que… necesito un médico.");
            SetState(MixState.Finished);
        }

        void SetState(MixState s) { State = s; OnStateChanged?.Invoke(s); }

        // =====================================================================
        // Servir
        // =====================================================================

        /// <summary>Sirve al cliente lo que hay en el vaso. Funciona también en el entrenamiento (sin puntaje).</summary>
        public bool Serve()
        {
            if (Busy || State == MixState.Finished || glass.Count == 0) return false;
            StartCoroutine(ServeRoutine());
            return true;
        }

        IEnumerator ServeRoutine()
        {
            Busy = true;
            glass.SetHeld(true);
            var id = ReactionRules.Evaluate(glass.Contents, lever.Current, CurrentOrder);
            bool stabilized = ReactionRules.IsStabilized(glass.Contents, lever.Current);
            var data = Get(id);
            LastReaction = data;

            yield return glass.FlyTo(customer.CoasterPoint.position, Quaternion.identity, 0.45f);

            if (id == ReactionId.Explosion)
            {
                if (explosionFx) { explosionFx.transform.position = customer.CoasterPoint.position + Vector3.up * 0.1f; explosionFx.Play(); }
                Play(explosionClip, 1f);
                glass.Burst();              // el vaso estalla frente al cliente
                glass.Empty();
            }
            else
            {
                Vector3 mouth = customer.MouthPoint.position;
                Quaternion tilt = Quaternion.AngleAxis(-75f, customer.transform.right) * Quaternion.identity;
                customer.HoldGlass(glass.transform);            // lo toma con la mano
                yield return glass.FlyTo(mouth, tilt, 0.4f);
                Play(drinkClip, 0.9f);
                yield return new WaitForSeconds(0.5f);
                glass.Empty();
                yield return glass.FlyTo(customer.CoasterPoint.position, Quaternion.identity, 0.35f);
                customer.ReleaseGlass();
            }

            customer.ShowReaction(data);
            bool isNew = !IsDiscovered(id);
            if (isNew)
            {
                save.discovered.Add(id.ToString());
                MixSaveRepository.Save(save);
                OnDiscoveredChanged?.Invoke();
                Play(newReactionClip, 0.8f);
            }

            if (State == MixState.Playing)
            {
                Served++;
                int gained = data.points;
                if (id == ReactionId.Perfecto)
                {
                    Perfects++;
                    gained = perfectPoints + Mathf.RoundToInt(PatienceRatio * 50f);
                }
                if (data.knockout) Knockouts++;
                if (stabilized) { gained += stabilizeBonus; Alert("¡Mezcla estabilizada con frío! +" + stabilizeBonus); }
                Score = Mathf.Max(0, Score + gained);
                OnScoreChanged?.Invoke();
            }
            Play(id == ReactionId.Perfecto ? perfectClip : data.knockout ? knockoutClip : badClip, 0.8f);
            OnReaction?.Invoke(data, isNew);

            yield return new WaitForSeconds(data.duration);

            customer.ResetPose();
            yield return glass.ReturnHome(0.4f);
            lever.ResetLever();
            Busy = false;

            if (State == MixState.Playing)
            {
                if (Knockouts >= maxKnockouts) Finish(false);
                else if (TimeLeft <= 0f) Finish(true);
                else NextOrder();
            }
            else if (State == MixState.Onboarding)
            {
                customer.Say("Ya me siento mejor. ¡Sigue practicando o inicia el turno!");
            }
        }

        /// <summary>Aviso en el tablero principal (lo usa el analizador de mezclas).</summary>
        public void Announce(string message) { if (State == MixState.Playing) Alert(message); }

        /// <summary>Una mezcla inestable estalló en la estación porque nadie la estabilizó a tiempo.</summary>
        public void GlassExploded()
        {
            Play(explosionClip, 1f);
            if (State == MixState.Playing)
            {
                Score = Mathf.Max(0, Score - explodePenalty);
                OnScoreChanged?.Invoke();
                Alert("¡EL VASO EXPLOTÓ! (-" + explodePenalty + ")  Lo inestable se estabiliza con FRÍO");
            }
            if (customer && !Busy) customer.Say("¡Cuidado! ¡Casi me sacas un ojo!");
        }

        // =====================================================================
        // Eventos inesperados
        // =====================================================================

        IEnumerator EventLoop()
        {
            yield return new WaitForSeconds(firstEventAt);
            while (State == MixState.Playing)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    Alert("¡HORA PICO! " + CustomerName + " tiene el doble de afán");
                    patienceRate = 2.2f;
                    yield return new WaitForSeconds(12f);
                    patienceRate = 1f;
                }
                else
                {
                    Alert("¡APAGÓN! Reconoce los ingredientes sin etiquetas");
                    SetBlackout(true);
                    yield return new WaitForSeconds(9f);
                    SetBlackout(false);
                }
                if (State == MixState.Playing) Alert("Todo volvió a la normalidad");
                yield return new WaitForSeconds(eventEvery);
            }
        }

        void SetBlackout(bool on)
        {
            if (Blackout == on) return;
            Blackout = on;
            if (roomLights != null)
                for (int i = 0; i < roomLights.Length; i++)
                    if (roomLights[i]) roomLights[i].intensity = lightIntensities[i] * (on ? 0.12f : 1f);
            float k = on ? 0.25f : 1f;
            RenderSettings.ambientSkyColor = ambientSky * k;
            RenderSettings.ambientEquatorColor = ambientEquator * k;
            RenderSettings.ambientGroundColor = ambientGround * k;
            if (bottles != null) foreach (var b in bottles) if (b) b.SetLabelVisible(!on);
        }

        void Alert(string message)
        {
            Play(alertClip, 0.7f);
            OnAlert?.Invoke(message);
        }

        void Play(AudioClip clip, float volume) { if (sfx && clip) sfx.PlayOneShot(clip, volume); }
    }
}
