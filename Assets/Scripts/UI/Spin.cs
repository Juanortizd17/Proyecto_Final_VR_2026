using UnityEngine;

namespace QualityControlXR
{
    /// <summary>Rotación lenta para las vistas previas de producto sobre cada contenedor.</summary>
    public class Spin : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 45f;
        [SerializeField] float bobAmplitude = 0.015f;
        Vector3 basePos;

        void Start() => basePos = transform.localPosition;

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * bobAmplitude;
        }
    }
}
