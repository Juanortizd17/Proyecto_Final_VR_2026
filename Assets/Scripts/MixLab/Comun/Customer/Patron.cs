using UnityEngine;

namespace MixLab
{
    /// <summary>
    /// Cliente de fondo (personaje animado de Kenney Mini Characters). Hace su animación base
    /// en bucle y reacciona a lo que pasa en la barra: asiente con un pedido perfecto y niega
    /// o se sorprende cuando una mezcla sale mal.
    /// </summary>
    public class Patron : MonoBehaviour
    {
        [SerializeField] string baseClip = "sit";
        [SerializeField] string happyClip = "emote-yes";
        [SerializeField] string shockedClip = "emote-no";

        Animation anim;

        public string BaseClip => baseClip;

        public void Setup(string loopClip) => baseClip = loopClip;

        void Start()
        {
            anim = GetComponentInChildren<Animation>();
            if (anim == null) return;
            anim.playAutomatically = false;
            Loop(baseClip);
            if (anim.GetClip(baseClip) != null)
            {
                anim.Play(baseClip);
                anim[baseClip].time = Random.Range(0f, anim[baseClip].length);   // que no vayan sincronizados
            }
            if (MixGameManager.Instance != null) MixGameManager.Instance.OnReaction += React;
        }

        void OnDestroy()
        {
            if (MixGameManager.Instance != null) MixGameManager.Instance.OnReaction -= React;
        }

        void Loop(string clip)
        {
            if (anim.GetClip(clip) != null) anim[clip].wrapMode = WrapMode.Loop;
        }

        void React(ReactionData reaction, bool isNew)
        {
            if (anim == null) return;
            string clip = reaction.id == ReactionId.Perfecto || reaction.id == ReactionId.Alegre || reaction.id == ReactionId.Bailarin ? happyClip : shockedClip;
            if (reaction.id == ReactionId.SinChiste || anim.GetClip(clip) == null) return;
            anim[clip].wrapMode = WrapMode.Once;
            anim.CrossFade(clip, 0.15f);
            anim.CrossFadeQueued(clip, 0.1f, QueueMode.CompleteOthers);
            anim.CrossFadeQueued(baseClip, 0.25f, QueueMode.CompleteOthers);
        }
    }
}
