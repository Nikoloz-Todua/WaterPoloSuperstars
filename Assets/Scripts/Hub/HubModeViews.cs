using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    public sealed partial class HubView
    {
        string selectedMode="division_1";
        void GameModes()
        {
            string[] ids={"division_1","premium_league","continental_cup","world_champions_league","lucky_shot","training"};
            // Independent hit regions preserve the source art's tall/wide proportions.
            Rect[] bounds={new Rect(.005f,.055f,.285f,.89f),new Rect(.295f,.055f,.215f,.89f),new Rect(.525f,.515f,.23f,.43f),new Rect(.765f,.515f,.23f,.43f),new Rect(.525f,.055f,.23f,.43f),new Rect(.765f,.055f,.23f,.43f)};
            for(int i=0;i<ids.Length;i++)
            {
                string id=ids[i];var r=bounds[i];bool locked=i<4&&!service.DivisionUnlocked(id);
                var button=FrontendControls.ArtButton(content,"Mode_"+id,shell.art.Mode(id),r.x,r.y,r.width,r.height,()=>SelectMode(id));
                if(locked)
                {
                    var artwork=button.transform.Find("Artwork").GetComponent<UnityEngine.UI.Image>();artwork.color=new Color(.58f,.65f,.77f);
                    var badge=HubWidgets.Panel(button.transform,"Locked",.08f,.85f,.84f,.105f,new Color(.02f,.04f,.10f,.96f));
                    HubWidgets.Text(badge,"State","LOCKED",0,0,1,1,23,HubWidgets.Gold,TextAlignmentOptions.Center);
                }
            }
            HubWidgets.Text(content,"ModeHint","OFFLINE CHAMPIONSHIPS  •  Win the preceding division to advance",.02f,0,.96f,.045f,22,HubWidgets.Muted,TextAlignmentOptions.Center);
        }
        void SelectMode(string id)
        {
            selectedMode=id;
            if(id=="lucky_shot"){ShowComingSoon("Lucky Shot");return;}
            if(id!="training"&&!service.DivisionUnlocked(id))
            {
                int index=service.Divisions.Index(id);
                ShowComingSoon(service.Divisions.divisions[index].title,"LOCKED","Win the "+service.Divisions.divisions[index-1].title+" championship to unlock this division.");return;
            }
            Show("Mode Details");
        }
        void ModeDetails(LocalProfile p)
        {
            bool practice=selectedMode=="training";var rules=service.Divisions;
            FrontendControls.Picture(content,"SelectedModeArtwork",shell.art.Mode(selectedMode),.04f,.08f,.35f,.84f);
            var panel=HubWidgets.Panel(content,"ChampionshipDetails",.43f,.09f,.53f,.82f,new Color(.025f,.065f,.15f,.94f));
            string name=practice?"TRAINING":rules.divisions[rules.Index(selectedMode)].title.ToUpperInvariant();
            HubWidgets.Text(panel,"Title",name,.07f,.77f,.86f,.15f,38,HubWidgets.Cyan);
            var run=p.championship;bool current=!practice&&run!=null&&run.divisionId==selectedMode&&!rules.Ended(run);
            string details=practice?"Practice movement, passing and shooting in OlympicMatchArena.\n\nFull 7 vs 7 match • no trophies, currency, packs or championship progress.":$"Win {rules.winsRequired} matches within {rules.matchLimit} attempts.\nFour periods • 7 vs 7 • OlympicMatchArena\n\n"+(current?$"CURRENT ATTEMPT\n{run.wins}/{rules.winsRequired} wins • {run.matches}/{rules.matchLimit} matches played":p.completedDivisions.Contains(selectedMode)?"CHAMPIONSHIP COMPLETED\nReplay with the same match rewards. Unlocks are granted once.":"NEW CHAMPIONSHIP\nOnly completed official match victories count.");
            if(!practice&&run!=null&&run.divisionId!=selectedMode&&!rules.Ended(run))details+="\nStarting this championship replaces your unfinished attempt.";
            HubWidgets.Text(panel,"Format",details,.07f,.25f,.86f,.49f,28,Color.white);
            HubWidgets.Button(panel,"LaunchMode",practice?"START TRAINING":current?"PLAY NEXT MATCH":"START CHAMPIONSHIP",.07f,.06f,.86f,.14f,()=>
            {
                if(practice)HubApplication.Instance.Launch("training");
                else if(run!=null&&run.divisionId!=selectedMode&&!rules.Ended(run))Confirm("START NEW CHAMPIONSHIP?","Your unfinished attempt will be replaced. Completed divisions and all inventory are retained.",()=>HubApplication.Instance.LaunchDivision(selectedMode));
                else HubApplication.Instance.LaunchDivision(selectedMode);
            },HubWidgets.Palette[0]);
        }
        void TeamTabs()
        {
            HubWidgets.Button(content,"TeamLineup","LINEUP & TACTICS",.02f,.91f,.30f,.085f,()=>ShowSibling("Tactics"),page=="Tactics"?HubWidgets.Palette[0]:HubWidgets.Card);
            HubWidgets.Button(content,"TeamCollection","COLLECTION",.34f,.91f,.30f,.085f,()=>ShowSibling("Collection"),page=="Collection"?HubWidgets.Palette[0]:HubWidgets.Card);
        }
    }
}
