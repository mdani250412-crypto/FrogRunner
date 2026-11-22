using FrogRunner.Core;
using FrogRunner.Networking;
using UnityEngine;

namespace FrogRunner.UI
{
    public partial class RetroUI
    {
        string _playerName = "Frog";
        string _manualIp = "127.0.0.1";
        Vector2 _roomsScroll;
        Vector2 _lobbyScroll;

        void DrawMultiplayerMenu()
        {
            DrawDim(0.65f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 460f, 40f, 920f, 640f);
            DrawPanel(panel, 0.96f);
            DrawLabel(new Rect(panel.x, 58f, panel.width, 54f), "LAN MULTIPLAYER", _titleStyle, new Color(1f, 0.84f, 0.22f));
            DrawLabel(new Rect(panel.x, 112f, panel.width, 24f), "Same Wi-Fi or mobile hotspot. No external servers.", _centerStyle, new Color(1f, 1f, 1f, 0.62f));

            var net = LanNetworkManager.Instance;
            if (net == null) { DrawLabel(new Rect(panel.x, 300f, panel.width, 30f), "Network manager missing", _centerStyle, Color.red); return; }

            GUI.Label(new Rect(panel.x + 44f, 160f, 180f, 28f), "PLAYER NAME", _smallLabelStyle);
            _playerName = GUI.TextField(new Rect(panel.x + 220f, 154f, 260f, 34f), _playerName, 16);
            net.LocalPlayerName = SanitizeName(_playerName);
            if (string.IsNullOrEmpty(net.LocalPlayerName)) net.LocalPlayerName = "Frog";

            if (DrawSmallButton(new Rect(panel.x + 500f, 154f, 160f, 34f), "HOST ROOM"))
            {
                if (IsDuplicateName(net.LocalPlayerName)) { net.StatusMessage = "Name already in use in room."; }
                else { net.HostRoom(net.LocalPlayerName + "'s Room"); _screen = UiScreen.Lobby; }
            }
            if (DrawSmallButton(new Rect(panel.x + 670f, 154f, 100f, 34f), "REFRESH")) net.Discovery?.Refresh();
            if (DrawSmallButton(new Rect(panel.x + 780f, 154f, 96f, 34f), "BACK")) { net.LeaveRoom(); _screen = UiScreen.Menu; }

            GUI.Label(new Rect(panel.x + 44f, 206f, 200f, 24f), "AVAILABLE ROOMS (LAN AUTO-SCAN)", _smallLabelStyle);
            Rect listRect = new Rect(panel.x + 44f, 234f, panel.width - 88f, 250f);
            GUI.Box(listRect, GUIContent.none, _panelStyle);
            _roomsScroll = GUI.BeginScrollView(listRect, _roomsScroll, new Rect(0, 0, listRect.width - 24f, 40f + (net.Discovery?.Rooms.Count ?? 0) * 46f));
            int i = 0;
            if (net.Discovery != null)
            {
                foreach (var kv in net.Discovery.Rooms)
                {
                    var r = kv.Value;
                    GUI.Label(new Rect(10, 8 + i * 46, 420, 30), $"{r.roomName}  [{r.players}/{r.maxPlayers}]  {r.hostIp}", _smallLabelStyle);
                    if (GUI.Button(new Rect(listRect.width - 140f, 6 + i * 46, 100, 32), "JOIN", _smallButtonStyle))
                    {
                        net.JoinRoom(r.hostIp, r.port);
                        _screen = UiScreen.Lobby;
                    }
                    i++;
                }
            }
            if (i == 0) GUI.Label(new Rect(10, 10, 500, 30), "No rooms found yet. Host one or use manual IP.", _smallLabelStyle);
            GUI.EndScrollView();

            GUI.Label(new Rect(panel.x + 44f, 506f, 170f, 26f), "MANUAL IP FALLBACK", _smallLabelStyle);
            _manualIp = GUI.TextField(new Rect(panel.x + 220f, 502f, 240f, 34f), _manualIp, 64);
            if (DrawSmallButton(new Rect(panel.x + 470f, 502f, 120f, 34f), "JOIN IP")) {
                if (IsDuplicateName(net.LocalPlayerName)) net.StatusMessage = "Choose a unique name before joining.";
                else { net.JoinRoom(_manualIp.Trim(), LanProtocol.GamePort); _screen = UiScreen.Lobby; }
            }

            DrawLabel(new Rect(panel.x + 44f, 560f, panel.width - 88f, 24f), "STATUS: " + net.StatusMessage, _smallLabelStyle, new Color(0.8f, 1f, 0.8f, 0.8f));
            DrawLabel(new Rect(panel.x + 44f, 590f, panel.width - 88f, 24f), "Tip: If UDP broadcast is blocked, enter the host's local IP manually.", _smallLabelStyle, new Color(1f, 1f, 1f, 0.55f));
        }

        void DrawLobby()
        {
            DrawDim(0.68f);
            float cx = _vw * 0.5f;
            Rect panel = new Rect(cx - 420f, 50f, 840f, 620f);
            DrawPanel(panel, 0.96f);
            var net = LanNetworkManager.Instance;
            if (net == null) { _screen = UiScreen.Menu; return; }
            if (!net.InRoom) { _screen = UiScreen.Multiplayer; return; }

            DrawLabel(new Rect(panel.x, 66f, panel.width, 52f), "ROOM LOBBY", _titleStyle, new Color(1f, 0.84f, 0.22f));
            DrawLabel(new Rect(panel.x, 118f, panel.width, 24f), net.RoomName + "  |  " + LanDiscovery.GetLocalIPAddress(), _centerStyle, new Color(1f, 1f, 1f, 0.62f));

            Rect list = new Rect(panel.x + 48f, 160f, panel.width - 96f, 290f);
            GUI.Box(list, GUIContent.none, _panelStyle);
            _lobbyScroll = GUI.BeginScrollView(list, _lobbyScroll, new Rect(0, 0, list.width - 24f, 40 + net.LobbyPlayers.Count * 42f));
            for (int i = 0; i < net.LobbyPlayers.Count; i++)
            {
                var p = net.LobbyPlayers[i];
                string status = p.ready ? "READY" : "NOT READY";
                string host = p.host ? " [HOST]" : "";
                GUI.Label(new Rect(12, 8 + i * 42, 420, 30), p.name + host, _smallLabelStyle);
                Color prev = GUI.color; GUI.color = p.ready ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.75f, 0.45f);
                GUI.Label(new Rect(list.width - 160f, 8 + i * 42, 140, 30), status, _smallLabelStyle);
                GUI.color = prev;
            }
            GUI.EndScrollView();

            bool localReady = false;
            var me = net.LobbyPlayers.Find(x => x.id == net.LocalPlayerId);
            if (me != null) localReady = me.ready;
            if (DrawButton(cx - 310f, 480f, 190f, 52f, localReady ? "UNREADY" : "READY")) net.SetReady(!localReady);
            if (DrawButton(cx - 110f, 480f, 190f, 52f, "SAVES")) { _screen = UiScreen.SaveMenu; _saveMenuReturn = UiScreen.Lobby; }
            if (DrawButton(cx + 90f, 480f, 190f, 52f, "LEAVE/BACK")) { net.LeaveRoom(); _screen = UiScreen.Multiplayer; }

            DrawLabel(new Rect(panel.x + 40f, 552f, panel.width - 80f, 24f),
                "Game starts when ALL connected players are READY.", _centerStyle, new Color(0.82f, 1f, 0.82f, 0.8f));
            DrawLabel(new Rect(panel.x + 40f, 584f, panel.width - 80f, 24f),
                "STATUS: " + net.StatusMessage, _smallLabelStyle, new Color(1f, 1f, 1f, 0.55f));
        }
    }
}
