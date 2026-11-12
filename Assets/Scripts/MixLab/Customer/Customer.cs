using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MixLab
{
    /// <summary>
    /// Cliente de prueba. Está "vivo": respira, parpadea, sigue al jugador con la mirada, mueve
    /// los brazos (hombro-codo-mano resueltos por cinemática inversa) y se impacienta.
    /// Reacciona a cada mezcla con síntomas crudos y visibles: color de piel, ojos inyectados,
    /// venas, labios hinchados, ronchas, lágrimas, babas, mocos, arcadas con vómito, tos con humo,
    /// espuma y espasmos al desmayarse. Sin sangre: es desagradable, no violento.
    /// Sus piezas se buscan por nombre en la jerarquía que genera MixSceneBuilder.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        enum Eyes { Normal, Wide, Closed, Happy, X, Dizzy, Half, Flat, Rolled, Pinpoint }
        enum Mouth { Smile, Big, O, Wavy, Puffed, Fire, Chatter, Tongue, Flat }
        enum Body { Idle, Wobble, Shake, Shiver, Faint, Hyper, Sway, Hiccup, Dance, Sleep, Retch, Cough, Pant }
        enum Gesture { Rest, Fan, Belly, HoldHead, OneHandHead, Cheer, Face, Limp, Tap, Dance, CoverMouth, Throat }

        [SerializeField] string customerName = "Ramiro";
        [SerializeField] Color normalSkin = new Color(0.95f, 0.76f, 0.61f);
        [SerializeField] Transform mouthPoint;
        [SerializeField] Transform coasterPoint;
        [SerializeField] GameObject bubble;
        [SerializeField] Text bubbleText;

        static readonly Color Blush = new Color(1f, 0.45f, 0.55f);
        static readonly Color LipRed = new Color(0.86f, 0.1f, 0.14f);
        static readonly Color LipBlue = new Color(0.35f, 0.42f, 0.8f);
        static readonly Color NoseRed = new Color(0.9f, 0.16f, 0.2f);
        static readonly Vector3 ShoulderL = new Vector3(-0.2f, 0.46f, 0.02f);
        static readonly Vector3 ShoulderR = new Vector3(0.2f, 0.46f, 0.02f);
        static readonly Vector3 RestL = new Vector3(-0.2f, 0.245f, 0.4f);
        static readonly Vector3 RestR = new Vector3(0.2f, 0.245f, 0.4f);
        const float UpperLen = 0.25f, ForeLen = 0.25f;

        Transform pivot, head, pupilL, pupilR, whiteL, whiteR, mouthOpen, teeth, flame, stars, browL, browR;
        Transform upperL, foreL, handL, upperR, foreR, handR;
        Transform foreheadVein, lips, tongueLong, snotBubble;
        GameObject eyesOpen, lids, eyesClosed, eyesHappy, eyesX, eyesFlat, brows;
        GameObject mouthSmile, mouthWavy, mouthFlat, tongue, cheeks, hairNormal, hairUp, splash, icicles;
        GameObject bloodshot, eyeBags, hives, soot, bile, foam, snotIcicle, lidL, lidR, noseBleed;
        Transform headFlames;
        Vector3 headFlamesScale;
        ParticleSystem fxFire, fxBlood;
        bool onFire;
        ParticleSystem fxSteam, fxSweat, fxSmoke, fxHearts, fxVomit, fxTearsL, fxTearsR, fxDrool, fxEarL, fxEarR, fxBurp, fxCough;
        readonly List<Renderer> skin = new List<Renderer>();
        Renderer[] cheekRenderers, lipRenderers;
        Renderer nose;
        MaterialPropertyBlock mpb;
        bool cached;

        Vector3 pivotPos, headPos, pupilLBase, pupilRBase, whiteScale, pupilScale, teethBase, flameScale, cheekScale, browLBase, browRBase;
        Vector3 handLPos, handRPos, lipsScale, veinScale, tongueLongPos, snotScale;
        Quaternion pivotRot;
        Color currentSkin, targetSkin;
        Eyes eyes;
        Mouth mouth;
        Body body;
        Gesture gesture;
        Transform heldGlass;
        float animTime, blinkAt, blinkUntil, emitAcc, headPuff, noseRedness, burpAt;
        bool heaving, bulging, throbbing, browsBurnt;

        public string Name => customerName;
        public Transform MouthPoint => mouthPoint;
        public Transform CoasterPoint => coasterPoint;
        public ReactionId? CurrentReaction { get; private set; }
        /// <summary>Lo activa el director del juego cuando queda poca paciencia.</summary>
        public bool Impatient { get; set; }

        public void Setup(string displayName, Color skinColor, Transform mouthTarget, Transform coaster, GameObject bubbleRoot, Text bubbleLabel)
        {
            customerName = displayName; normalSkin = skinColor; mouthPoint = mouthTarget; coasterPoint = coaster;
            bubble = bubbleRoot; bubbleText = bubbleLabel;
        }

        void Awake() { Cache(); ResetPose(); }

        void Cache()
        {
            if (cached) return;
            cached = true;
            mpb = new MaterialPropertyBlock();
            pivot = transform.Find("Pivot");
            head = pivot.Find("Head");
            pivotPos = pivot.localPosition; pivotRot = pivot.localRotation; headPos = head.localPosition;

            upperL = pivot.Find("ArmL_Upper"); foreL = pivot.Find("ArmL_Fore"); handL = pivot.Find("Skin_HandL");
            upperR = pivot.Find("ArmR_Upper"); foreR = pivot.Find("ArmR_Fore"); handR = pivot.Find("Skin_HandR");
            handLPos = RestL; handRPos = RestR;

            eyesOpen = G("EyesOpen"); lids = G("Lids"); eyesClosed = G("EyesClosed"); eyesHappy = G("EyesHappy");
            eyesX = G("EyesX"); eyesFlat = G("EyesFlat"); brows = G("Brows");
            lidL = O("Lids/Skin_LidL"); lidR = O("Lids/Skin_LidR");
            whiteL = head.Find("EyesOpen/WhiteL"); whiteR = head.Find("EyesOpen/WhiteR");
            pupilL = head.Find("EyesOpen/PupilL"); pupilR = head.Find("EyesOpen/PupilR");
            browL = head.Find("Brows/BrowL"); browR = head.Find("Brows/BrowR");
            browLBase = browL.localPosition; browRBase = browR.localPosition;
            whiteScale = whiteL.localScale; pupilScale = pupilL.localScale;
            pupilLBase = pupilL.localPosition; pupilRBase = pupilR.localPosition;

            mouthSmile = G("MouthSmile"); mouthWavy = G("MouthWavy"); mouthFlat = G("MouthFlat");
            mouthOpen = head.Find("MouthOpen"); tongue = G("Tongue");
            teeth = head.Find("Teeth"); teethBase = teeth.localPosition;
            flame = head.Find("Flame"); flameScale = flame.localScale;
            cheeks = G("Cheeks"); cheekRenderers = cheeks.GetComponentsInChildren<Renderer>(true);
            cheekScale = cheeks.transform.GetChild(0).localScale;
            hairNormal = G("HairNormal"); hairUp = G("HairUp"); splash = G("Splash"); icicles = G("Icicles");
            stars = head.Find("Stars");

            // Síntomas crudos (opcionales: si la escena es antigua simplemente no aparecen)
            bloodshot = O("Bloodshot"); eyeBags = O("EyeBags"); hives = O("Hives"); soot = O("Soot");
            bile = O("Bile"); foam = O("Foam"); snotIcicle = O("SnotIcicle"); noseBleed = O("NoseBleed");
            headFlames = head.Find("HeadFlames"); if (headFlames) headFlamesScale = headFlames.localScale;
            foreheadVein = head.Find("ForeheadVein"); if (foreheadVein) veinScale = foreheadVein.localScale;
            lips = head.Find("Lips"); if (lips) { lipsScale = lips.localScale; lipRenderers = lips.GetComponentsInChildren<Renderer>(true); }
            tongueLong = head.Find("TongueLong"); if (tongueLong) tongueLongPos = tongueLong.localPosition;
            snotBubble = head.Find("SnotBubble"); if (snotBubble) snotScale = snotBubble.localScale;

            fxSteam = P("FX_Steam"); fxSweat = P("FX_Sweat"); fxSmoke = P("FX_Smoke"); fxHearts = P("FX_Hearts");
            fxVomit = P("FX_Vomit"); fxTearsL = P("FX_TearsL"); fxTearsR = P("FX_TearsR"); fxDrool = P("FX_Drool");
            fxEarL = P("FX_EarSteamL"); fxEarR = P("FX_EarSteamR"); fxBurp = P("FX_Burp"); fxCough = P("FX_Cough");
            fxFire = P("FX_HeadFire"); fxBlood = P("FX_Blood");

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("Skin_")) continue;
                if (r.name == "Skin_Nose") nose = r; else skin.Add(r);
            }
            currentSkin = targetSkin = normalSkin;
            blinkAt = 2f;
        }

        GameObject G(string name) => head.Find(name).gameObject;
        GameObject O(string name) { var t = head.Find(name); return t ? t.gameObject : null; }
        ParticleSystem P(string name) { var t = head.Find(name); return t ? t.GetComponent<ParticleSystem>() : null; }

        // =====================================================================
        // API
        // =====================================================================

        public void Say(string text)
        {
            if (bubbleText) bubbleText.text = text;
            if (bubble) bubble.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>La mano derecha sigue al vaso (para tomarlo y beber).</summary>
        public void HoldGlass(Transform glass) => heldGlass = glass;
        public void ReleaseGlass() => heldGlass = null;

        public void ShowReaction(ReactionData data)
        {
            Cache();
            ClearExtras();
            CurrentReaction = data.id;
            targetSkin = data.skinColor;
            Say(data.customerLine);

            switch (data.id)
            {
                case ReactionId.Perfecto:
                    Pose(Eyes.Happy, Mouth.Big, Body.Idle, Gesture.Cheer); Cheeks(Blush, 1f); Play(fxHearts); break;

                case ReactionId.Picante:        // lengua afuera, jadeo, lágrimas, labios hinchados y ronchas
                    Pose(Eyes.Half, Mouth.O, Body.Pant, Gesture.Fan);
                    Show(bloodshot); Show(hives); Lips(LipRed, 1f); TongueOut(); Play(fxSweat); Play(fxTearsL); Play(fxTearsR); break;

                case ReactionId.Infierno:       // ojos desorbitados, vena en la frente, vapor por las orejas, cabeza hinchada
                    Pose(Eyes.Wide, Mouth.Fire, Body.Shake, Gesture.Throat);
                    Show(bloodshot); Show(hives); Lips(LipRed, 1.35f); bulging = true; throbbing = true; headPuff = 0.09f;
                    if (foreheadVein) foreheadVein.gameObject.SetActive(true);
                    Play(fxSteam); Play(fxSweat); Play(fxEarL); Play(fxEarR); Play(fxTearsL); Play(fxTearsR); break;

                case ReactionId.Explosion:      // cara embarrada, tos y un eructo de gas
                    Pose(Eyes.Wide, Mouth.O, Body.Cough, Gesture.Face);
                    splash.SetActive(true); Hair(true); Show(bloodshot); Play(fxTearsL); Play(fxTearsR); burpAt = 1.4f; break;

                case ReactionId.Borracho:       // párpado caído, ojeras, nariz roja, babas
                    Pose(Eyes.Dizzy, Mouth.Wavy, Body.Wobble, Gesture.OneHandHead);
                    Cheeks(Blush, 1.1f); Show(eyeBags); Droop(); noseRedness = 1f; Play(fxDrool); burpAt = 1.8f; break;

                case ReactionId.Alegre:
                    Pose(Eyes.Happy, Mouth.Smile, Body.Sway, Gesture.Rest); Cheeks(Blush, 1f); noseRedness = 0.6f; break;

                case ReactionId.Nausea:         // arcadas: se tapa la boca, se infla y vomita sobre la barra
                    Pose(Eyes.Half, Mouth.Puffed, Body.Retch, Gesture.CoverMouth);
                    Cheeks(data.skinColor * 0.92f, 1.7f); Show(eyeBags); Play(fxSweat); break;

                case ReactionId.Congelado:      // labios azules, moco congelado, tiritona
                    Pose(Eyes.Closed, Mouth.Chatter, Body.Shiver, Gesture.HoldHead);
                    icicles.SetActive(true); Show(snotIcicle); Lips(LipBlue, 0.9f); noseRedness = 0.8f; break;

                case ReactionId.Hiper:          // pupilas diminutas, ojos inyectados, tic en el ojo, rechina los dientes
                    Pose(Eyes.Pinpoint, Mouth.Chatter, Body.Hyper, Gesture.Cheer);
                    Show(bloodshot); Show(eyeBags); throbbing = true; bulging = true;
                    if (foreheadVein) foreheadVein.gameObject.SetActive(true);
                    Show(noseBleed); Play(fxBlood); Play(fxSweat); break;

                case ReactionId.Azucar:
                    Pose(Eyes.Pinpoint, Mouth.Big, Body.Hyper, Gesture.Cheer); Cheeks(Blush, 1.2f); Show(bloodshot); Play(fxDrool); break;

                case ReactionId.Llamarada:      // se le prende el pelo: pánico, y al apagarse queda con hollín, sin cejas y tosiendo humo
                    Pose(Eyes.Wide, Mouth.Big, Body.Shake, Gesture.HoldHead);
                    Hair(true); Show(bloodshot); onFire = true;
                    if (headFlames) headFlames.gameObject.SetActive(true);
                    Play(fxFire); Play(fxTearsL); Play(fxTearsR); break;

                case ReactionId.Desmayo:        // ojos en blanco, espuma en la boca y espasmos
                    Pose(Eyes.Rolled, Mouth.Tongue, Body.Faint, Gesture.Limp);
                    stars.gameObject.SetActive(true); Show(foam); Show(eyeBags); Show(noseBleed); Play(fxBlood); Play(fxDrool); break;

                case ReactionId.Hipo:
                    Pose(Eyes.Wide, Mouth.O, Body.Hiccup, Gesture.Belly); Cheeks(Blush, 1.3f); burpAt = 1.6f; break;

                case ReactionId.Agrio:          // la cara se le arruga entera
                    Pose(Eyes.Closed, Mouth.Puffed, Body.Shiver, Gesture.Face);
                    Cheeks(data.skinColor * 0.9f, 1.4f); headPuff = -0.08f; Play(fxTearsL); Play(fxTearsR); break;

                case ReactionId.Dormido:        // burbuja de moco y babas
                    Pose(Eyes.Closed, Mouth.O, Body.Sleep, Gesture.Rest);
                    Show(eyeBags); if (snotBubble) snotBubble.gameObject.SetActive(true); Play(fxDrool); break;

                case ReactionId.Bailarin:
                    Pose(Eyes.Happy, Mouth.Big, Body.Dance, Gesture.Dance); Cheeks(Blush, 1.1f); Play(fxHearts); break;

                default: Pose(Eyes.Flat, Mouth.Flat, Body.Idle, Gesture.Rest); break;
            }
            ApplyBrows();
            if (!Application.isPlaying) SnapForPreview();
        }

        public void ResetPose()
        {
            Cache();
            ClearExtras();
            CurrentReaction = null;
            targetSkin = normalSkin;
            Pose(Eyes.Normal, Mouth.Smile, Body.Idle, Gesture.Rest);
            if (!Application.isPlaying) SnapForPreview();
        }

        /// <summary>En el Editor (capturas) no corre Update: se aplica la pose final de una vez.</summary>
        void SnapForPreview()
        {
            ApplySkin(currentSkin = targetSkin);
            GestureTargets(0.4f, out handLPos, out handRPos);
            SolveArms();
            ApplyBrows();
            if (body == Body.Retch) { ApplyMouth(Mouth.Big); Show(bile); }
            if (CurrentReaction == ReactionId.Llamarada) { Show(soot); browsBurnt = true; ApplyBrows(); }
            head.localScale = HeadScale(0f);
        }

        // =====================================================================
        // Pose
        // =====================================================================

        void Pose(Eyes e, Mouth m, Body b, Gesture g)
        {
            eyes = e; mouth = m; body = b; gesture = g; animTime = 0f; emitAcc = 0f; heaving = false;
            pivot.localPosition = pivotPos; pivot.localRotation = pivotRot;
            ApplyEyes(e);
            ApplyMouth(m);
            ApplyBrows();
        }

        void ApplyEyes(Eyes e)
        {
            bool open = e == Eyes.Normal || e == Eyes.Wide || e == Eyes.Dizzy || e == Eyes.Half || e == Eyes.Rolled || e == Eyes.Pinpoint;
            eyesOpen.SetActive(open);
            lids.SetActive(e == Eyes.Half || e == Eyes.Rolled);
            if (lidL) lidL.SetActive(true);
            if (lidR) lidR.SetActive(true);
            eyesClosed.SetActive(e == Eyes.Closed);
            eyesHappy.SetActive(e == Eyes.Happy);
            eyesX.SetActive(e == Eyes.X);
            eyesFlat.SetActive(e == Eyes.Flat);
            bool big = e == Eyes.Wide || e == Eyes.Pinpoint;
            float w = big ? 1.22f : 1f, p = e == Eyes.Wide ? 0.6f : e == Eyes.Pinpoint ? 0.32f : e == Eyes.Rolled ? 0.8f : 1f;
            whiteL.localScale = whiteR.localScale = whiteScale * w;
            pupilL.localScale = pupilR.localScale = pupilScale * p;
            Vector3 push = big ? new Vector3(0f, 0f, 0.008f) : e == Eyes.Rolled ? new Vector3(0f, 0.036f, -0.014f) : Vector3.zero;
            pupilL.localPosition = pupilLBase + push; pupilR.localPosition = pupilRBase + push;
        }

        void ApplyMouth(Mouth m)
        {
            mouthSmile.SetActive(m == Mouth.Smile);
            mouthWavy.SetActive(m == Mouth.Wavy);
            mouthFlat.SetActive(m == Mouth.Flat || m == Mouth.Tongue);
            tongue.SetActive(m == Mouth.Tongue);
            teeth.gameObject.SetActive(m == Mouth.Chatter);
            flame.gameObject.SetActive(m == Mouth.Fire);
            bool round = m == Mouth.Big || m == Mouth.O || m == Mouth.Puffed || m == Mouth.Fire;
            mouthOpen.gameObject.SetActive(round);
            mouthOpen.localScale =
                m == Mouth.Big ? new Vector3(0.13f, 0.085f, 0.03f) :
                m == Mouth.Puffed ? new Vector3(0.035f, 0.02f, 0.03f) :
                m == Mouth.Fire ? new Vector3(0.085f, 0.095f, 0.03f) :
                new Vector3(0.055f, 0.07f, 0.03f);
        }

        /// <summary>Cejas: ángulo positivo = enojo/dolor (punta interna abajo); negativo = tristeza.</summary>
        void ApplyBrows()
        {
            float angle = 0f, lift = 0f;
            bool show = !browsBurnt;
            switch (eyes)
            {
                case Eyes.Wide: lift = 0.024f; break;
                case Eyes.Pinpoint: lift = 0.03f; angle = 8f; break;
                case Eyes.Closed: angle = 24f; break;
                case Eyes.Happy: lift = 0.012f; break;
                case Eyes.Dizzy: angle = -15f; break;
                case Eyes.Half: angle = -20f; break;
                case Eyes.Rolled: angle = -12f; lift = 0.01f; break;
                case Eyes.X: show = false; break;
                case Eyes.Normal: if (Impatient && CurrentReaction == null) angle = 16f; break;
            }
            brows.SetActive(show);
            browL.localPosition = browLBase + Vector3.up * lift; browR.localPosition = browRBase + Vector3.up * lift;
            browL.localRotation = Quaternion.Euler(0f, 0f, -angle);
            browR.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void ClearExtras()
        {
            cheeks.SetActive(false); splash.SetActive(false); icicles.SetActive(false);
            stars.gameObject.SetActive(false);
            Hair(false);
            Hide(bloodshot); Hide(eyeBags); Hide(hives); Hide(soot); Hide(bile); Hide(foam); Hide(snotIcicle); Hide(noseBleed);
            if (headFlames) headFlames.gameObject.SetActive(false);
            onFire = false;
            if (foreheadVein) foreheadVein.gameObject.SetActive(false);
            if (lips) lips.gameObject.SetActive(false);
            if (tongueLong) tongueLong.gameObject.SetActive(false);
            if (snotBubble) snotBubble.gameObject.SetActive(false);
            bulging = throbbing = browsBurnt = false;
            headPuff = 0f; noseRedness = 0f; burpAt = 0f;
            if (head) head.localScale = Vector3.one;
            Stop(fxSteam); Stop(fxSweat); Stop(fxSmoke); Stop(fxHearts); Stop(fxVomit); Stop(fxTearsL); Stop(fxTearsR);
            Stop(fxDrool); Stop(fxEarL); Stop(fxEarR); Stop(fxBurp); Stop(fxCough); Stop(fxFire); Stop(fxBlood);
        }

        static void Show(GameObject go) { if (go) go.SetActive(true); }
        static void Hide(GameObject go) { if (go) go.SetActive(false); }

        void Hair(bool up) { hairNormal.SetActive(!up); hairUp.SetActive(up); }

        /// <summary>Un párpado caído (borrachera): solo se ve el izquierdo.</summary>
        void Droop()
        {
            lids.SetActive(true);
            if (lidR) lidR.SetActive(false);
        }

        void TongueOut() { if (tongueLong) tongueLong.gameObject.SetActive(true); }

        void Lips(Color color, float scale)
        {
            if (!lips) return;
            lips.gameObject.SetActive(true);
            lips.localScale = lipsScale * scale;
            foreach (var r in lipRenderers) { r.GetPropertyBlock(mpb); mpb.SetColor("_Color", color); r.SetPropertyBlock(mpb); }
        }

        void Cheeks(Color color, float scale)
        {
            cheeks.SetActive(true);
            foreach (var r in cheekRenderers)
            {
                r.transform.localScale = cheekScale * scale;
                r.GetPropertyBlock(mpb); mpb.SetColor("_Color", color); r.SetPropertyBlock(mpb);
            }
        }

        void ApplySkin(Color c)
        {
            foreach (var r in skin)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb); mpb.SetColor("_Color", c); r.SetPropertyBlock(mpb);
            }
            if (nose)
            {
                nose.GetPropertyBlock(mpb); mpb.SetColor("_Color", Color.Lerp(c, NoseRed, noseRedness)); nose.SetPropertyBlock(mpb);
            }
        }

        Vector3 HeadScale(float t)
        {
            if (headPuff > 0f) return Vector3.one * (1f + headPuff * (0.75f + 0.25f * Mathf.Sin(t * 9f)));      // hinchada y latiendo
            if (headPuff < 0f) return new Vector3(1f - headPuff * 0.6f, 1f + headPuff, 1f);                       // arrugada
            return Vector3.one;
        }

        static void Play(ParticleSystem ps) { if (ps && Application.isPlaying) ps.Play(); }
        static void Stop(ParticleSystem ps) { if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }

        /// <summary>Emite a chorros controlados por código (vómito, tos).</summary>
        void Spray(ParticleSystem ps, float perSecond, float dt)
        {
            if (!ps || !Application.isPlaying) return;
            if (!ps.isPlaying) ps.Play();
            emitAcc += perSecond * dt;
            int n = Mathf.FloorToInt(emitAcc);
            if (n <= 0) return;
            emitAcc -= n;
            ps.Emit(n);
        }

        // =====================================================================
        // Brazos (cinemática inversa de dos huesos, en el espacio local del torso)
        // =====================================================================

        void GestureTargets(float t, out Vector3 l, out Vector3 r)
        {
            l = RestL; r = RestR;
            var g = gesture;
            if (CurrentReaction == null && Impatient && g == Gesture.Rest) g = Gesture.Tap;
            switch (g)
            {
                case Gesture.Fan: r = new Vector3(0.1f + Mathf.Sin(t * 18f) * 0.06f, 0.68f, 0.3f); break;
                case Gesture.Belly: l = new Vector3(-0.07f, 0.2f, 0.2f); r = new Vector3(0.07f, 0.17f, 0.2f); break;
                case Gesture.HoldHead: l = new Vector3(-0.2f, 0.78f, 0.1f); r = new Vector3(0.2f, 0.78f, 0.1f); break;
                case Gesture.OneHandHead: r = new Vector3(0.19f, 0.83f, 0.08f); break;
                case Gesture.Cheer:
                    float w = Mathf.Sin(t * 10f) * 0.04f;
                    l = new Vector3(-0.3f, 0.93f + w, 0.1f); r = new Vector3(0.3f, 0.93f - w, 0.1f); break;
                case Gesture.Face: l = new Vector3(-0.17f, 0.66f, 0.25f); r = new Vector3(0.17f, 0.66f, 0.25f); break;
                case Gesture.Limp: l = new Vector3(-0.27f, -0.02f, 0.05f); r = new Vector3(0.27f, -0.02f, 0.05f); break;
                case Gesture.Tap: r = RestR + Vector3.up * (Mathf.Max(0f, Mathf.Sin(t * 11f)) * 0.05f); break;
                case Gesture.Dance:
                    float beatSwing = Mathf.Sin(DiscoLights.Beat * Mathf.PI) * 0.2f;
                    l = new Vector3(-0.3f, 0.7f + beatSwing, 0.15f); r = new Vector3(0.3f, 0.7f - beatSwing, 0.15f); break;
                case Gesture.CoverMouth:      // una mano a la boca y otra al estómago; al vomitar se agarra de la barra
                    if (heaving) { l = RestL + new Vector3(-0.05f, 0f, 0.02f); r = RestR + new Vector3(0.05f, 0f, 0.02f); }
                    else { l = new Vector3(-0.06f, 0.2f, 0.2f); r = new Vector3(0.03f, 0.64f, 0.27f); }
                    break;
                case Gesture.Throat: l = new Vector3(-0.07f, 0.57f, 0.19f); r = new Vector3(0.07f, 0.55f, 0.2f); break;
            }
            if (heldGlass != null)
                r = pivot.InverseTransformPoint(heldGlass.position + Vector3.up * 0.07f) + new Vector3(0.05f, 0f, -0.02f);
        }

        void SolveArms()
        {
            SolveArm(upperL, foreL, handL, ShoulderL, handLPos, -1f);
            SolveArm(upperR, foreR, handR, ShoulderR, handRPos, 1f);
        }

        static void SolveArm(Transform upper, Transform fore, Transform hand, Vector3 shoulder, Vector3 target, float side)
        {
            Vector3 d = target - shoulder;
            float dist = Mathf.Clamp(d.magnitude, 0.06f, UpperLen + ForeLen - 0.004f);
            Vector3 dir = d.sqrMagnitude < 1e-6f ? Vector3.down : d.normalized;
            Vector3 wrist = shoulder + dir * dist;
            float a = (UpperLen * UpperLen - ForeLen * ForeLen + dist * dist) / (2f * dist);
            float h = Mathf.Sqrt(Mathf.Max(0f, UpperLen * UpperLen - a * a));
            Vector3 hint = Vector3.ProjectOnPlane(new Vector3(side * 0.8f, -0.6f, -0.3f), dir);
            if (hint.sqrMagnitude < 1e-5f) hint = Vector3.ProjectOnPlane(new Vector3(side, 0f, 0f), dir);
            Vector3 elbow = shoulder + dir * a + hint.normalized * h;
            Segment(upper, shoulder, elbow);
            Segment(fore, elbow, wrist);
            hand.localPosition = wrist;
        }

        static void Segment(Transform seg, Vector3 a, Vector3 b)
        {
            seg.localPosition = (a + b) * 0.5f;
            seg.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            var s = seg.localScale; s.y = (b - a).magnitude * 0.5f + 0.03f; seg.localScale = s;
        }

        // =====================================================================
        // Animación
        // =====================================================================

        void Update()
        {
            if (!cached) return;
            float dt = Time.deltaTime;
            currentSkin = Color.Lerp(currentSkin, targetSkin, 1f - Mathf.Exp(-6f * dt));
            ApplySkin(currentSkin);

            animTime += dt;
            float t = animTime;
            Vector3 pos = pivotPos; Quaternion rot = pivotRot;
            switch (body)
            {
                case Body.Idle:      // respiración + leve balanceo
                    pos += Vector3.up * (Mathf.Sin(t * 1.7f) * 0.005f);
                    rot = pivotRot * Quaternion.Euler(Mathf.Sin(t * 0.9f) * 1.2f, 0f, Mathf.Sin(t * 0.6f) * 1.5f); break;
                case Body.Wobble:
                    rot = pivotRot * Quaternion.Euler(Mathf.Sin(t * 2.2f) * 6f, 0f, Mathf.Sin(t * 3f) * 13f);
                    pos += Vector3.right * (Mathf.Sin(t * 3f) * 0.03f); break;
                case Body.Shake: pos += Random.insideUnitSphere * 0.014f; break;
                case Body.Shiver: pos += Vector3.right * (Mathf.Sin(t * 60f) * 0.007f); break;
                case Body.Hyper: pos += Vector3.up * (Mathf.Abs(Mathf.Sin(t * 14f)) * 0.035f) + Random.insideUnitSphere * 0.004f; break;
                case Body.Sway: rot = pivotRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f) * 5f); break;
                case Body.Pant:       // jadeo rápido con el pecho
                    pos += Vector3.up * (Mathf.Sin(t * 13f) * 0.008f);
                    rot = pivotRot * Quaternion.Euler(4f + Mathf.Sin(t * 13f) * 2f, 0f, Mathf.Sin(t * 1.8f) * 4f); break;
                case Body.Hiccup:     // saltito seco cada cierto tiempo
                    float hp = Mathf.Repeat(t, 0.9f);
                    if (hp < 0.12f) pos += Vector3.up * (Mathf.Sin(hp / 0.12f * Mathf.PI) * 0.05f);
                    break;
                case Body.Dance:      // baila al ritmo de la música del bar
                    float db = DiscoLights.Beat;
                    pos += Vector3.up * (Mathf.Abs(Mathf.Sin(db * Mathf.PI)) * 0.04f);
                    rot = pivotRot * Quaternion.Euler(0f, Mathf.Sin(db * Mathf.PI * 0.5f) * 14f, Mathf.Sin(db * Mathf.PI) * 7f); break;
                case Body.Sleep:      // cabecea y respira hondo
                    float sk = Mathf.Clamp01(t / 1.2f);
                    rot = pivotRot * Quaternion.Euler(9f * sk + Mathf.Sin(t * 1.2f) * 1.5f, 0f, 4f * sk); break;
                case Body.Retch:      // arcadas: aguanta, se dobla sobre la barra y vomita
                    {
                        float ph = Mathf.Repeat(t, 1.5f);
                        bool now = ph > 0.85f;
                        if (now != heaving)
                        {
                            heaving = now;
                            ApplyMouth(now ? Mouth.Big : Mouth.Puffed);
                            cheeks.SetActive(!now);
                            if (now) Show(bile);
                        }
                        if (now)
                        {
                            float k = Mathf.Sin((ph - 0.85f) / 0.65f * Mathf.PI);
                            rot = pivotRot * Quaternion.Euler(30f * k, 0f, 0f);
                            pos += Vector3.down * (0.02f * k);
                            if (k > 0.35f) Spray(fxVomit, 110f, dt);
                        }
                        else
                        {
                            rot = pivotRot * Quaternion.Euler(-5f + Mathf.Sin(t * 22f) * 1.2f, 0f, Mathf.Sin(t * 2f) * 4f);
                            pos += Vector3.up * (Mathf.Max(0f, Mathf.Sin(ph * 9f)) * 0.012f);          // espasmos del estómago
                        }
                    }
                    break;
                case Body.Cough:      // tos seca: sacudidas hacia adelante con humo
                    {
                        float ph = Mathf.Repeat(t, 0.95f);
                        if (ph < 0.22f)
                        {
                            float k = Mathf.Sin(ph / 0.22f * Mathf.PI);
                            rot = pivotRot * Quaternion.Euler(16f * k, 0f, 0f);
                            Spray(fxCough, 70f, dt);
                        }
                        else pos += Random.insideUnitSphere * 0.004f;
                    }
                    break;
                case Body.Faint:      // se desploma y luego le dan espasmos
                    {
                        float k = Mathf.Clamp01(t / 0.7f); k = k * k * (3f - 2f * k);
                        rot = pivotRot * Quaternion.Euler(0f, 0f, 80f * k);
                        pos += Vector3.down * (0.04f * k);
                        if (t > 1f && Mathf.Repeat(t, 1.1f) < 0.3f)
                        {
                            pos += Random.insideUnitSphere * 0.012f;
                            rot *= Quaternion.Euler(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                        }
                    }
                    break;
            }
            if (heldGlass != null) rot *= Quaternion.Euler(9f, 0f, 0f);   // se inclina hacia el vaso
            pivot.localPosition = pos; pivot.localRotation = rot;

            UpdateHead(dt, t);
            UpdateBlink();
            UpdateSymptoms(dt, t);

            // Brazos
            GestureTargets(t, out var tl, out var tr);
            float follow = 1f - Mathf.Exp(-(heldGlass != null ? 22f : 9f) * dt);
            handLPos = Vector3.Lerp(handLPos, tl, follow);
            handRPos = Vector3.Lerp(handRPos, tr, follow);
            SolveArms();
            if (CurrentReaction == null) ApplyBrows();

            if (eyes == Eyes.Dizzy)
            {
                var o = new Vector3(Mathf.Cos(t * 9f), Mathf.Sin(t * 9f), 0f) * 0.016f;
                pupilL.localPosition = pupilLBase + o; pupilR.localPosition = pupilRBase - o;
            }
            if (stars.gameObject.activeSelf) stars.Rotate(0f, 220f * dt, 0f, Space.Self);
            if (teeth.gameObject.activeSelf) teeth.localPosition = teethBase + Vector3.up * (Mathf.Sin(t * 50f) * 0.006f);
            if (flame.gameObject.activeSelf) flame.localScale = flameScale * (1f + Mathf.Sin(t * 25f) * 0.18f);
        }

        /// <summary>Detalles que laten, tiemblan o gotean mientras dura la reacción.</summary>
        void UpdateSymptoms(float dt, float t)
        {
            head.localScale = Vector3.Lerp(head.localScale, HeadScale(t), 1f - Mathf.Exp(-8f * dt));

            if (bulging)
            {
                // Ojos que se salen y laten; uno con tic
                float beat = 1.22f + 0.1f * Mathf.Abs(Mathf.Sin(t * 8f));
                float tic = Mathf.PerlinNoise(t * 7f, 0.3f) > 0.68f ? 0.82f : 1f;
                whiteL.localScale = whiteScale * beat;
                whiteR.localScale = whiteScale * (beat * tic);
            }
            if (throbbing && foreheadVein)
                foreheadVein.localScale = veinScale * (1f + 0.35f * Mathf.Abs(Mathf.Sin(t * 8f)));
            if (lips && lips.gameObject.activeSelf && CurrentReaction != ReactionId.Congelado)
            {
                float baseScale = CurrentReaction == ReactionId.Infierno ? 1.35f : 1f;
                lips.localScale = lipsScale * (baseScale * (1f + 0.07f * Mathf.Sin(t * 9f)));
            }
            if (tongueLong && tongueLong.gameObject.activeSelf)
            {
                tongueLong.localPosition = tongueLongPos + Vector3.up * (Mathf.Sin(t * 13f) * 0.006f);
                tongueLong.localRotation = Quaternion.Euler(18f + Mathf.Sin(t * 13f) * 10f, 0f, Mathf.Sin(t * 6f) * 8f);
            }
            if (snotBubble && snotBubble.gameObject.activeSelf)
            {
                // Se infla despacio y revienta
                float ph = Mathf.Repeat(t, 2.4f) / 2.4f;
                snotBubble.localScale = snotScale * (ph < 0.92f ? 0.25f + ph * 1.5f : 0.05f);
            }
            if (onFire)
            {
                // Llamas en la cabeza; a los dos segundos se apagan y queda el desastre
                if (headFlames) headFlames.localScale = headFlamesScale * (1f + 0.22f * Mathf.Sin(t * 27f) + 0.1f * Mathf.Sin(t * 41f));
                if (t > 2f)
                {
                    onFire = false;
                    if (headFlames) headFlames.gameObject.SetActive(false);
                    if (fxFire) fxFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    Show(soot); browsBurnt = true; ApplyBrows();
                    body = Body.Cough; gesture = Gesture.Throat; ApplyMouth(Mouth.O);
                    Play(fxSmoke);
                }
            }
            if (burpAt > 0f && t >= burpAt)
            {
                burpAt = 0f;
                Play(fxBurp);
            }
        }

        /// <summary>La cabeza sigue al jugador (con límites) salvo que esté desmayado.</summary>
        void UpdateHead(float dt, float t)
        {
            Quaternion want = Quaternion.identity;
            var cam = Camera.main;
            if (cam != null && body != Body.Faint && body != Body.Sleep && eyes != Eyes.Dizzy && !heaving)
            {
                Vector3 local = pivot.InverseTransformPoint(cam.transform.position) - headPos;
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -32f, 32f);
                float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -16f, 16f);
                want = Quaternion.Euler(pitch, yaw, 0f);
            }
            if (body == Body.Wobble) want *= Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.5f) * 10f);
            if (body == Body.Faint) want = Quaternion.Euler(0f, 0f, 12f);
            if (body == Body.Sleep) want = Quaternion.Euler(20f + Mathf.Sin(t * 1.2f) * 4f, 0f, 9f);
            if (heaving) want = Quaternion.Euler(22f, 0f, 0f);
            head.localRotation = Quaternion.Slerp(head.localRotation, want, 1f - Mathf.Exp(-(heaving ? 14f : 5f) * dt));
        }

        void UpdateBlink()
        {
            if (eyes != Eyes.Normal) return;
            float now = Time.time;
            if (now >= blinkAt && blinkUntil <= 0f) { blinkUntil = now + 0.11f; eyesOpen.SetActive(false); eyesFlat.SetActive(true); }
            if (blinkUntil > 0f && now >= blinkUntil)
            {
                blinkUntil = 0f; blinkAt = now + Random.Range(2.2f, 5f);
                eyesOpen.SetActive(true); eyesFlat.SetActive(false);
            }
        }
    }
}
