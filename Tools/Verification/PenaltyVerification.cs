using System;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class PenaltyVerification
{
    static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();
    static async Task Until(Func<bool> condition)
    {
        double end = Time.realtimeSinceStartupAsDouble + 16;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > end) throw new Exception("Timed out at " + One<PrototypeMatch>().Phase);
            await Task.Delay(12);
        }
    }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Shot(string name) => ScreenCapture.CaptureScreenshot("Logs/PenaltiesExclusionsCamera/" + name + ".png");

    public static Task<object> Run() => Attempt("miss");
    public static Task<object> Post() => Attempt("post");
    public static Task<object> Touch() => Attempt("touch");
    public static Task<object> ThirdPenalty() => Attempt("third");
    public static Task<object> EndPeriod() => Attempt("period");
    public static Task<object> Goal() => Attempt("goal");
    public static Task<object> Save() => Attempt("save");
    public static Task<object> AdvantageGoal() => Attempt("advantage");
    static async Task<object> Attempt(string outcome)
    {
        var match = One<PrototypeMatch>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var field = One<PrototypeRoster>().FieldPlayers;
        var ai = Object.FindObjectsByType<PrototypeSwimmerAI>();
        var keepers = Object.FindObjectsByType<PrototypeGoalkeeper>();
        try
        {
            match.RestartMatch();
            await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession);
            foreach (var player in ai) player.enabled = false;
            var shooter = field.First(p => p.Team == PrototypeTeam.Blue);
            var offender = field.First(p => p.Team == PrototypeTeam.White);
            if (outcome == "third") typeof(SwimmerIdentity).GetProperty("PersonalFouls").SetValue(offender,2);
            int oldCap = offender.CapNumber;
            var goal = shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
            var keeper = keepers.First(k => k.Identity.Team == PrototypeTeam.White);
            if (outcome == "period" || outcome == "goal" || outcome == "advantage") keeper.enabled = false;
            if (outcome == "save")
            {
                var settings = new UnityEditor.SerializedObject(keeper);
                settings.FindProperty("saveChance").floatValue = 1;
                settings.FindProperty("catchChance").floatValue = 1;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var dir = goal.transform.forward;
            bool frameHit = false;
            One<BallController>().Contact += e => { if(e.Kind == BallImpactKind.Frame) frameHit = true; };
            var point = goal.transform.position - dir * 4;
            foreach (var p in field) p.Motor.ResetPose(Vector3.left * 8 + Vector3.forward * (p.CapNumber - 4), Quaternion.identity);
            shooter.Motor.ResetPose(point,Quaternion.LookRotation(dir));
            offender.Motor.ResetPose(point-dir*.8f,Quaternion.LookRotation(dir));
            var ball = One<BallController>();
            ball.ResetForRestart(shooter.Motor.Position);
            Check(shooter.Interaction.TryCollect(),"Carrier pickup");
            One<PlayerSelection>().Select(shooter);
            var charge = shooter.GetComponent<ShotCharge>();
            Check(charge.BeginCharge(),"Shooting action");
            Check(match.Rules.ReportContact(offender,shooter,IllegalContact.Push) && match.IsPlaying,"Delayed whistle");
            if(outcome=="period")
            {
                match.Clock.Tick(match.Clock.Remaining-.005f);
                match.AdvanceLiveTime(.02f);
            }
            if (outcome == "advantage")
            {
                await Task.Delay(420);
                Check(charge.ReleaseCharge(), "Advantage shot released");
                await Until(()=>match.BlueScore == 1);
                Check(offender.PersonalFouls == 0 && match.Phase == MatchPhase.GoalRestart, "No second award");
                return new { advantageGoal=true };
            }
            await Until(()=>match.Phase==MatchPhase.PenaltyReady);
            if (outcome == "third")
            {
                Check(offender.CapNumber != oldCap && offender.LastDisqualifiedCap == oldCap && offender.EligibleForPlay &&
                    offender.PersonalFouls == 0 && goal.transform.InverseTransformPoint(offender.Motor.Position).z <= -6f && match.Rules.ActiveFields(offender.Team)==6,"Immediate third-penalty replacement");
                return new { passed=true, oldCap, newCap=offender.CapNumber };
            }
            Check(offender.PersonalFouls==1 && offender.EligibleForPlay,"Discipline");
            Check(Mathf.Abs(goal.transform.InverseTransformPoint(shooter.Motor.Position).z+5)<.05f,"Five metre station");
            match.SetPaused(true);
            float held = match.Clock.Remaining;
            await Task.Delay(130);
            Check(match.Clock.Remaining == held && match.Phase == MatchPhase.PenaltyReady,"Pause preserves ready");
            match.SetPaused(false);
            Shot("Penalty-Setup");
            await Task.Delay(120);
            if (outcome == "touch")
            {
                var device = InputSystem.AddDevice<Touchscreen>();
                try
                {
                    var joystick = One<VirtualSwimJoystick>();
                    var shoot = Object.FindObjectsByType<GameplayActionButton>().First(button=>button.Action==MobileMatchAction.Shoot);
                    Vector2 Center(Transform t) => RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)t).TransformPoint(((RectTransform)t).rect.center));
                    void Finger(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 position) =>
                        InputSystem.QueueStateEvent(device,new TouchState{touchId=id,phase=phase,position=position});
                    var center=Center(joystick.transform);
                    Finger(1,UnityEngine.InputSystem.TouchPhase.Began,center);await Task.Delay(90);
                    Finger(1,UnityEngine.InputSystem.TouchPhase.Moved,center+Vector2.right*70);
                    await Until(()=>goal.transform.InverseTransformPoint(match.Rules.PenaltyAimPoint).x>.3f);
                    Finger(1,UnityEngine.InputSystem.TouchPhase.Ended,center+Vector2.right*70);
                    var button=Center(shoot.transform);
                    Finger(2,UnityEngine.InputSystem.TouchPhase.Began,button);
                    await Until(()=>charge.NormalizedCharge>.4f);
                    Shot("Penalty-Aim-Power");await Task.Delay(90);
                    Finger(2,UnityEngine.InputSystem.TouchPhase.Ended,button);
                    await Until(()=>match.Phase!=MatchPhase.PenaltyReady);
                    return new {touchAim=true, touchChargeRelease=true,phase=match.Phase.ToString()};
                }
                finally{InputSystem.RemoveDevice(device);}
            }
            InputSystem.QueueStateEvent(keyboard,outcome != "save" ? new KeyboardState(Key.D,Key.Space) : new KeyboardState(Key.Space));
            await Until(()=>charge.NormalizedCharge > (outcome == "save" ? .02f : .45f));
            if (outcome == "miss") Shot("Penalty-Aim-Power");
            await Task.Delay(65);
            if(outcome == "post" || outcome == "miss")
                await Until(()=>goal.transform.InverseTransformPoint(match.Rules.PenaltyAimPoint).x >= (outcome=="post"?1.45f:1.7f));
            Vector3 facing=shooter.Motor.Facing; int before = ball.ShotSequence;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
            await Until(()=>ball.ShotSequence!=before);
            Check(Mathf.Abs(goal.transform.InverseTransformPoint(ball.ShotOrigin).z + 5f) < .05f,"Ball released on the five-metre line");
            await Until(()=>match.IsPlaying || match.Phase==MatchPhase.GoalRestart || match.Phase==MatchPhase.QuarterBreak);
            if(outcome=="period")
            {
                Check(match.Phase==MatchPhase.QuarterBreak,"Dead ball at period end");
                return new {periodEnded=true, score=match.BlueScore};
            }
            Shot(match.Phase==MatchPhase.GoalRestart?"Penalty-Goal":ball.Owner!=null?"Penalty-Possession":"Penalty-Loose");
            if (outcome == "post") Check(frameHit && match.BlueScore==0,"Physical frame contact");
            if (outcome == "miss") Check(match.IsPlaying && match.BlueScore==0,"Miss resumes play");
            if (outcome == "goal") Check(match.BlueScore == 1 && match.Phase == MatchPhase.GoalRestart,$"Physical penalty goal: {match.Phase} facing {facing} ball {ball.PhysicsPosition} origin {ball.ShotOrigin} goal {goal.transform.position} keeper {keeper.Identity.Motor.Position}");
            if (outcome == "save")
            {
                Check(ball.Owner == keeper.Identity.Interaction && match.BlueScore == 0,$"Physical penalty catch: {match.Phase} facing {facing} ball {ball.PhysicsPosition} origin {ball.ShotOrigin} goal {goal.transform.position} keeper {keeper.Identity.Motor.Position} attempts {keeper.SaveAttempts}");
                Shot("Goalkeeper-Penalty-Save");
            }
            await Task.Delay(600);
            if(match.IsPlaying) Shot("After-Penalty-HUD");
            await Task.Delay(150);
            return new { passed=true, score=match.BlueScore, phase=match.Phase.ToString(), offender.PersonalFouls };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            foreach(var player in ai) player.enabled=true;
            foreach(var keeper in keepers) keeper.enabled=true;
        }
    }
}


