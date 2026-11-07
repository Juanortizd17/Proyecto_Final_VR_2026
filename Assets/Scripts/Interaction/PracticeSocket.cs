using System;
using UnityEngine;

namespace QualityControlXR
{
    /// <summary>
    /// Soporte del entrenamiento: cuando la caja de práctica queda dentro (ya soltada),
    /// la encaja en su sitio y avisa al tutorial. Funciona igual en PC y en VR.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PracticeSocket : MonoBehaviour
    {
        [SerializeField] Transform snapPoint;

        public bool Occupied { get; private set; }
        public Vector3 SnapPosition => snapPoint ? snapPoint.position : transform.position;
        public event Action OnPlaced;

        public void Setup(Transform snap) => snapPoint = snap;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (Occupied) return;
            var target = other.GetComponentInParent<PracticeTarget>();
            if (target == null || Grabbable.IsHeldObject(target)) return;

            Occupied = true;
            var rb = target.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = true; }
            target.transform.SetPositionAndRotation(SnapPosition, Quaternion.identity);
            OnPlaced?.Invoke();
        }
    }
}
