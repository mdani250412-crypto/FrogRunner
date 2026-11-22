using FrogRunner.Core;
using FrogRunner.Networking;
using FrogRunner.SaveSystem;
using UnityEngine;

namespace FrogRunner.UI
{
    public partial class RetroUI
    {
        static readonly Color[] FrogColors =
        {
            new Color(0.35f, 0.85f, 0.35f),
            new Color(0.25f, 0.65f, 0.95f),
            new Color(0.95f, 0.55f, 0.25f),
            new Color(0.85f, 0.35f, 0.85f),
            new Color(0.95f, 0.9f, 0.35f),
            new Color(0.9f, 0.9f, 0.9f)
        };
        static readonly string[] HatNames = { "None", "Cap", "Crown", "Wizard", "Helmet" };

        void DrawCloset()
        {
            DrawDim(0.72f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 420f, 50f, 840f, 620f);
            DrawPanel(panel, 0.96f);
            DrawLabel(new Rect(panel.x, 68f, panel.width, 54f), "CLOSET", _titleStyle, new Color(1f, 0.84f, 0.22f));
            DrawLabel(new Rect(panel.x, 120f, panel.width, 24f), "Customize your frog: Color, Hat, Name", _centerStyle, new Color(1f, 1f, 1f, 0.6f));

            var save = SaveSystemManager.Instance;
            var profile = save != null ? save.Profile : null;
            if (profile != null && string.IsNullOrEmpty(_closetName)) _closetName = profile.playerName;

            // Preview box
            Rect preview = new Rect(panel.x + 56f, 170f, 260f, 320f);
            DrawPanel(preview, 0.85f);
            DrawLabel(new Rect(preview.x, preview.y + 12f, preview.width, 28f), "PREVIEW", _centerStyle, new Color(1f, 1f, 1f, 0.7f));
            // Simple frog preview using colored rects
            Color frogC = FrogColors[Mathf.Clamp(_closetColor, 0, FrogColors.Length - 1)];
            Color prev = GUI.color;
            GUI.color = frogC;
            GUI.DrawTexture(new Rect(preview.x + 80f, preview.y + 120f, 100f, 80f), _whiteTex);
            GUI.DrawTexture(new Rect(preview.x + 95f, preview.y + 85f, 70f, 45f), _whiteTex);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(preview.x + 110f, preview.y + 95f, 12f, 12f), _whiteTex);
            GUI.DrawTexture(new Rect(preview.x + 138f, preview.y + 95f, 12f, 12f), _whiteTex);
            GUI.color = prev;
            DrawLabel(new Rect(preview.x, preview.y + 230f, preview.width, 24f), HatNames[Mathf.Clamp(_closetHat, 0, HatNames.Length - 1)], _centerStyle, new Color(1f, 0.9f, 0.5f));
            DrawLabel(new Rect(preview.x, preview.y + 260f, preview.width, 24f), _closetName, _centerStyle, Color.white);

            float rx = panel.x + 360f;
            DrawLabel(new Rect(rx, 180f, 200f, 28f), "FROG COLOR", _labelStyle, Color.white);
            if (DrawSmallButton(new Rect(rx + 220f, 176f, 44f, 36f), "<")) _closetColor = (_closetColor - 1 + FrogColors.Length) % FrogColors.Length;
            DrawLabel(new Rect(rx + 270f, 180f, 120f, 28f), (_closetColor + 1) + "/" + FrogColors.Length, _centerStyle, frogC);
            if (DrawSmallButton(new Rect(rx + 396f, 176f, 44f, 36f), ">")) _closetColor = (_closetColor + 1) % FrogColors.Length;

            DrawLabel(new Rect(rx, 250f, 200f, 28f), "HAT", _labelStyle, Color.white);
            if (DrawSmallButton(new Rect(rx + 220f, 246f, 44f, 36f), "<")) _closetHat = (_closetHat - 1 + HatNames.Length) % HatNames.Length;
            DrawLabel(new Rect(rx + 270f, 250f, 120f, 28f), HatNames[Mathf.Clamp(_closetHat, 0, HatNames.Length - 1)], _centerStyle, new Color(1f, 0.9f, 0.5f));
            if (DrawSmallButton(new Rect(rx + 396f, 246f, 44f, 36f), ">")) _closetHat = (_closetHat + 1) % HatNames.Length;

            DrawLabel(new Rect(rx, 320f, 200f, 28f), "PLAYER NAME", _labelStyle, Color.white);
            _closetName = GUI.TextField(new Rect(rx + 220f, 316f, 220f, 34f), _closetName ?? "Frog", 16);
            if (!string.IsNullOrEmpty(_nameError))
                DrawLabel(new Rect(rx, 356f, 440f, 24f), _nameError, _smallLabelStyle, new Color(1f, 0.45f, 0.45f));

            if (DrawButton(rx, 410f, 200f, 52f, "SAVE"))
            {
                string name = SanitizeName(_closetName);
                if (string.IsNullOrEmpty(name)) { _nameError = "Name cannot be empty."; }
                else if (IsDuplicateName(name))
                {
                    _nameError = "Name already in use. Choose a different name.";
                }
                else
                {
                    _nameError = "";
                    if (profile != null)
                    {
                        profile.playerName = name;
                        profile.frogColorIndex = _closetColor;
                        profile.hatIndex = _closetHat;
                        save.SaveProfile();
                    }
                    _playerName = name;
                    GameManager.Instance?.ShowToast("Closet saved!");
                }
            }
            if (DrawButton(rx + 220f, 410f, 200f, 52f, "BACK"))
            {
                _nameError = "";
                _screen = UiScreen.Menu;
            }
        }

        void DrawSaveMenu()
        {
            DrawDim(0.72f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 460f, 40f, 920f, 640f);
            DrawPanel(panel, 0.96f);
            DrawLabel(new Rect(panel.x, 58f, panel.width, 54f), "SAVE MENU", _titleStyle, new Color(1f, 0.84f, 0.22f));

            var save = SaveSystemManager.Instance;
            var gm = GameManager.Instance;
            bool inRun = gm != null && (gm.State == GameState.Playing || gm.State == GameState.Paused || gm.State == GameState.SafeBase);
            bool hostCanSave = true;
            var net = LanNetworkManager.Instance;
            if (net != null && net.InRoom) hostCanSave = net.IsHost;

            DrawLabel(new Rect(panel.x, 112f, panel.width, 24f), hostCanSave ? "Select a slot." : "Only the host can save/load shared saves.", _centerStyle, new Color(1f, 1f, 1f, 0.62f));

            float y = 160f;
            for (int i = 0; i < SaveSystemManager.SlotCount; i++)
            {
                var slot = save?.GetSlot(i);
                bool selected = _saveSlotSelected == i;
                Rect row = new Rect(panel.x + 48f, y + i * 110f, panel.width - 96f, 96f);
                DrawPanel(row, selected ? 0.95f : 0.75f);
                string info = (slot != null && slot.hasData)
                    ? $"Slot {i + 1}: {slot.playerName}  Round {slot.round}  {Mathf.FloorToInt(slot.distance)}m  Score {slot.score}  {slot.savedAtUtc}"
                    : $"Slot {i + 1}: Empty";
                Color c = selected ? new Color(1f, 0.95f, 0.6f) : Color.white;
                DrawLabel(new Rect(row.x + 16f, row.y + 10f, row.width - 32f, 30f), info, _smallLabelStyle, c);
                if (GUI.Button(new Rect(row.x + 16f, row.y + 48f, 160f, 36f), "SELECT", _smallButtonStyle)) _saveSlotSelected = i;
                if (GUI.Button(new Rect(row.x + 186f, row.y + 48f, 160f, 36f), "CREATE GAME", _smallButtonStyle))
                {
                    if (!hostCanSave) { gm?.ShowToast("Only host can save."); }
                    else if (gm != null) { gm.SaveCurrentGame(i); }
                }
                bool canJoin = slot != null && slot.hasData;
                if (GUI.Button(new Rect(row.x + 356f, row.y + 48f, 160f, 36f), "JOIN GAME", _smallButtonStyle))
                {
                    if (!hostCanSave) { gm?.ShowToast("Only host can load."); }
                    else if (gm != null && canJoin) { gm.LoadGameFromSlot(i); _screen = UiScreen.Hud; }
                    else gm?.ShowToast("Empty save slot");
                }
                if (GUI.Button(new Rect(row.x + 526f, row.y + 48f, 160f, 36f), "DELETE GAME", _smallButtonStyle))
                {
                    if (!hostCanSave) gm?.ShowToast("Only host can delete.");
                    else save?.DeleteSlot(i);
                }
            }

            if (DrawButton(cx - 120f, 590f, 240f, 50f, "BACK"))
            {
                if (_saveMenuReturn == UiScreen.Pause && gm != null && gm.State == GameState.Paused) _screen = UiScreen.Pause;
                else if (_saveMenuReturn == UiScreen.Lobby) _screen = UiScreen.Lobby;
                else if (_saveMenuReturn == UiScreen.SafeBase && gm != null && gm.State == GameState.SafeBase) _screen = UiScreen.SafeBase;
                else _screen = UiScreen.Menu;
            }
        }

        void DrawSafeBase()
        {
            var gm = GameManager.Instance;
            DrawDim(0.35f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 360f, 120f, 720f, 430f);
            DrawPanel(panel, 0.94f);
            DrawLabel(new Rect(panel.x, 140f, panel.width, 56f), "SAFE BASE", _titleStyle, new Color(0.6f, 1f, 0.6f));
            float timer = gm != null ? gm.SafeBaseTimer : 0f;
            int coins = CoinBank.Instance != null ? CoinBank.Instance.Coins : 0;
            DrawLabel(new Rect(panel.x, 200f, panel.width, 34f), $"Time left: {Mathf.CeilToInt(timer)}s   Coins: {coins}", _centerStyle, new Color(1f, 0.9f, 0.5f));
            DrawLabel(new Rect(panel.x + 40f, 244f, panel.width - 80f, 24f), "Race clean, buy 1 boost card per round end, and keep your lead.", _centerStyle, new Color(1f, 1f, 1f, 0.65f));

            bool isNet = LanNetworkManager.Instance != null && LanNetworkManager.Instance.InRoom;
            if (DrawButton(cx - 220f, 300f, 200f, 52f, "CONTINUE"))
            {
                if (isNet) { gm?.ShowToast("Vote Continue sent."); LanNetworkManager.Instance?.SendVote(true); }
                else gm?.ContinueFromSafeBase();
            }
            if (DrawButton(cx + 20f, 300f, 200f, 52f, "END GAME"))
            {
                if (isNet) { gm?.ShowToast("Vote End sent."); LanNetworkManager.Instance?.SendVote(false); }
                else gm?.EndGameFromSafeBase();
            }
            if (DrawButton(cx - 220f, 364f, 200f, 52f, "SAVE GAME"))
            {
                _screen = UiScreen.SaveMenu; _saveMenuReturn = UiScreen.SafeBase;
            }
            if (DrawButton(cx + 20f, 364f, 200f, 52f, "BOOST SHOP"))
            {
                gm?.OpenBoostShop();
            }

            DrawLabel(new Rect(panel.x, 440f, panel.width, 24f), isNet ? "Multiplayer: votes resolve Continue/End." : "Solo: timer auto-continues to next round if no choice.", _smallLabelStyle, new Color(1f, 1f, 1f, 0.55f));
        }

        void DrawRoundLeaderboard()
        {
            var gm = GameManager.Instance;
            DrawDim(0.55f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 420f, 70f, 840f, 560f);
            DrawPanel(panel, 0.95f);
            DrawLabel(new Rect(panel.x, 90f, panel.width, 58f), "ROUND LEADERBOARD", _titleStyle, new Color(1f, 0.86f, 0.3f));
            DrawLabel(new Rect(panel.x, 150f, panel.width, 26f), "Points awarded by finish placement. Waiting for next round...", _centerStyle, new Color(1f,1f,1f,0.65f));

            if (gm != null)
            {
                for (int i = 0; i < gm.RoundLeaderboardOrder.Count && i < 8; i++)
                {
                    int pid = gm.RoundLeaderboardOrder[i];
                    int pts = gm.PlayerRoundPoints.TryGetValue(pid, out int p) ? p : 0;
                    string name = "Player " + (pid + 1);
                    var pl = gm.Players.Find(x => x != null && x.PlayerIndex == pid);
                    var cos = pl != null ? pl.GetComponent<FrogRunner.Player.PlayerCosmetics>() : null;
                    if (cos != null && !string.IsNullOrEmpty(cos.PlayerName)) name = cos.PlayerName;
                    string place = i == 0 ? "1st" : i == 1 ? "2nd" : i == 2 ? "3rd" : (i + 1) + "th";
                    DrawLabel(new Rect(panel.x + 70f, 210f + i * 44f, panel.width - 140f, 34f),
                        $"{place}  {name}  +{pts} pts", _labelStyle, i == 0 ? new Color(1f,0.9f,0.4f) : Color.white);
                }
                DrawLabel(new Rect(panel.x, 560f, panel.width, 28f), $"Continuing to Boost Shop in {Mathf.CeilToInt(Mathf.Max(0, gm.LeaderboardTimer))}s", _centerStyle, new Color(0.8f,1f,0.8f,0.8f));
            }
        }

        void DrawBoostShop()
        {
            var gm = GameManager.Instance;
            DrawDim(0.6f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 500f, 55f, 1000f, 610f);
            DrawPanel(panel, 0.96f);
            int coins = CoinBank.Instance != null ? CoinBank.Instance.Coins : 0;
            DrawLabel(new Rect(panel.x, 72f, panel.width, 56f), "BOOST CARD SHOP", _titleStyle, new Color(1f, 0.85f, 0.25f));
            DrawLabel(new Rect(panel.x, 128f, panel.width, 28f), $"Pick 1 card for next round. Coins: {coins}", _centerStyle, new Color(1f,1f,1f,0.7f));

            if (gm != null && gm.BoostOffers != null)
            {
                for (int i = 0; i < gm.BoostOffers.Length && i < 3; i++)
                {
                    var offer = gm.BoostOffers[i];
                    float x = panel.x + 40f + i * 320f;
                    Rect card = new Rect(x, 190f, 290f, 300f);
                    DrawPanel(card, 0.9f);
                    DrawLabel(new Rect(card.x, card.y + 20f, card.width, 34f), offer.title, _centerStyle, new Color(1f,0.9f,0.45f));
                    DrawLabel(new Rect(card.x + 16f, card.y + 80f, card.width - 32f, 80f), offer.description, _smallLabelStyle, Color.white);
                    DrawLabel(new Rect(card.x, card.y + 170f, card.width, 30f), $"Cost: {offer.cost} coins", _centerStyle, new Color(0.9f,0.95f,0.6f));
                    bool canBuy = !gm.BoostPurchasedThisRound && coins >= offer.cost;
                    if (DrawButton(card.x + 30f, card.y + 220f, card.width - 60f, 48f, canBuy ? "BUY (1)" : (gm.BoostPurchasedThisRound ? "BOUGHT" : "NO COINS")))
                    {
                        if (canBuy) gm.BuyBoostOffer(i);
                    }
                }
            }

            if (DrawButton(cx - 160f, 540f, 320f, 54f, "CONTINUE")) { if (gm != null) gm.ContinueAfterBoostShop(); _screen = UiScreen.SafeBase; }
            DrawLabel(new Rect(panel.x, 500f, panel.width, 24f), "You may buy only ONE boost card per round end.", _smallLabelStyle, new Color(1f,1f,1f,0.55f));
        }

        static string SanitizeName(string n)
        {
            if (string.IsNullOrWhiteSpace(n)) return "";
            n = n.Trim();
            if (n.Length > 16) n = n.Substring(0, 16);
            return n;
        }

        bool IsDuplicateName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var net = LanNetworkManager.Instance;
            if (net != null && net.InRoom)
            {
                foreach (var p in net.LobbyPlayers)
                {
                    if (p.id != net.LocalPlayerId && string.Equals(p.name, name, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }
    }
}
