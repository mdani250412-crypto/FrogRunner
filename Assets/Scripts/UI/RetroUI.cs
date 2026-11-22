using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.UI
{
    /// <summary>
    /// Retro IMGUI front end: HUD, pause/settings, main menu, retry and victory screens, plus the
    /// on-screen touch buttons. IMGUI is used deliberately so there are no Canvas/prefab dependencies
    /// and the whole game remains one drag-and-drop scene.
    /// TUNING: the 1280x720 virtual canvas is scaled to screen height in OnGUI().
    /// </summary>
    [DisallowMultipleComponent]
    public partial class RetroUI : MonoBehaviour
    {
        enum UiScreen
        {
            Hud,
            Menu,
            Multiplayer,
            Lobby,
            Pause,
            Settings,
            GameOver,
            Victory,
            Closet,
            SaveMenu,
            SafeBase,
            RoundLeaderboard,
            BoostShop
        }

        Texture2D _panelTex;
        Texture2D _buttonTex;
        Texture2D _buttonHoverTex;
        Texture2D _buttonDownTex;
        Texture2D _whiteTex;
        Texture2D _heartTex;
        Texture2D _heartEmptyTex;

        GUIStyle _panelStyle;
        GUIStyle _buttonStyle;
        GUIStyle _smallButtonStyle;
        GUIStyle _labelStyle;
        GUIStyle _smallLabelStyle;
        GUIStyle _titleStyle;
        GUIStyle _subtitleStyle;
        GUIStyle _centerStyle;

        UiScreen _screen = UiScreen.Menu;
        UiScreen _settingsReturn = UiScreen.Menu;
        UiScreen _saveMenuReturn = UiScreen.Menu;
        int _saveSlotSelected = 0;
        string _closetName = "Frog";
        int _closetColor;
        int _closetHat;
        string _nameError = "";
        GameState _lastState = (GameState)(-1);
        float _scale = 1f;
        float _vw = 1280f;
        const float Vh = 720f;

        void Awake()
        {
            _panelTex = PixelTextureFactory.UIPanel();
            _buttonTex = PixelTextureFactory.UIButton(false, false);
            _buttonHoverTex = PixelTextureFactory.UIButton(true, false);
            _buttonDownTex = PixelTextureFactory.UIButton(false, true);
            _whiteTex = PixelTextureFactory.White();
            _heartTex = PixelTextureFactory.Heart(true);
            _heartEmptyTex = PixelTextureFactory.Heart(false);
            var save = FrogRunner.SaveSystem.SaveSystemManager.Instance;
            if (save != null && save.Profile != null)
            {
                _closetName = save.Profile.playerName;
                _closetColor = save.Profile.frogColorIndex;
                _closetHat = save.Profile.hatIndex;
                _playerName = save.Profile.playerName;
            }
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.State != _lastState)
            {
                _lastState = gm.State;
                switch (gm.State)
                {
                    case GameState.Menu:
                        if (_screen != UiScreen.Multiplayer && _screen != UiScreen.Lobby && _screen != UiScreen.Settings
                            && _screen != UiScreen.Closet && _screen != UiScreen.SaveMenu)
                            _screen = UiScreen.Menu;
                        break;
                    case GameState.Playing: _screen = UiScreen.Hud; break;
                    case GameState.Paused: _screen = UiScreen.Pause; break;
                    case GameState.SafeBase: _screen = UiScreen.SafeBase; break;
                    case GameState.RoundLeaderboard: _screen = UiScreen.RoundLeaderboard; break;
                    case GameState.BoostShop: _screen = UiScreen.BoostShop; break;
                    case GameState.GameOver: _screen = UiScreen.GameOver; break;
                    case GameState.Victory: _screen = UiScreen.Victory; break;
                }
            }

            bool enter = UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter);
            if (!enter) return;
            if (_screen == UiScreen.Menu) StartSolo();
            else if (_screen == UiScreen.GameOver || _screen == UiScreen.Victory) StartRun();
            else if (_screen == UiScreen.Pause) Resume();
        }

        void OnGUI()
        {
            if (Event.current != null && Event.current.type == EventType.Repaint)
                DrawLowResWorld();

            if (GameManager.Instance == null) return;

            _scale = Mathf.Max(0.55f, Screen.height / Vh);
            _vw = Screen.width / _scale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            GUI.backgroundColor = Color.white;
            EnsureStyles();

            switch (_screen)
            {
                case UiScreen.Hud: DrawHud(true); break;
                case UiScreen.Menu: DrawMenu(); break;
                case UiScreen.Multiplayer: DrawMultiplayerMenu(); break;
                case UiScreen.Lobby: DrawLobby(); break;
                case UiScreen.Pause: DrawHud(false); DrawPauseMenu(); break;
                case UiScreen.Settings: DrawHud(false); DrawSettings(); break;
                case UiScreen.GameOver: DrawHud(false); DrawGameOver(); break;
                case UiScreen.Victory: DrawHud(false); DrawVictory(); break;
                case UiScreen.Closet: DrawCloset(); break;
                case UiScreen.SaveMenu: DrawSaveMenu(); break;
                case UiScreen.SafeBase: DrawHud(false); DrawSafeBase(); break;
                case UiScreen.RoundLeaderboard: DrawHud(false); DrawRoundLeaderboard(); break;
                case UiScreen.BoostShop: DrawHud(false); DrawBoostShop(); break;
            }

            GUI.matrix = previous;
        }

        void EnsureStyles()
        {
            if (_panelStyle != null) return;

            _panelStyle = new GUIStyle
            {
                border = new RectOffset(7, 7, 7, 7),
                padding = new RectOffset(22, 22, 20, 20)
            };
            _panelStyle.normal.background = _panelTex;

            _buttonStyle = new GUIStyle
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(4, 4, 3, 3),
                padding = new RectOffset(12, 12, 6, 6)
            };
            _buttonStyle.normal.background = _buttonTex;
            _buttonStyle.hover.background = _buttonHoverTex;
            _buttonStyle.active.background = _buttonDownTex;
            _buttonStyle.normal.textColor = new Color(0.98f, 0.93f, 0.76f);
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.active.textColor = new Color(1f, 0.94f, 0.66f);

            _smallButtonStyle = new GUIStyle(_buttonStyle) { fontSize = 17 };
            _smallButtonStyle.border = new RectOffset(3, 3, 2, 2);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = Color.white;

            _smallLabelStyle = new GUIStyle(_labelStyle) { fontSize = 16, fontStyle = FontStyle.Normal };

            _titleStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 68,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            _subtitleStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            _centerStyle = new GUIStyle(_labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
        }

        void DrawLowResWorld()
        {
            RetroRenderScaler scaler = RetroRenderScaler.Current;
            if (scaler == null || scaler.CurrentRT == null) return;

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), scaler.CurrentRT, ScaleMode.StretchToFill, false);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        void DrawDim(float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, _vw, Vh), _whiteTex);
            GUI.color = previous;
        }

        void DrawPanel(Rect rect, float alpha = 0.92f)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.color = previous;
        }

        static void DrawLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, color.a * 0.78f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }

        bool DrawButton(float x, float y, float width, float height, string text)
        {
            return GUI.Button(new Rect(x, y, width, height), text, _buttonStyle);
        }

        bool DrawSmallButton(Rect rect, string text)
        {
            return GUI.Button(rect, text, _smallButtonStyle);
        }

        void StartRun()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Menu || gm.State == GameState.GameOver || gm.State == GameState.Victory)
                gm.StartRun();
        }

        void Resume()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Paused) gm.Resume();
        }

        void OpenSettings(UiScreen returnScreen)
        {
            _settingsReturn = returnScreen;
            _screen = UiScreen.Settings;
        }

        void CloseSettings()
        {
            _screen = _settingsReturn;
        }
    }
}
