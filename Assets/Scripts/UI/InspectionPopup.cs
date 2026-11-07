using UnityEngine;
using UnityEngine.UI;

namespace QualityControlXR
{
    /// <summary>
    /// Texto flotante en el mundo (resultado de una inspección): sube, mira a la cámara y se desvanece.
    /// </summary>
    public class InspectionPopup : MonoBehaviour
    {
        const float Life = 1.8f;
        Text text;
        Outline outline;
        float t;
        Vector3 start;

        public static void Show(Vector3 worldPos, string message, Color color)
        {
            var go = new GameObject("InspectionPopup", typeof(RectTransform), typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(700, 120);
            rt.localScale = Vector3.one * 0.0022f;
            rt.position = worldPos;

            var label = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            label.rectTransform.SetParent(rt, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 56; label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.text = message; label.color = color; label.raycastTarget = false;
            var o = label.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.85f); o.effectDistance = new Vector2(3f, -3f);

            var popup = go.AddComponent<InspectionPopup>();
            popup.text = label; popup.outline = o; popup.start = worldPos;
        }

        void LateUpdate()
        {
            t += Time.deltaTime;
            float k = t / Life;
            transform.position = start + Vector3.up * (0.25f * Mathf.Sqrt(k));
            var cam = Camera.main;
            if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            float a = k < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.7f) / 0.3f);
            var c = text.color; c.a = a; text.color = c;
            var oc = outline.effectColor; oc.a = 0.85f * a; outline.effectColor = oc;
            if (t >= Life) Destroy(gameObject);
        }
    }
}
