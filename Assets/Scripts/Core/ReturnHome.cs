using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QualityControlXR
{
    /// <summary>
    /// Devuelve el objeto a su posición inicial si cae al vacío, se aleja demasiado
    /// o queda abandonado en el piso. Evita que el usuario pierda las herramientas.
    /// </summary>
    public class ReturnHome : MonoBehaviour
    {
        [SerializeField] float maxDistance = 4f;
        [SerializeField] float floorY = 0.12f;
        [SerializeField] float idleOnFloorSeconds = 4f;

        Vector3 homePos;
        Quaternion homeRot;
        Rigidbody rb;
        Grabbable grab;
        float onFloorTime;

        void Awake()
        {
            homePos = transform.position;
            homeRot = transform.rotation;
            rb = GetComponent<Rigidbody>();
            grab = GetComponent<Grabbable>();
        }

        void Update()
        {
            if (grab != null && grab.IsHeld) { onFloorTime = 0f; return; }
            bool lost = transform.position.y < -1f || Vector3.Distance(transform.position, homePos) > maxDistance;
            onFloorTime = transform.position.y < floorY ? onFloorTime + Time.deltaTime : 0f;
            if (lost || onFloorTime > idleOnFloorSeconds) GoHome();
        }

        public void GoHome()
        {
            onFloorTime = 0f;
            if (rb)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                rb.position = homePos; rb.rotation = homeRot;
            }
            transform.SetPositionAndRotation(homePos, homeRot);
        }
    }
}
