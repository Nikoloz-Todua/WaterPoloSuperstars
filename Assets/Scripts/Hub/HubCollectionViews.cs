using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    public sealed partial class HubView
    {
        void Collection(LocalProfile p)
        {
            var scroll=HubWidgets.Scroll(content,"CollectionScroll");int rows=(service.Catalog.players.Length+3)/4;scroll.sizeDelta=new Vector2(0,rows*290);
            for(int i=0;i<service.Catalog.players.Length;i++)
            {
                var def=service.Catalog.players[i];var owned=p.collection.FirstOrDefault(x=>x.playerId==def.id);float height=1f/rows;
                var card=HubWidgets.Panel(scroll,"Card_"+def.id,(i%4)*.25f,1-(i/4+1)*height,.237f,height*.94f,service.Catalog.Rarity(def.rarity).color*.6f);
                HubWidgets.Text(card,"Rarity",def.rarity.ToUpperInvariant(),.06f,.81f,.88f,.14f,20,HubWidgets.Gold);
                HubWidgets.Text(card,"Name",def.name,.06f,.59f,.88f,.22f,29);
                HubWidgets.Text(card,"Stats",def.role+"\n"+(owned==null?"NOT OWNED":$"LV {owned.level}   OVR {def.Overall(owned.level)}   {owned.shards} shards"),.06f,.25f,.88f,.32f,22);
                HubWidgets.Button(card,"Details_"+def.id,"VIEW PLAYER",.05f,.04f,.9f,.2f,()=>{selectedPlayer=def.id;Show("Details");});
            }
        }
        void Details(LocalProfile p)
        {
            var def=service.Catalog.Player(selectedPlayer??p.lineup[1]);var owned=p.collection.FirstOrDefault(x=>x.playerId==def.id);int level=owned?.level??1;
            var art=HubWidgets.Panel(content,"Portrait",.02f,.13f,.32f,.7f,service.Catalog.Rarity(def.rarity).color*.6f);HubWidgets.Athlete(art,service.Catalog.Rarity(def.rarity).color,HubWidgets.Palette[p.swimwearColor]);
            HubWidgets.Text(content,"Player",def.name+"\n"+def.rarity+" • "+def.role+" • OVR "+def.Overall(level),.39f,.63f,.59f,.22f,36);
            int bonus=2*(level-1);
            HubWidgets.Text(content,"StatTable",$"SWIM {def.swim+bonus}      STAMINA {def.stamina+bonus}\nSHOT {def.shot+bonus}      PASS {def.pass+bonus}\nDEFENSE {def.defense+bonus}      HANDLING {def.handling+bonus}"+(def.goalkeeper?$"\nKEEPING {def.keeping+bonus}":""),.39f,.3f,.59f,.32f,29);
            HubWidgets.Text(content,"Integration","Collection ratings only; live movement/shot tuning is unchanged.",.39f,.2f,.59f,.09f,21,HubWidgets.Muted);
            string label=owned==null?"NOT OWNED • FIND IN SHOP / FREE PACKS":level>=service.Catalog.maxLevel?"MAX LEVEL":$"UPGRADE • {service.Catalog.UpgradeCost(def,level)} COINS + {service.Catalog.UpgradeShards(level)} SHARDS ({owned.shards} owned)";
            HubWidgets.Button(content,"Upgrade",label,.39f,.04f,.59f,.13f,()=>Confirm("UPGRADE "+def.name,label,()=>{bool ok=service.Upgrade(def.id,level,out var m);Feedback(ok,m);}),HubWidgets.Palette[0],owned!=null&&level<service.Catalog.maxLevel);
        }
        void Tactics(LocalProfile p)
        {
            var board=HubWidgets.Rect(content,"FormationBoard",0,.04f,.58f,.81f);HubWidgets.Pool(board);
            Vector2[] slots={new Vector2(.4f,.09f),new Vector2(.4f,.7f),new Vector2(.09f,.62f),new Vector2(.71f,.62f),new Vector2(.4f,.25f),new Vector2(.17f,.4f),new Vector2(.63f,.4f)};
            for(int i=0;i<7;i++)
            {
                int slot=i;var def=service.Catalog.Player(p.lineup[i]);HubWidgets.Button(board,"Slot"+i,(i==0?"GK":"F"+i)+"\n"+def.name,slots[i].x,slots[i].y,.2f,.15f,()=>{selectedSlot=slot;Render();},selectedSlot==i?HubWidgets.Palette[2]:HubWidgets.Navy);
            }
            int ovr=Mathf.RoundToInt((float)p.lineup.Average(id=>service.Catalog.Player(id).Overall(p.collection.First(x=>x.playerId==id).level)));
            HubWidgets.Text(content,"Summary","CLASSIC SIX • TEAM OVR "+ovr+"\nTap a slot, then an athlete to swap / replace.",.6f,.72f,.39f,.16f,25,HubWidgets.Cyan);
            HubWidgets.Text(content,"Defense","LIVE DEFENSE: "+p.tactics.ToUpperInvariant(),.6f,.62f,.39f,.08f,25);
            HubWidgets.Button(content,"Press","PRESS",.6f,.52f,.18f,.09f,()=>{bool ok=service.SetTactics("Press",out var m);Feedback(ok,m);},p.tactics=="Press"?HubWidgets.Palette[0]:HubWidgets.Card);
            HubWidgets.Button(content,"Zone","ZONE",.8f,.52f,.18f,.09f,()=>{bool ok=service.SetTactics("Zone",out var m);Feedback(ok,m);},p.tactics=="Zone"?HubWidgets.Palette[0]:HubWidgets.Card);
            var area=HubWidgets.Rect(content,"EligibleArea",.6f,.01f,.39f,.48f);var list=HubWidgets.Scroll(area,"RosterScroll");
            var eligible=p.collection.Where(x=>service.Catalog.Player(x.playerId)!=null&&(selectedSlot<0||service.Catalog.Player(x.playerId).goalkeeper==(selectedSlot==0))).ToArray();
            list.sizeDelta=new Vector2(0,eligible.Length*72);
            for(int i=0;i<eligible.Length;i++)
            {
                var owned=eligible[i];var def=service.Catalog.Player(owned.playerId);HubWidgets.Button(list,"Assign_"+def.id,def.name+(p.lineup.Contains(def.id)?" • STARTER":" • BENCH"),0,1-(i+1f)/eligible.Length,1,.9f/eligible.Length,()=>{if(selectedSlot<0){Toast("Select a formation slot first.");return;}bool ok=service.Assign(selectedSlot,def.id,out var m);Feedback(ok,m);});
            }
        }
        void Profile(LocalProfile p)
        {
            var preview=HubWidgets.Panel(content,"AppearancePreview",.01f,.08f,.29f,.76f,HubWidgets.Card);HubWidgets.Athlete(preview,HubWidgets.Palette[p.capColor],HubWidgets.Palette[p.swimwearColor]);
            HubWidgets.Text(preview,"Avatar",p.avatarId.ToUpperInvariant()+" / "+p.displayName,.05f,.01f,.9f,.12f,25,HubWidgets.Cyan,TextAlignmentOptions.Center);
            HubWidgets.Text(content,"Progress",$"LEVEL {p.level}  •  {p.xp%100}/100 XP\n{p.trophies} trophies • {p.wins} wins / {p.played} matches\n{p.collection.Count}/{service.Catalog.players.Length} athletes • LOCAL ONLY",.34f,.65f,.64f,.2f,28);
            var inputRect=HubWidgets.Panel(content,"NameInput",.34f,.53f,.64f,.09f,HubWidgets.Card);var field=inputRect.gameObject.AddComponent<TMP_InputField>();
            var txt=HubWidgets.Text(inputRect,"Text",p.displayName,.04f,.05f,.92f,.9f,28);txt.raycastTarget=true;field.textViewport=inputRect;field.textComponent=txt;field.characterLimit=18;field.text=p.displayName;
            string avatar=p.avatarId;int cap=p.capColor,suit=p.swimwearColor,accent=p.accentColor;
            var draftLabel=HubWidgets.Text(content,"Draft","Choose an avatar or color, then SAVE PROFILE.",.34f,.02f,.64f,.08f,21,HubWidgets.Muted);
            string[] avatars={"wave","anchor","sun","reef"};
            for(int i=0;i<4;i++){string id=avatars[i];HubWidgets.Button(content,"Avatar_"+id,id.ToUpperInvariant(),.34f+i*.16f,.4f,.15f,.09f,()=>{avatar=id;draftLabel.text="Selected avatar: "+id+" • Save to apply";},id==avatar?HubWidgets.Palette[0]:HubWidgets.Card);}
            string[] kinds={"CAP","SUIT","ACCENT"};
            for(int row=0;row<3;row++)
            {
                int kind=row;HubWidgets.Text(content,"ColorLabel"+row,kinds[row],.34f,.29f-row*.075f,.15f,.065f,20);
                for(int i=0;i<4;i++){int choice=i;HubWidgets.Button(content,kinds[row]+i,(i+1).ToString(),.5f+i*.095f,.29f-row*.075f,.085f,.065f,()=>{if(kind==0)cap=choice;else if(kind==1)suit=choice;else accent=choice;draftLabel.text=kinds[kind]+" color "+(choice+1)+" selected • Save to apply";},HubWidgets.Palette[i]);}
            }
            HubWidgets.Button(content,"SaveProfile","SAVE PROFILE",.75f,.015f,.23f,.1f,()=>{bool ok=service.Customize(field.text,avatar,cap,suit,accent,out var m);Feedback(ok,m);},HubWidgets.Palette[0]);
        }
        void Trophies(LocalProfile p)
        {
            string rank=p.trophies>=300?"DEEPWATER":p.trophies>=100?"SURGE":"RIPPLE";
            HubWidgets.Text(content,"Rank",$"{rank} RANK  •  {p.trophies} TROPHIES\nNext rank: {(p.trophies<100?100:p.trophies<300?300:600)} trophies • {p.wins} wins • {p.played} matches • {p.goals} goals",.02f,.7f,.96f,.16f,31,HubWidgets.Gold);
            var area=HubWidgets.Rect(content,"AchievementArea",0,0,1,.68f);var list=HubWidgets.Scroll(area,"Achievements");var defs=service.Catalog.achievements;list.sizeDelta=new Vector2(0,defs.Length*100);
            for(int i=0;i<defs.Length;i++)
            {
                var a=defs[i];bool claimed=p.claimedRewards.Contains("achievement:"+a.id);var row=HubWidgets.Panel(list,a.id,0,1-(i+1f)/defs.Length,1,.9f/defs.Length,HubWidgets.Card);
                HubWidgets.Text(row,"Title",$"{a.title}   {Math.Min(a.target,service.Counter(a.counter))}/{a.target}   •   +{a.coins} coins",.03f,.12f,.71f,.76f,27);
                HubWidgets.Button(row,"Claim_"+a.id,claimed?"CLAIMED":"CLAIM",.77f,.1f,.2f,.8f,()=>{bool ok=service.ClaimAchievement(a.id,out var m);Feedback(ok,m);},HubWidgets.Palette[0],!claimed&&service.Counter(a.counter)>=a.target);
            }
        }
    }
}
