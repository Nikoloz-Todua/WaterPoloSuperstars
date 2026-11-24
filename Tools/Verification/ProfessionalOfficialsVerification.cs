using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ProfessionalOfficialsVerification
{
    const string Output = "Logs/Milestone29/Officials";
    static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();

    public static async Task<string> Candidate()
    {
        var intro = One<PreMatchPresentation>();
        if (intro.Active) intro.SkipAll();
        await Until(() => !intro.Active && !One<PrototypeMatch>().GameplayView.CinematicOverride);
        var official = Object.FindObjectsByType<RefereeVisual>().First();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Equipment/ProfessionalReferee/Professional Official.prefab");
        ProfessionalRefereeSetup.Bind(official, prefab);
        var pose = official.GetComponentInChildren<ProfessionalRefereePose>();
        var animator = pose.GetComponent<Animator>();
        if (!animator.isHuman || !animator.avatar.isValid || animator.applyRootMotion)
            throw new InvalidOperationException("Candidate referee needs a valid in-place Humanoid.");
        var observer = official.GetComponentInParent<PoolsideReferee>();
        if (observer != null) observer.enabled = false;
        pose.Stand();
        await Capture("Official-Standing", pose);
        float sample = Time.time;
        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Vector3 firstSole = left.position;
        float change = 0;
        while (Time.time - sample < .65f)
        {
            pose.Animate(1.4f, RefereeSignal.None, Vector3.zero, Vector3.zero, false);
            change = Mathf.Max(change, Vector3.Distance(left.position, firstSole));
            await Task.Delay(13);
        }
        if (change < .05f || pose.GetComponentsInChildren<Collider>().Length != 0)
            throw new InvalidOperationException("Walking must visibly change the skinned foot position without a collider.");
        await Capture("Official-Walking", pose);
        sample = Time.time;
        Vector3 forward = pose.transform.parent.forward;
        while (Time.time - sample < .48f)
        {
            pose.Animate(0, RefereeSignal.Goal, pose.transform.position + forward * 4f, Vector3.zero, false);
            await Task.Delay(13);
        }
        Vector3 hand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        if (hand.y < pose.transform.position.y + .9f) throw new InvalidOperationException("Goal point did not lift the hand.");
        await Capture("Official-Signal", pose);
        var meshes = pose.GetComponentsInChildren<SkinnedMeshRenderer>();
        uint triangles = meshes.Sum(r => (int)r.sharedMesh.GetIndexCount(0) / 3) == 8316 ? 8316u : 0u;
        if (triangles == 0) throw new InvalidOperationException("The official mesh binding changed.");
        return $"PASS. Separate in-place Humanoid official with {triangles} triangles. Live standing, walking and goal point sampled. Left foot changed by {change:F3} m.";
    }

    public static async Task<string> Lineup()
    {
        var intro = One<PreMatchPresentation>();
        if (Object.FindObjectsByType<RefereeVisual>().Count(v => v.UsesProfessional) != 2)
            throw new InvalidOperationException("Both saved referee roots must use the new model.");
        await Until(() => intro.Stage == PreMatchStage.FormalLineup && intro.StageElapsed > .65f);
        var cast = GameObject.Find("Prematch visual cast");
        if (cast == null || cast.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Count(v => v.sharedMesh != null && v.sharedMesh.name == "ProfessionalOfficial") != 2)
            throw new InvalidOperationException("Lineup should copy both officials' skinned meshes.");
        await Screenshot("Official-Lineup");
        await Until(() => intro.Stage == PreMatchStage.CaptainExchange && intro.StageElapsed > .9f);
        await Screenshot("Official-Pennant");
        return "PASS. Both professional officials are present in formal lineup and pennant exchange.";
    }

    static async Task Capture(string name, ProfessionalRefereePose pose)
    {
        var dev = One<ArenaDeveloperCamera>();
        var view = dev.viewpoints[2];
        Vector3 point = pose.transform.position + Vector3.up * .90f;
        view.position = point + pose.transform.forward * 2.9f + pose.transform.right * 1.2f + Vector3.up * .1f;
        view.LookAt(point);
        dev.SetView(2);
        await Screenshot(name);
        dev.SetView(-1);
    }

    static async Task Screenshot(string name)
    {
        Directory.CreateDirectory(Output);
        int frames = Time.frameCount;
        await Until(() => Time.frameCount > frames + 3);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/" + name + ".png"));
        await Until(() => Time.frameCount > frames + 6);
    }

    static async Task Until(Func<bool> condition)
    {
        float timeout = Time.realtimeSinceStartup + 14f;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > timeout) throw new TimeoutException("Official verification timed out.");
            await Task.Delay(15);
        }
    }
}
