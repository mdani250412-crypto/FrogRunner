using UnityEngine;

namespace FrogRunner.RiverGlider
{
    /// <summary>
    /// Trigger volume under the river surface. The frog controller uses it as a second safety net
    /// behind the y-position check, so a fast fall can never skip the splash and respawn.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaterVolume : MonoBehaviour
    {
        public float WaterLevel = -0.8f;

        void Awake()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }
    }
}
