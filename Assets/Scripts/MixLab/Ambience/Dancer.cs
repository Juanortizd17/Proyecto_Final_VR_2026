using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Personaje que baila en la pista (o el DJ): rebota y gira al ritmo de la música y va
    /// alternando animaciones cada pocos compases. Salta cuando sale un pedido perfecto.
    /// </summary>
    public class Dancer : MonoBehaviour
    {
        [SerializeField] string[] clips = { "walk", "emote-yes", "interact-right", "interact-left", "jump" };
        [SerializeField] float bounce = 0.05f;
        [SerializeField] float twist = 20f;
        [SerializeField] int beatsPerMove = 4;

        Animation anim;
        Vector3 basePos;
        Quaternion baseRot;
        float offset;
        int lastSlot = -1;
        string current;
        float cheerUntil;

        public void Setup(string[] danceClips, float bounceHeight, float twistDegrees)
        {
            clips = danceClips; bounce = bounceHeight; twist = twistDegrees;
        }

        void Start()
        {
            anim = GetComponentInChildren<Animation>();
            basePos = transform.localPosition; baseRot = transform.localRotation;
            offset = Random.Range(0, 4);
            if (anim != null) anim.playAutomatically = false;
            if (MixGameManager.Instance != null) MixGameManager.Instance.OnReaction += React;
        }

        void OnDestroy()
        {
            if (MixGameManager.Instance != null) MixGameManager.Instance.OnReaction -= React;
        }

        void React(ReactionData reaction, bool isNew)
        {
            if (reaction.id != ReactionId.Perfecto || anim == null || anim.GetClip("jump") == null) return;
            anim["jump"].wrapMode = WrapMode.Loop;
            anim.CrossFade("jump", 0.12f);
            current = "jump";
            cheerUntil = Time.time + 1.6f;
        }

        void Update()
        {
            float beat = DiscoLights.Beat + offset;
            float phase = beat - Mathf.Floor(beat);
            transform.localPosition = basePos + Vector3.up * (Mathf.Abs(Mathf.Sin(phase * Mathf.PI)) * bounce);
            transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(beat * Mathf.PI * 0.5f) * twist, Mathf.Sin(beat * Mathf.PI) * 3f);

            if (anim == null || clips == null || clips.Length == 0 || Time.time < cheerUntil) return;
            int slot = Mathf.FloorToInt(beat / Mathf.Max(1, beatsPerMove));
            if (slot == lastSlot) return;
            lastSlot = slot;
            string next = clips[Random.Range(0, clips.Length)];
            if (next == current && clips.Length > 1) next = clips[(System.Array.IndexOf(clips, next) + 1) % clips.Length];
            if (anim.GetClip(next) == null) return;
            anim[next].wrapMode = WrapMode.Loop;
            anim[next].speed = 1.15f;
            anim.CrossFade(next, 0.3f);
            current = next;
        }
    }
}
