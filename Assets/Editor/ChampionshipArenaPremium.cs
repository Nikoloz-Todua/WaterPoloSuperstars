using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ToduStudios.WaterPolo.WaterVisuals;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static partial class ChampionshipArenaRebuild
    {
        public const string PremiumName = "Championship Arena Premium Finish";
        public const string PremiumRecovery = "Assets/Scenes/Recovery/OlympicMatchArena_PrePremiumPolish.unity";
        [MenuItem("Tools/Water Polo/Arena/Apply Premium Championship Finish")]
        public static void PremiumFinish()
        {
            RequireSaved();
            var env = Object.FindAnyObjectByType<ArenaQualityController>();
            root = env.transform.Find(Name);
            if (root == null) throw new InvalidOperationException("The existing championship venue is required.");
            if (root.Find(PremiumName) != null) { Debug.Log("Premium finish already installed; Inspector tuning preserved."); return; }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PremiumRecovery) == null && !AssetDatabase.CopyAsset(OlympicArenaSetup.ScenePath, PremiumRecovery)) throw new InvalidOperationException("Cannot create native recovery.");
            layout = AssetDatabase.LoadAssetAtPath<ChampionshipVenueLayout>(Root + "/Settings/ChampionshipVenue.asset");
            Materials();
            var finish = Child(PremiumName, root);
            // Retire the two broad end bridges. The basin reads as one long architectural pool.
            foreach (var r in root.Find("Movable Bulkheads and Competition End Access").GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var field = GameObject.Find(AuthenticFieldSetup.Name).transform;
            foreach (var r in field.Find("Measured Competition Ropes").GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            PremiumRopes(finish);
            PremiumFloor(finish);
            PremiumGoals(finish);
            PremiumAccess(finish);
            PremiumArchitecture(finish);
            OrientDeckLettering(root);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Premium championship finish saved in OlympicMatchArena. Original gameplay field, detectors, actors and match systems retained.");
        }

        static void PremiumRopes(Transform parent)
        {
            var pool = Object.FindAnyObjectByType<PoolBounds>();
            float half = pool.Length * .5f, width = pool.Width * .5f;
            var red = AssetDatabase.LoadAssetAtPath<Material>(AuthenticFieldSetup.Root + "/Materials/Competition Red.mat");
            var yellow = AssetDatabase.LoadAssetAtPath<Material>(AuthenticFieldSetup.Root + "/Materials/Competition Yellow.mat");
            var green = AssetDatabase.LoadAssetAtPath<Material>(AuthenticFieldSetup.Root + "/Materials/Competition Green.mat");
            var b = new ChampionshipArenaBatch(Child("Continuous Competition Ropes and Reentry Rectangles", parent));
            void Rope(Vector3 a, Vector3 c, Material color)
            {
                // An uninterrupted tensile core plus contiguous molded float modules, combined per material.
                b.Tube(color, a, c, .027f, 6);
                int n = Mathf.CeilToInt(Vector3.Distance(a, c) / .17f);
                for (int i = 0; i < n; i++) b.Tube(color, Vector3.Lerp(a, c, (i + .015f) / n), Vector3.Lerp(a, c, (i + .985f) / n), .055f, 8);
            }
            foreach (int s in new[] { -1, 1 })
            {
                foreach (int end in new[] { -1, 1 })
                {
                    float z = end * half;
                    Rope(new Vector3(s * width, .025f, z), new Vector3(s * width, .025f, z - end * 2), red);
                    Rope(new Vector3(s * width, .025f, z - end * 2), new Vector3(s * width, .025f, z - end * 6), yellow);
                    Rope(new Vector3(s * width, .025f, z - end * 6), new Vector3(s * width, .025f, end * .09f), green);
                    Rope(new Vector3(s * width, .025f, z), new Vector3(s * width, .025f, z + end * .3f), red);
                    // End anchorage is exactly 0.30m behind the goal line. No floating disconnected end.
                    b.Tube(metal, new Vector3(s * width, .025f, z + end * .3f), new Vector3(s * 12.48f, .18f, z + end * .3f), .018f, 8);
                    b.Bevel(metal, new Vector3(s * 12.48f, .18f, z + end * .3f), new Vector3(.14f, .07f, .27f), .025f);
                    foreach (float d in new[] { 0f, 2f, 5f, 6f })
                    {
                        var color = d == 0 ? white : d == 6 ? yellow : red;
                        if (d == 0f) b.Floor(color, new Vector3(s * 11.3f, -.025f, z - end * d), new Vector2(2.55f, .10f));
                        b.Box(color, new Vector3(s * 12.48f, .12f, z - end * d), new Vector3(.055f, .22f, .11f));
                    }
                }
                Rope(new Vector3(s * width, .025f, -.09f), new Vector3(s * width, .025f, .09f), white);
                b.Box(white, new Vector3(s * 12.48f, .12f, 0), new Vector3(.055f, .22f, .13f));
            }
            foreach (int end in new[] { -1, 1 })
            {
                float z = end * (half + .3f);
                foreach (int side in new[] { -1, 1 })
                {
                    Rope(new Vector3(side * 1.65f, .025f, z), new Vector3(side * 3.6f, .025f, z), red);
                    Rope(new Vector3(side * 3.6f, .025f, z), new Vector3(side * 8, .025f, z), white);
                    Rope(new Vector3(side * 8, .025f, z), new Vector3(side * 10, .025f, z), side > 0 ? red : white);
                }
                // Existing exclusion logic waits in this east corner, opposite the west operations table.
                float rear = z + end * .8f;
                Rope(new Vector3(8, .025f, z), new Vector3(8, .025f, rear), red);
                Rope(new Vector3(8, .025f, rear), new Vector3(10, .025f, rear), red);
                Rope(new Vector3(10, .025f, z), new Vector3(10, .025f, rear), red);
                b.Tube(metal, new Vector3(10, .025f, rear), new Vector3(12.48f, .18f, rear), .016f, 8);
            }
            b.Save(Root + "/Meshes");
        }

        /// <summary>Updates only the generated entry-landing mesh after a clearance review.</summary>
        public static void ReviewPremiumLanding()
        {
            RequireSaved();
            Materials();
            var temporary = Child("Premium Landing Mesh Review", null);
            try { PremiumAccess(temporary); }
            finally { Object.DestroyImmediate(temporary.gameObject); }
            var installed = Object.FindAnyObjectByType<ArenaQualityController>().transform.Find(Name);
            OrientDeckLettering(installed);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        static void OrientDeckLettering(Transform venue)
        {
            foreach (var label in venue.GetComponentsInChildren<TMP_Text>(true))
                if (label.transform.position.x > 0 && (label.text == "OFFICIALS  /  KEEP CLEAR" || label.text == "TEAM ACCESS" || label.text == "RE-ENTRY"))
                    label.transform.rotation = Quaternion.Euler(90, -90, 0);
        }

        static void PremiumFloor(Transform parent)
        {
            var ink = Mat("Underwater Anthracite Ceramic", "#102B39", .02f, .23f);
            var b = new ChampionshipArenaBatch(Child("Inlaid Swimming Lane T Markings", parent));
            float y = -layout.waterDepth + .004f;
            for (int lane = -4; lane <= 4; lane++)
            {
                float x = lane * 2.5f;
                b.Floor(ink, new Vector3(x, y, 0), new Vector2(.20f, 46));
                foreach (int end in new[] { -1, 1 })
                {
                    b.Floor(ink, new Vector3(x, y + .001f, end * 23), new Vector2(1.55f, .20f));
                    b.Box(ink, new Vector3(x, -1.20f, end * 24.992f), new Vector3(.20f, 1.7f, .009f));
                    b.Box(ink, new Vector3(x, -1.02f, end * 24.986f), new Vector3(.80f, .12f, .012f));
                }
            }
            // Thin perimeter course reads as ceramic detail, with no football circle or painted pitch.
            foreach (int s in new[] { -1, 1 }) b.Floor(ice, new Vector3(s * 12.2f, y, 0), new Vector2(.075f, 49.5f));
            b.Save(Root + "/Meshes");
        }

        static void PremiumGoals(Transform parent)
        {
            foreach (var goal in Object.FindObjectsByType<GoalMouth>())
            {
                // Existing rigid frame and reactive enclosing net already match 3m x .9m; retain those references.
                var g = Child(goal.name + " Professional Goal Engineering", parent);
                g.SetPositionAndRotation(goal.transform.position, goal.transform.rotation);
                var b = new ChampionshipArenaBatch(g);
                float w = goal.Width * .5f, d = goal.Depth, h = goal.Height;
                foreach (int side in new[] { -1, 1 })
                {
                    float x = side * (w + .17f);
                    b.Bevel(white, new Vector3(x, -.015f, d * .53f), new Vector3(.22f, .17f, d + .38f), .045f);
                    b.Box(cobalt, new Vector3(x, -.085f, d * .53f), new Vector3(.225f, .045f, d + .25f));
                    b.Tube(metal, new Vector3(x, h - .07f, .10f), new Vector3(x, .13f, d + .13f), .035f, 8);
                    b.Tube(white, new Vector3(x, .13f, d + .13f), new Vector3(x, h - .10f, d + .13f), .035f, 8);
                    b.Bevel(metal, new Vector3(x, .15f, d + .10f), new Vector3(.14f, .10f, .19f), .025f);
                    // Lateral stays connect to side-wall fittings without blocking the goal mouth.
                    Vector3 anchor = new Vector3(side * 12.45f, .16f, d + .15f);
                    b.Tube(metal, new Vector3(x, .10f, d + .15f), anchor, .012f, 6);
                    b.Bevel(metal, anchor, new Vector3(.12f, .09f, .22f), .02f);
                }
                b.Tube(white, new Vector3(-w - .17f, h - .10f, d + .13f), new Vector3(w + .17f, h - .10f, d + .13f), .035f, 8);
                for (int i = 0; i <= 12; i++)
                {
                    float x = Mathf.Lerp(-w, w, i / 12f);
                    b.Bevel(white, new Vector3(x, h + .025f, .075f), new Vector3(.025f, .055f, .12f), .008f);
                }
                b.Save(Root + "/Meshes", true);
            }
        }

        static void PremiumAccess(Transform parent)
        {
            var b = new ChampionshipArenaBatch(Child("Corner Team Entry Landings", parent));
            foreach (int end in new[] { -1, 1 })
            {
                // Retain a compact corner landing on the actual bench route. The centre end water stays open.
                b.Bevel(navy, new Vector3(11.18f, .39f, end * 14.2f), new Vector3(2.95f, .62f, 1.0f), .055f);
                b.Floor(rubber, new Vector3(11.18f, .704f, end * 14.2f), new Vector2(2.95f, 1.0f));
                b.Floor(rubber, new Vector3(9.4f, .704f, end * 14.35f), new Vector2(1.35f, .30f));
                b.Part(rubber).Face(new Vector3(8.8f, .70f, end * 14.2f), new Vector3(10, .70f, end * 14.2f), new Vector3(10, .12f, end * 13.45f), new Vector3(8.8f, .12f, end * 13.45f), Vector3.up);
                b.Tube(metal, new Vector3(12.2f, .76f, end * 14.66f), new Vector3(10.1f, .76f, end * 14.66f), .025f, 8);
                // Thin architectural slats and wheel-free equipment mats stay behind bench circulation.
                b.Floor(ice, new Vector3(17.4f, .716f, end * 14.2f), new Vector2(1.0f, .07f));
                Label("RE-ENTRY", b.root, new Vector3(11.25f, .718f, end * 14.22f), Quaternion.Euler(90, -90, 0), new Vector2(1.45f, .28f), 1.8f, white.color);
            }
            b.Save(Root + "/Meshes");
        }

        static void PremiumArchitecture(Transform parent)
        {
            var b = new ChampionshipArenaBatch(Child("Championship Architectural Finish", parent));
            // Deliberate vertical rhythm breaks up otherwise empty end walls without increasing real light count.
            foreach (int end in new[] { -1, 1 }) foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 7; i++)
                {
                    float x = side * (22 + i * 4.4f);
                    b.Box(cobalt, new Vector3(x, 22.5f, end * 69.6f), new Vector3(1.7f, 17, .075f));
                    b.Box(ice, new Vector3(x - side * .91f, 24, end * 69.5f), new Vector3(.055f, 11, .04f));
                }
            }
            // Two-sided original graphic fascia on the entrance screen: no blank rear slab in wide opening shots.
            var screenPrint = Mat("Championship Portal Graphic", "#123A6C", .12f, .30f);
            b.Box(screenPrint, new Vector3(0, 7.7f, -33.0f), new Vector3(21.4f, 5.7f, .025f));
            for (int i = 0; i < 8; i++)
            {
                float x = -9.4f + i * 2.6f;
                b.Box(ice, new Vector3(x, 7.7f, -33.025f), new Vector3(.13f, 5.1f, .03f), Quaternion.Euler(0, 0, -24));
            }
            Label("WATER POLO SUPERSTARS", b.root, new Vector3(0, 8.15f, -33.07f), Quaternion.identity, new Vector2(17, 1.0f), 6.8f, white.color);
            Label("CHAMPIONSHIP SERIES  /  ATHLETE ENTRANCE", b.root, new Vector3(0, 6.75f, -33.07f), Quaternion.identity, new Vector2(17, .52f), 3.0f, white.color);
            b.Save(Root + "/Meshes");
            // Put wide inspection under the service trusses, preserving the actual gameplay camera profiles.
            var dev = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
            Vector3 p = new Vector3(34, 23, -43);
            dev.viewpoints[0].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(0, 4, 0) - p));
            // All current board observers, original pool lighting and quality budgets remain authoritative.
        }
    }
}
