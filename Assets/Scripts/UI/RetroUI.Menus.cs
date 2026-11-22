using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.UI
{
    public partial class RetroUI
    {
        void DrawMenu()
        {
            GameManager gm = GameManager.Instance;
            DrawDim(0.16f);

            float cx = _vw * 0.5f;
            DrawLabel(new Rect(cx - 430f, 20f, 860f, 88f), "FROG RUNNER", _titleStyle,
                new Color(1f, 0.84f, 0.18f));
            DrawLabel(new Rect(cx - 430f, 102f, 860f, 28f),
                "HIGHWAY / GRASS BANK / RIVER / ROUNDS", _subtitleStyle,
                new Color(0.72f, 0.96f, 0.58f));

            Rect panel = new Rect(cx - 280f, 150f, 560f, 500f);
            DrawPanel(panel, 0.90f);

            float bx = cx - 210f;
            if (DrawButton(bx, 184f, 420f, 54f, "PLAY SOLO")) StartSolo();
            if (DrawButton(bx, 246f, 420f, 54f, "MULTIPLAYER")) OpenMultiplayerMenu();
            if (DrawButton(bx, 308f, 420f, 54f, "CLOSET")) { _screen = UiScreen.Closet; }
            if (DrawButton(bx, 370f, 420f, 54f, "SAVE MENU")) { _screen = UiScreen.SaveMenu; _saveMenuReturn = UiScreen.Menu; }
            if (DrawButton(bx, 432f, 420f, 54f, "GRAPHICS & AUDIO")) OpenSettings(UiScreen.Menu);
            if (DrawButton(bx, 494f, 420f, 54f, "QUIT")) QuitGame();

            DrawLabel(new Rect(panel.x + 28f, 560f, panel.width - 56f, 28f),
                "BEST RUN  " + (gm != null ? gm.BestDistance : 0) + " M", _centerStyle,
                new Color(0.95f, 0.90f, 0.68f));
            DrawLabel(new Rect(panel.x + 28f, 592f, panel.width - 56f, 22f),
                "WASD/ARROWS MOVE  SPACE JUMP/GLIDE  SHIFT DASH  CTRL/C CROUCH  ESC PAUSE",
                _smallLabelStyle, new Color(1f, 1f, 1f, 0.58f));
        }

        void DrawPauseMenu()
        {
            DrawDim(0.56f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 280f, 100f, 560f, 540f);
            DrawPanel(panel, 0.94f);

            DrawLabel(new Rect(panel.x + 30f, 124f, panel.width - 60f, 66f), "PAUSED", _titleStyle,
                new Color(1f, 0.88f, 0.38f));

            float bx = cx - 200f;
            if (DrawButton(bx, 210f, 400f, 54f, "RESUME")) Resume();
            if (DrawButton(bx, 274f, 400f, 54f, "SAVE GAME")) { _screen = UiScreen.SaveMenu; _saveMenuReturn = UiScreen.Pause; }
            if (DrawButton(bx, 338f, 400f, 54f, "SETTINGS")) OpenSettings(UiScreen.Pause);
            if (DrawButton(bx, 402f, 400f, 54f, "LEAVE"))
            {
                var net = FrogRunner.Networking.LanNetworkManager.Instance;
                if (net != null) net.LeaveRoom();
                GameManager.Instance.ReturnToMenu();
                _screen = UiScreen.Menu;
            }
            if (DrawButton(bx, 466f, 400f, 54f, "BACK")) Resume();

            DrawLabel(new Rect(panel.x + 30f, 540f, panel.width - 60f, 24f),
                "ESC RESUMES  |  SAVE KEEPS YOU PLAYING", _centerStyle, new Color(1f, 1f, 1f, 0.56f));
        }

        void DrawSettings()
        {
            GraphicsSettingsManager settings = GraphicsSettingsManager.Instance;
            if (settings == null) return;

            DrawDim(0.70f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 410f, 56f, 820f, 620f);
            DrawPanel(panel, 0.96f);

            DrawLabel(new Rect(panel.x + 40f, 74f, panel.width - 80f, 56f),
                "GRAPHICS & INPUT", _titleStyle, new Color(1f, 0.84f, 0.22f));

            float labelX = panel.x + 44f;
            float rowY = 190f;
            const float rowStep = 72f;

            DrawSettingRow(labelX, rowY, "GRAPHICS PRESET", settings.PresetLabel,
                () => settings.CyclePreset(-1), () => settings.CyclePreset(1));
            DrawSettingRow(labelX, rowY + rowStep, "FPS CAP", settings.FpsLabel,
                () => settings.CycleFps(-1), () => settings.CycleFps(1));
            DrawSettingRow(labelX, rowY + rowStep * 2f, "TOUCH CONTROLS",
                settings.TouchControlsEnabled ? "ON" : "OFF",
                settings.ToggleTouchControls, settings.ToggleTouchControls);

            var audio = FrogRunner.Audio.AudioManager.Instance;
            if (audio != null)
            {
                DrawSettingRow(labelX, rowY + rowStep * 3f, "MUSIC VOL",
                    Mathf.RoundToInt(audio.musicVolume * 100f) + "%",
                    () => audio.SetMusicVolume(audio.musicVolume - 0.1f),
                    () => audio.SetMusicVolume(audio.musicVolume + 0.1f));
                DrawSettingRow(labelX, rowY + rowStep * 4f, "SFX VOL",
                    Mathf.RoundToInt(audio.sfxVolume * 100f) + "%",
                    () => audio.SetSfxVolume(audio.sfxVolume - 0.1f),
                    () => audio.SetSfxVolume(audio.sfxVolume + 0.1f));
                DrawSettingRow(labelX, rowY + rowStep * 5f, "MUTE",
                    audio.muted ? "ON" : "OFF",
                    audio.ToggleMute, audio.ToggleMute);
            }

            if (DrawButton(cx - 190f, 640f, 380f, 50f, "BACK")) CloseSettings();

            string presetDetail = $"RENDER {settings.RenderScale:0.##}x  |  SHADOWS {settings.ShadowLabel.ToUpper()}  |  DRAW {Mathf.RoundToInt(settings.DrawDistance)}m";
            DrawLabel(new Rect(panel.x + 40f, 610f, panel.width - 80f, 24f),
                presetDetail, _centerStyle, new Color(0.82f, 1f, 0.72f, 0.85f));
            DrawLabel(new Rect(panel.x + 40f, 556f, panel.width - 80f, 24f),
                "LOW = BEST FPS ON LAPTOPS. HIGH = SHARPEST PIXELS AND LONGER VIEW.",
                _smallLabelStyle, new Color(1f, 1f, 1f, 0.52f));
            DrawLabel(new Rect(panel.x + 40f, 584f, panel.width - 80f, 24f),
                "PRESET CONTROLS RENDER SCALE, SHADOW QUALITY, AND DRAW DISTANCE TOGETHER.",
                _smallLabelStyle, new Color(1f, 1f, 1f, 0.52f));
            DrawLabel(new Rect(panel.x + 40f, 628f, panel.width - 80f, 22f),
                "SETTINGS ARE SAVED AUTOMATICALLY IN PLAYERPREFS.", _smallLabelStyle,
                new Color(1f, 0.82f, 0.48f, 0.58f));
        }

        void DrawSettingRow(float x, float y, string label, string value,
            System.Action previous, System.Action next)
        {
            DrawLabel(new Rect(x, y, 330f, 44f), label, _labelStyle, new Color(0.94f, 0.92f, 0.80f));

            float rowX = x + 354f;
            if (DrawSmallButton(new Rect(rowX, y, 46f, 44f), "<")) previous();
            if (DrawSmallButton(new Rect(rowX + 52f, y, 230f, 44f), value)) next();
            if (DrawSmallButton(new Rect(rowX + 288f, y, 46f, 44f), ">")) next();
        }

        void DrawGameOver()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            DrawDim(0.66f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 320f, 116f, 640f, 492f);
            DrawPanel(panel, 0.96f);

            bool allDown = gm.AllPlayersDown;
            DrawLabel(new Rect(panel.x + 30f, 138f, panel.width - 60f, 76f), allDown ? "ALL DOWN" : "WIPED OUT", _titleStyle,
                new Color(1f, 0.48f, 0.30f));
            DrawLabel(new Rect(panel.x + 30f, 214f, panel.width - 60f, 30f),
                allDown ? "ALL ACTIVE PLAYERS WERE KNOCKED OUT" : "THE HIGHWAY ALWAYS WINS",
                _subtitleStyle, new Color(1f, 0.84f, 0.46f));

            DrawLabel(new Rect(panel.x + 70f, 270f, panel.width - 140f, 30f),
                "DISTANCE      " + Mathf.FloorToInt(gm.Distance) + " M", _labelStyle, Color.white);
            DrawLabel(new Rect(panel.x + 70f, 306f, panel.width - 140f, 30f),
                "SCORE         " + gm.Score.ToString("000000"), _labelStyle, Color.white);
            DrawLabel(new Rect(panel.x + 70f, 342f, panel.width - 140f, 30f),
                "BEST          " + gm.BestDistance + " M", _labelStyle,
                new Color(0.94f, 0.86f, 0.48f));

            if (DrawButton(cx - 200f, 404f, 400f, 58f, "RESTART")) { Time.timeScale = 1f; StartRun(); }
            if (DrawButton(cx - 200f, 474f, 400f, 58f, "LEAVE"))
            {
                Time.timeScale = 1f;
                gm.ReturnToMenu();
                _screen = UiScreen.Menu;
            }

            DrawLabel(new Rect(panel.x + 30f, 552f, panel.width - 60f, 24f),
                "RESTART OR LEAVE", _centerStyle, new Color(1f, 1f, 1f, 0.56f));
        }

        void DrawVictory()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            DrawDim(0.64f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 320f, 112f, 640f, 500f);
            DrawPanel(panel, 0.96f);

            DrawLabel(new Rect(panel.x + 30f, 132f, panel.width - 60f, 82f), "COURSE CLEAR!", _titleStyle,
                new Color(0.66f, 1f, 0.42f));
            DrawLabel(new Rect(panel.x + 30f, 214f, panel.width - 60f, 30f),
                "FROG OF THE OPEN ROAD", _subtitleStyle, new Color(1f, 0.88f, 0.44f));

            DrawLabel(new Rect(panel.x + 70f, 272f, panel.width - 140f, 30f),
                "DISTANCE      " + Mathf.FloorToInt(gm.Distance) + " M", _labelStyle, Color.white);
            DrawLabel(new Rect(panel.x + 70f, 308f, panel.width - 140f, 30f),
                "FINAL SCORE   " + gm.Score.ToString("000000"), _labelStyle, Color.white);
            DrawLabel(new Rect(panel.x + 70f, 344f, panel.width - 140f, 30f),
                "BEST          " + gm.BestDistance + " M", _labelStyle,
                new Color(0.94f, 0.86f, 0.48f));

            if (DrawButton(cx - 200f, 408f, 400f, 58f, "RUN AGAIN")) StartRun();
            if (DrawButton(cx - 200f, 478f, 400f, 58f, "MAIN MENU"))
            {
                gm.ReturnToMenu();
                _screen = UiScreen.Menu;
            }

            DrawLabel(new Rect(panel.x + 30f, 558f, panel.width - 60f, 24f),
                "FINISH BONUS INCLUDED", _centerStyle, new Color(1f, 1f, 1f, 0.56f));
        }

        void StartSolo()
        {
            var net = FrogRunner.Networking.LanNetworkManager.Instance;
            if (net != null) net.LeaveRoom();
            var mp = FrogRunner.Multiplayer.MultiplayerManager.Instance;
            if (mp != null && GameManager.Instance != null && GameManager.Instance.Player != null)
                mp.ConfigureSingle(GameManager.Instance.Player);
            StartRun();
        }

        void OpenMultiplayerMenu()
        {
            _screen = UiScreen.Multiplayer;
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
