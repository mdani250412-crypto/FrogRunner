using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FrogRunner.Core;
using FrogRunner.Player;
using UnityEngine;

namespace FrogRunner.Networking
{
    /// <summary>
    /// Simple LAN room networking: TCP server/client for lobby + state sync, UDP discovery via LanDiscovery.
    /// No external packages; works on same Wi-Fi/hotspot.
    /// </summary>
    [DisallowMultipleComponent]
    public class LanNetworkManager : MonoBehaviour
    {
        public static LanNetworkManager Instance { get; private set; }

        public LanDiscovery Discovery { get; private set; }
        public bool IsHost { get; private set; }
        public bool IsClient => _client != null;
        public bool InRoom => IsHost || IsClient;
        public string LocalPlayerId { get; private set; }
        public string LocalPlayerName { get; set; } = "Frog";
        public string RoomName { get; private set; } = "Frog Room";
        public readonly List<LanPlayerInfo> LobbyPlayers = new List<LanPlayerInfo>();
        public string StatusMessage = "";

        TcpListener _server;
        Thread _acceptThread;
        readonly List<TcpClient> _clients = new List<TcpClient>();
        readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        TcpClient _client;
        NetworkStream _clientStream;
        Thread _clientReadThread;
        float _stateSendTimer;
        bool _running;
        readonly Dictionary<string, LanState> _remoteStates = new Dictionary<string, LanState>();
        public IReadOnlyDictionary<string, LanState> RemoteStates => _remoteStates;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LocalPlayerId = Guid.NewGuid().ToString("N").Substring(0, 8);
            Discovery = gameObject.AddComponent<LanDiscovery>();
            Discovery.StartListening();
        }

        void OnDestroy() { Shutdown(); if (Instance == this) Instance = null; }
        void OnDisable() { /* keep alive across UI, shutdown on destroy */ }

        void Update()
        {
            while (_mainThread.TryDequeue(out var a)) { try { a(); } catch (Exception e) { Debug.LogWarning(e); } }
            if (InRoom) SyncStateTick();
        }

        public void HostRoom(string roomName)
        {
            LeaveRoom();
            IsHost = true;
            RoomName = string.IsNullOrWhiteSpace(roomName) ? "Frog Room" : roomName.Trim();
            LocalPlayerName = string.IsNullOrWhiteSpace(LocalPlayerName) ? "Host" : LocalPlayerName.Trim();
            LobbyPlayers.Clear();
            LobbyPlayers.Add(new LanPlayerInfo { id = LocalPlayerId, name = LocalPlayerName, ready = false, host = true });
            try
            {
                _server = new TcpListener(IPAddress.Any, LanProtocol.GamePort);
                _server.Start();
                _running = true;
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
                _acceptThread.Start();
                Discovery.StartHosting(RoomName, LobbyPlayers.Count);
                StatusMessage = "Hosting room on " + LanDiscovery.GetLocalIPAddress();
            }
            catch (Exception e)
            {
                StatusMessage = "Host failed: " + e.Message;
                Shutdown();
            }
        }

        public void JoinRoom(string ip, int port)
        {
            LeaveRoom();
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port);
                _clientStream = _client.GetStream();
                _running = true;
                _clientReadThread = new Thread(() => ReadLoop(_client, _clientStream, false)) { IsBackground = true };
                _clientReadThread.Start();
                SendToServer(new LanMessage { type = "join", playerId = LocalPlayerId, playerName = LocalPlayerName });
                StatusMessage = "Joined " + ip;
            }
            catch (Exception e)
            {
                StatusMessage = "Join failed: " + e.Message;
                LeaveRoom();
            }
        }

        public void LeaveRoom()
        {
            try { if (IsClient) SendToServer(new LanMessage { type = "leave", playerId = LocalPlayerId }); } catch { }
            Shutdown();
            LobbyPlayers.Clear();
            _remoteStates.Clear();
            StatusMessage = "Left room.";
            Discovery.StartListening();
        }

        public void SetReady(bool ready)
        {
            if (IsHost)
            {
                var p = LobbyPlayers.Find(x => x.id == LocalPlayerId);
                if (p != null) p.ready = ready;
                BroadcastLobby();
                CheckAllReady();
            }
            else if (IsClient)
            {
                SendToServer(new LanMessage { type = "ready", playerId = LocalPlayerId, ready = ready });
            }
        }

        readonly Dictionary<string, bool> _votes = new Dictionary<string, bool>();
        public void ResetVotes() { _votes.Clear(); }
        public void SendVote(bool continueGame)
        {
            if (IsHost)
            {
                _votes[LocalPlayerId] = continueGame;
                CheckVotes();
            }
            else if (IsClient)
            {
                SendToServer(new LanMessage { type = "vote", playerId = LocalPlayerId, ready = continueGame });
            }
        }

        void CheckVotes()
        {
            if (!IsHost) return;
            int continueVotes = 0, endVotes = 0;
            for (int i = 0; i < LobbyPlayers.Count; i++)
            {
                var p = LobbyPlayers[i];
                if (_votes.TryGetValue(p.id, out bool v)) { if (v) continueVotes++; else endVotes++; }
            }
            // Resolve when majority or all voted
            int total = LobbyPlayers.Count;
            int voted = _votes.Count;
            if (continueVotes > total / 2 || (voted == total && continueVotes >= endVotes))
            {
                _votes.Clear();
                BroadcastRaw(LanProtocol.Json(new LanMessage { type = "vote_result", ready = true }), null);
                GameManager.Instance?.ContinueFromSafeBase();
            }
            else if (endVotes > total / 2 || (voted == total && endVotes > continueVotes))
            {
                _votes.Clear();
                BroadcastRaw(LanProtocol.Json(new LanMessage { type = "vote_result", ready = false }), null);
                GameManager.Instance?.EndGameFromSafeBase();
            }
        }

        void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var c = _server.AcceptTcpClient();
                    lock (_clients) _clients.Add(c);
                    var stream = c.GetStream();
                    var t = new Thread(() => ReadLoop(c, stream, true)) { IsBackground = true };
                    t.Start();
                }
                catch { if (_running) Thread.Sleep(100); }
            }
        }

        void ReadLoop(TcpClient c, NetworkStream stream, bool fromClientToHost)
        {
            try
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
                {
                    while (_running && c.Connected)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        string msg = line;
                        _mainThread.Enqueue(() => HandleMessage(msg, c, fromClientToHost));
                    }
                }
            }
            catch { }
            finally
            {
                _mainThread.Enqueue(() => OnDisconnected(c, fromClientToHost));
            }
        }

        void OnDisconnected(TcpClient c, bool clientOnHost)
        {
            if (clientOnHost)
            {
                lock (_clients) _clients.Remove(c);
                // remove player by matching any unknown? easiest: rebuild on leave messages; keep simple
                LobbyPlayers.RemoveAll(p => p.id != LocalPlayerId && !_clientIds.ContainsValue(c)); // fallback cleanup
                BroadcastLobby();
                Discovery.UpdateHosting(LobbyPlayers.Count);
                try { c.Close(); } catch { }
            }
            else
            {
                StatusMessage = "Disconnected from host.";
                Shutdown();
            }
        }

        readonly Dictionary<string, TcpClient> _clientIds = new Dictionary<string, TcpClient>();

        void HandleMessage(string json, TcpClient sender, bool fromClientToHost)
        {
            LanMessage msg;
            try { msg = LanProtocol.FromJson<LanMessage>(json); } catch { return; }
            if (msg == null || string.IsNullOrEmpty(msg.type)) return;

            if (IsHost && fromClientToHost)
            {
                switch (msg.type)
                {
                    case "join":
                        {
                            // Enforce unique names
                            string requested = string.IsNullOrWhiteSpace(msg.playerName) ? "Frog" : msg.playerName.Trim();
                            bool dup = LobbyPlayers.Exists(p => !string.Equals(p.id, msg.playerId, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(p.name, requested, StringComparison.OrdinalIgnoreCase));
                            if (dup)
                            {
                                var reject = new LanMessage { type = "join_rejected", payload = "Name already in use. Choose a different name." };
                                try { byte[] d = Encoding.UTF8.GetBytes(LanProtocol.Json(reject) + "\n"); sender.GetStream().Write(d, 0, d.Length); } catch { }
                                break;
                            }
                            _clientIds[msg.playerId] = sender;
                            var existing = LobbyPlayers.Find(p => p.id == msg.playerId);
                            if (existing != null) existing.name = requested;
                            else LobbyPlayers.Add(new LanPlayerInfo { id = msg.playerId, name = requested, ready = false, host = false });
                            Discovery.UpdateHosting(LobbyPlayers.Count);
                            BroadcastLobby();
                        }
                        break;
                    case "ready":
                        var p = LobbyPlayers.Find(x => x.id == msg.playerId);
                        if (p != null) p.ready = msg.ready;
                        BroadcastLobby();
                        CheckAllReady();
                        break;
                    case "leave":
                        LobbyPlayers.RemoveAll(x => x.id == msg.playerId);
                        _clientIds.Remove(msg.playerId);
                        BroadcastLobby();
                        Discovery.UpdateHosting(LobbyPlayers.Count);
                        break;
                    case "state":
                        HandleRemoteState(msg.payload);
                        BroadcastRaw(json, sender); // relay to others
                        break;
                    case "vote":
                        _votes[msg.playerId] = msg.ready;
                        CheckVotes();
                        break;
                }
            }
            else if (!IsHost)
            {
                switch (msg.type)
                {
                    case "lobby":
                        var payload = LanProtocol.FromJson<LanPlayersPayload>(msg.payload);
                        LobbyPlayers.Clear();
                        if (payload?.players != null) LobbyPlayers.AddRange(payload.players);
                        break;
                    case "start":
                        StatusMessage = "Starting...";
                        GameManager.Instance?.BeginNetworkedStart();
                        break;
                    case "state":
                        HandleRemoteState(msg.payload);
                        break;
                    case "join_rejected":
                        StatusMessage = msg.payload;
                        LeaveRoom();
                        break;
                    case "vote_result":
                        if (msg.ready) GameManager.Instance?.ContinueFromSafeBase();
                        else GameManager.Instance?.EndGameFromSafeBase();
                        break;
                }
            }
        }

        void HandleRemoteState(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;
            try
            {
                var st = LanProtocol.FromJson<LanState>(payload);
                if (st == null || string.IsNullOrEmpty(st.id) || st.id == LocalPlayerId) return;
                _remoteStates[st.id] = st;
            }
            catch { }
        }

        void BroadcastLobby()
        {
            var payload = new LanPlayersPayload { players = LobbyPlayers };
            var msg = new LanMessage { type = "lobby", payload = LanProtocol.Json(payload) };
            BroadcastRaw(LanProtocol.Json(msg), null);
        }

        void CheckAllReady()
        {
            if (LobbyPlayers.Count < 1) return;
            for (int i = 0; i < LobbyPlayers.Count; i++) if (!LobbyPlayers[i].ready) return;
            // All ready - start
            BroadcastRaw(LanProtocol.Json(new LanMessage { type = "start" }), null);
            GameManager.Instance?.BeginNetworkedStart();
        }

        void BroadcastRaw(string json, TcpClient except)
        {
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");
            lock (_clients)
            {
                for (int i = _clients.Count - 1; i >= 0; i--)
                {
                    var c = _clients[i];
                    if (c == except) continue;
                    try { c.GetStream().Write(data, 0, data.Length); } catch { }
                }
            }
        }

        void SendToServer(LanMessage msg)
        {
            if (_clientStream == null) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(LanProtocol.Json(msg) + "\n");
                _clientStream.Write(data, 0, data.Length);
            }
            catch { }
        }

        void SyncStateTick()
        {
            _stateSendTimer -= Time.deltaTime;
            if (_stateSendTimer > 0f) return;
            _stateSendTimer = 0.08f; // ~12.5Hz
            var gm = GameManager.Instance;
            var player = gm != null ? gm.Player : null;
            if (player == null) return;
            var h = player.GetComponent<PlayerHealth>();
            var st = new LanState
            {
                id = LocalPlayerId,
                x = player.transform.position.x, y = player.transform.position.y, z = player.transform.position.z,
                grounded = player.IsGrounded, gliding = player.IsGliding, dashing = player.IsDashing, tumbling = player.IsTumbling, crouching = player.IsCrouching,
                dead = h != null && h.IsDead, distance = gm.Distance, score = gm.Score, hearts = h != null ? h.Hearts : gm.Lives,
                round = FrogRunner.Rounds.RoundManager.Instance != null ? FrogRunner.Rounds.RoundManager.Instance.Round : 1
            };
            var msg = new LanMessage { type = "state", playerId = LocalPlayerId, payload = LanProtocol.Json(st) };
            string json = LanProtocol.Json(msg);
            if (IsHost) BroadcastRaw(json, null);
            else SendToServer(msg);
        }

        void Shutdown()
        {
            _running = false;
            try { _server?.Stop(); } catch { }
            _server = null;
            try { _client?.Close(); } catch { }
            _client = null; _clientStream = null;
            lock (_clients) { foreach (var c in _clients) try { c.Close(); } catch { } _clients.Clear(); }
            _clientIds.Clear();
            IsHost = false;
            Discovery?.StopHosting();
        }
    }
}
