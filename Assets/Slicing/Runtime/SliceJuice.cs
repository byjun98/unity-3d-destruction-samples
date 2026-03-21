using System.Collections;
using UnityEngine;

namespace Slicing
{
    // Adds "game feel" on each slice: brief slow-mo, camera kick, tiny screen shake.
    public class SliceJuice : MonoBehaviour
    {
        public SliceController controller;
        public Camera cam;

        [Header("Slow-mo")]
        public float slowMoScale = 0.18f;
        public float slowMoHold = 0.06f;
        public float slowMoRamp = 0.22f;

        [Header("Camera shake")]
        public float shakeAmplitude = 0.07f;
        public float shakeDuration = 0.25f;

        Vector3 _camHome;
        Quaternion _camRotHome;
        bool _hooked;
        bool _running;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (controller == null) controller = FindObjectOfType<SliceController>();
            HookIfReady();
        }

        void OnEnable() => HookIfReady();

        void HookIfReady()
        {
            if (_hooked || controller == null) return;
            _hooked = true;
        }

        int _lastCount;

        void LateUpdate()
        {
            if (controller == null) return;
            if (controller.SliceCount > _lastCount)
            {
                _lastCount = controller.SliceCount;
                Trigger();
            }
        }

        public void Trigger()
        {
            if (!_running) StartCoroutine(JuiceRoutine());
        }

        IEnumerator JuiceRoutine()
        {
            _running = true;

            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                _camHome = cam.transform.localPosition;
                _camRotHome = cam.transform.localRotation;
            }

            float originalFixed = Time.fixedDeltaTime;
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = originalFixed * slowMoScale;

            float t = 0f;
            while (t < slowMoHold)
            {
                t += Time.unscaledDeltaTime;
                ApplyShake(t / Mathf.Max(0.01f, slowMoHold + slowMoRamp));
                yield return null;
            }

            t = 0f;
            while (t < slowMoRamp)
            {
                t += Time.unscaledDeltaTime;
                float k = t / slowMoRamp;
                Time.timeScale = Mathf.Lerp(slowMoScale, 1f, k);
                Time.fixedDeltaTime = originalFixed * Mathf.Lerp(slowMoScale, 1f, k);
                ApplyShake((slowMoHold + t) / Mathf.Max(0.01f, slowMoHold + slowMoRamp));
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixed;
            if (cam != null)
            {
                cam.transform.localPosition = _camHome;
                cam.transform.localRotation = _camRotHome;
            }
            _running = false;
        }

        void ApplyShake(float lifetime01)
        {
            if (cam == null) return;
            float k = Mathf.Clamp01(1f - lifetime01);
            Vector3 jitter = new Vector3(
                (Mathf.PerlinNoise(Time.unscaledTime * 28f, 0.31f) - 0.5f),
                (Mathf.PerlinNoise(0.71f, Time.unscaledTime * 28f) - 0.5f),
                0f) * shakeAmplitude * k;
            cam.transform.localPosition = _camHome + jitter;
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
        }
    }
}
