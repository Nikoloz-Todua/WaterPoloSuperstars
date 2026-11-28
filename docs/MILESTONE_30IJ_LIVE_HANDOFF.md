# Milestones 30I and 30J live handoff

Start from completed 30H recovery. The developer approves the current keeper and accepts the current swimwear for now. Garment and walking refinement are deferred.

Workstream B starts with physical movement, live goalkeeper recovery and visible boundary flight. Workstream A then addresses ordinary free throws and post-goal reactions. Localized water refinements follow. Only deterministic short fixtures are in scope.

## Implementation checkpoint

1. **Movement and keeper recovery.** OlympicMatchArena now references its existing Competition Dimensions asset from PoolBounds. SwimmerMotor uses the basin and capsule footprint. GoalMouth constrains posts and net travel. The existing keeper pursues a nearby live loose ball through the motor and normal pickup. The saved arena GUID is unchanged.
2. **Dead-ball flight.** MatchRules awards once, then the same ball finishes a short physical flight with score and acquisition gates off. Replacement, player placement and restart readiness happen at the existing free-throw transition. End-rope and goal planes are distinct.
3. **Match continuity.** Ordinary fouls proceed directly to ready once possession and the mark are established. Other field athletes use the 30F support and defensive assignments. Goal celebration shows the live athletes with contextual gestures and bounded swimming toward the upcoming restart. Replay still uses separate render-only recorded actors.
4. **Water.** The existing contact history and bounded mesh draw sampled hand sculls, a small floating-ball ring and speed-sensitive wakes and water impacts. The approved animation clips and ordinary/skip physics remain in use.
5. **Focused verification complete.** 17 isolated cases passed. Field athletes closed both physical side edges, slid tangentially and entered/exited both shallow recesses. The keeper physically recovered a live loose ball behind the station. Whole-ball scoring, in-play rebound and pickup, five visible exit outcomes, ordinary-foul release, contextual goals and centre return, two replay views, and representative idle/crawl/sprint/drop/skip effects were checked. The final report records these observations and their limits. [Test Lab guide](MILESTONE_30IJ_MANUAL_TEST_GUIDE.md) · [Evidence](Evidence/Milestone30IJ/)

Stopped at 30I and 30J. Unity was left in clean Edit Mode with Boot as the Play entry. Project compilation and current Console are clean. Arena GUID and real profile are preserved. Personal visual approval, Android performance, deeper rule compliance and the remaining development backlog are open. Retain the approved keeper, 30G swim motion and 30H garment.
