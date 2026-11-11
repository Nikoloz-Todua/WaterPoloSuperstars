using System;
using System.Collections.Generic;

namespace ToduStudios.WaterPolo.Hub
{
    [Serializable] public sealed class OwnedAthlete { public string playerId; public int level=1, shards; }
    [Serializable] public sealed class RewardPack { public string instanceId, type="tide", rarity="Common"; public long startUtc; public int duration, outcome; }
    [Serializable] public sealed class LocalMatchSession { public string id, mode; public List<string> lineup=new List<string>(); }
    [Serializable] public sealed class MatchReceipt { public string id,result,pack; public int blue,white,coins,xp,trophies; }
    [Serializable] public sealed class LocalProfile
    {
        public int schemaVersion=1; public long revision; public string profileId, displayName="Captain", avatarId="wave", seasonId="tidebreak-01";
        public int level=1,xp,trophies,coins=600,gems=20,seasonXp,played,wins,draws,goals,upgrades,packsOpened;
        public string tactics="Zone",formation="Classic Six"; public int capColor,swimwearColor=1,accentColor=2;
        public List<OwnedAthlete> collection=new List<OwnedAthlete>(); public List<string> lineup=new List<string>();
        public List<RewardPack> packs=new List<RewardPack>(); public List<string> transactions=new List<string>();
        public List<string> claimedRewards=new List<string>(); public List<MatchReceipt> history=new List<MatchReceipt>();
        public LocalMatchSession activeMatch; public MatchReceipt pendingResult; public long lastSeenUtc;
    }
    public enum CloudSyncStatus { LocalOnly, PendingSync, Synced, Conflict, SyncError }
    public interface ICloudSyncAdapter { CloudSyncStatus Status {get;} }
    public interface IPurchaseProvider { bool IsAvailable {get;} } // Future verified store provider, no implementation in M12.
    public interface IProfileRepository { LocalProfile Load(); void Save(LocalProfile profile); }
    public interface IEconomyService { bool CanAfford(int coins,int gems); }
    public interface IProgressionService { bool ClaimSeason(int level,out string message); }
    public interface IPlayerCollectionRepository { bool Assign(int slot,string id,out string message); }
    public interface IRewardService { bool ClaimPack(string id,out string message); }
}
