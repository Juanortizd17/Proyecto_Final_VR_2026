using System.Collections;
using UnityEngine;

namespace QualityControlXR
{
    /// <summary>
    /// Banda transportadora física: un Rigidbody cinemático que "arrastra" por fricción
    /// lo que está encima (truco clásico: se mueve hacia atrás y MovePosition hacia adelante).
    /// Así los productos siguen siendo objetos físicos que se pueden agarrar y soltar.
    /// También genera productos y dispara eventos inesperados (aceleración o avería).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] Vector3 localDirection = Vector3.right;
        [SerializeField] Product[] productPrefabs;          // 5 prefabs, uno por ProductType
        [SerializeField] float speed = 0.25f;
        [SerializeField] Vector2 spawnInterval = new Vector2(5f, 8f);
        [SerializeField, Range(0f, 1f)] float defectChance = 0.4f;

        [Header("Eventos inesperados")]
        [SerializeField] float firstEventAt = 40f;
        [SerializeField] float eventEverySeconds = 45f;
        [SerializeField] float surgeMultiplier = 2.2f;
        [SerializeField] float eventDuration = 8f;
        [SerializeField] AudioSource alarm;

        Rigidbody rb;
        float currentSpeed;

        public float CurrentSpeed => currentSpeed;
        public event System.Action<string> OnBeltEvent;

        public void Setup(Transform spawn, Product[] prefabs, AudioSource alarmSource)
        {
            spawnPoint = spawn; productPrefabs = prefabs; alarm = alarmSource;
        }

        /// <summary>Adelanta el primer evento inesperado (modo demostración).</summary>
        public void OverrideFirstEvent(float seconds) => firstEventAt = Mathf.Max(5f, seconds);

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            currentSpeed = 0f;
        }

        void Start() => GameManager.Instance.OnStateChanged += OnState;

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnState;
        }

        void FixedUpdate()
        {
            if (currentSpeed <= 0f) return;
            Vector3 dir = transform.TransformDirection(localDirection).normalized;
            Vector3 pos = rb.position;
            rb.position -= dir * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(pos);
        }

        void OnState(GameState s)
        {
            StopAllCoroutines();
            currentSpeed = s == GameState.Playing ? speed : 0f;
            if (s == GameState.Playing) { StartCoroutine(SpawnLoop()); StartCoroutine(EventLoop()); }
        }

        IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(1f);
            while (GameManager.Instance.State == GameState.Playing)
            {
                Spawn();
                yield return new WaitForSeconds(Random.Range(spawnInterval.x, spawnInterval.y));
            }
        }

        IEnumerator EventLoop()
        {
            yield return new WaitForSeconds(firstEventAt);
            while (GameManager.Instance.State == GameState.Playing)
            {
                bool surge = Random.value > 0.5f;
                if (alarm) alarm.Play();
                if (surge)
                {
                    currentSpeed = speed * surgeMultiplier;
                    OnBeltEvent?.Invoke("¡ALERTA! La banda se aceleró");
                }
                else
                {
                    currentSpeed = 0f;
                    GameManager.Instance.AddTime(eventDuration);
                    OnBeltEvent?.Invoke("¡AVERÍA! Banda detenida: aprovecha para inspeccionar");
                }
                yield return new WaitForSeconds(eventDuration);
                if (GameManager.Instance.State != GameState.Playing) yield break;
                currentSpeed = speed;
                OnBeltEvent?.Invoke("Banda funcionando con normalidad");
                yield return new WaitForSeconds(eventEverySeconds);
            }
        }

        void Spawn()
        {
            if (productPrefabs == null || productPrefabs.Length == 0) return;
            var prefab = productPrefabs[Random.Range(0, productPrefabs.Length)];
            var p = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            var defect = Random.value < defectChance ? (DefectType)Random.Range(1, 4) : DefectType.None;
            p.Configure(p.Type, defect);
        }
    }
}
