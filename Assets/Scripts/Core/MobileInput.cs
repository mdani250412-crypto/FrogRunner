using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Touch/mouse fallback input for the on-screen runner controls.
    /// The HUD draws the same rectangles this class hit-tests, so mobile testing needs no extra UI setup.
    /// TUNING: edit the fractions in GetLeftRect/GetRightRect/GetJumpRect/GetDashRect.
    /// </summary>
    public static class MobileInput
    {
        public static float Horizontal { get; private set; }
        public static float Vertical { get; private set; }
        public static bool JumpHeld { get; private set; }
        public static bool JumpPressed { get; private set; }
        public static bool DashPressed { get; private set; }

        static bool _prevJump;

        public static void Tick(bool controlsActive)
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpHeld = false;
            JumpPressed = false;
            DashPressed = false;

            if (!controlsActive)
            {
                _prevJump = false;
                return;
            }

            float w = Screen.width;
            float h = Screen.height;
            Rect leftRect = GetLeftRect(w, h);
            Rect rightRect = GetRightRect(w, h);
            Rect upRect = GetUpRect(w, h);
            Rect downRect = GetDownRect(w, h);
            Rect jumpRect = GetJumpRect(w, h);
            Rect dashRect = GetDashRect(w, h);

            bool left = false, right = false, up = false, down = false, jump = false, dashDown = false;

            int touchCount = UnityEngine.Input.touchCount;
            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
                Vector2 p = touch.position;
                if (leftRect.Contains(p)) left = true;
                else if (rightRect.Contains(p)) right = true;
                else if (upRect.Contains(p)) up = true;
                else if (downRect.Contains(p)) down = true;
                else if (jumpRect.Contains(p)) jump = true;
                else if (dashRect.Contains(p) && touch.phase == TouchPhase.Began) dashDown = true;
            }

            // Editor/mouse fallback so touch controls can be tested without a device.
            if (UnityEngine.Input.GetMouseButton(0))
            {
                Vector2 p = UnityEngine.Input.mousePosition;
                if (leftRect.Contains(p)) left = true;
                else if (rightRect.Contains(p)) right = true;
                else if (upRect.Contains(p)) up = true;
                else if (downRect.Contains(p)) down = true;
                else if (jumpRect.Contains(p)) jump = true;
                else if (dashRect.Contains(p) && UnityEngine.Input.GetMouseButtonDown(0)) dashDown = true;
            }

            Horizontal = (right ? 1f : 0f) - (left ? 1f : 0f);
            Vertical = (up ? 1f : 0f) - (down ? 1f : 0f);
            JumpHeld = jump;
            if (jump && !_prevJump) JumpPressed = true;
            _prevJump = jump;
            DashPressed = dashDown;
        }

        public static void Clear()
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpHeld = false;
            JumpPressed = false;
            DashPressed = false;
            _prevJump = false;
        }

        public static Rect GetLeftRect(float w, float h)  => new Rect(w * 0.03f, h * 0.72f, w * 0.10f, h * 0.14f);
        public static Rect GetRightRect(float w, float h) => new Rect(w * 0.15f, h * 0.72f, w * 0.10f, h * 0.14f);
        public static Rect GetUpRect(float w, float h)    => new Rect(w * 0.09f, h * 0.56f, w * 0.10f, h * 0.14f);
        public static Rect GetDownRect(float w, float h)  => new Rect(w * 0.09f, h * 0.88f, w * 0.10f, h * 0.10f);
        public static Rect GetJumpRect(float w, float h)  => new Rect(w * 0.82f, h * 0.66f, w * 0.15f, h * 0.24f);
        public static Rect GetDashRect(float w, float h)  => new Rect(w * 0.66f, h * 0.70f, w * 0.13f, h * 0.18f);
    }
}
