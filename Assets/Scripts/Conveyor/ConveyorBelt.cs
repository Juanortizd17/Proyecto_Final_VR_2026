using System.Collections;
using UnityEngine;

namespace QualityControlXR
{
    /// <summary>
    /// Banda transportadora: genera productos (5 tipos, ~35% defectuosos) y los
    /// mueve. Incluye eventos inesperados: aceleración de la banda y avería breve.
    /// Los productos se mueven por transformación, así el jugador puede agarrarlos
    /// (al agarrarlos el Rigidbody deja de estar kinematic-controlado por la banda).
    /// </summary>
    public class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform endPoint;
        [SerializeField] Product[] productPrefabs;          // 5 prefabs, uno por ProductType
        [SerializeField] float speed = 0.4f;
        [SerializeField] Vector2 spawnInterval = new Vector2(4f, 7f);
        [SerializeField, Range(0f, 1f)] float defectChance = 0.35f;

        [Header("Eventos inesperados")]
        [SerializeField] float eventEverySeconds = 45f;
        [SerializeField] float surgeMultiplier = 2f;
        [SerializeField] float eventDuration = 8f;

        float currentSpeed;

        void Start()
        {
            currentSpeed = speed;
            GameManager.Instance.OnStateChanged += OnState;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnState;
        }

        void OnState(GameState s)
        {
            StopAllCoroutines();
            currentSpeed = speed;
            if (s == GameState.Playing) { StartCoroutine(SpawnLoop()); StartCoroutine(EventLoop()); }
        }

        IEnumerator SpawnLoop()
        {
            while (GameManager.Instance.State == GameState.Playing)
            {
                Spawn();
                yield return new WaitForSeconds(Random.Range(spawnInterval.x, spawnInterval.y));
            }
        }

        IEnumerator EventLoop()
        {
            while (GameManager.Instance.State == GameState.Playing)
            {
                yield return new WaitForSeconds(eventEverySeconds);
                bool surge = Random.value > 0.5f;
                currentSpeed = surge ? speed * surgeMultiplier : 0f; // aceleración o avería
                GameManager.Instance.AddTime(surge ? 0f : 5f);       // compensa la avería
                yield return new WaitForSeconds(eventDuration);
                currentSpeed = speed;
            }
        }

        void Spawn()
        {
            if (productPrefabs == null || productPrefabs.Length == 0) return;
            var prefab = productPrefabs[Random.Range(0, productPrefabs.Length)];
            var p = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);
            var defect = Random.value < defectChance
                ? (DefectType)Random.Range(1, 4) : DefectType.None;
            p.Configure(p.Type, defect);
            p.gameObject.AddComponent<BeltRider>().Init(this);
        }

        public Vector3 Direction => (endPoint.position - spawnPoint.position).normalized;
        public float Speed => currentSpeed;
        public Transform End => endPoint;
    }

    /// <summary>Mueve el producto por la banda hasta que el jugador lo agarra.</summary>
    public class BeltRider : MonoBehaviour
    {
        ConveyorBelt belt;
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

        public void Init(ConveyorBelt b)
        {
            belt = b;
            grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        void Update()
        {
            if (belt == null) return;
            if (grab != null && grab.isSelected) { enabled = false; return; }
            transform.position += belt.Direction * belt.Speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, belt.End.position) < 0.1f)
            {
                // Producto perdido sin revisar = fallo
                GameManager.Instance.RegisterMistake($"Producto {GetComponent<Product>().Type} se escapó de la banda");
                Destroy(gameObject);
            }
        }
    }
}
