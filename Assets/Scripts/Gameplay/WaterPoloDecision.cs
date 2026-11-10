using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public readonly struct SwimmerDecision
    {
        public readonly SwimmerAIBehaviour Behaviour;
        public readonly Vector3 Target;
        public readonly SwimmerIdentity Receiver;
        public SwimmerDecision(SwimmerAIBehaviour behaviour, Vector3 target, SwimmerIdentity receiver = null)
        { Behaviour = behaviour; Target = target; Receiver = receiver; }
    }

    /// <summary>Shared tactical reasoning for either team. Returns intent; never moves bodies or assigns ownership.</summary>
    public static class WaterPoloDecision
    {
        public static SwimmerDecision Evaluate(SwimmerIdentity player, PrototypeTeamTactics team)
        {
            BallController ball = team.Ball;
            SwimmerIdentity carrier = team.Carrier;
            if (carrier == player)
            {
                float held = Time.time - ball.OwnershipStartedAt;
                if (held >= team.MinimumHoldTime || team.ShotUrgent)
                {
                    Vector3 goal = team.ShotTarget();
                    if (team.ShotQuality(player.Motor.Position) >= team.ShotQualityThreshold || team.ShotUrgent)
                        return new SwimmerDecision(SwimmerAIBehaviour.Shoot, goal);
                    var receiver = team.CanPass ? team.BetterPass(player) : null;
                    if (receiver != null) return new SwimmerDecision(SwimmerAIBehaviour.Pass, receiver.Motor.Position, receiver);
                    if (held >= team.MaximumHoldTime) return new SwimmerDecision(SwimmerAIBehaviour.Shoot, goal);
                }
                return new SwimmerDecision(SwimmerAIBehaviour.Carry, team.CarryTarget(player));
            }
            if (carrier != null)
            {
                if (carrier.Team == player.Team) return new SwimmerDecision(SwimmerAIBehaviour.Support, team.Support(player, carrier));
                return new SwimmerDecision(carrier.IsFieldPlayer && team.Presser == player ? SwimmerAIBehaviour.Press : SwimmerAIBehaviour.Defend, team.Defense(player, carrier));
            }
            if(ball.IsAirborne)
                return new SwimmerDecision(team.LoosePursuer==player || ball.IntendedReceiver==player.Interaction?SwimmerAIBehaviour.PursueLooseBall:SwimmerAIBehaviour.Formation,
                    team.LoosePursuer==player || ball.IntendedReceiver==player.Interaction?team.Clamp(ball.LandingPoint):team.Formation(player));
            if (ball.IntendedReceiver != null)
            {
                var receiver=ball.IntendedReceiver.GetComponent<SwimmerIdentity>();
                if(receiver==player)return new SwimmerDecision(SwimmerAIBehaviour.AwaitPass,player.Motor.Position);
                if(receiver.Team==player.Team)return new SwimmerDecision(SwimmerAIBehaviour.Support,team.Support(player,receiver));
                if(ball.CanCollect && team.LoosePursuer==player)return new SwimmerDecision(SwimmerAIBehaviour.PursueLooseBall,team.Clamp(ball.PhysicsPosition));
                return new SwimmerDecision(SwimmerAIBehaviour.Defend,team.Defense(player,receiver));
            }
            if (ball.CanCollect && team.LoosePursuer == player) return new SwimmerDecision(SwimmerAIBehaviour.PursueLooseBall, team.Clamp(ball.PhysicsPosition));
            return new SwimmerDecision(SwimmerAIBehaviour.Formation, team.Formation(player));
        }
    }
}
