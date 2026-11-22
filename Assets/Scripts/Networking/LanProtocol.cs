using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FrogRunner.Networking
{
    [Serializable] public class LanRoomInfo { public string roomName; public string hostIp; public int port; public int players; public int maxPlayers; public long lastSeenMs; }
    [Serializable] public class LanPlayerInfo { public string id; public string name; public bool ready; public bool host; public bool finished; public bool dead; }
    [Serializable] public class LanState { public string id; public float x; public float y; public float z; public float vy; public bool grounded; public bool gliding; public bool dashing; public bool tumbling; public bool crouching; public bool dead; public float distance; public int score; public int hearts; public int round; }
    [Serializable] public class LanMessage { public string type; public string roomName; public string hostIp; public int port; public string playerId; public string playerName; public bool ready; public string payload; }
    [Serializable] public class LanPlayersPayload { public List<LanPlayerInfo> players = new List<LanPlayerInfo>(); }
    [Serializable] public class LanStatesPayload { public List<LanState> states = new List<LanState>(); }

    public static class LanProtocol
    {
        public const int DiscoveryPort = 47777;
        public const int GamePort = 47778;
        public const string DiscoverRequest = "FR_DISCOVER";
        public const string DiscoverResponse = "FR_ROOM";
        public const float BroadcastInterval = 1.0f;
        public const float RoomTimeoutSeconds = 3.5f;

        public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);
        public static string Utf8(byte[] b, int len) => Encoding.UTF8.GetString(b, 0, len);
        public static string Json<T>(T obj) => JsonUtility.ToJson(obj);
        public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    }
}
