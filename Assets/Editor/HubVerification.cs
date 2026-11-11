using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Focused Editor checks; economy fixtures never touch the developer's production profile.</summary>
    public static class HubVerification
    {
        sealed class MemoryRepository:IProfileRepository
        {
            public string json;public bool fail;
            public LocalProfile Load()=>json==null?null:JsonUtility.FromJson<LocalProfile>(json);
            public void Save(LocalProfile p){if(fail)throw new IOException("Fixture interruption");json=JsonUtility.ToJson(p);}
        }
        static void Check(bool value,string text){if(!value)throw new Exception("M12 verification: "+text);}
        public static object Core()
        {
            var catalog=Resources.Load<HubCatalog>("HubCatalog");var repo=new MemoryRepository();long now=1800000000;
            var s=new HubProgression(catalog,repo,()=>now);string id=s.Snapshot.profileId;int coins=s.Snapshot.coins;
            Check(new HubProgression(catalog,repo).Snapshot.profileId==id,"First run only once");
            Check(s.Purchase("reef-card","fixed-request",out _)&&s.Snapshot.coins==coins-150,"Fixed purchase spends/grants");
            Check(!s.Purchase("reef-card","fixed-request",out _),"Duplicate purchase rejected");
            Check(!s.Purchase("unknown","invalid",out _),"Unknown offer rejected");
            for(int i=0;i<8;i++)s.Purchase("crest-card","spend-"+i,out _);
            Check(s.Snapshot.coins>=0&&!s.CanAfford(int.MaxValue,0),"No negative balances");
            var p=s.Snapshot;Check(!s.Assign(0,"wp_002",out _)&&!s.Assign(1,"wp_012",out _),"Wrong role / unowned rejected");
            Check(s.Assign(1,"wp_008",out _)&&s.Assign(2,"wp_008",out _),"Bench replacement and starter swap");
            Check(s.Snapshot.lineup.Distinct().Count()==7&&catalog.Player(s.Snapshot.lineup[0]).goalkeeper,"Legal seven-player lineup");
            Check(s.Customize("Test Captain","reef",2,3,1,out _),"Profile customization");
            Check(s.SetTactics("Press",out _),"Tactics saved");
            Check(s.BeginMatch("training",out _),"Practice session");string session=s.Snapshot.activeMatch.id;
            int before=s.Snapshot.coins;Check(s.CompleteMatch(session,3,0,out _)&&s.Snapshot.coins==before&&s.Snapshot.played==0,"Practice cannot farm rewards");
            Check(!s.CompleteMatch(session,3,0,out _),"Duplicate result rejected");
            Check(s.BeginMatch("quick",out _),"Quick session");session=s.Snapshot.activeMatch.id;
            Check(s.CompleteMatch(session,2,1,out _)&&s.Snapshot.coins==before+catalog.winCoins&&s.Snapshot.wins==1,"Completed match grants configured rewards");
            Check(!s.CompleteMatch(session,2,1,out _),"Match receipt idempotent");
            Check(s.ClaimAchievement("first-win",out _)&&!s.ClaimAchievement("first-win",out _),"Achievement once");
            Check(s.Upgrade("wp_001",1,out _)&&!s.Upgrade("wp_001",1,out _),"Upgrade expected-level idempotency");
            Check(!s.ClaimSeason(1,out _),"Season threshold enforced");
            s.BeginMatch("quick",out _);s.CompleteMatch(s.Snapshot.activeMatch.id,1,1,out _);
            Check(s.ClaimSeason(1,out _)&&!s.ClaimSeason(1,out _),"Free season claims once");
            string pack=s.Snapshot.packs[0].instanceId;Check(!s.ClaimPack(pack,out _)&&s.StartPack(pack,out _),"Unlock required");
            Check(new HubProgression(catalog,repo,()=>now).Snapshot.packs[0].startUtc==now,"Unlock survives restart");
            now-=300;Check(s.Remaining(s.Snapshot.packs[0])==catalog.unlockSeconds,"Backward time bounded");now+=300+catalog.unlockSeconds;
            Check(s.ClaimPack(pack,out _)&&!s.ClaimPack(pack,out _),"Pack grants and clears once");
            for(int i=0;i<6;i++){s.BeginMatch("quick",out _);s.CompleteMatch(s.Snapshot.activeMatch.id,0,1,out _);}
            Check(s.Snapshot.packs.Count==catalog.slotCapacity&&s.Snapshot.pendingResult.pack.Contains("Slots full"),"Deterministic overflow");
            var reloaded=new HubProgression(catalog,repo,()=>now);Check(reloaded.Snapshot.avatarId=="reef"&&reloaded.Snapshot.tactics=="Press"&&reloaded.Snapshot.collection.First(x=>x.playerId=="wp_001").level==2,"Collection/profile/tactics persistence");
            string frozen=repo.json;repo.fail=true;Check(!s.Purchase("reef-card","failed-save",out _)&&repo.json==frozen,"Failed save cannot publish spend");repo.fail=false;
            Check(s.Snapshot.transactions.All(x=>!x.Contains("failed-save")),"Failed transaction retryable");
            Check(!repo.json.Contains("swim\"")&&!repo.json.Contains("rarities")&&!repo.json.Contains("defense\""),"Compact IDs, no definitions in save");
            string folder=Path.Combine(Directory.GetCurrentDirectory(),"Logs","M12-SaveFixture");Directory.CreateDirectory(folder);string path=Path.Combine(folder,Guid.NewGuid().ToString("N")+".json");
            var disk=new JsonProfileRepository(path);disk.Save(s.Snapshot);disk.Save(s.Snapshot);File.WriteAllText(path,"{broken");Check(disk.Load().profileId==id,"Corrupt primary recovers backup");
            File.WriteAllText(path+".bak","{broken");bool rejected=false;try{disk.Load();}catch(IOException){rejected=true;}Check(rejected&&File.ReadAllText(path)=="{broken","No empty-default overwrite");
            File.Delete(path);File.Delete(path+".bak");
            return new{passed=true,groups=18,firstRun=true,transactionAtomicity=true,profilePersistence=true,packPersistence=true,duplicateClaimsRejected=true,corruptRecovery=true,offlineClockBounds=true};
        }
        public static object Setup()
        {
            Check(!Application.isPlaying,"Edit Mode setup");Milestone12HubSetup.SetUp();var boot=AssetDatabase.AssetPathToGUID(Milestone12HubSetup.BootPath);var lobby=AssetDatabase.AssetPathToGUID(Milestone12HubSetup.LobbyPath);
            Milestone12HubSetup.SetUp();Check(boot==AssetDatabase.AssetPathToGUID(Milestone12HubSetup.BootPath)&&lobby==AssetDatabase.AssetPathToGUID(Milestone12HubSetup.LobbyPath),"Scene identities preserved");
            Check(EditorBuildSettings.scenes[0].path==Milestone12HubSetup.BootPath,"Boot first in shared scene list");
            Check(UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>().Members.Count==14&&UnityEngine.Object.FindObjectsByType<HubMatchBridge>().Length==1,"Fourteen athletes and one bridge");
            return new{passed=true,repeatSafe=true,scenes=EditorBuildSettings.scenes.Select(x=>x.path).ToArray()};
        }
        static async Task Frames(float seconds=.2f){int frame=Time.frameCount;await Task.Delay((int)(seconds*1000));Check(Time.frameCount>frame,"Advancing player loop");}
        static HubView View()=>UnityEngine.Object.FindAnyObjectByType<HubView>();
        public static async Task<object> NavigationAndPointer()
        {
            Check(Application.isPlaying&&View()!=null,"Boot reached lobby");var v=View();var ids=v.Service.Snapshot.profileId;var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                var b=v.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name=="Nav_Shop");var r=b.GetComponent<RectTransform>();Vector2 point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Frames(.08f);InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});await Frames(.08f);InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Frames(.12f);
                Check(v.Page=="Shop","Actual Input System pointer opens Shop");
                foreach(string page in new[]{"Lobby","Play","Collection","Details","Tactics","Shop","Season Pass","Profile","Trophies","Friends","Clubs","Settings"})
                {v.Show(page);await Frames(.08f);Check(v.content.childCount>0,"Populated "+page);Check(UnityEngine.Object.FindObjectsByType<Canvas>().Length==1&&UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"One Canvas/EventSystem");}
                var keyboard=InputSystem.AddDevice<Keyboard>();try{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Frames(.08f);Check(v.Page=="Lobby","Keyboard Back returns to lobby");}finally{InputSystem.RemoveDevice(keyboard);}
                Check(v.Service.Snapshot.profileId==ids,"Navigation retains identity");
                return new{passed=true,pages=12,actualPointer=true,keyboardBack=true,profileStable=true};
            }
            finally{InputSystem.RemoveDevice(mouse);v.Show("Lobby");}
        }
        public static async Task<object> ActualLayouts()
        {
            var v=View();Check(v!=null,"Lobby required");const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
            var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);var prop=type.GetProperty("selectedSizeIndex",flags);int original=(int)prop.GetValue(view);
            var sizesType=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",flags).GetValue(null);var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(view)});var gt=group.GetType();var added=new List<int>();var results=new List<object>();var overflow=new List<string>();
            Directory.CreateDirectory("Logs/M12");var safe=v.GetComponent<SafeAreaLayout>();bool wasEnabled=safe.enabled;
            try
            {
                foreach(int width in new[]{1920,2340,2400})
                {
                    var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M12 verification"});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});int selected=(int)gt.GetMethod("GetTotalCount").Invoke(group,null)-1;added.Add(selected);prop.SetValue(view,selected);view.Repaint();await Frames(.4f);Check(Screen.width==width&&Screen.height==1080,"Actual resolution");
                    safe.enabled=false;safe.Apply(new Rect(70,24,width-140,1032),new Vector2(width,1080));
                    foreach(string page in new[]{"Lobby","Play","Collection","Details","Tactics","Shop","Season Pass","Profile","Trophies","Friends","Clubs","Settings"})
                    {
                        v.Show(page);await Frames(.2f);Canvas.ForceUpdateCanvases();
                        foreach(var t in v.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(width+" "+page+" "+t.name+": "+t.text);}
                        foreach(var g in v.GetComponentsInChildren<RoundedPanelGraphic>()){Check(g.GetComponent<CanvasRenderer>()!=null,"Required renderer");var mesh=g.canvasRenderer.GetMesh();Check(mesh!=null&&mesh.vertexCount>0,"Panel rendered "+g.name);}
                        ScreenCapture.CaptureScreenshot("Logs/M12/"+width+"-"+page.Replace(" ","")+".png");await Frames(.1f);results.Add(new{width,page});
                    }
                }
                File.WriteAllText("Logs/M12/overflow.txt",string.Join("\n",overflow));Check(overflow.Count==0,"Text overflow; inspect Logs/M12/overflow.txt");return new{passed=true,captures=results,safeInsets=true};
            }
            finally{prop.SetValue(view,original);for(int i=added.Count-1;i>=0;i--)gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added[i]});safe.Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));safe.enabled=wasEnabled;v.Show("Lobby");view.Repaint();}
        }
        static async Task Until(Func<bool> condition)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>15)throw new TimeoutException("M12 expected transition did not occur.");await Task.Delay(30);}
        }
        public static async Task<object> MatchLoop()
        {
            Check(Application.isPlaying&&View()!=null,"Lobby required");var app=HubApplication.Instance;var original=app.Progression;var repo=new MemoryRepository();var isolated=new HubProgression(original.Catalog,repo);app.UseVerificationProfile(isolated);
            try
            {
                app.Launch("quick");await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==HubApplication.Match);await Frames(.4f);
                var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
                Check(roster.Members.Count==14&&roster.Members.Count(x=>x.Team==PrototypeTeam.Blue&&x.IsFieldPlayer)==6,"Preserved 7 vs 7");
                Check(roster.Members.Where(x=>x.Team==PrototypeTeam.Blue).All(x=>!string.IsNullOrEmpty(x.CollectionDefinitionId)),"Stable collection mapping applied");
                await Frames(.5f);Check(match.IsPlaying&&Time.fixedTime>0,"Physics and AI advancing");
                var menus=UnityEngine.Object.FindAnyObjectByType<MatchMenuController>();menus.OpenPause();float clock=match.Clock.Remaining;await Frames(.15f);Check(match.IsPaused&&match.Clock.Remaining==clock,"Authoritative pause retained");menus.Resume();
                foreach(var ai in UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=false;
                foreach(var keeper in UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>())keeper.enabled=false;
                int b=0,w=0;foreach(var player in roster.FieldPlayers)player.Motor.ResetPose(new Vector3(player.Team==PrototypeTeam.Blue?-8:8,0,-11+4*(player.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);
                var shooter=roster.FieldPlayers.First(x=>x.Team==PrototypeTeam.Blue);var team=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(x=>x.Team==PrototypeTeam.Blue);var goal=team.AttackingGoal;
                Vector3 origin=goal.transform.TransformPoint(new Vector3(0,0,-5));origin.y=0;shooter.Motor.ResetPose(origin,Quaternion.LookRotation(goal.transform.forward));ball.ResetForRestart(origin);Check(shooter.Interaction.TryCollect(),"Range-checked physical possession");
                Check(shooter.Interaction.TryShoot(22,ShotType.Normal),"Real shot release");await Until(()=>match.BlueScore==1);Check(match.Phase==MatchPhase.GoalRestart,"Authoritative physical goal");await Until(()=>match.IsPlaying);
                for(int i=0;i<4;i++){ball.ResetForRestart(Vector3.zero);match.ShotClock.ResetPossession();match.AdvanceLiveTime(match.Clock.RealDuration);if(i<3)match.ContinueMatch();}
                Check(match.Phase==MatchPhase.FullTime&&match.EndSwitchCount==1,"Four periods, one halftime end swap");
                var awarded=isolated.Snapshot;Check(awarded.played==1&&awarded.wins==1&&awarded.pendingResult!=null,"Real full-time observer grants receipt");int balance=awarded.coins;Check(!isolated.CompleteMatch(awarded.pendingResult.id,1,0,out _),"Duplicate scene-result grant rejected");
                menus.Continue();await Until(()=>View()!=null);await Frames(.2f);Check(View().Page=="Result"&&isolated.Snapshot.coins==balance,"Result screen and unchanged grant on return");
                isolated.DismissResult(out _);View().Show("Lobby");Check(new HubProgression(original.Catalog,repo).Snapshot.played==1,"Match rewards reload");
                app.Launch("training");await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==HubApplication.Match);await Frames(.3f);
                menus=UnityEngine.Object.FindAnyObjectByType<MatchMenuController>();menus.OpenPause();menus.AskExit();menus.Confirm();await Until(()=>View()!=null);await Frames(.2f);
                Check(isolated.Snapshot.coins==balance&&isolated.Snapshot.activeMatch==null&&Time.timeScale==1,"Abandon restores lobby without reward or stuck pause");
                return new{passed=true,physicalGoal=true,athletes=14,fullTime=true,receiptOnce=true,resultScreen=true,lineupMapping=true,pause=true,abandon=true,productionSaveUntouched=true};
            }
            finally{app.UseVerificationProfile(original);app.ReturnToLobby(false);}
        }
    }
}
