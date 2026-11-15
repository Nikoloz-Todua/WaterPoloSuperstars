using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Only references to the developer's imported art. No economy or inventory definitions.</summary>
    public sealed class MainMenuArt : ScriptableObject
    {
        public Sprite ranking, shop, team, battlePass, missions, worldTournament, friends, clubs, play;
        public Sprite coin, diamond, settings, messages, plus;
        public Sprite commonPack, rarePack, epicPack, legendaryPack;
        public Sprite mascot, profile, background;

        public Sprite PackSprite(RewardPack pack)
        {
            if (pack == null) return null;
            string rarity = (pack.rarity ?? "").ToLowerInvariant();
            if (rarity == "rare") return rarePack;
            if (rarity == "epic") return epicPack;
            if (rarity == "legendary") return legendaryPack;
            return rarity == "common" || rarity.Length == 0 && pack.type == "tide" ? commonPack : null;
        }
    }
}
