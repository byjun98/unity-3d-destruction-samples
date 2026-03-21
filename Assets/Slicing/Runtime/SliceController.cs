using System.Collections.Generic;
using UnityEngine;

namespace Slicing
{
    public enum SliceMode { Slash, Punch }

    public class SliceController : MonoBehaviour
    {
        [Header("References")]
        public Camera cam;

        [Header("Slash (left mouse drag)")]
        public float slashTrailDepth = 8f;
        public Color slashColor = new Color(0.4f, 0.85f, 1f, 1f);
        public float slashWidth = 0.06f;
        public float slashLifetime = 0.18f;

        [Header("Punch (right mouse click)")]
        public float punchRayLength = 100f;
        public LayerMask punchLayers = ~0;

        [Header("FX")]
        public GameObject impactPrefab;

        public SliceMode Mode { get; set; } = SliceMode.Slash;
        public int SliceCount { get; private set; }

        public void ResetCount() => SliceCount = 0;

        Vector3 _dragStartScreen;
        bool _dragging;
        LineRenderer _previewLine;

        void Reset() => cam = Camera.main;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            EnsurePreviewLine();
        }

        void EnsurePreviewLine()
        {
            if (_previewLine != null) return;
            var go = new GameObject("SlashPreview");
            go.transform.SetParent(transform, false);
            _previewLine = go.AddComponent<LineRenderer>();
            _previewLine.positionCount = 0;
            _previewLine.widthMultiplier = slashWidth;
            _previewLine.useWorldSpace = true;
            _previewLine.numCapVertices = 4;
            _previewLine.numCornerVertices = 4;
            var mat = new Material(SliceShaders.ParticleUnlit());
            SliceShaders.SetTint(mat, slashColor);
            _previewLine.material = mat;
            _previewLine.startColor = slashColor;
            _previewLine.endColor = new Color(slashColor.r, slashColor.g, slashColor.b, 0f);
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            HandleModeKeys();

            if (Mode == SliceMode.Slash)
                HandleSlash();
            else
                HandlePunch();
        }

        void HandleModeKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Mode = SliceMode.Slash;
            if (Input.GetKeyDown(KeyCode.Alpha2)) Mode = SliceMode.Punch;
            if (Input.GetKeyDown(KeyCode.R)) RebuildArena();
        }

        void RebuildArena()
        {
            var bootstrap = FindObjectOfType<SliceArenaBootstrap>();
            if (bootstrap != null) bootstrap.Rebuild();
        }

        void HandleSlash()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _dragStartScreen = Input.mousePosition;
                _previewLine.positionCount = 2;
                Vector3 wp = ScreenToTrailWorld(_dragStartScreen);
                _previewLine.SetPosition(0, wp);
                _previewLine.SetPosition(1, wp);
            }
            else if (_dragging && Input.GetMouseButton(0))
            {
                _previewLine.SetPosition(1, ScreenToTrailWorld(Input.mousePosition));
            }
            else if (_dragging && Input.GetMouseButtonUp(0))
            {
                _dragging = false;
                Vector3 endScreen = Input.mousePosition;
                if (Vector3.Distance(_dragStartScreen, endScreen) > 12f)
                {
                    SliceWithSlash(_dragStartScreen, endScreen);
                }
                StartCoroutine(FadeAndClearLine());
            }
        }

        void HandlePunch()
        {
            if (!Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(0)) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, punchRayLength, punchLayers)) return;

            var target = hit.collider.GetComponentInParent<SliceTarget>();
            if (target == null) return;

            var plane = SlicePlane.FromNormalAndPoint(hit.normal, hit.point);
            SpawnImpact(hit.point, hit.normal);
            if (target.TrySlice(plane)) SliceCount++;
        }

        void SliceWithSlash(Vector3 startScreen, Vector3 endScreen)
        {
            Ray r1 = cam.ScreenPointToRay(startScreen);
            Ray r2 = cam.ScreenPointToRay(endScreen);
            Vector3 normal = Vector3.Cross(r1.direction, r2.direction).normalized;
            if (normal.sqrMagnitude < 1e-6f) return;

            // Plane passes through the camera position (and both rays).
            var plane = SlicePlane.FromNormalAndPoint(normal, cam.transform.position);

            int countBefore = SliceCount;
            var hits = new List<SliceTarget>();
            foreach (var t in FindObjectsOfType<SliceTarget>())
                hits.Add(t);

            // Visual flash at midpoint hit
            Vector3 midScreen = (startScreen + endScreen) * 0.5f;
            Ray midRay = cam.ScreenPointToRay(midScreen);
            if (Physics.Raycast(midRay, out var midHit, 200f))
                SpawnImpact(midHit.point, normal);

            foreach (var t in hits)
            {
                if (t == null) continue;
                if (t.TrySlice(plane)) SliceCount++;
            }
        }

        void SpawnImpact(Vector3 worldPos, Vector3 worldNormal)
        {
            if (impactPrefab == null) return;
            var rot = Quaternion.LookRotation(worldNormal);
            var fx = Instantiate(impactPrefab, worldPos, rot);
            fx.SetActive(true);
            Destroy(fx, 3f);
        }

        Vector3 ScreenToTrailWorld(Vector3 screenPos)
        {
            Vector3 sp = screenPos;
            sp.z = slashTrailDepth;
            return cam.ScreenToWorldPoint(sp);
        }

        System.Collections.IEnumerator FadeAndClearLine()
        {
            float t = 0f;
            Color c0 = slashColor;
            while (t < slashLifetime)
            {
                t += Time.deltaTime;
                float k = 1f - t / slashLifetime;
                _previewLine.startColor = new Color(c0.r, c0.g, c0.b, k);
                _previewLine.endColor = new Color(c0.r, c0.g, c0.b, 0f);
                yield return null;
            }
            _previewLine.positionCount = 0;
        }
    }
}
