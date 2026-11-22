using System;
using System.IO;
using UnityEngine;

namespace FrogRunner.SaveSystem
{
    /// <summary>Simple JSON save/profile storage in persistentDataPath. Lightweight for mobile/low-spec.</summary>
    [DisallowMultipleComponent]
    public class SaveSystemManager : MonoBehaviour
    {
        public static SaveSystemManager Instance { get; private set; }

        public const int SlotCount = 3;
        public SaveFile SaveFileData { get; private set; } = new SaveFile();
        public PlayerProfileData Profile { get; private set; } = new PlayerProfileData();

        string SavePath => Path.Combine(Application.persistentDataPath, "frogrunner_saves.json");
        string ProfilePath => Path.Combine(Application.persistentDataPath, "frogrunner_profile.json");

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadAll();
            EnsureSlots();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void EnsureSlots()
        {
            while (SaveFileData.slots.Count < SlotCount)
                SaveFileData.slots.Add(new SaveSlotData { slotName = "Slot " + (SaveFileData.slots.Count + 1), hasData = false });
        }

        public void LoadAll()
        {
            SaveFileData = ReadJson<SaveFile>(SavePath) ?? new SaveFile();
            Profile = ReadJson<PlayerProfileData>(ProfilePath) ?? new PlayerProfileData();
            EnsureSlots();
        }

        public void SaveAll()
        {
            EnsureSlots();
            WriteJson(SavePath, SaveFileData);
            WriteJson(ProfilePath, Profile);
        }

        public void SaveProfile() => WriteJson(ProfilePath, Profile);

        public void CreateOrOverwriteSlot(int slotIndex, SaveSlotData data)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            EnsureSlots();
            data.hasData = true;
            data.slotName = "Slot " + (slotIndex + 1);
            data.savedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "Z";
            SaveFileData.slots[slotIndex] = data;
            SaveAll();
        }

        public SaveSlotData GetSlot(int slotIndex)
        {
            EnsureSlots();
            if (slotIndex < 0 || slotIndex >= SlotCount) return null;
            return SaveFileData.slots[slotIndex];
        }

        public void DeleteSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            EnsureSlots();
            SaveFileData.slots[slotIndex] = new SaveSlotData { slotName = "Slot " + (slotIndex + 1), hasData = false };
            SaveAll();
        }

        static T ReadJson<T>(string path) where T : class
        {
            try { if (!File.Exists(path)) return null; return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
            catch { return null; }
        }

        static void WriteJson(string path, object obj)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonUtility.ToJson(obj, true));
            }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }
    }
}
