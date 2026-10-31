using UnityEngine;

namespace NightCourier
{
    // Only the label scales; its layout slot and neighbouring controls stay fixed.
    public sealed class CourierValuePulse : MonoBehaviour
    {
        private Vector3 home;
        private bool captured;
        private float age = 1;
        public bool IsPlaying { get { return age < .32f; } }
        private void Awake() { home = transform.localScale; captured = true; }
        public void Play() { if(!captured){home=transform.localScale;captured=true;}age = 0; }
        public void ResetPulse() { age = 1; if(captured)transform.localScale = home; }
        private void Update()
        {
            if (!IsPlaying) return;
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / .32f);
            transform.localScale = home * (1 + .18f * Mathf.Sin(t * Mathf.PI) * (1 - t));
        }
        private void OnDisable() { ResetPulse(); }
    }
}
