using System;
using FrogRunner.Player;
using FrogRunner.PowerUps;
using UnityEngine;

namespace FrogRunner.Rounds
{
    public enum BoostCardType { SpeedBoost, SuperShield, CoinMagnet }

    [Serializable]
    public class BoostCardOffer
    {
        public BoostCardType type;
        public string title;
        public string description;
        public int cost;

        public static BoostCardOffer[] CreateThree(System.Random rng)
        {
            Array values = Enum.GetValues(typeof(BoostCardType));
            // pick 3 unique
            BoostCardType[] all = (BoostCardType[])values;
            for (int i = all.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = all[i]; all[i] = all[j]; all[j] = t; }
            var offers = new BoostCardOffer[3];
            for (int i = 0; i < 3; i++) offers[i] = FromType(all[i % all.Length]);
            // ensure unique types
            if (offers[1].type == offers[0].type) offers[1] = FromType(all[2 % all.Length]);
            if (offers[2].type == offers[0].type || offers[2].type == offers[1].type)
            {
                foreach (var v in all) { if (v != offers[0].type && v != offers[1].type) { offers[2] = FromType(v); break; } }
            }
            return offers;
        }

        public static BoostCardOffer FromType(BoostCardType t)
        {
            switch (t)
            {
                case BoostCardType.SpeedBoost:
                    return new BoostCardOffer { type = t, title = "SPEED BOOST", description = "Faster move + higher jumps next round.", cost = 8 };
                case BoostCardType.SuperShield:
                    return new BoostCardOffer { type = t, title = "SUPER SHIELD", description = "Start next round with a force field.", cost = 10 };
                default:
                    return new BoostCardOffer { type = t, title = "COIN MAGNET", description = "Longer magnet + score bonus next round.", cost = 7 };
            }
        }

        public void Apply(FrogController player)
        {
            if (player == null) return;
            var inv = player.GetComponent<PowerUpInventory>();
            if (inv == null) inv = player.gameObject.AddComponent<PowerUpInventory>();
            switch (type)
            {
                case BoostCardType.SpeedBoost:
                    inv.speedBoostDuration = Mathf.Max(inv.speedBoostDuration, 10f);
                    inv.Collect(PowerUpType.SpeedBoost);
                    break;
                case BoostCardType.SuperShield:
                    inv.shieldDuration = Mathf.Max(inv.shieldDuration, 12f);
                    inv.Collect(PowerUpType.ForceField);
                    break;
                case BoostCardType.CoinMagnet:
                    inv.magnetDuration = Mathf.Max(inv.magnetDuration, 14f);
                    inv.Collect(PowerUpType.ScoreMagnet);
                    break;
            }
        }
    }
}