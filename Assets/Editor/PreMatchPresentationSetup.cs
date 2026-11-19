using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static partial class MatchUxSetup
    {
        [MenuItem("Tools/Water Polo/Match UX/Set Up Pre-Match Presentation")]
        public static void SetUpPreMatchPresentation()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=OlympicArenaSetup.ScenePath)
                throw new InvalidOperationException("Open the saved production OlympicMatchArena in Edit Mode.");
            var menus=Object.FindAnyObjectByType<MatchMenuController>();
            if(menus.GetComponent<PreMatchPresentation>()!=null)return;
            var anchors=Object.FindAnyObjectByType<VenueIntroductionAnchors>();
            var match=Object.FindAnyObjectByType<PrototypeMatch>();
            var roster=Object.FindAnyObjectByType<PrototypeRoster>();
            var refs=Object.FindObjectsByType<RefereeVisual>();
            var blue=FindKit("Blue team");
            var white=FindKit("White team");
            if(match==null || roster==null || anchors==null || refs.Length<2 || blue==null || white==null)
                throw new InvalidOperationException("The saved competition anchors, rigs, officials and kit assets are required.");

            var intro=menus.gameObject.AddComponent<PreMatchPresentation>();
            var safe=menus.transform.Find("MatchSafeArea");
            var overlay=new GameObject("PreMatchPresentation",typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.SetParent(safe,false);
            overlay.anchorMin=Vector2.zero;
            overlay.anchorMax=Vector2.one;
            overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            var touch=overlay.gameObject.AddComponent<Image>();
            touch.color=Color.clear;
            var advance=overlay.gameObject.AddComponent<Button>();
            advance.targetGraphic=touch;
            advance.transition=Selectable.Transition.None;
            advance.navigation=new Navigation{mode=Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(advance.onClick,intro.Advance);

            var header=Panel(overlay,"CeremonyCard",new Vector2(.5f,1),new Vector2(0,-83),new Vector2(660,120),Navy);
            var eyebrow=Text(header,"Event","OLYMPIC MATCH ARENA",new Vector2(0,37),new Vector2(600,26),15,Cyan);
            eyebrow.characterSpacing=10;
            var title=Text(header,"Stage","CHAMPIONSHIP NIGHT",new Vector2(0,5),new Vector2(615,46),35,Color.white);
            title.fontStyle=FontStyles.Bold;
            var subtitle=Text(header,"Detail","BLUE  /  WHITE",new Vector2(0,-34),new Vector2(590,30),19,Muted);
            subtitle.characterSpacing=4;

            var lineups=new GameObject("StartingLineups",typeof(RectTransform)).GetComponent<RectTransform>();
            lineups.SetParent(overlay,false);
            lineups.anchorMin=Vector2.zero;
            lineups.anchorMax=Vector2.one;
            lineups.offsetMin=lineups.offsetMax=Vector2.zero;
            var blueLines=BuildCeremonyRoster(lineups,"BlueLineup","BLUE",1,Cyan);
            var whiteLines=BuildCeremonyRoster(lineups,"WhiteLineup","WHITE",0,Gold);
            lineups.gameObject.SetActive(false);

            var hint=Panel(overlay,"NextHint",new Vector2(.5f,0),new Vector2(0,78),new Vector2(285,46),Navy);
            Text(hint,"Label","TAP TO ADVANCE",Vector2.zero,new Vector2(275,38),19,Color.white);
            var track=Panel(overlay,"ProgressTrack",new Vector2(.5f,0),new Vector2(0,46),new Vector2(340,5),
                new Color(.56f,.70f,.82f,.5f));
            var progress=Rect(track,"Progress",new Vector2(0,.5f),Vector2.zero,new Vector2(340,5));
            progress.pivot=new Vector2(0,.5f);
            var bar=progress.gameObject.AddComponent<Image>();
            bar.color=Cyan;
            bar.raycastTarget=false;

            var skip=Button(overlay,"SkipIntro","SKIP INTRO",Vector2.zero,new Vector2(210,56),intro.SkipAll,Card);
            Move(skip,overlay,new Vector2(1,0),new Vector2(-139,60),new Vector2(210,56));
            var pause=Button(overlay,"IntroPause","PAUSE",Vector2.zero,new Vector2(140,56),menus.OpenPause,Card);
            Move(pause,overlay,new Vector2(1,1),new Vector2(-101,-56),new Vector2(140,56));

            Assign(intro,"match",match);
            Assign(intro,"roster",roster);
            Assign(intro,"anchors",anchors);
            Array(intro,"referees",refs);
            Assign(intro,"bluePennant",blue);
            Assign(intro,"whitePennant",white);
            Assign(intro,"overlay",overlay.gameObject);
            Assign(intro,"lineups",lineups.gameObject);
            Assign(intro,"title",title);
            Assign(intro,"subtitle",subtitle);
            Assign(intro,"blueLines",blueLines);
            Assign(intro,"whiteLines",whiteLines);
            Assign(intro,"progress",progress);
            overlay.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static TMP_Text BuildCeremonyRoster(Transform parent,string name,string team,int side,Color accent)
        {
            var panel=Panel(parent,name,new Vector2(side,.5f),new Vector2(side==0?223:-223,0),new Vector2(375,466),Navy);
            Panel(panel,"Accent",new Vector2(.5f,1),new Vector2(0,-4),new Vector2(375,7),accent);
            var heading=Text(panel,"Team",team,new Vector2(0,176),new Vector2(318,62),35,accent);
            heading.fontStyle=FontStyles.Bold;
            heading.alignment=TextAlignmentOptions.Left;
            heading.characterSpacing=8;
            var players=Text(panel,"Athletes","",new Vector2(0,-23),new Vector2(318,332),24,Color.white);
            players.alignment=TextAlignmentOptions.TopLeft;
            players.lineSpacing=12;
            return players;
        }

        private static Material FindKit(string name)
        {
            foreach(string guid in AssetDatabase.FindAssets(name+" t:Material"))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(material!=null && material.name==name)return material;
            }
            return null;
        }
    }
}
