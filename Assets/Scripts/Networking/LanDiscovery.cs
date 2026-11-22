using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace FrogRunner.Networking
{
    /// <summary>
    /// UDP LAN discovery over Wi-Fi/hotspot. Hosts broadcast; clients listen. Manual IP fallback remains in UI.
    /// </summary>
    public class LanDiscovery : MonoBehaviour
    {
        public readonly Dictionary<string, LanRoomInfo> Rooms = new Dictionary<string, LanRoomInfo>();
        UdpClient _listen;
        UdpClient _broadcast;
        float _nextBroadcast;
        bool _hosting;
        string _roomName = "Frog Room";
        int _playerCount;
        int _maxPlayers = 8;

        public void StartHosting(string roomName, int playerCount, int maxPlayers = 8)
        {
            _hosting = true;
            _roomName = string.IsNullOrWhiteSpace(roomName) ? "Frog Room" : roomName;
            _playerCount = playerCount; _maxPlayers = maxPlayers;
            EnsureBroadcast();
        }

        public void UpdateHosting(int playerCount) { _playerCount = playerCount; }

        public void StopHosting() { _hosting = false; CloseBroadcast(); }

        public void StartListening()
        {
            StopListening();
            try
            {
                _listen = new UdpClient();
                _listen.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listen.Client.Bind(new IPEndPoint(IPAddress.Any, LanProtocol.DiscoveryPort));
                _listen.EnableBroadcast = true;
                _listen.BeginReceive(OnReceive, null);
            }
            catch (Exception e) { Debug.LogWarning("[LAN] Discovery listen failed: " + e.Message); }
        }

        public void StopListening()
        {
            try { _listen?.Close(); } catch { }
            _listen = null;
        }

        public void Refresh()
        {
            Rooms.Clear();
            SendDiscoverRequest();
        }

        void EnsureBroadcast()
        {
            if (_broadcast != null) return;
            try { _broadcast = new UdpClient(); _broadcast.EnableBroadcast = true; }
            catch (Exception e) { Debug.LogWarning("[LAN] Broadcast socket failed: " + e.Message); }
        }

        void CloseBroadcast() { try { _broadcast?.Close(); } catch { } _broadcast = null; }

        void SendDiscoverRequest()
        {
            EnsureBroadcast();
            try
            {
                var msg = LanProtocol.Utf8(LanProtocol.DiscoverRequest);
                _broadcast?.Send(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, LanProtocol.DiscoveryPort));
            }
            catch { }
        }

        void Update()
        {
            // prune stale
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var stale = new List<string>();
            foreach (var kv in Rooms) if (now - kv.Value.lastSeenMs > LanProtocol.RoomTimeoutSeconds * 1000) stale.Add(kv.Key);
            for (int i = 0; i < stale.Count; i++) Rooms.Remove(stale[i]);

            if (_hosting)
            {
                _nextBroadcast -= Time.unscaledDeltaTime;
                if (_nextBroadcast <= 0f)
                {
                    _nextBroadcast = LanProtocol.BroadcastInterval;
                    BroadcastRoom();
                }
            }
        }

        void BroadcastRoom()
        {
            EnsureBroadcast();
            try
            {
                string ip = GetLocalIPAddress();
                var msg = new LanMessage { type = LanProtocol.DiscoverResponse, roomName = _roomName, hostIp = ip, port = LanProtocol.GamePort, payload = _playerCount + "/" + _maxPlayers };
                byte[] data = LanProtocol.Utf8(LanProtocol.Json(msg));
                _broadcast?.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, LanProtocol.DiscoveryPort));
            }
            catch { }
        }

        void OnReceive(IAsyncResult ar)
        {
            UdpClient c = _listen;
            if (c == null) return;
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] data;
            try { data = c.EndReceive(ar, ref ep); } catch { return; }
            try { c.BeginReceive(OnReceive, null); } catch { }

            try
            {
                string text = LanProtocol.Utf8(data, data.Length);
                if (text == LanProtocol.DiscoverRequest)
                {
                    if (_hosting) BroadcastRoom();
                    return;
                }
                var msg = LanProtocol.FromJson<LanMessage>(text);
                if (msg != null && msg.type == LanProtocol.DiscoverResponse)
                {
                    string ip = string.IsNullOrEmpty(msg.hostIp) ? ep.Address.ToString() : msg.hostIp;
                    string key = ip + ":" + msg.port;
                    int players = 0; int max = 8;
                    if (!string.IsNullOrEmpty(msg.payload))
                    {
                        var parts = msg.payload.Split('/');
                        if (parts.Length > 0) int.TryParse(parts[0], out players);
                        if (parts.Length > 1) int.TryParse(parts[1], out max);
                    }
                    Rooms[key] = new LanRoomInfo { roomName = msg.roomName, hostIp = ip, port = msg.port, players = players, maxPlayers = max, lastSeenMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                }
            }
            catch { }
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                for (int i = 0; i < host.AddressList.Length; i++)
                {
                    var ip = host.AddressList[i];
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) return ip.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        void OnDestroy() { StopHosting(); StopListening(); }
        void OnDisable() { StopHosting(); StopListening(); }
    }
}
