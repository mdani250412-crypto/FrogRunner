using FrogRunner.Core;
using UnityEngine;

namespace FrogRunner.UI
{
    public partial class RetroUI
    {
        void DrawHud(bool interactive)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            Rect left = new Rect(18f, 18f, 352f, 132f);
            DrawPanel(left, 0.82f);
            int round = FrogRunner.Rounds.RoundManager.Instance != null ? FrogRunner.Rounds.RoundManager.Instance.Round : 1;
            int coins = CoinBank.Instance != null ? CoinBank.Instance.Coins : 0;
            DrawLabel(new Rect(left.x + 24f, left.y + 18f, left.width - 40f, 30f),
                "SCORE  " + gm.Score.ToString("000000"), _labelStyle, new Color(1f, 0.92f, 0.55f));
            DrawLabel(new Rect(left.x + 24f, left.y + 50f, left.width - 40f, 30f),
                $"DIST {Mathf.FloorToInt(gm.Distance)} M   R{round}  C{coins}", _labelStyle, Color.white);
            DrawLabel(new Rect(left.x + 24f, left.y + 82f, left.width - 40f, 30f),
                "ZONE   " + gm.ZoneName.ToUpperInvariant(), _labelStyle, new Color(0.62f, 0.92f, 0.46f));

            Rect right = new Rect(_vw - 18f - 312f, 18f, 312f, 62f);
            DrawPanel(right, 0.82f);
            var hp = gm.Player != null ? gm.Player.GetComponent<FrogRunner.Player.PlayerHealth>() : null;
            int hearts = hp != null ? hp.Hearts : gm.Lives;
            int maxHearts = hp != null ? hp.MaxHearts : gm.MaxLives;
            for (int i = 0; i < maxHearts; i++)
            {
                Texture2D heart = i < hearts ? _heartTex : _heartEmptyTex;
                GUI.DrawTexture(new Rect(right.x + 20f + i * 32f, right.y + 16f, 28f, 28f), heart, ScaleMode.ScaleToFit);
            }
            DrawLabel(new Rect(right.x + 138f, right.y + 16f, 158f, 28f),
                "BEST " + gm.BestDistance + " M", _smallLabelStyle, new Color(0.95f, 0.90f, 0.72f));

            var rounds = FrogRunner.Rounds.RoundManager.Instance;
            if (rounds != null && rounds.BannerTimer > 0f)
            {
                float a = Mathf.Clamp01(rounds.BannerTimer / 0.35f);
                DrawPanel(new Rect(_vw * 0.5f - 220f, 230f, 440f, 72f), 0.85f * a);
                DrawLabel(new Rect(_vw * 0.5f - 220f, 230f, 440f, 72f), rounds.BannerText, _titleStyle, new Color(1f, 0.9f, 0.35f, a));
            }

            if (interactive && DrawSmallButton(new Rect(_vw - 18f - 60f, 90f, 60f, 48f), "II"))
                gm.Pause();

            if (gm.ToastRemaining > 0f && !string.IsNullOrEmpty(gm.ToastText))
            {
                float alpha = Mathf.Clamp01(gm.ToastRemaining / 0.45f);
                Rect toast = new Rect(_vw * 0.5f - 330f, 166f, 660f, 50f);
                DrawPanel(toast, 0.72f * alpha);
                DrawLabel(toast, gm.ToastText, _centerStyle, new Color(1f, 0.94f, 0.60f, alpha));
            }

            DrawPowerUpIndicators();

            // Fast starts: no countdown/cutscene overlay.

            if (gm.State == GameState.Playing)
            {
                if (gm.IsSafeBaseActive)
                {
                    DrawLabel(new Rect(0f, Vh - 30f, _vw, 24f),
                        "SAFE BASE - HAZARDS DISABLED",
                        _smallLabelStyle, new Color(0.6f, 1f, 0.6f, 0.7f));
                }
                else
                {
                    DrawLabel(new Rect(0f, Vh - 30f, _vw, 24f),
                        "WASD/ARROWS MOVE  SPACE JUMP+GLIDE  SHIFT DASH  CTRL/C CROUCH  ESC PAUSE",
                        _smallLabelStyle, new Color(1f, 1f, 1f, 0.48f));
                    DrawTouchControls();
                }
            }
        }

        void DrawTouchControls()
        {
            GraphicsSettingsManager settings = GraphicsSettingsManager.Instance;
            if (settings == null || !settings.TouchControlsEnabled) return;

            DrawTouchButton(MobileInput.GetUpRect(Screen.width, Screen.height), "UP");
            DrawTouchButton(MobileInput.GetLeftRect(Screen.width, Screen.height), "<");
            DrawTouchButton(MobileInput.GetRightRect(Screen.width, Screen.height), ">");
            DrawTouchButton(MobileInput.GetDownRect(Screen.width, Screen.height), "DN");
            DrawTouchButton(MobileInput.GetJumpRect(Screen.width, Screen.height), "JUMP");
            DrawTouchButton(MobileInput.GetDashRect(Screen.width, Screen.height), "DASH");
        }

        void DrawTouchButton(Rect screenRect, string label)
        {
            Rect virtualRect = new Rect(
                screenRect.x / _scale,
                (Screen.height - screenRect.yMax) / _scale,
                screenRect.width / _scale,
                screenRect.height / _scale);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.72f);
            GUI.Box(virtualRect, label, _buttonStyle);
            GUI.color = previous;
        }

        void DrawPowerUpIndicators()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;
            var inv = gm.Player.GetComponent<FrogRunner.PowerUps.PowerUpInventory>();
            if (inv == null) return;
            float y = 100f;
            if (inv.ShieldActive) { DrawPowerUpBadge("SHIELD " + Mathf.CeilToInt(inv.ShieldRemaining) + "s", new Color(0.3f, 0.9f, 1f), ref y); }
            if (inv.SpeedActive) { DrawPowerUpBadge("SPEED " + Mathf.CeilToInt(inv.SpeedRemaining) + "s", new Color(1f, 0.8f, 0.2f), ref y); }
            if (inv.MagnetActive) { DrawPowerUpBadge("MAGNET x" + inv.CurrentScoreMultiplier + " " + Mathf.CeilToInt(inv.MagnetRemaining) + "s", new Color(0.95f, 0.45f, 1f), ref y); }
        }

        void DrawPowerUpBadge(string text, Color color, ref float y)
        {
            Rect r = new Rect(_vw - 240f, y, 222f, 34f);
            DrawPanel(r, 0.72f);
            DrawLabel(r, text, _smallLabelStyle, color);
            y += 40f;
        }
    }
}
