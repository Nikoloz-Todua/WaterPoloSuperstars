using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    public sealed partial class HubView
    {
        void Packs(LocalProfile p)
        {
            for(int i=0;i<service.Catalog.slotCapacity;i++)
            {
                float width=.97f/service.Catalog.slotCapacity;var pack=i<p.packs.Count?p.packs[i]:null;
                var card=HubWidgets.Panel(content,"PackSlot"+i,.015f+i*width,.015f,width-.014f,.185f,HubWidgets.Card);
                HubWidgets.Text(card,"Title",pack==null?"EMPTY REWARD SLOT":"TIDE PACK • FREE",.06f,.65f,.88f,.27f,22,pack==null?HubWidgets.Muted:HubWidgets.Gold);
                HubWidgets.Text(card,"Timer",pack==null?"Complete Quick Match to earn":PackText(pack),.06f,.36f,.88f,.28f,21,Color.white).gameObject.name="PackTimer_"+(pack?.instanceId??i.ToString());
                HubWidgets.Button(card,"PackButton"+i,pack==null?"NO PACK":pack.startUtc==0?"START UNLOCK":"VIEW / OPEN",.06f,.05f,.88f,.27f,pack==null?null:(Action)(()=>PackPanel(pack.instanceId)),null,pack!=null);
            }
        }
        string PackText(RewardPack pack)=>pack.startUtc==0?"Waiting • "+pack.duration+" second unlock":service.Remaining(pack)==0?"READY TO OPEN":TimeSpan.FromSeconds(service.Remaining(pack)).ToString(@"mm\:ss")+" REMAINING";
        void UpdatePackTimers()
        {
            if(page!="Lobby")return;var p=service.Snapshot;
            foreach(var pack in p.packs){var t=content.Find("PackSlot"+p.packs.IndexOf(pack)+"/PackTimer_"+pack.instanceId);if(t!=null)t.GetComponent<TMP_Text>().text=PackText(pack);}
        }
        void PackPanel(string id)
        {
            var pack=service.Snapshot.packs.FirstOrDefault(x=>x.instanceId==id);if(pack==null)return;
            string odds=string.Join("\n",service.Catalog.packOutcomes.Select(x=>$"{x.weight*100f/service.Catalog.packOutcomes.Sum(o=>o.weight):0.#}% • {x.label}"));
            if(pack.startUtc==0)Confirm("UNLOCK TIDE PACK",$"{pack.duration}s offline unlock • entirely free\n"+odds,()=>{bool ok=service.StartPack(id,out var m);Feedback(ok,m);});
            else if(service.Remaining(pack)>0)Toast(PackText(pack)+" • leave and return later. Local device time is trusted.");
            else Confirm("OPEN FREE PACK",odds,()=>
            {
                var outcome=service.Catalog.packOutcomes[pack.outcome];bool ok=service.ClaimPack(id,out var m);Feedback(ok,m);
                if(ok){Confirm("CURRENT CAUGHT!",outcome.label+"\nAdded to your saved collection and balances.",()=>{});}
            });
        }
        void Shop(LocalProfile p)
        {
            string[] tabs={"Featured","Player Cards","Upgrade Materials","Coins","Gems","Special Offers"};
            for(int i=0;i<tabs.Length;i++){string tab=tabs[i];HubWidgets.Button(content,"Tab"+i,tab.ToUpperInvariant(),i/6f,.79f,.16f,.09f,()=>{shopTab=tab;Render();},shopTab==tab?HubWidgets.Palette[0]:HubWidgets.Card);}
            var offers=service.Catalog.offers.Where(x=>x.section==shopTab).ToArray();
            for(int i=0;i<offers.Length;i++)
            {
                var offer=offers[i];var def=service.Catalog.Player(offer.reward.playerId);var card=HubWidgets.Panel(content,"Offer_"+offer.id,.01f+i*.33f,.19f,.31f,.55f,def==null?HubWidgets.Card:service.Catalog.Rarity(def.rarity).color*.5f);
                HubWidgets.Text(card,"Rarity",def?.rarity.ToUpperInvariant()??"FIXED CONTENTS",.06f,.74f,.88f,.16f,24,HubWidgets.Gold);
                HubWidgets.Text(card,"Title",offer.title,.06f,.34f,.88f,.4f,32);
                string price=offer.coins>0?offer.coins+" COINS":offer.gems+" GEMS";
                HubWidgets.Button(card,"Buy_"+offer.id,price,.06f,.08f,.88f,.18f,()=>
                {
                    confirmationId=Guid.NewGuid().ToString("N");string request=confirmationId;
                    Confirm("CONFIRM PURCHASE",offer.title+"\nPrice: "+price+"\nFixed contents • local currency only",()=>{bool ok=service.Purchase(offer.id,request,out var m);Feedback(ok,m);});
                },HubWidgets.Palette[0]);
            }
            if(offers.Length==0)HubWidgets.Text(content,"FutureStore",shopTab=="Gems"?"PREMIUM STORE PREVIEW\nReal-money gem purchases are coming later.\nNo billing provider is connected.":"SPECIAL OFFERS\nFuture curated offers will appear here.\nNo paid random packs.",.08f,.28f,.84f,.4f,34,HubWidgets.Muted);
            HubWidgets.Button(content,"Ads","WATCH AD — COMING LATER",.01f,.025f,.47f,.1f,null,null,false);
            HubWidgets.Button(content,"Payments","REAL-MONEY STORE — COMING LATER",.51f,.025f,.48f,.1f,null,null,false);
        }
        void Season(LocalProfile p)
        {
            int level=Math.Min(service.Catalog.seasonLevels,p.seasonXp/service.Catalog.seasonXpPerLevel);
            HubWidgets.Text(content,"SeasonHeader",$"{service.Catalog.seasonTitle}\nLEVEL {level} • {p.seasonXp} XP • NEXT {Math.Min(service.Catalog.seasonLevels,level+1)*service.Catalog.seasonXpPerLevel} XP",.02f,.67f,.96f,.2f,30,HubWidgets.Gold);
            var area=HubWidgets.Rect(content,"SeasonTrack",0,.03f,1,.64f);var track=HubWidgets.Scroll(area,"HorizontalTrack",true);
            track.anchorMin=track.anchorMax=new Vector2(0,1);track.sizeDelta=new Vector2(service.Catalog.seasonLevels*240,480);
            for(int i=1;i<=service.Catalog.seasonLevels;i++)
            {
                int tier=i;bool claimed=p.claimedRewards.Contains("season:"+service.Catalog.seasonId+":"+tier);var reward=service.Catalog.SeasonReward(tier);
                var card=HubWidgets.Panel(track,"Tier"+tier,(i-1f)/service.Catalog.seasonLevels,.02f,.95f/service.Catalog.seasonLevels,.96f,HubWidgets.Card);
                HubWidgets.Text(card,"Level","TIER "+tier,.08f,.81f,.84f,.15f,30,HubWidgets.Cyan);
                HubWidgets.Text(card,"Free",$"FREE\n{reward.coins} coins"+(reward.gems>0?$"\n{reward.gems} gems":""),.08f,.47f,.84f,.32f,26);
                HubWidgets.Button(card,"ClaimTier"+tier,claimed?"CLAIMED":level>=tier?"CLAIM":"LOCKED",.06f,.31f,.88f,.14f,()=>{bool ok=service.ClaimSeason(tier,out var m);Feedback(ok,m);},HubWidgets.Palette[0],!claimed&&level>=tier);
                HubWidgets.Text(card,"Premium","PREMIUM\nLocked preview\nComing later",.08f,.03f,.84f,.25f,22,HubWidgets.Muted);
            }
        }
    }
}
