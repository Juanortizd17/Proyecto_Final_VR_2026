using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MixLab
{
    /// <summary>
    /// VR: ingrediente que se agarra con la mano (XR Grab Interactable) y se vierte
    /// inclinándolo sobre el vaso. Cada inclinación sirve una dosis; para otra hay que
    /// enderezar la botella y volver a inclinarla. Al soltarla vuelve sola al estante.
    /// </summary>
    [RequireComponent(typeof(Bottle))]
    public class VrPourable : MonoBehaviour
    {
        [SerializeField] XRGrabInteractable grab;
        [SerializeField] float pourAngle = 100f;        // inclinación mínima para que caiga (grados)
        [SerializeField] float pieceAngle = 75f;        // recipientes de piezas: basta con menos
        [SerializeField] float rearmAngle = 65f;        // por debajo de este ángulo se puede volver a verter
        [SerializeField] float pourSeconds = 0.35f;     // tiempo inclinada sobre el vaso para que entre la dosis
        [SerializeField] float aimRadius = 0.11f;       // qué tan centrada debe estar la boca sobre el vaso

        Bottle bottle;
        Transform stream;
        float pourTimer;
        bool armed = true;
        IXRSelectInteractor holder;

        /// <summary>Lo usan las pruebas automáticas para simular la mano sin visor.</summary>
        public bool SimulateHeld { get; set; }
        public bool Held => SimulateHeld || (grab != null && grab.isSelected && holder is not XRSocketInteractor);

        public void Setup(XRGrabInteractable g) => grab = g;

        void Awake()
        {
            bottle = GetComponent<Bottle>();
            if (grab == null) grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            if (grab == null) return;
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            if (grab == null) return;
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }

        void OnGrab(SelectEnterEventArgs a) { holder = a.interactorObject; armed = true; pourTimer = 0f; }

        void OnRelease(SelectExitEventArgs a)
        {
            holder = null;
            SetStream(false, Vector3.zero, Vector3.zero);
            StartCoroutine(bottle.ReturnHome());
        }

        void Update()
        {
            if (!Held) { SetStream(false, Vector3.zero, Vector3.zero); return; }
            var glass = bottle.Glass;
            Vector3 up = transform.up;
            float tilt = Vector3.Angle(up, Vector3.up);
            if (tilt < rearmAngle) armed = true;

            Vector3 mouth = transform.position + up * bottle.MouthHeight;
            Vector3 rim = glass.transform.position + glass.transform.up * 0.16f;
            bool pieces = bottle.Style == PourStyle.Pieces;
            bool canPour = armed && glass != null && !glass.Busy && !glass.Shattered && !glass.IsFull
                           && tilt >= (pieces ? pieceAngle : pourAngle) && IsOver(mouth, rim, aimRadius);

            if (!canPour) { pourTimer = 0f; SetStream(false, mouth, rim); return; }

            if (!pieces) SetStream(true, mouth, rim);
            pourTimer += Time.deltaTime;
            if (pourTimer < pourSeconds) return;

            // Entra una dosis
            if (pieces) bottle.SpawnPiece(mouth, rim);
            glass.Add(bottle.Ingredient);
            armed = false; pourTimer = 0f;
            Haptic(0.45f, 0.12f);
        }

        /// <summary>La boca está encima del vaso: cerca en horizontal y por arriba del borde.</summary>
        public static bool IsOver(Vector3 mouth, Vector3 rim, float radius)
        {
            Vector2 d = new Vector2(mouth.x - rim.x, mouth.z - rim.z);
            float h = mouth.y - rim.y;
            return d.magnitude <= radius && h >= -0.05f && h <= 0.45f;
        }

        void SetStream(bool on, Vector3 from, Vector3 to)
        {
            if (!on) { if (stream) stream.gameObject.SetActive(false); return; }
            if (stream == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "VrStream";
                Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().material.color = bottle.Ingredient.color;
                stream = go.transform;
            }
            stream.gameObject.SetActive(true);
            Vector3 bottom = new Vector3(from.x, to.y - 0.1f, from.z);
            stream.position = (from + bottom) * 0.5f;
            stream.rotation = Quaternion.identity;
            stream.localScale = new Vector3(0.012f, Mathf.Max(0.01f, (from.y - bottom.y) * 0.5f), 0.012f);
        }

        void Haptic(float amplitude, float duration)
        {
            if (holder is XRBaseInputInteractor input) input.SendHapticImpulse(amplitude, duration);
        }
    }
}
