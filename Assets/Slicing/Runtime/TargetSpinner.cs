using UnityEngine;

namespace Slicing
{
    public class TargetSpinner : MonoBehaviour
    {
        public Vector3 axis = new Vector3(0.2f, 1f, 0.1f);
        public float speed = 28f;
        public float bobAmplitude = 0.08f;
        public float bobFrequency = 0.6f;

        Vector3 _origin;
        float _phase;

        void Awake()
        {
            _origin = transform.position;
            _phase = Random.value * Mathf.PI * 2f;
            axis.Normalize();
        }

        void Update()
        {
            transform.Rotate(axis * speed * Time.deltaTime, Space.World);
            float y = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + _phase) * bobAmplitude;
            var p = transform.position;
            p.y = _origin.y + y;
            transform.position = p;
        }
    }
}
