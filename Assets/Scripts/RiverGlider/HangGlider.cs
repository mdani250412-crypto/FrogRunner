using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.RiverGlider
{
    /// <summary>
    /// Procedural hang glider that folds out of the frog's backpack while Space is held in the air.
    /// It is built from the shared low-poly meshes so it can later be swapped for an imported model:
    /// replace Build() and keep SetDeployed()/the root transform shape.
    /// TUNING: deploySpeed and canopy size control the visual timing and silhouette.
    /// </summary>
    public class HangGlider : MonoBehaviour
    {
        [SerializeField] private float deploySpeed = 7.5f;

        Transform _canopy;
        Vector3 _canopyBasePosition;
        Quaternion _canopyBaseRotation = Quaternion.identity;
        Vector3 _basePosition;
        float _scale;
        float _swayTime;
        bool _deployed;
        bool _built;

        public bool IsDeployed => _deployed;

        public static HangGlider Build(Transform parent)
        {
            var root = new GameObject("HangGlider");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            var glider = root.AddComponent<HangGlider>();
            glider.Construct();
            glider.SetDeployed(false);
            glider.SnapStowed();
            return glider;
        }

        void Construct()
        {
            if (_built) return;
            _built = true;
            _basePosition = transform.localPosition;

            _canopy = new GameObject("Canopy").transform;
            _canopy.SetParent(transform, false);
            _canopy.localPosition = new Vector3(0f, 2.25f, 0.05f);
            _canopy.localRotation = Quaternion.Euler(-7f, 0f, 0f);
            _canopy.localScale = new Vector3(3.5f, 1f, 3.3f);
            _canopyBasePosition = _canopy.localPosition;
            _canopyBaseRotation = _canopy.localRotation;

            RetroBuilder.Wing("CanopyFabric", _canopy, Vector3.zero, Vector3.one, RetroAssets.Glider);

            // A-frame, keel and side struts.
            RetroBuilder.Cylinder("CrossBar", transform, new Vector3(0f, 2.02f, 0.35f),
                new Vector3(0.055f, 3.05f, 0.055f), RetroAssets.Metal, false, true, false,
                Quaternion.Euler(0f, 0f, 90f));
            RetroBuilder.Cylinder("KeelBar", transform, new Vector3(0f, 2.02f, 0.15f),
                new Vector3(0.05f, 1.9f, 0.05f), RetroAssets.Metal, false, true, false,
                Quaternion.Euler(90f, 0f, 0f));
            RetroBuilder.Cylinder("StrutL", transform, new Vector3(-0.45f, 1.55f, 0.25f),
                new Vector3(0.04f, 1.15f, 0.04f), RetroAssets.Metal, false, true, false,
                Quaternion.Euler(24f, 0f, -27f));
            RetroBuilder.Cylinder("StrutR", transform, new Vector3(0.45f, 1.55f, 0.25f),
                new Vector3(0.04f, 1.15f, 0.04f), RetroAssets.Metal, false, true, false,
                Quaternion.Euler(24f, 0f, 27f));

            // Ropes from the canopy tips down to the frog's hands.
            Rope("RopeL", new Vector3(-0.52f, 1.02f, 0.28f), new Vector3(-1.65f, 2.12f, 0.06f));
            Rope("RopeR", new Vector3(0.52f, 1.02f, 0.28f), new Vector3(1.65f, 2.12f, 0.06f));
            Rope("RopeC", new Vector3(0f, 1.02f, 0.36f), new Vector3(0f, 2.1f, -0.6f));
        }

        void Rope(string name, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            RetroBuilder.Cylinder(name, transform, (from + to) * 0.5f,
                new Vector3(0.025f, delta.magnitude, 0.025f), RetroAssets.Metal, false, false, false,
                Quaternion.FromToRotation(Vector3.up, delta.normalized));
        }

        public void SetDeployed(bool deployed)
        {
            _deployed = deployed;
            if (deployed && !gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void SnapStowed()
        {
            _scale = 0f;
            transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_built) return;

            float target = _deployed ? 1f : 0f;
            _scale = Mathf.MoveTowards(_scale, target, deploySpeed * Time.deltaTime);
            if (_scale <= 0.001f)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            transform.localScale = new Vector3(_scale, _scale, _scale);

            // Gentle canopy sway plus a little flex while gliding.
            _swayTime += Time.deltaTime * 2.2f;
            if (_canopy != null)
            {
                _canopy.localPosition = _canopyBasePosition + new Vector3(0f, Mathf.Sin(_swayTime) * 0.025f, 0f);
                _canopy.localRotation = _canopyBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(_swayTime * 0.7f) * 1.6f);
            }
        }
    }
}
