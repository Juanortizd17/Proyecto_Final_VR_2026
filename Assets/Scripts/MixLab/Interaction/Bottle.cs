using System.Collections;
using UnityEngine;

namespace MixLab
{
    /// <summary>Cómo llega el ingrediente al vaso.</summary>
    public enum PourStyle
    {
        /// <summary>La botella vuela sobre el vaso, se inclina y sale un chorro.</summary>
        Liquid,
        /// <summary>El recipiente se queda en su sitio y saltan piezas al vaso (hielo, rodajas, pastillas).</summary>
        Pieces
    }

    public enum PieceShape { Cube, Disc, Stick }

    /// <summary>Ingrediente del estante: botella que se vierte o recipiente del que se sacan piezas.</summary>
    public class Bottle : MixInteractable
    {
        [SerializeField] IngredientData ingredient;
        [SerializeField] MixGlass glass;
        [SerializeField] GameObject label;
        [SerializeField] float mouthHeight = 0.24f;
        [SerializeField] PourStyle style = PourStyle.Liquid;
        [SerializeField] PieceShape pieceShape = PieceShape.Cube;
        [SerializeField] int pieceCount = 1;
        [SerializeField] string verb = "Verter";

        static bool anyPouring;
        Vector3 home;
        Quaternion homeRot;

        public IngredientData Ingredient => ingredient;
        public static bool AnyPouring => anyPouring;

        public void Setup(IngredientData data, MixGlass target, GameObject labelObject, float mouth)
        {
            ingredient = data; glass = target; label = labelObject; mouthHeight = mouth;
        }

        public void SetPieces(PieceShape shape, int count, string actionVerb)
        {
            style = PourStyle.Pieces; pieceShape = shape; pieceCount = Mathf.Max(1, count); verb = actionVerb;
        }

        void Awake()
        {
            home = transform.position; homeRot = transform.rotation;
            anyPouring = false;
        }

        /// <summary>Durante el apagón se ocultan las etiquetas: toca reconocer por forma y color.</summary>
        public void SetLabelVisible(bool visible) { if (label) label.SetActive(visible); }

        public override string Prompt(MixInteractor hand)
        {
            if (anyPouring || glass.Busy) return null;
            if (hand.HoldingGlass || !glass.AtHome) return "Deja el vaso en la estación para servir ingredientes";
            if (glass.IsFull) return "El vaso está lleno (máximo 4)";
            return $"{verb} {ingredient.displayName} <size=16><color=#B6A8D6>({ingredient.tag})</color></size>";
        }

        public override bool CanInteract(MixInteractor hand) =>
            !anyPouring && !glass.Busy && glass.AtHome && !hand.HoldingGlass && !glass.IsFull;

        public override void Interact(MixInteractor hand) => Pour();

        public bool Pour()
        {
            if (anyPouring || glass.Busy || !glass.AtHome || glass.IsFull) return false;
            StartCoroutine(style == PourStyle.Pieces ? PiecesRoutine() : PourRoutine());
            return true;
        }

        // ---------- Líquidos ----------

        IEnumerator PourRoutine()
        {
            anyPouring = true;
            Vector3 rim = glass.HomePosition + Vector3.up * 0.16f;
            Vector3 dir = glass.HomePosition - home; dir.y = 0f;
            dir = dir.sqrMagnitude < 0.001f ? Vector3.forward : dir.normalized;
            Vector3 axis = Vector3.Cross(Vector3.up, dir);
            const float tilt = 118f;
            float rad = tilt * Mathf.Deg2Rad;
            // Con la botella inclinada, la boca queda justo encima del vaso.
            Vector3 pourPos = rim + Vector3.up * (0.1f - mouthHeight * Mathf.Cos(rad)) - dir * (mouthHeight * Mathf.Sin(rad));
            Quaternion pourRot = Quaternion.AngleAxis(tilt, axis) * homeRot;

            yield return Move(transform, home, pourPos, homeRot, homeRot, 0.28f, 0.12f);
            yield return Move(transform, pourPos, pourPos, homeRot, pourRot, 0.2f, 0f);

            var stream = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(stream.GetComponent<Collider>());
            Vector3 mouth = rim + Vector3.up * 0.1f;
            stream.transform.position = (mouth + glass.HomePosition) * 0.5f + Vector3.up * 0.03f;
            stream.transform.localScale = new Vector3(0.014f, (mouth.y - glass.HomePosition.y) * 0.5f, 0.014f);
            stream.GetComponent<Renderer>().material.color = ingredient.color;
            yield return new WaitForSeconds(0.2f);
            glass.Add(ingredient);
            yield return new WaitForSeconds(0.22f);
            Destroy(stream);

            yield return Move(transform, pourPos, pourPos, pourRot, homeRot, 0.16f, 0f);
            yield return Move(transform, pourPos, home, homeRot, homeRot, 0.25f, 0.1f);
            anyPouring = false;
        }

        // ---------- Piezas (hielo, rodajas, pastillas) ----------

        IEnumerator PiecesRoutine()
        {
            anyPouring = true;
            // El recipiente da un saltito y las piezas vuelan en arco hasta el vaso.
            StartCoroutine(Move(transform, home, home, homeRot, homeRot, 0.25f, 0.05f));
            Vector3 from = home + Vector3.up * (mouthHeight + 0.03f);
            Vector3 to = glass.HomePosition + Vector3.up * 0.1f;
            for (int i = 0; i < pieceCount; i++)
            {
                StartCoroutine(FlyPiece(from, to + new Vector3(Random.Range(-0.012f, 0.012f), 0f, Random.Range(-0.012f, 0.012f)), i == pieceCount - 1));
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(0.75f);
            anyPouring = false;
        }

        IEnumerator FlyPiece(Vector3 from, Vector3 to, bool last)
        {
            var piece = GameObject.CreatePrimitive(pieceShape == PieceShape.Cube ? PrimitiveType.Cube
                : pieceShape == PieceShape.Disc ? PrimitiveType.Cylinder : PrimitiveType.Capsule);
            Destroy(piece.GetComponent<Collider>());
            piece.transform.localScale =
                pieceShape == PieceShape.Cube ? Vector3.one * 0.034f :
                pieceShape == PieceShape.Disc ? new Vector3(0.05f, 0.006f, 0.05f) : new Vector3(0.018f, 0.03f, 0.018f);
            piece.GetComponent<Renderer>().material.color = ingredient.color;
            Quaternion spin = Random.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
            {
                piece.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.22f);
                piece.transform.rotation = Quaternion.AngleAxis(t * 540f, Vector3.right) * spin;
                yield return null;
            }
            Destroy(piece);
            if (last) glass.Add(ingredient);
        }

        static IEnumerator Move(Transform tr, Vector3 p0, Vector3 p1, Quaternion r0, Quaternion r1, float seconds, float arc)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float k = t * t * (3f - 2f * t);
                tr.position = Vector3.Lerp(p0, p1, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * arc);
                tr.rotation = Quaternion.Slerp(r0, r1, k);
                yield return null;
            }
            tr.SetPositionAndRotation(p1, r1);
        }
    }
}
