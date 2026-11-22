using System;
using System.Collections.Generic;

namespace FrogRunner.SaveSystem
{
    [Serializable]
    public class SaveSlotData
    {
        public string slotName = "Slot";
        public bool hasData;
        public string playerName = "Frog";
        public int round = 1;
        public float distance;
        public int score;
        public int hearts = 3;
        public float playerX;
        public float playerY;
        public float playerZ;
        public string savedAtUtc;
    }

    [Serializable]
    public class SaveFile
    {
        public List<SaveSlotData> slots = new List<SaveSlotData>();
    }

    [Serializable]
    public class PlayerProfileData
    {
        public string playerName = "Frog";
        public int frogColorIndex = 0;
        public int hatIndex = 0;
    }
}
