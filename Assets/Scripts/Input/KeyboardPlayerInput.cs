using UnityEngine;

namespace FrogRunner.Input
{
    public class KeyboardPlayerInput : IPlayerInput
    {
        public KeyCode left = KeyCode.A;
        public KeyCode right = KeyCode.D;
        public KeyCode forward = KeyCode.W;
        public KeyCode backward = KeyCode.S;
        public KeyCode altLeft = KeyCode.LeftArrow;
        public KeyCode altRight = KeyCode.RightArrow;
        public KeyCode altForward = KeyCode.UpArrow;
        public KeyCode altBackward = KeyCode.DownArrow;
        public KeyCode jump = KeyCode.Space;
        public KeyCode dash = KeyCode.LeftShift;
        public KeyCode altDash = KeyCode.RightShift;
        public KeyCode crouch = KeyCode.LeftControl;
        public KeyCode altCrouch = KeyCode.C;
        public bool useMobileFallback;

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool PausePressed { get; private set; }

        public void Tick(bool active)
        {
            Horizontal = 0f; Vertical = 0f;
            JumpHeld = JumpPressed = DashPressed = CrouchHeld = PausePressed = false;
            if (!active) return;

            float x = 0f; float y = 0f;
            if (UnityEngine.Input.GetKey(left) || UnityEngine.Input.GetKey(altLeft)) x -= 1f;
            if (UnityEngine.Input.GetKey(right) || UnityEngine.Input.GetKey(altRight)) x += 1f;
            if (UnityEngine.Input.GetKey(forward) || UnityEngine.Input.GetKey(altForward)) y += 1f;
            if (UnityEngine.Input.GetKey(backward) || UnityEngine.Input.GetKey(altBackward)) y -= 1f;
            if (useMobileFallback)
            {
                x += FrogRunner.Core.MobileInput.Horizontal;
                y += FrogRunner.Core.MobileInput.Vertical;
                JumpHeld |= FrogRunner.Core.MobileInput.JumpHeld;
                JumpPressed |= FrogRunner.Core.MobileInput.JumpPressed;
                DashPressed |= FrogRunner.Core.MobileInput.DashPressed;
            }
            Horizontal = Mathf.Clamp(x, -1f, 1f);
            Vertical = Mathf.Clamp(y, -1f, 1f);
            JumpHeld |= UnityEngine.Input.GetKey(jump);
            JumpPressed |= UnityEngine.Input.GetKeyDown(jump);
            DashPressed |= UnityEngine.Input.GetKeyDown(dash) || UnityEngine.Input.GetKeyDown(altDash);
            CrouchHeld |= UnityEngine.Input.GetKey(crouch) || UnityEngine.Input.GetKey(altCrouch);
            PausePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P);
        }
    }
}
