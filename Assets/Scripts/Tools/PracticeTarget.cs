using UnityEngine;

namespace QualityControlXR
{
    /// <summary>Caja de práctica del onboarding: avisa al tutorial cuando una herramienta la toca.</summary>
    public class PracticeTarget : MonoBehaviour
    {
        [SerializeField] OnboardingTutorial tutorial;
        public void Setup(OnboardingTutorial t) => tutorial = t;
        public void Inspected() { if (tutorial) tutorial.OnPracticeInspected(); }
    }
}
