using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Single local transaction authority. Mutate a copy, validate, persist, then publish it.</summary>
    public sealed class HubProgression : IEconomyService,IProgressionService,IPlayerCollectionRepository,IRewardService
    {
        readonly IProfileRepository repository; readonly Func<long> utc; LocalProfile profile;
        public HubCatalog Catalog {get;} public event Action Changed;
        public LocalProfile Snapshot=>Copy(profile);
        static LocalProfile Copy(LocalProfile p)=>JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(p));
        public HubProgression(HubCatalog catalog,IProfileRepository repository,Func<long> clock=null)
        {
            Catalog=catalog;this.repository=repository;utc=clock??(()=>DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if(repository is JsonProfileRepository disk)disk.SetContentValidation(Validate);
            profile=repository.Load();
            if(profile==null)
            {
                profile=new LocalProfile{profileId=Guid.NewGuid().ToString("N"),seasonId=catalog.seasonId};
                foreach(var def in catalog.players.Take(9))profile.collection.Add(new OwnedAthlete{playerId=def.id,shards=20});
                profile.lineup=catalog.players.Take(7).Select(p=>p.id).ToList();
                profile.packs.Add(new RewardPack{instanceId=Guid.NewGuid().ToString("N"),duration=catalog.unlockSeconds,outcome=0});
                Validate(profile);repository.Save(profile);
            }
            Validate(profile);
        }
        void Validate(LocalProfile p)
        {
            JsonProfileRepository.Validate(p);
            if(p.capColor<0||p.capColor>3||p.swimwearColor<0||p.swimwearColor>3||p.accentColor<0||p.accentColor>3||string.IsNullOrWhiteSpace(p.displayName)||p.displayName.Length>18||!new[]{"wave","anchor","sun","reef"}.Contains(p.avatarId)||p.played<0||p.wins<0||p.draws<0||p.goals<0||p.upgrades<0||p.packsOpened<0||p.transactions.Any(string.IsNullOrWhiteSpace)||p.claimedRewards.Any(string.IsNullOrWhiteSpace))throw new InvalidDataException("Invalid profile values; progress retained.");
            if(p.level<1 || p.trophies<0 || p.collection.Any(x=>x==null || x.level<1 || x.level>Catalog.maxLevel || x.shards<0) || p.collection.Select(x=>x.playerId).Distinct().Count()!=p.collection.Count || p.packs.Any(x=>x==null || string.IsNullOrEmpty(x.instanceId) || x.duration<1 || x.startUtc<0 || x.outcome<0 || x.outcome>=Catalog.packOutcomes.Length) || p.packs.Select(x=>x.instanceId).Distinct().Count()!=p.packs.Count)throw new InvalidDataException("Invalid collection or pack state; progress retained.");
            if(p.lineup.Count!=7 || p.lineup.Distinct().Count()!=7 || p.lineup.Any(id=>Catalog.Player(id)==null || !p.collection.Any(x=>x.playerId==id)) || !Catalog.Player(p.lineup[0]).goalkeeper || p.lineup.Skip(1).Any(id=>Catalog.Player(id).goalkeeper))throw new InvalidDataException("Invalid starting lineup; progress retained.");
        }
        bool Transaction(string id,Action<LocalProfile> change,out string message)
        {
            if(string.IsNullOrWhiteSpace(id)){message="Invalid transaction.";return false;}
            if(profile.transactions.Contains(id)){message="Already received / applied.";return false;}
            try
            {
                var next=Copy(profile);change(next);next.transactions.Add(id);next.revision=checked(next.revision+1);next.lastSeenUtc=Math.Max(next.lastSeenUtc,utc());Validate(next);repository.Save(next);profile=next;
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is InvalidOperationException || e is ArgumentException || e is OverflowException){message=e is IOException||e is UnauthorizedAccessException?"Save unavailable. No transaction applied; retry safely.":e.Message;return false;}
            message="Saved locally.";Changed?.Invoke();return true;
        }
        public bool CanAfford(int coins,int gems)=>coins>=0&&gems>=0&&profile.coins>=coins&&profile.gems>=gems;
        static void Spend(LocalProfile p,int coins,int gems){if(coins<0||gems<0||p.coins<coins||p.gems<gems)throw new InvalidOperationException("Insufficient currency.");p.coins-=coins;p.gems-=gems;}
        void Grant(LocalProfile p,RewardDefinition r)
        {
            if(r==null || r.coins<0 || r.gems<0 || r.shards<0)throw new InvalidOperationException("Invalid reward.");
            p.coins=checked(p.coins+r.coins);p.gems=checked(p.gems+r.gems);
            if(!string.IsNullOrEmpty(r.playerId))
            {
                if(Catalog.Player(r.playerId)==null)throw new InvalidOperationException("Catalog player unavailable.");
                var owned=p.collection.FirstOrDefault(x=>x.playerId==r.playerId);if(owned==null){owned=new OwnedAthlete{playerId=r.playerId};p.collection.Add(owned);}owned.shards=checked(owned.shards+r.shards);
            }
        }
        public bool Purchase(string offerId,string requestId,out string message)=>Transaction("shop:"+requestId,p=>{var offer=Catalog.offers.FirstOrDefault(x=>x.id==offerId);if(offer==null)throw new InvalidOperationException("Offer unavailable.");Spend(p,offer.coins,offer.gems);Grant(p,offer.reward);},out message);
        public bool Upgrade(string id,int expectedLevel,out string message)=>Transaction("upgrade:"+id+":"+expectedLevel,p=>
        {
            var owned=p.collection.FirstOrDefault(x=>x.playerId==id);var def=Catalog.Player(id);
            if(owned==null||def==null||owned.level!=expectedLevel||owned.level>=Catalog.maxLevel)throw new InvalidOperationException("Upgrade unavailable.");
            int shards=Catalog.UpgradeShards(owned.level);if(owned.shards<shards)throw new InvalidOperationException("More upgrade shards needed.");
            Spend(p,Catalog.UpgradeCost(def,owned.level),0);owned.shards-=shards;owned.level++;p.upgrades++;
        },out message);
        public bool Assign(int slot,string id,out string message)=>Transaction("lineup:"+Guid.NewGuid().ToString("N"),p=>
        {
            var def=Catalog.Player(id);if(slot<0||slot>6||def==null||!p.collection.Any(x=>x.playerId==id)||def.goalkeeper!=(slot==0))throw new InvalidOperationException("Choose an owned player with the correct role.");
            int previous=p.lineup.IndexOf(id);string old=p.lineup[slot];p.lineup[slot]=id;if(previous>=0)p.lineup[previous]=old;
        },out message);
        public bool SetTactics(string value,out string message)=>Transaction("tactics:"+Guid.NewGuid().ToString("N"),p=>{if(value!="Press"&&value!="Zone")throw new InvalidOperationException("Unsupported tactic.");p.tactics=value;},out message);
        public bool Customize(string name,string avatar,int cap,int suit,int accent,out string message)=>Transaction("profile:"+Guid.NewGuid().ToString("N"),p=>
        {
            name=(name??"").Trim();if(name.Length<2||name.Length>18||name.Any(char.IsControl)||name.Contains("<")||name.Contains(">"))throw new InvalidOperationException("Use 2–18 plain-text characters.");
            if(!new[]{"wave","anchor","sun","reef"}.Contains(avatar)||cap<0||cap>3||suit<0||suit>3||accent<0||accent>3)throw new InvalidOperationException("Invalid appearance.");
            p.displayName=name;p.avatarId=avatar;p.capColor=cap;p.swimwearColor=suit;p.accentColor=accent;
        },out message);
        public int Counter(string name){switch(name){case "wins":return profile.wins;case "goals":return profile.goals;case "played":return profile.played;case "upgrades":return profile.upgrades;case "packs":return profile.packsOpened;default:return 0;}}
        public bool ClaimAchievement(string id,out string message)=>Transaction("achievement:"+id,p=>{var a=Catalog.achievements.FirstOrDefault(x=>x.id==id);if(a==null||Counter(a.counter)<a.target)throw new InvalidOperationException("Achievement not yet complete.");Grant(p,new RewardDefinition{coins=a.coins});p.claimedRewards.Add("achievement:"+id);},out message);
        public bool ClaimSeason(int level,out string message)=>Transaction("season:"+Catalog.seasonId+":"+level,p=>
        {if(p.seasonId!=Catalog.seasonId||level<1||level>Catalog.seasonLevels||p.seasonXp<level*Catalog.seasonXpPerLevel)throw new InvalidOperationException("Earn more season XP.");Grant(p,Catalog.SeasonReward(level));p.claimedRewards.Add("season:"+Catalog.seasonId+":"+level);},out message);
        public long Remaining(RewardPack pack)=>pack.startUtc==0?pack.duration:Math.Max(0,Math.Min(pack.duration,pack.duration-(utc()-pack.startUtc)));
        public bool StartPack(string id,out string message)=>Transaction("unlock:"+id,p=>
        {var pack=p.packs.FirstOrDefault(x=>x.instanceId==id);if(pack==null||pack.startUtc!=0)throw new InvalidOperationException("Pack is already unlocking.");pack.startUtc=Math.Max(1,utc());},out message);
        public bool ClaimPack(string id,out string message)=>Transaction("pack:"+id,p=>
        {var pack=p.packs.FirstOrDefault(x=>x.instanceId==id);if(pack==null||pack.startUtc==0||Remaining(pack)>0)throw new InvalidOperationException("Pack is not ready yet.");Grant(p,Catalog.packOutcomes[pack.outcome].reward);p.packs.Remove(pack);p.packsOpened++;},out message);
        int Roll(string id)
        {
            // Stable outcome fixed at acquisition. Animations and reloads never reroll rewards.
            uint hash=2166136261;foreach(char c in id)hash=unchecked((hash^c)*16777619);int total=Catalog.packOutcomes.Sum(x=>Math.Max(0,x.weight));int roll=(int)(hash%(uint)Math.Max(1,total));
            for(int i=0;i<Catalog.packOutcomes.Length;i++){roll-=Catalog.packOutcomes[i].weight;if(roll<0)return i;}return 0;
        }
        public bool BeginMatch(string mode,out string message)=>Transaction("launch:"+Guid.NewGuid().ToString("N"),p=>
        {if(mode!="quick"&&mode!="training")throw new InvalidOperationException("Mode unavailable.");p.activeMatch=new LocalMatchSession{id=Guid.NewGuid().ToString("N"),mode=mode,lineup=p.lineup.ToList()};p.pendingResult=null;},out message);
        public bool CompleteMatch(string sessionId,int blue,int white,out string message)=>Transaction("match:"+sessionId,p=>
        {
            if(p.activeMatch==null||p.activeMatch.id!=sessionId||blue<0||white<0)throw new InvalidOperationException("No eligible active match.");
            bool eligible=p.activeMatch.mode=="quick";var receipt=new MatchReceipt{id=sessionId,blue=blue,white=white,result=blue==white?"DRAW":blue>white?"VICTORY":"DEFEAT"};
            if(eligible)
            {
                receipt.coins=blue>white?Catalog.winCoins:blue==white?Catalog.drawCoins:Catalog.lossCoins;receipt.xp=Catalog.matchXp;receipt.trophies=blue>white?Catalog.winTrophies:blue==white?0:-Math.Min(p.trophies,Catalog.lossTrophies);
                Grant(p,new RewardDefinition{coins=receipt.coins});p.xp+=receipt.xp;p.level=1+p.xp/100;p.trophies+=receipt.trophies;p.seasonXp+=Catalog.matchSeasonXp;p.played++;p.goals+=blue;if(blue>white)p.wins++;if(blue==white)p.draws++;
                if(p.packs.Count<Catalog.slotCapacity){var pack=new RewardPack{instanceId="match-"+sessionId,duration=Catalog.unlockSeconds};pack.outcome=Roll(pack.instanceId);p.packs.Add(pack);receipt.pack="Tide pack received";}else receipt.pack="Slots full • pack refused; no existing pack removed";
            }
            else receipt.pack="Practice • no progression rewards";
            p.history.Add(receipt);if(p.history.Count>20)p.history.RemoveAt(0);p.pendingResult=receipt;p.activeMatch=null;
        },out message);
        public bool LeaveMatch(out string message)=>Transaction("leave:"+Guid.NewGuid().ToString("N"),p=>p.activeMatch=null,out message);
        public bool DismissResult(out string message)=>Transaction("result:"+Guid.NewGuid().ToString("N"),p=>p.pendingResult=null,out message);
    }
}
