using System.Collections.Generic;
using System.Linq;
using ToduStudios.WaterPolo.Hub;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public sealed partial class MatchRules
    {
        // A scene swimmer is a tactical slot. Personal records follow the match athlete between slots.
        sealed class MatchAthlete
        {
            public AthleteDefinition Definition;
            public int Fouls;
            public int Cap;
            public bool Reserved, Disqualified;
            public Transform BenchVisual;
        }
        readonly Dictionary<SwimmerIdentity, MatchAthlete> occupants = new();
        readonly Dictionary<SwimmerIdentity, MatchAthlete> incoming = new();
        readonly Dictionary<PrototypeTeam, List<MatchAthlete>> benches = new();
        readonly Dictionary<SwimmerIdentity, string> originalBindings = new();
        public IReadOnlyList<SwimmerIdentity> MatchMembers => roster.Members;
        public event System.Action<SwimmerIdentity, string> SubstitutionStage;

        sealed class Exchange
        {
            public Transform Visual;
            public Animator Animator;
            public Animator OutgoingAnimator;
            public bool Flying;
            public bool Contact;
            public bool Arrived;
        }
        readonly Dictionary<SwimmerIdentity, Exchange> exchanges = new();
        readonly HashSet<SwimmerIdentity> immediateReplacement = new();

        Vector3 Gate(SwimmerIdentity slot, bool flying)
        {
            Vector3 p = WaitingPoint(slot);
            if (!flying) return p;
            p.x = pool.transform.position.x + pool.Width * .5f + .6f;
            p.z = Mathf.Lerp(pool.transform.position.z, Team(slot.Team).DefendedGoal.transform.position.z, .55f);
            return p;
        }

        WaterVisuals.VenueTeamFacility[] facilities;
        System.Collections.IEnumerator ReturnToBench(SwimmerIdentity slot, MatchAthlete athlete, Exchange exchange)
        {
            var facility = facilities.FirstOrDefault(f => f.Serves(slot.Team));
            if (facility == null || facility.seats.Length == 0 || facility.waterEntry == null)
            {
                Destroy(exchange.Visual.gameObject);
                yield break;
            }
            var visual = exchange.Visual;
            athlete.BenchVisual = visual;
            var head = exchange.Animator.GetBoneTransform(HumanBodyBones.Head);
            int seat = Mathf.Abs(athlete.Cap) % Mathf.Min(9, facility.seats.Length);
            for (int i = facility.accessRoute.Length - 1; i >= -1; i--)
            {
                Transform anchor = i >= 0 ? facility.accessRoute[i] : facility.seats[seat];
                if (anchor == null) continue;
                while (visual != null && athlete.BenchVisual == visual)
                {
                    Vector3 offset = anchor.position + Vector3.up * (i < 0 ? .7f : .3f) - head.position;
                    if (offset.sqrMagnitude < .016f) break;
                    if (!match.IsPaused && !match.GoalPresentationActive) visual.position += Vector3.ClampMagnitude(offset, 3.5f * Time.deltaTime);
                    yield return null;
                }
                if (visual == null || athlete.BenchVisual != visual) yield break;
            }
            SubstitutionStage?.Invoke(slot, "On bench");
            // No seated clip exists yet. The reserve slot remains available in the match roster.
            Destroy(visual.gameObject);
            athlete.BenchVisual = null;
        }

        void StageReserve(SwimmerIdentity slot, bool flying)
        {
            var staged = incoming[slot];
            if (staged.BenchVisual != null) { Destroy(staged.BenchVisual.gameObject); staged.BenchVisual = null; }
            var source = slot.GetComponentInChildren<WaterVisuals.RiggedSwimmerVisual>();
            if (source == null) return;
            var station = Gate(slot, flying);
            var actor = Instantiate(source.gameObject);
            actor.name = "Reserve " + incoming[slot].Definition.name;
            // The clone contains only the animated skin. Gameplay stays on the original slot.
            var driver = actor.GetComponent<WaterVisuals.RiggedSwimmerVisual>();
            driver.enabled = false;
            var animator = actor.GetComponent<Animator>();
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            actor.transform.localScale = source.transform.lossyScale;
            actor.transform.rotation = Quaternion.LookRotation(Vector3.left);
            var start = station;
            if (flying) start.z = WaitingPoint(slot).z;
            start.y += .26f;
            actor.transform.position += start - head.position;
            exchanges[slot] = new Exchange { Visual = actor.transform, Animator = animator, OutgoingAnimator = source.Animator, Flying = flying };
        }

        bool AdvanceReserve(SwimmerIdentity slot, Vector3 station)
        {
            if (!exchanges.TryGetValue(slot, out var exchange)) return true;
            var head = exchange.Animator.GetBoneTransform(HumanBodyBones.Head);
            var target = station + Vector3.right * .38f + Vector3.up * .26f;
            var delta = target - head.position;
            exchange.Visual.position += Vector3.ClampMagnitude(delta, 3.5f * Time.fixedDeltaTime);
            exchange.Arrived = delta.sqrMagnitude < .01f;
            if (!exchange.Flying) return exchange.Arrived;
            if (!exchange.Arrived || (slot.Motor.Position - station).sqrMagnitude > .012f) return false;
            // Interim contact pose using the existing hand bones, above the water in the flying area.
            var outgoing = exchange.OutgoingAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var incomingHand = exchange.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Vector3 contact = station + Vector3.right * .19f + Vector3.up * .23f;
            outgoing.position = incomingHand.position = contact;
            if (!exchange.Contact)
            {
                exchange.Contact = true;
                SubstitutionStage?.Invoke(slot, "Hand contact");
                return false;
            }
            return true;
        }

        void PrepareBench()
        {
            if (benches.Count > 0 || !useCompetitionReentryArea) return;
            facilities = FindObjectsByType<WaterVisuals.VenueTeamFacility>();
            var service = HubApplication.Instance?.Progression;
            var catalog = service?.Catalog ?? Resources.Load<HubCatalog>("HubCatalog");
            if (catalog == null) return;
            var owned = service?.Snapshot.collection.Select(entry => entry.playerId).ToHashSet();
            foreach (var team in new[] { PrototypeTeam.Blue, PrototypeTeam.White })
            {
                var bench = new List<MatchAthlete>();
                benches.Add(team, bench);
                var slots = roster.Members.Where(p => p.Team == team).OrderBy(p => p.IsFieldPlayer).ThenBy(p => p.FieldPosition).ToArray();
                var starters = new HashSet<string>();
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    originalBindings[slot] = slot.CollectionDefinitionId;
                    var definition = string.IsNullOrEmpty(slot.CollectionDefinitionId) ? catalog.players[i] : catalog.Player(slot.CollectionDefinitionId);
                    occupants[slot] = new MatchAthlete { Definition = definition, Cap = slot.CapNumber, Fouls = slot.PersonalFouls };
                    starters.Add(definition.id);
                }
                var usedCaps = slots.Select(slot => slot.CapNumber).ToHashSet();
                var freeCaps = new Queue<int>(Enumerable.Range(2,13).Where(cap => !usedCaps.Contains(cap)));
                foreach (var definition in catalog.players)
                    if (freeCaps.Count > 0 && !starters.Contains(definition.id) && (team != PrototypeTeam.Blue || owned == null || owned.Contains(definition.id)))
                        bench.Add(new MatchAthlete { Definition = definition, Cap = freeCaps.Dequeue() });
            }
        }

        public string ReserveName(string id)
        {
            foreach (var bench in benches.Values)
                foreach (var athlete in bench)
                    if (athlete.Definition.id == id) return athlete.Definition.name;
            return id;
        }

        public string[] Replacements(SwimmerIdentity slot)
        {
            PrepareBench();
            if (slot == null || slot.EnteringField || !benches.TryGetValue(slot.Team, out var bench) || incoming.ContainsKey(slot)) return System.Array.Empty<string>();
            return bench.Where(p => !p.Reserved && !p.Disqualified && p.Fouls < personalFoulLimit && p.Definition.goalkeeper == !slot.IsFieldPlayer)
                        .Select(p => p.Definition.id).ToArray();
        }

        public bool ReserveReplacement(SwimmerIdentity slot, string id)
        {
            if (slot == null || slot.Interaction.HasBall || !Replacements(slot).Contains(id)) return false;
            bool disciplinary = slot.PermanentlyRemoved || slot.ExclusionRemaining > 0 || slot.AwaitingExit;
            if (!disciplinary && match.Phase != MatchPhase.Playing) return false;
            var replacement = benches[slot.Team].Find(p => p.Definition.id == id);
            replacement.Reserved = true;
            incoming.Add(slot, replacement);
            Park(slot);
            StageReserve(slot, !disciplinary);
            selection.RefreshEligibility();
            SubstitutionStage?.Invoke(slot, "Departing");
            if (immediateReplacement.Contains(slot)) Reenter(slot, true);
            return true;
        }

        void RecordDiscipline(SwimmerIdentity slot, bool penaltyFoul)
        {
            PrepareBench();
            if (!occupants.TryGetValue(slot, out var athlete)) return;
            athlete.Fouls = slot.PersonalFouls;
            if (!slot.PermanentlyRemoved) return;
            athlete.Disqualified = true;
            slot.LastDisqualifiedCap = slot.CapNumber;
            if (penaltyFoul) immediateReplacement.Add(slot);
            if (slot.Team != selection.HumanTeam)
            {
                var candidates = Replacements(slot);
                if (candidates.Length > 0) ReserveReplacement(slot, candidates[0]);

            }
        }

        void SwapAtEntry(SwimmerIdentity slot)
        {
            if (!incoming.Remove(slot, out var replacement)) return;
            immediateReplacement.Remove(slot);
            var bench = benches[slot.Team];
            var leaving = occupants[slot];
            leaving.Fouls = slot.PersonalFouls;
            bench.Remove(replacement);
            bench.Add(leaving);
            if (exchanges.Remove(slot, out var exchange)) StartCoroutine(ReturnToBench(slot, leaving, exchange));
            occupants[slot] = replacement;
            replacement.Reserved = false;
            slot.SetCollectionDefinition(replacement.Definition.id);
            slot.MatchCapNumber = replacement.Cap;
            slot.PersonalFouls = replacement.Fouls;
            slot.PermanentlyRemoved = false;
            slot.AwaitingExit = false;
            SubstitutionStage?.Invoke(slot, "Entering");
        }

        void ResetBench()
        {
            foreach (var pair in originalBindings)
            {
                pair.Key.SetCollectionDefinition(pair.Value);
                pair.Key.MatchCapNumber = 0;
                pair.Key.LastDisqualifiedCap = 0;
            }
            StopAllCoroutines();
            foreach (var bench in benches.Values) foreach (var athlete in bench) if (athlete.BenchVisual != null) Destroy(athlete.BenchVisual.gameObject);
            foreach (var entry in exchanges.Values) if (entry.Visual != null) Destroy(entry.Visual.gameObject);
            exchanges.Clear();
            immediateReplacement.Clear();
            originalBindings.Clear();
            occupants.Clear();
            incoming.Clear();
            benches.Clear();
        }
    }
}
