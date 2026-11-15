using System.Linq;
using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Asset-driven home presentation. Existing HubView owns routes, dialogs and pack actions.</summary>
    public sealed class MainMenuHomeView : MonoBehaviour
    {
        public MainMenuArt art;
        public HubView hub;
        public GameObject backdrop;
        public UnityEngine.UI.Button profile, settings, messages, diamondPlus, coinPlus;
        public UnityEngine.UI.Button ranking, shop, team, battlePass, missions, friends, clubs, worldTournament, play;
        public TMP_Text displayName, level, diamonds, coins, missionCount, seasonCount;
        public UnityEngine.UI.Image levelFill;
        public GameObject missionBadge, seasonBadge;
        public MainMenuPackSlot[] slots;
        void Awake()
        {
            profile.onClick.AddListener(() => hub.Show("Profile")); settings.onClick.AddListener(() => hub.Show("Settings"));
            messages.onClick.AddListener(() => hub.ShowComingSoon("Messages"));
            diamondPlus.onClick.AddListener(() => hub.OpenShop("Gems")); coinPlus.onClick.AddListener(() => hub.OpenShop("Coins"));
            ranking.onClick.AddListener(() => hub.Show("Trophies")); shop.onClick.AddListener(() => hub.Show("Shop")); team.onClick.AddListener(() => hub.Show("Tactics"));
            battlePass.onClick.AddListener(() => hub.Show("Season Pass")); missions.onClick.AddListener(() => hub.Show("Missions"));
            friends.onClick.AddListener(() => hub.ShowComingSoon("Friends")); clubs.onClick.AddListener(() => hub.ShowComingSoon("Clubs"));
            worldTournament.onClick.AddListener(() => hub.ShowComingSoon("World Tournament")); play.onClick.AddListener(() => hub.Show("Play"));
            foreach (var slot in slots) { var target = slot; target.button.onClick.AddListener(() => { if (!string.IsNullOrEmpty(target.PackId)) hub.OpenRewardPack(target.PackId); }); }
        }
        public void Refresh(LocalProfile profileData)
        {
            displayName.text = profileData.displayName;
            level.text = $"LEVEL {profileData.level}   {profileData.xp % 100}/100 XP";
            levelFill.fillAmount = (profileData.xp % 100) / 100f;
            levelFill.rectTransform.anchorMax = new Vector2(levelFill.fillAmount, 1f);
            diamonds.text = profileData.gems.ToString("N0"); coins.text = profileData.coins.ToString("N0");
            int completed = hub.Service.Catalog.achievements.Count(a => hub.Service.Counter(a.counter) >= a.target && !profileData.claimedRewards.Contains("achievement:" + a.id));
            int available = 0;
            if (profileData.seasonId == hub.Service.Catalog.seasonId)
                for (int tier = 1; tier <= hub.Service.Catalog.seasonLevels; tier++)
                    if (profileData.seasonXp >= tier * hub.Service.Catalog.seasonXpPerLevel && !profileData.claimedRewards.Contains("season:" + profileData.seasonId + ":" + tier)) available++;
            missionBadge.SetActive(completed > 0); missionCount.text = completed.ToString();
            seasonBadge.SetActive(available > 0); seasonCount.text = available.ToString();
            RefreshPacks(profileData);
        }
        public void RefreshPacks(LocalProfile profileData)
        {
            for (int i = 0; i < slots.Length; i++) slots[i].Refresh(i < profileData.packs.Count ? profileData.packs[i] : null, hub.Service, art);
        }
    }
}
