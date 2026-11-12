using UnityEngine;
using SB = QualityControlXR.EditorTools.SceneBuilder;

namespace QualityControlXR.EditorTools
{
    /// <summary>
    /// Piezas de los síntomas crudos del cliente (las activa Customer según la reacción):
    /// ojos inyectados, vena de la frente, labios hinchados, lengua afuera, ojeras, ronchas,
    /// hollín, bilis, espuma, mocos y las partículas de vómito, lágrimas, babas, vapor y tos.
    /// </summary>
    public static partial class MixSceneBuilder
    {
        const float SkullR = 0.18f;

        /// <summary>Mancha aplastada pegada a la superficie de la cara en (x, y).</summary>
        static GameObject OnFace(Transform group, float x, float y, float size, Material mat, float lift = 0f)
        {
            float z = Mathf.Sqrt(Mathf.Max(0.0004f, SkullR * SkullR - x * x - y * y));
            var p = new Vector3(x, y, z);
            var go = L(PrimitiveType.Sphere, "Spot", group, p * (1f + lift / SkullR) - p.normalized * 0.003f, new Vector3(size, size, 0.014f), mat);
            go.transform.localRotation = Quaternion.LookRotation(p.normalized);
            return go;
        }

        static void BuildSymptoms(Transform head)
        {
            var vein = SB.Mat("SymptomVein", new Color(0.78f, 0.06f, 0.1f));
            var purple = SB.Mat("SymptomBruise", new Color(0.5f, 0.33f, 0.5f));
            var lip = SB.Mat("SymptomLips", Color.white);
            var rash = SB.Mat("SymptomRash", new Color(0.8f, 0.16f, 0.18f));
            var sootM = SB.Mat("SymptomSoot", new Color(0.07f, 0.07f, 0.08f));
            var bileM = SB.Mat("SymptomBile", new Color(0.5f, 0.68f, 0.14f));
            var white = SB.Mat("TabletWhite", Color.white);
            var ice = SB.Mat("IceCube", new Color(0.85f, 0.96f, 1f));
            var snot = SB.Mat("SymptomSnot", new Color(0.7f, 0.9f, 0.5f, 0.6f), transparent: true);
            var tongueM = SB.Mat("Tongue", new Color(1f, 0.48f, 0.55f));

            // Ojos inyectados: venitas rojas en el borde de cada ojo
            var bloodshot = Group("Bloodshot", head, Vector3.zero);
            float[] angles = { 25f, 155f, -35f, -145f, 95f };
            foreach (float cx in new[] { -0.07f, 0.07f })
                foreach (float a in angles)
                {
                    float r = a * Mathf.Deg2Rad;
                    L(PrimitiveType.Cube, "Vein", bloodshot, new Vector3(cx + Mathf.Cos(r) * 0.036f, 0.03f + Mathf.Sin(r) * 0.036f, 0.18f),
                        new Vector3(0.024f, 0.005f, 0.006f), vein, a);
                }

            // Vena de la frente (late)
            var fv = Group("ForeheadVein", head, new Vector3(-0.105f, 0.112f, 0.094f));
            fv.localRotation = Quaternion.LookRotation(new Vector3(-0.105f, 0.112f, 0.094f).normalized);
            L(PrimitiveType.Cube, "A", fv, new Vector3(-0.016f, -0.014f, 0f), new Vector3(0.03f, 0.009f, 0.01f), vein, 52f);
            L(PrimitiveType.Cube, "B", fv, new Vector3(0f, 0f, 0f), new Vector3(0.03f, 0.009f, 0.01f), vein, -38f);
            L(PrimitiveType.Cube, "C", fv, new Vector3(0.016f, 0.014f, 0f), new Vector3(0.03f, 0.009f, 0.01f), vein, 52f);

            // Labios hinchados (el color lo pone Customer)
            var lips = Group("Lips", head, new Vector3(0f, -0.092f, 0.158f));
            L(PrimitiveType.Sphere, "Upper", lips, new Vector3(0f, 0.03f, 0.004f), new Vector3(0.1f, 0.032f, 0.036f), lip);
            L(PrimitiveType.Sphere, "Lower", lips, new Vector3(0f, -0.034f, 0.004f), new Vector3(0.112f, 0.042f, 0.042f), lip);

            // Lengua afuera (jadeo)
            L(PrimitiveType.Capsule, "TongueLong", head, new Vector3(0f, -0.132f, 0.172f), new Vector3(0.05f, 0.052f, 0.022f), tongueM);

            // Ojeras
            var bags = Group("EyeBags", head, Vector3.zero);
            L(PrimitiveType.Sphere, "L", bags, new Vector3(-0.07f, -0.024f, 0.157f), new Vector3(0.082f, 0.03f, 0.03f), purple);
            L(PrimitiveType.Sphere, "R", bags, new Vector3(0.07f, -0.024f, 0.157f), new Vector3(0.082f, 0.03f, 0.03f), purple);

            // Ronchas
            var hives = Group("Hives", head, Vector3.zero);
            Vector3[] rashSpots =
            {
                new Vector3(-0.125f, -0.02f, 0.04f), new Vector3(0.128f, 0f, 0.035f), new Vector3(-0.1f, -0.09f, 0.03f), new Vector3(0.09f, -0.1f, 0.038f),
                new Vector3(-0.035f, 0.105f, 0.03f), new Vector3(0.135f, 0.065f, 0.028f), new Vector3(-0.14f, 0.055f, 0.032f), new Vector3(0.03f, -0.145f, 0.026f),
            };
            foreach (var s in rashSpots) OnFace(hives, s.x, s.y, s.z, rash);

            // Hollín
            var soot = Group("Soot", head, Vector3.zero);
            Vector3[] sootSpots =
            {
                new Vector3(-0.1f, 0.085f, 0.085f), new Vector3(0.105f, 0.08f, 0.09f), new Vector3(-0.115f, -0.06f, 0.1f), new Vector3(0.115f, -0.065f, 0.09f),
                new Vector3(0f, -0.145f, 0.1f), new Vector3(0.02f, 0.11f, 0.07f), new Vector3(-0.15f, 0.01f, 0.07f),
            };
            foreach (var s in sootSpots) OnFace(soot, s.x, s.y, s.z, sootM);

            // Bilis en la barbilla después de vomitar
            var bile = Group("Bile", head, Vector3.zero);
            OnFace(bile, 0f, -0.132f, 0.05f, bileM, 0.004f);
            OnFace(bile, -0.035f, -0.148f, 0.035f, bileM, 0.004f);
            OnFace(bile, 0.04f, -0.122f, 0.03f, bileM, 0.004f);
            L(PrimitiveType.Capsule, "Drip", bile, new Vector3(0.008f, -0.19f, 0.09f), new Vector3(0.024f, 0.036f, 0.024f), bileM);

            // Espuma en la boca
            var foam = Group("Foam", head, Vector3.zero);
            Vector3[] bubbles =
            {
                new Vector3(-0.04f, -0.095f, 0.03f), new Vector3(-0.012f, -0.108f, 0.038f), new Vector3(0.02f, -0.1f, 0.032f), new Vector3(0.045f, -0.09f, 0.026f),
                new Vector3(0.0f, -0.126f, 0.03f), new Vector3(0.034f, -0.122f, 0.024f), new Vector3(-0.03f, -0.126f, 0.022f),
            };
            foreach (var b in bubbles)
            {
                float z = Mathf.Sqrt(Mathf.Max(0.0004f, SkullR * SkullR - b.x * b.x - b.y * b.y));
                L(PrimitiveType.Sphere, "Bubble", foam, new Vector3(b.x, b.y, z + 0.006f), Vector3.one * b.z, white);
            }

            // Mocos: carámbano (frío) y burbuja (dormido)
            L(PrimitiveType.Capsule, "SnotIcicle", head, new Vector3(-0.012f, -0.074f, 0.186f), new Vector3(0.015f, 0.034f, 0.015f), ice);
            L(PrimitiveType.Sphere, "SnotBubble", head, new Vector3(0.016f, -0.052f, 0.213f), Vector3.one * 0.06f, snot);

            // Pelo en llamas (llamarada) y sangrado de nariz
            var flameM = SB.Mat("HeadFlame", new Color(0.95f, 0.26f, 0.04f), emissive: true);
            var coreM = SB.Mat("FlameCore", new Color(1f, 0.62f, 0.12f), emissive: true);
            var flames = Group("HeadFlames", head, new Vector3(0f, 0.19f, -0.02f));
            L(PrimitiveType.Sphere, "Mid", flames, new Vector3(0f, 0.1f, 0f), new Vector3(0.17f, 0.32f, 0.15f), flameM);
            L(PrimitiveType.Sphere, "Left", flames, new Vector3(-0.1f, 0.05f, 0f), new Vector3(0.11f, 0.21f, 0.1f), flameM, 14f);
            L(PrimitiveType.Sphere, "Right", flames, new Vector3(0.1f, 0.06f, 0.02f), new Vector3(0.1f, 0.23f, 0.1f), flameM, -12f);
            L(PrimitiveType.Sphere, "Back", flames, new Vector3(0.02f, 0.06f, -0.09f), new Vector3(0.12f, 0.2f, 0.1f), flameM);
            L(PrimitiveType.Sphere, "Core", flames, new Vector3(0f, 0.06f, 0.02f), new Vector3(0.09f, 0.19f, 0.08f), coreM);
            Fx("FX_HeadFire", head, new Vector3(0f, 0.2f, 0f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.9f, 0.3f), 42f, 0.6f, 0.9f, 0.12f, -0.2f, ParticleSystemShapeType.Cone, 0.1f);
            var blood = SB.Mat("SymptomBlood", new Color(0.6f, 0.02f, 0.05f));
            var bleed = Group("NoseBleed", head, Vector3.zero);
            L(PrimitiveType.Capsule, "L", bleed, new Vector3(-0.012f, -0.062f, 0.186f), new Vector3(0.012f, 0.022f, 0.012f), blood);
            L(PrimitiveType.Capsule, "R", bleed, new Vector3(0.014f, -0.07f, 0.185f), new Vector3(0.012f, 0.03f, 0.012f), blood);
            Fx("FX_Blood", head, new Vector3(0.005f, -0.085f, 0.19f), new Color(0.6f, 0.02f, 0.05f), new Color(0.75f, 0.05f, 0.08f), 3f, 0.7f, 0.02f, 0.02f, 0.8f, ParticleSystemShapeType.Sphere, 0.006f)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Partículas (las que salen "hacia adelante" se orientan a mano; por defecto Fx emite hacia arriba)
            var green = new Color(0.52f, 0.7f, 0.14f); var yellow = new Color(0.8f, 0.76f, 0.28f);
            Fx("FX_Vomit", head, new Vector3(0f, -0.095f, 0.19f), green, yellow, 0f, 0.75f, 1.9f, 0.06f, 1.6f, ParticleSystemShapeType.Cone, 0.02f)
                .transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            var tear = new Color(0.6f, 0.85f, 1f);
            foreach (float s in new[] { -1f, 1f })
            {
                Fx(s < 0 ? "FX_TearsL" : "FX_TearsR", head, new Vector3(s * 0.07f, 0f, 0.182f), tear, Color.white, 9f, 0.55f, 0.15f, 0.022f, 1f, ParticleSystemShapeType.Sphere, 0.008f)
                    .transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                Fx(s < 0 ? "FX_EarSteamL" : "FX_EarSteamR", head, new Vector3(s * 0.2f, 0.01f, 0f), Color.white, new Color(1f, 1f, 1f, 0.6f), 34f, 0.45f, 1.3f, 0.07f, -0.1f, ParticleSystemShapeType.Cone, 0.01f)
                    .transform.localRotation = Quaternion.Euler(0f, s * 90f, 0f);
            }
            Fx("FX_Drool", head, new Vector3(0.035f, -0.105f, 0.166f), new Color(0.85f, 0.92f, 0.95f, 0.85f), new Color(1f, 1f, 1f, 0.7f), 6f, 0.8f, 0.03f, 0.022f, 0.6f, ParticleSystemShapeType.Sphere, 0.005f)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Fx("FX_Burp", head, new Vector3(0f, -0.09f, 0.2f), new Color(0.6f, 0.8f, 0.3f, 0.55f), new Color(0.75f, 0.85f, 0.4f, 0.4f), 0f, 1.3f, 0.45f, 0.16f, -0.04f, ParticleSystemShapeType.Cone, 0.03f, 16)
                .transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            Fx("FX_Cough", head, new Vector3(0f, -0.09f, 0.2f), new Color(0.22f, 0.22f, 0.24f, 0.8f), new Color(0.45f, 0.45f, 0.47f, 0.6f), 0f, 0.8f, 0.9f, 0.1f, -0.06f, ParticleSystemShapeType.Cone, 0.02f)
                .transform.localRotation = Quaternion.identity;
        }
    }
}
