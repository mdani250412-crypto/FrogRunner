using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.UI
{
    /// <summary>Lightweight world-space floating player name above a frog. Faces camera, no canvas.</summary>
    [DisallowMultipleComponent]
    public class FloatingNameTag : MonoBehaviour
    {
        TextMesh _mesh;
        Transform _target;
        Camera _cam;
        float _nextLookup;
        public Vector3 Offset = new Vector3(0f, 2.15f, 0f);

        public static FloatingNameTag Attach(Transform target)
        {
            var go = new GameObject("NameTag");
            go.transform.SetParent(target, false);
            var tag = go.AddComponent<FloatingNameTag>();
            tag._target = target;
            tag.Init();
            return tag;
        }

        void Init()
        {
            _mesh = gameObject.AddComponent<TextMesh>();
            _mesh.anchor = TextAnchor.LowerCenter;
            _mesh.alignment = TextAlignment.Center;
            _mesh.fontSize = 48;
            _mesh.characterSize = 0.03f;
            _mesh.color = Color.white;
            _mesh.fontStyle = FontStyle.Bold;
            _mesh.text = "Frog";
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        void LateUpdate()
        {
            if (_mesh == null) return;
            if (_cam == null || Time.unscaledTime >= _nextLookup)
            {
                _cam = FindFirstObjectByType<Camera>();
                _nextLookup = Time.unscaledTime + 1f;
            }
            if (_target != null)
            {
                transform.position = _target.position + Offset;
                var cos = _target.GetComponent<PlayerCosmetics>();
                if (cos != null && !string.IsNullOrEmpty(cos.PlayerName))
                    _mesh.text = cos.PlayerName;
            }
            if (_cam != null)
            {
                // Billboard face camera
                Vector3 dir = transform.position - _cam.transform.position;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}