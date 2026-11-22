using FrogRunner.Audio;
using FrogRunner.Multiplayer;
using FrogRunner.Networking;
using FrogRunner.Obstacles;
using FrogRunner.Player;
using FrogRunner.PowerUps;
using FrogRunner.Rounds;
using FrogRunner.SaveSystem;
using FrogRunner.Traffic;
using FrogRunner.UI;
using FrogRunner.WorldGen;
using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// One-component scene entry point. It reuses an existing Main Camera and lights when present,
    /// then builds the course, frog, traffic, HUD and settings entirely at runtime.
    /// If the scene has no bootstrap object, the RuntimeInitialize fallback still boots the game,
    /// so any empty scene is playable while iterating.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (GameManager.Instance != null) return;
            if (FindAnyObjectByType<Bootstrap>() != null) return;
            var go = new GameObject("GameBootstrap");
            go.AddComponent<Bootstrap>();
        }

        void Awake()
        {
            if (GameManager.Instance != null) return;
            Time.timeScale = 1f;
            BuildGame();
        }

        void BuildGame()
        {
            var root = new GameObject("FrogRunner");

            Camera camera = EnsureCamera();
            if (camera.GetComponent<RetroRenderScaler>() == null) camera.gameObject.AddComponent<RetroRenderScaler>();
            ChaseCamera chase = camera.GetComponent<ChaseCamera>();
            if (chase == null) chase = camera.gameObject.AddComponent<ChaseCamera>();

            var managerGo = new GameObject("GameManager");
            managerGo.transform.SetParent(root.transform, false);
            var game = managerGo.AddComponent<GameManager>();

            var settingsGo = new GameObject("GraphicsSettings");
            settingsGo.transform.SetParent(root.transform, false);
            settingsGo.AddComponent<GraphicsSettingsManager>();

            CourseBuilder course = CourseBuilder.Build(root.transform);
            if (course == null) return;

            Vector3 start = new Vector3(0f, 0.15f, 4f);
            FrogController frog = FrogFactory.Create(root.transform, start);

            chase.target = frog.transform;
            chase.controller = frog;
            chase.SnapToTarget();

            var trafficGo = new GameObject("Traffic");
            trafficGo.transform.SetParent(root.transform, false);
            var traffic = trafficGo.AddComponent<TrafficSpawner>();
            traffic.Configure(frog);

            var powerGo = new GameObject("PowerUps");
            powerGo.transform.SetParent(root.transform, false);
            powerGo.AddComponent<PowerUpSpawner>();

            var audioGo = new GameObject("AudioManager");
            audioGo.transform.SetParent(root.transform, false);
            audioGo.AddComponent<AudioManager>();

            var mpGo = new GameObject("Multiplayer");
            mpGo.transform.SetParent(root.transform, false);
            var mp = mpGo.AddComponent<MultiplayerManager>();
            mp.ConfigureSingle(frog);

            var netGo = new GameObject("LanNetwork");
            netGo.transform.SetParent(root.transform, false);
            netGo.AddComponent<LanNetworkManager>();

            var roundsGo = new GameObject("RoundManager");
            roundsGo.transform.SetParent(root.transform, false);
            roundsGo.AddComponent<RoundManager>();

            var obstacleGo = new GameObject("ObstacleSpawner");
            obstacleGo.transform.SetParent(root.transform, false);
            obstacleGo.AddComponent<ObstacleSpawner>();

            var saveGo = new GameObject("SaveSystem");
            saveGo.transform.SetParent(root.transform, false);
            saveGo.AddComponent<SaveSystemManager>();

            var coinsGo = new GameObject("CoinBank");
            coinsGo.transform.SetParent(root.transform, false);
            coinsGo.AddComponent<CoinBank>();

            // CutsceneManager removed (volcano/lava/checkpoints stripped for fast 10-round racing).

            var uiGo = new GameObject("RetroUI");
            uiGo.transform.SetParent(root.transform, false);
            uiGo.AddComponent<RetroUI>();

            var policeGo = new GameObject("AnimalPolice");
            policeGo.transform.SetParent(root.transform, false);
            policeGo.AddComponent<FrogRunner.Police.AnimalPoliceChaser>();

            var inputGo = new GameObject("MobileInput");
            inputGo.transform.SetParent(root.transform, false);
            inputGo.AddComponent<MobileInputReader>();

            game.Configure(frog, traffic, start, CourseBuilder.FinishZ);
            frog.SetControlEnabled(false);
        }

        Camera EnsureCamera()
        {
            Camera camera = FindFirstObjectByType<Camera>();
            if (camera == null) camera = FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                camera = cameraGo.AddComponent<Camera>();
            }

            camera.gameObject.tag = "MainCamera";
            if (!camera.gameObject.activeSelf) camera.gameObject.SetActive(true);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.58f, 0.80f, 0.93f);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.12f;
            camera.farClipPlane = 180f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.allowDynamicResolution = false;

            if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i].gameObject != camera.gameObject) listeners[i].enabled = false;
            }

            return camera;
        }
    }
}
