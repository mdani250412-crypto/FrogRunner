using FrogRunner.Core;
using FrogRunner.RiverGlider;
using UnityEngine;

namespace FrogRunner.Player
{
    /// <summary>
    /// Builds the placeholder frog from primitive meshes and wires every animated pivot to FrogAnimator.
    /// To use custom art later: replace the visual root with your imported frog, then assign the same
    /// transforms (body/head/arms/legs/feet) in the animator. Gameplay code never depends on mesh names.
    /// TUNING: change part sizes below if the frog should read bigger against the traffic.
    /// </summary>
    public static class FrogFactory
    {
        public static FrogController Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("Frog");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.42f;
            controller.radius = 0.36f;
            controller.center = new Vector3(0f, 0.71f, 0f);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.32f;
            controller.skinWidth = 0.035f;
            controller.minMoveDistance = 0f;

            var frog = go.AddComponent<FrogController>();
            var animator = go.AddComponent<FrogAnimator>();
            animator.controller = frog;
            frog.animator = animator;
            go.AddComponent<PlayerCosmetics>();
            FrogRunner.UI.FloatingNameTag.Attach(go.transform);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            animator.visualRoot = visual;

            var bodyRoot = new GameObject("BodyRoot").transform;
            bodyRoot.SetParent(visual, false);
            bodyRoot.localPosition = new Vector3(0f, 0.62f, 0f);
            animator.bodyRoot = bodyRoot;

            var body = RetroBuilder.Sphere("Body", bodyRoot, new Vector3(0f, 0.12f, 0f),
                new Vector3(0.96f, 0.74f, 1.14f), RetroAssets.FrogSkin);
            animator.bodyMesh = body.transform;

            RetroBuilder.Sphere("Belly", bodyRoot, new Vector3(0f, 0.02f, 0.32f),
                new Vector3(0.68f, 0.54f, 0.52f), RetroAssets.FrogBelly);

            var head = new GameObject("HeadPivot").transform;
            head.SetParent(bodyRoot, false);
            head.localPosition = new Vector3(0f, 0.30f, 0.42f);
            animator.headPivot = head;

            RetroBuilder.Sphere("Head", head, new Vector3(0f, 0.02f, 0.08f),
                new Vector3(0.64f, 0.52f, 0.60f), RetroAssets.FrogSkin);
            RetroBuilder.Sphere("EyeWhiteL", head, new Vector3(-0.21f, 0.18f, 0.23f),
                new Vector3(0.24f, 0.25f, 0.20f), RetroAssets.EyeWhite);
            RetroBuilder.Sphere("EyeWhiteR", head, new Vector3(0.21f, 0.18f, 0.23f),
                new Vector3(0.24f, 0.25f, 0.20f), RetroAssets.EyeWhite);
            RetroBuilder.Sphere("EyePupilL", head, new Vector3(-0.24f, 0.18f, 0.32f),
                new Vector3(0.12f, 0.15f, 0.10f), RetroAssets.EyeBlack);
            RetroBuilder.Sphere("EyePupilR", head, new Vector3(0.24f, 0.18f, 0.32f),
                new Vector3(0.12f, 0.15f, 0.10f), RetroAssets.EyeBlack);
            RetroBuilder.Box("Mouth", head, new Vector3(0f, -0.12f, 0.36f),
                new Vector3(0.38f, 0.05f, 0.06f), RetroAssets.EyeBlack);
            RetroBuilder.Sphere("CheekL", head, new Vector3(-0.26f, 0.02f, 0.21f),
                new Vector3(0.18f, 0.16f, 0.18f), RetroAssets.FrogBelly);
            RetroBuilder.Sphere("CheekR", head, new Vector3(0.26f, 0.02f, 0.21f),
                new Vector3(0.18f, 0.16f, 0.18f), RetroAssets.FrogBelly);

            // Arms. The animator rotates the pivot transforms, not the visible boxes.
            animator.armL = BuildArm(bodyRoot, "ArmL", -0.47f, out animator.forearmL);
            animator.armR = BuildArm(bodyRoot, "ArmR", 0.47f, out animator.forearmR);

            // Legs: big frog thighs, short shins, wide webbed feet.
            animator.legL = BuildLeg(bodyRoot, "LegL", -0.38f, out animator.legLowerL, out animator.footL);
            animator.legR = BuildLeg(bodyRoot, "LegR", 0.38f, out animator.legLowerR, out animator.footR);

            var glider = HangGlider.Build(visual);
            animator.glider = glider;

            animator.CaptureBasePose();
            animator.ResetPose();
            frog.RespawnAt(position);
            return frog;
        }

        static Transform BuildArm(Transform bodyRoot, string name, float x, out Transform forearm)
        {
            var shoulder = new GameObject(name).transform;
            shoulder.SetParent(bodyRoot, false);
            shoulder.localPosition = new Vector3(x, 0.16f, 0.18f);
            RetroBuilder.Sphere("UpperArm", shoulder, new Vector3(0f, -0.13f, 0f),
                new Vector3(0.18f, 0.30f, 0.18f), RetroAssets.FrogSkin);

            forearm = new GameObject(name + "Fore").transform;
            forearm.SetParent(shoulder, false);
            forearm.localPosition = new Vector3(0f, -0.30f, 0f);
            RetroBuilder.Sphere("Forearm", forearm, new Vector3(0f, -0.12f, 0f),
                new Vector3(0.16f, 0.26f, 0.16f), RetroAssets.FrogSkin);
            RetroBuilder.Sphere("Hand", forearm, new Vector3(0f, -0.27f, 0.04f),
                new Vector3(0.21f, 0.12f, 0.24f), RetroAssets.FrogBelly);
            return shoulder;
        }

        static Transform BuildLeg(Transform bodyRoot, string name, float x, out Transform lower, out Transform foot)
        {
            var hip = new GameObject(name).transform;
            hip.SetParent(bodyRoot, false);
            hip.localPosition = new Vector3(x, 0.40f, -0.24f);
            RetroBuilder.Sphere("Thigh", hip, new Vector3(0f, -0.17f, 0.02f),
                new Vector3(0.30f, 0.42f, 0.34f), RetroAssets.FrogSkin);

            lower = new GameObject(name + "Lower").transform;
            lower.SetParent(hip, false);
            lower.localPosition = new Vector3(0f, -0.36f, 0f);
            RetroBuilder.Sphere("Shin", lower, new Vector3(0f, -0.16f, 0f),
                new Vector3(0.23f, 0.34f, 0.25f), RetroAssets.FrogSkin);

            foot = new GameObject(name + "Foot").transform;
            foot.SetParent(lower, false);
            foot.localPosition = new Vector3(0f, -0.34f, 0.03f);
            RetroBuilder.Box("WebbedFoot", foot, new Vector3(0f, 0f, 0.16f),
                new Vector3(0.32f, 0.10f, 0.52f), RetroAssets.FrogSkin);
            return hip;
        }
    }
}
