using UnityEngine;

namespace FrogRunner.RiverGlider
{
    /// <summary>
    /// Scrolls the water texture toward the camera. Two speeds are layered (main texture + ripples)
    /// by CourseBuilder through two scrolling surfaces if more motion is desired.
    /// TUNING: scrollSpeed is in UV units per second.
    /// </summary>
    public class WaterScroller : MonoBehaviour
    {
        public float scrollSpeed = 0.055f;

        Material _material;
        Renderer _renderer;

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _material = _renderer.material;
        }

        void Update()
        {
            if (_material == null) return;
            Vector2 offset = _material.mainTextureOffset;
            offset.y -= scrollSpeed * Time.deltaTime;
            offset.x += scrollSpeed * 0.12f * Time.deltaTime;
            _material.mainTextureOffset = offset;
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
