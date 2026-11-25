using UnityEngine;
using UnityEngine.InputSystem;

namespace MixLab
{
    /// <summary>
    /// Jugador en primera persona para PC: WASD para caminar, Shift para correr y mouse para mirar.
    /// Usa un CharacterController con gravedad, así que choca con paredes, mesas y máquinas
    /// y no puede subirse a los contenedores.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayer : MonoBehaviour
    {
        public const string SensitivityKey = "qc_mouse_sensitivity";
        public const float DefaultSensitivity = 0.12f;

        [SerializeField] Transform cameraPivot;
        [SerializeField] float walkSpeed = 2.4f;
        [SerializeField] float sprintSpeed = 4.0f;
        [SerializeField] float gravity = -20f;
        [SerializeField] AudioSource footstepSource;
        [SerializeField] AudioClip[] footstepClips;
        [SerializeField] float stepLength = 0.65f;

        public static DesktopPlayer Instance { get; private set; }

        CharacterController cc;
        float yaw, pitch, verticalVelocity, stepDistance;
        float sensitivity;
        Vector2 simulatedMove;
        bool simulating;

        public bool Paused { get; set; }
        public Transform CameraPivot => cameraPivot;
        public bool IsGrounded => cc != null && cc.isGrounded;

        public float Sensitivity
        {
            get => sensitivity;
            set { sensitivity = Mathf.Clamp(value, 0.02f, 0.5f); PlayerPrefs.SetFloat(SensitivityKey, sensitivity); }
        }

        public void Setup(Transform cam, AudioSource steps, AudioClip[] clips)
        {
            cameraPivot = cam; footstepSource = steps; footstepClips = clips;
        }

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();
            // Con FPS muy altos el paso por cuadro es diminuto: no debe descartarse.
            cc.minMoveDistance = 0f;
            sensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
            yaw = transform.eulerAngles.y;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start() => SetCursorLocked(true);

        public static void SetCursorLocked(bool locked)
        {
            if (Application.isBatchMode) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            Vector3 move = Vector3.zero;
            float speed = walkSpeed;

            if (!Paused)
            {
                var mouse = Mouse.current;
                if (mouse != null && !simulating && Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    yaw += d.x * sensitivity;
                    pitch = Mathf.Clamp(pitch - d.y * sensitivity, -80f, 80f);
                }
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

                bool sprint = false;
                Vector2 input = simulating ? simulatedMove : ReadKeyboard(out sprint);
                if (sprint) speed = sprintSpeed;
                move = (transform.right * input.x + transform.forward * input.y);
                if (move.sqrMagnitude > 1f) move.Normalize();
                move *= speed;
            }

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            cc.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);

            Footsteps(move, speed);
        }

        static Vector2 ReadKeyboard(out bool sprint)
        {
            sprint = false;
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            sprint = kb.leftShiftKey.isPressed;
            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        void Footsteps(Vector3 move, float speed)
        {
            if (!cc.isGrounded || move.sqrMagnitude < 0.05f || footstepSource == null || footstepClips == null || footstepClips.Length == 0)
                return;
            stepDistance += move.magnitude * Time.deltaTime;
            if (stepDistance < stepLength * (speed > walkSpeed ? 1.25f : 1f)) return;
            stepDistance = 0f;
            footstepSource.pitch = Random.Range(0.92f, 1.08f);
            footstepSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], 0.35f);
        }

        /// <summary>Mueve al jugador a una posición y orientación (p. ej. el puesto de trabajo).</summary>
        public void TeleportTo(Vector3 position, float yawDegrees, float pitchDegrees)
        {
            cc.enabled = false;
            transform.position = position;
            yaw = yawDegrees; pitch = pitchDegrees;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            verticalVelocity = 0f;
            cc.enabled = true;
        }

        // Para pruebas automáticas: simula la entrada de movimiento.
        public void SimulateMove(Vector2 input) { simulatedMove = input; simulating = true; }
        public void StopSimulation() { simulatedMove = Vector2.zero; simulating = false; }
    }
}
