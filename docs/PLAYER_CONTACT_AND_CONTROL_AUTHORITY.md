# Player contact and control authority

## Human and tactical releases

`BallController` validates action source alongside possession, eligibility and the match phase. Human input must come from the current selectable owner. Tactical AI can make ordinary decisions for the opposing team. The opposing team's centre, boundary, foul and penalty restarts retain their existing automatic delivery. A human restart waits for a deliberate command. The direct Editor fixture source is available only in the Editor with the hub inactive.

The accepted source stays with the pending action until the evaluated hand releases the ball. Changing active athlete cancels a pending human action immediately. Pause and loss of ownership keep their cancellation paths. The `LastReleaseSource` property reports the physical release to focused fixtures without logging in production. Retreat sprint and involuntary possession loss have distinct release sources. During manual selection protection an off-ball human teammate can retain possession without independently throwing. Selection returns to that carrier after the existing grace period.

## Contact and possession

The former movement gave each kinematic Rigidbody an independent target. Two kinematic bodies did not experience a reciprocal force even with existing capsules and an enabled collision matrix. The saved arena opts the roster into a planar contact pass. All eligible active motored swimmers first propose their existing movement. Three bounded pair loops resolve upright or oriented crawl capsule overlap and give each existing Rigidbody one target. The motor reports the movement that survived contact and the pool or goalkeeper limits. This removes most of the inward component while leaving tangent movement and small shared displacement. The sprint-off, penalty, stoppage and replay authorities retain their phase behavior. Contact itself does not award fouls.

The resolve step uses one per-roster array and a maximum of 91 pair candidates per iteration for 14 swimmers. The existing ball Rigidbody and SphereCollider still use their existing physical interactions. Automatic pickup keeps the 1.20 m target reach, 2.35 m/s speed cap, bounded projection, bob tolerance, recent release suppression and live match eligibility. A landed dry pass can be recovered at the normal float speed despite its retained scoring attribution. Active shots keep their tighter speed and goal pocket checks. Automatic recovery passes the actual physical point into gathering.

## Windows Editor evidence

| Case | Result |
| --- | --- |
| Nearby loose pickup | Selected athlete collected at 0.94 m without an explicit button. |
| Human actions | One HumanInput pass with a physical catch and one HumanInput shot. Movement, rotation and pause test keys caused no release. Manual protection held and switching cancelled a queued pass. |
| Opponent shot and pass | Fresh TacticalAI physical release for each. The passing fixture staged an open outlet. |
| Ordinary restarts | Human restart waited 6.25 s with the match clock held, then passed by command. Opponent delivered a RuleRestart pass automatically. |
| Missed dry pass | One physical water impact. The loose ball was automatically recovered at about 2.108 m/s. |
| Keyboard | W and Up Arrow each measured 4.00 m/s in Lengthwise and Sideline Broadcast in the test lane. |
| Head-on teammates and centre opponents | Both retained their ordering and a 0.900 m minimum root gap. |
| Shoulder slide and perpendicular crossing | Minimum gaps 0.655 and 0.647 m. The offset approach cleared past the defender. |
| Sprint into a stationary athlete at the sideline | 0.780 m minimum gap, 5.750 m/s approach, defender stayed at x=8.900 m. |
| Contested ball | Minimum gap 0.790 m and one exclusive owner. |
| Resolution CPU marker | 9.48 μs mean per invocation, 11.20 μs peak recorded frame from 13 samples in this run. The marker covers the pair solve only. |

Root distance is measured between motors, so these figures do not report exact animated mesh clearance. The overhead and close side sequence shows contact and recovery into tread. Arms and hands can visually cross during close contact. The paired fixture parks other actors and applies controlled production motor commands. There is no Android timing, memory, thermal or performance comparison to 30B.

The runners are `Milestone30CControlVerification.Run` and `Milestone30CContactVerification.Run` under `Tools/Verification/`. Use a direct Play entry with the hub inactive and run the Task entry points with Unity Pipeline script run. Captures are under `docs/Evidence/Milestone30C/`. The successful local reports are `Logs/Milestone30C/Control-A-Final.json` and `Contact-B-Final.json`.

## Recovery

In the saved arena, select **PrototypeRoster** and turn off **Resolve Player Contact** in Edit Mode to return to independent motor movement. Enable it again and save the arena to restore the current contact solver. The pre-contact arena snapshot is `Logs/Milestone30C/OlympicMatchArena-BeforeContact.unity`. The authority and pickup changes are shared source changes. If recovery is needed, compare the files in the inventory against the 30B versions and restore the affected sources from a developer-selected source backup as a group. Finish direct tests with **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**.
