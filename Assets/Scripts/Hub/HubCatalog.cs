using System;
using System.Linq;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    [Serializable] public sealed class RarityDefinition { public string id; public Color color; public int upgradeCoins; }
    [Serializable] public sealed class AthleteDefinition
    {
        public string id, name, rarity, role; public bool goalkeeper; public int swim, stamina, shot, pass, defense, handling, keeping;
        public int Overall(int level) => Mathf.RoundToInt((swim+stamina+shot+pass+defense+handling+(goalkeeper?keeping:0))/(goalkeeper?7f:6f))+2*(level-1);
    }
    [Serializable] public sealed class RewardDefinition { public int coins, gems, shards; public string playerId; }
    [Serializable] public sealed class PackOutcome { public string label; public int weight; public RewardDefinition reward; }
    [Serializable] public sealed class OfferDefinition { public string id, title, section; public int coins, gems; public RewardDefinition reward; }
    [Serializable] public sealed class AchievementDefinition { public string id, title, counter; public int target, coins; }
    [CreateAssetMenu(menuName="Water Polo/Hub Catalog")]
    public sealed class HubCatalog : ScriptableObject
    {
        public string catalogVersion="m12.1", seasonId="tidebreak-01", seasonTitle="TIDEBREAK • FOUNDING SEASON";
        public int maxLevel=10, slotCapacity=4, unlockSeconds=45, seasonLevels=20, seasonXpPerLevel=60;
        public int winCoins=120, drawCoins=85, lossCoins=60, matchXp=40, matchSeasonXp=35, winTrophies=12, lossTrophies=3;
        public RarityDefinition[] rarities; public AthleteDefinition[] players; public OfferDefinition[] offers; public PackOutcome[] packOutcomes; public AchievementDefinition[] achievements;
        public AthleteDefinition Player(string id)=>players.FirstOrDefault(p=>p.id==id);
        public RarityDefinition Rarity(string id)=>rarities.First(p=>p.id==id);
        public int UpgradeCost(AthleteDefinition p,int level)=>Rarity(p.rarity).upgradeCoins*level;
        public int UpgradeShards(int level)=>level*5;
        public RewardDefinition SeasonReward(int level)=>new RewardDefinition{coins=80+level*10,gems=level%5==0?3:0};
        public void Seed()
        {
            rarities=new[]{R("Common",0x78B9CD,60),R("Rare",0x40A8FF,90),R("Epic",0xAD79F7,120),R("Legendary",0xFFC65A,160),R("Mythic",0xFF668E,200)};
            string[] names={"Mika Cove","Niko Reef","Ari Current","Luca Wave","Ivo Storm","Sami Drift","Remy Tide","Kian Harbor","Alex Foam","Noa Crest","Rio Surge","Eli Deep"};
            string[] roles={"Goalkeeper","Centre Forward","Left Wing","Right Wing","Centre Back","Left Flat","Right Flat","Left Wing","Goalkeeper","Centre Forward","Right Flat","Centre Back"};
            players=names.Select((n,i)=>new AthleteDefinition{id="wp_"+(i+1).ToString("000"),name=n,role=roles[i],goalkeeper=i==0||i==8,rarity=rarities[i==11?4:Math.Min(4,i/3)].id,swim=55+i*2,stamina=66,shot=60+i,pass=63,defense=62,handling=65,keeping=i==0||i==8?78:20}).ToArray();
            offers=new[]{new OfferDefinition{id="reef-card",title="Niko Reef • 15 shards",section="Player Cards",coins=150,reward=new RewardDefinition{playerId="wp_002",shards=15}},new OfferDefinition{id="crest-card",title="Noa Crest • unlock + 10 shards",section="Player Cards",coins=350,reward=new RewardDefinition{playerId="wp_010",shards=10}},new OfferDefinition{id="surge-card",title="Rio Surge • unlock + 10 shards",section="Featured",gems=12,reward=new RewardDefinition{playerId="wp_011",shards=10}},new OfferDefinition{id="keeper-material",title="Mika Cove • 20 shards",section="Upgrade Materials",coins=180,reward=new RewardDefinition{playerId="wp_001",shards=20}},new OfferDefinition{id="coin-bundle",title="Known bundle • 250 coins",section="Coins",gems=5,reward=new RewardDefinition{coins=250}}};
            packOutcomes=new[]{new PackOutcome{label="80 coins + 8 Reef shards",weight=60,reward=new RewardDefinition{coins=80,playerId="wp_002",shards=8}},new PackOutcome{label="120 coins + Noa Crest + 5 shards",weight=30,reward=new RewardDefinition{coins=120,playerId="wp_010",shards=5}},new PackOutcome{label="160 coins + Eli Deep + 3 gems",weight=10,reward=new RewardDefinition{coins=160,gems=3,playerId="wp_012",shards=5}}};
            achievements=new[]{A("first-win","First Victory","wins",1),A("first-goal","First Goal","goals",1),A("five-wins","Five Wins","wins",5),A("ten-matches","Ten Completed Matches","played",10),A("first-upgrade","First Player Upgrade","upgrades",1),A("first-pack","First Pack Opened","packs",1)};
        }
        static AchievementDefinition A(string id,string title,string counter,int target)=>new AchievementDefinition{id=id,title=title,counter=counter,target=target,coins=100};
        static RarityDefinition R(string id,int hex,int cost)=>new RarityDefinition{id=id,color=new Color(((hex>>16)&255)/255f,((hex>>8)&255)/255f,(hex&255)/255f),upgradeCoins=cost};
    }
}
