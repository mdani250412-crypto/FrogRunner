#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace FrogRunner.EditorTools
{
    public static class FrogRunnerBuildTool
    {
        const string WindowsOutputDir = "Builds/Windows";
        const string WindowsExeName = "FrogRunner.exe";
        const string WindowsZipName = "FrogRunner-Windows.zip";
        const string AndroidApkName = "FrogRunner-Android.apk";

        [MenuItem("Tools/FrogRunner/Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "FrogRunnerDev";
            PlayerSettings.productName = "FrogRunner";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Android
            PlayerSettings.applicationIdentifier = "com.frogrunner.game";
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24; // Android 7.0 (Nougat) minimum
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33; // target modern SDK
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.useCustomKeystore = false;
#pragma warning disable CS0618
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#pragma warning restore CS0618
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;

            // Orientation: landscape only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Keep graphics lightweight defaults
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.use32BitDisplayBuffer = true;

            // Windows
#pragma warning disable CS0618
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1); // x86_64
#pragma warning restore CS0618

            Debug.Log("[FrogRunnerBuildTool] Player Settings configured for Windows + Android.");
        }

        static void ConfigureKeystore()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string keystorePath = Path.Combine(projectRoot, "FrogRunnerKeystore.keystore");

            if (!File.Exists(keystorePath))
            {
                Debug.LogWarning("[FrogRunnerBuildTool] Keystore not found. Run Tools -> FrogRunner -> Setup Android Keystore & Icons first. Building with debug signing.");
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = "frogrunner123";
            PlayerSettings.Android.keyaliasName = "frogrunner";
            PlayerSettings.Android.keyaliasPass = "frogrunner123";
            Debug.Log("[FrogRunnerBuildTool] Keystore configured for signed build.");
        }

        [MenuItem("Tools/FrogRunner/Build Windows & Android")]
        public static void BuildWindowsAndAndroid()
        {
            EnsureScenesInBuildSettings();
            ConfigurePlayerSettings();
            // Ensure keystore is configured
            ConfigureKeystore();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string buildsRoot = Path.Combine(projectRoot, "Builds");
            Directory.CreateDirectory(buildsRoot);

            bool windowsOk = BuildWindows(buildsRoot);
            bool androidOk = BuildAndroid(buildsRoot);

            if (windowsOk && androidOk)
                Debug.Log("[FrogRunnerBuildTool] Both builds completed successfully.");
            else if (windowsOk)
                Debug.LogWarning("[FrogRunnerBuildTool] Windows build succeeded, Android build failed or skipped.");
            else if (androidOk)
                Debug.LogWarning("[FrogRunnerBuildTool] Android build succeeded, Windows build failed.");
            else
                Debug.LogError("[FrogRunnerBuildTool] Both builds failed.");
        }

        static void EnsureScenesInBuildSettings()
        {
            // Ensure at least one scene is in build settings. Prefer the active scene.
            string activeScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            if (string.IsNullOrEmpty(activeScenePath))
            {
                // Fallback: find any .unity under Assets/Scenes
                string[] found = Directory.GetFiles(Path.Combine(Application.dataPath, "Scenes"), "*.unity", SearchOption.AllDirectories);
                if (found.Length > 0) activeScenePath = "Assets/Scenes/" + Path.GetFileName(found[0]);
            }

            if (!string.IsNullOrEmpty(activeScenePath))
            {
                bool exists = false;
                foreach (var s in scenes)
                {
                    if (s.path == activeScenePath) { exists = true; break; }
                }
                if (!exists)
                {
                    scenes.Add(new EditorBuildSettingsScene(activeScenePath, true));
                }
            }

            // Make sure all enabled scenes are marked enabled
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null) scenes[i].enabled = true;
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[FrogRunnerBuildTool] Build settings scenes: {EditorBuildSettings.scenes.Length}");
        }

        static bool BuildWindows(string buildsRoot)
        {
            string windowsDir = Path.Combine(buildsRoot, "Windows");
            Directory.CreateDirectory(windowsDir);
            string exePath = Path.Combine(windowsDir, WindowsExeName);

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[FrogRunnerBuildTool] Windows build failed: {report.summary}");
                return false;
            }

            // Zip the Windows build folder
            string zipPath = Path.Combine(buildsRoot, WindowsZipName);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(windowsDir, zipPath);
            Debug.Log($"[FrogRunnerBuildTool] Windows build + zip created: {zipPath}");
            return true;
        }

        static bool BuildAndroid(string buildsRoot)
        {
            // Check if Android module is installed
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[FrogRunnerBuildTool] Android Build Support is NOT installed. " +
                              "Open Unity Hub -> Installs -> (your Unity 6 version) -> Add Modules -> tick 'Android Build Support' " +
                              "(includes Android SDK & NDK Tools + OpenJDK). Then retry.");
                return false;
            }

            string apkPath = Path.Combine(buildsRoot, AndroidApkName);
            if (File.Exists(apkPath)) File.Delete(apkPath);

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[FrogRunnerBuildTool] Android build failed: {report.summary}");
                return false;
            }

            Debug.Log($"[FrogRunnerBuildTool] Android APK created: {apkPath}");
            return true;
        }

        static string[] GetEnabledScenePaths()
        {
            List<string> paths = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s != null && s.enabled && !string.IsNullOrEmpty(s.path))
                    paths.Add(s.path);
            }
            if (paths.Count == 0)
            {
                // Fallback to active scene
                string active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(active)) paths.Add(active);
            }
            return paths.ToArray();
        }
    }
}
#endif
