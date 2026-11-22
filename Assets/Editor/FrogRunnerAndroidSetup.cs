#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FrogRunner.EditorTools
{
    /// <summary>
    /// Helper to configure Android keystore signing and app icons for FrogRunner.
    /// Menu: Tools -> FrogRunner -> Setup Android Keystore & Icons
    /// </summary>
    public static class FrogRunnerAndroidSetup
    {
        const string KeystoreFileName = "FrogRunnerKeystore.keystore";
        const string KeyAlias = "frogrunner";
        const string KeystorePassword = "frogrunner123"; // test keystore only
        const string KeyPassword = "frogrunner123";

        [MenuItem("Tools/FrogRunner/Setup Android Keystore & Icons")]
        public static void SetupKeystoreAndIcons()
        {
            // --- Keystore Setup ---
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string keystorePath = Path.Combine(projectRoot, KeystoreFileName);

            if (!File.Exists(keystorePath))
            {
                // Generate keystore using keytool
                string keytool = FindKeytool();
                if (string.IsNullOrEmpty(keytool))
                {
                    Debug.LogError("[FrogRunnerAndroidSetup] keytool not found. Install JDK or use Unity's built-in keytool at: " +
                                  "Unity/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe");
                    return;
                }

                string args = $"-genkeypair -v -keystore \"{keystorePath}\" -alias {KeyAlias} " +
                              $"-keyalg RSA -keysize 2048 -validity 10000 " +
                              $"-storepass {KeystorePassword} -keypass {KeyPassword} " +
                              $"-dname \"CN=FrogRunnerDev, OU=Dev, O=FrogRunnerDev, L=Unknown, ST=Unknown, C=US\"";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = keytool,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                var proc = System.Diagnostics.Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    Debug.LogError($"[FrogRunnerAndroidSetup] keytool failed: {err}");
                    return;
                }
                Debug.Log($"[FrogRunnerAndroidSetup] Keystore created: {keystorePath}\n{output}");
            }
            else
            {
                Debug.Log($"[FrogRunnerAndroidSetup] Keystore already exists: {keystorePath}");
            }

            // Configure Player Settings to use the keystore
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = KeystorePassword;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keyaliasPass = KeyPassword;

            Debug.Log("[FrogRunnerAndroidSetup] Keystore configured in Player Settings.");

            // --- App Icon Setup ---
            // Create a placeholder icon texture if none exists
            string iconFolder = Path.Combine(Application.dataPath, "Resources", "Icons");
            Directory.CreateDirectory(iconFolder);
            string iconPath = Path.Combine(iconFolder, "FrogRunnerIcon.png");

            if (!File.Exists(iconPath))
            {
                // Generate a simple frog icon (32x32 placeholder)
                var tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                Color bg = new Color(0.1f, 0.15f, 0.1f);
                Color frog = new Color(0.4f, 0.85f, 0.3f);
                Color eye = Color.white;
                Color pupil = Color.black;

                for (int y = 0; y < 512; y++)
                    for (int x = 0; x < 512; x++)
                        tex.SetPixel(x, y, bg);

                // Frog body (circle-ish)
                for (int y = 100; y < 420; y++)
                    for (int x = 100; x < 420; x++)
                    {
                        float dx = x - 256, dy = y - 220;
                        if (dx * dx + dy * dy < 180 * 180)
                            tex.SetPixel(x, y, frog);
                    }

                // Eyes
                for (int i = 0; i < 2; i++)
                {
                    int ex = i == 0 ? 190 : 322;
                    for (int y = 280; y < 360; y++)
                        for (int x = ex - 30; x < ex + 30; x++)
                            if ((x - ex) * (x - ex) + (y - 320) * (y - 320) < 30 * 30)
                                tex.SetPixel(x, y, eye);
                    for (int y = 295; y < 345; y++)
                        for (int x = ex - 15; x < ex + 15; x++)
                            if ((x - ex) * (x - ex) + (y - 320) * (y - 320) < 15 * 15)
                                tex.SetPixel(x, y, pupil);
                }

                tex.Apply();
                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(iconPath, png);
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.Refresh();
                Debug.Log($"[FrogRunnerAndroidSetup] Placeholder icon created: {iconPath}");
            }

            // Load icon and assign to Player Settings
            string assetPath = "Assets/Resources/Icons/FrogRunnerIcon.png";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (icon != null)
            {
                // Set default icon for all platforms
#pragma warning disable CS0618
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });
#pragma warning restore CS0618
                Debug.Log("[FrogRunnerAndroidSetup] App icon assigned to Player Settings.");
            }
            else
            {
                Debug.LogWarning("[FrogRunnerAndroidSetup] Could not load icon asset. Assign manually in Player Settings -> Icons.");
            }

            // Keep package name and API levels
            PlayerSettings.applicationIdentifier = "com.frogrunner.game";
            PlayerSettings.productName = "FrogRunner";
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;

            Debug.Log("[FrogRunnerAndroidSetup] Android keystore + icons configured successfully.");
            EditorUtility.DisplayDialog("FrogRunner Android Setup",
                "Keystore and icons configured!\n\n" +
                "Keystore: FrogRunnerKeystore.keystore\n" +
                "Alias: frogrunner\n" +
                "Password: frogrunner123\n\n" +
                "Icon: Assets/Resources/Icons/FrogRunnerIcon.png\n\n" +
                "You can replace the icon with your own frog/runner art.\n" +
                "Rebuild the APK to apply changes.",
                "OK");
        }

        static string FindKeytool()
        {
            // Check Unity's bundled JDK
            string unityRoot = Directory.GetParent(EditorApplication.applicationPath).FullName;
            string[] candidates = new[]
            {
                Path.Combine(unityRoot, "Editor", "Data", "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", "keytool.exe"),
                Path.Combine(unityRoot, "Editor", "Data", "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", "keytool"),
                "keytool",
                "keytool.exe"
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
                // try PATH
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo { FileName = c, Arguments = "-version", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                    var p = System.Diagnostics.Process.Start(psi);
                    p.WaitForExit(3000);
                    if (p.ExitCode == 0) return c;
                }
                catch { }
            }
            return null;
        }
    }
}
#endif
