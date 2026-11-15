using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    [CreateAssetMenu(menuName="Water Polo/Frontend Art")]
    public sealed class FrontendArt : ScriptableObject
    {
        public Sprite division1,premiumLeague,continentalCup,worldChampionsLeague,luckyShot,training,gameModesBackground,battlePassBackground;
        public Sprite Mode(string id)
        {
            switch(id){case "division_1":return division1;case "premium_league":return premiumLeague;case "continental_cup":return continentalCup;case "world_champions_league":return worldChampionsLeague;case "lucky_shot":return luckyShot;case "training":return training;default:return null;}
        }
    }
}
