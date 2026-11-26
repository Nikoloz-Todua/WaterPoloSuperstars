# Milestone 30D technical report

Recovery started at `d44f116` with the approved 30C implementation in the working tree. The developer approved contact, input authority, pickup, walking and Test Lab, and rejected the freestyle appearance. Unity is pinned to 6000.6.7f1. Blender MCP answered at localhost:9876. The TMP fallback asset started clean with a recorded SHA-256. The unavailable prior local font diff was left alone.

## Six-feature disposition

| Feature | Status and result |
| --- | --- |
| Freestyle anatomy | Active in production. Matched-phase rendered crawl and sprint compared at 0/25/50/75/100 percent, with side, rear, front, overhead and underwater views of the crawl. The new pose awaits visual acceptance for the originally reported inversion. |
| Receiving | Implemented and verified for the live 8.2 m pass to an athlete facing away. The turn began before release, automatic selection happened in flight, and the receiver faced the incoming ball at catch. The initial orientation dot was −1.000 and the catch dot was 0.997. The first rendered palm landmark touched the sphere to the fixture's precision. |
| Directional aim | Implemented and verified. Alt plus keyboard direction aims in place. Drag PASS to aim and release a dry pass through the existing event system. Stationary hold retains wet delivery. Both camera arrow directions and the unassisted horizontal pass matched the requested direction to the fixture's displayed precision. Targeted throws can depart within the existing receiver-assist cone. |
| Protected rotation | Implemented and verified for in-place left, right, diagonal, forward and backward aim steps. The measured ball-to-palm error and translation were below display precision. A turn resets the dribble entry and sustaining a swim heading uses the existing leading action. Ordinary retreat and intentional sprint release retain their separate motor path. |
| Shot elevation | Implemented with power-dependent rise, leg press, existing arm mechanics, modest lateral lean, and bounded existing water effects. Focused quick/full key-up and physical release passed. Aesthetic acceptance of the rise and splash remains with the developer. |
| Ordinary fouls | Implemented and verified. In the fixture 5 attackers and 5 defenders repositioned while both clocks and the taker's mark remained steady. Defender root clearance stayed at least 2.200 m. The low palm landmark matched the ball and a single human pass resumed play. Penalty readiness retained its movement gate. |

## Freestyle diagnostic

The 30C calf-axis repair left the original thigh motion and the source toe excursion. The editable Blender Action and motion-only reimported FBX agreed on the sampled joint positions and retained a roughly 0.40 m toe excursion relative to the hip. The legs used a fixed world-space roll reference while the torso rolled. The new post-Humanoid pass places the complete thigh, knee, shin, ankle and toe chain in the pelvis frame with a tighter flutter, using the original stroke timing. The sampled toes ranged from about 0.194 m below to 0.061 m above the hip in ordinary crawl. The exact phase render compared the actual production skinned mesh. A 180° anatomical flip at the pelvis was not established in that sample. The visible defect remains open for user approval of the corrected motion. [Matched phases](Evidence/Milestone30D/Freestyle-Matched-Phases.jpg) · [Other views](Evidence/Milestone30D/Freestyle-Other-Views.jpg)

## Shooting measurements

| Action | Pelvis rise | Shoulder rise | Key-up to release | Ball at release above water | Highest brief vertex at release |
| --- | --- | --- | --- | --- | --- |
| Quick | 0.265 m | 0.268 m | 182 ms | 0.611 m | −0.206 m |
| Full | 0.647 m | 0.648 m | 270 ms | 0.990 m | +0.176 m |

Each command released once from the evaluated grip. The separation was below 0.1 mm display precision. The highest skinned vertex is a garment-height proxy. The first numerical run framed a goalkeeper, so its screenshots were replaced by a fresh run after fixing the Test Lab camera's interpolation offset. A retry also missed the synthetic Space edge, which was corrected by focusing the Game view and waiting for a genuine charge state before timing the hold. [Quick shot sequence](Evidence/Milestone30D/Quick-Shot.jpg) · [Full-power sequence](Evidence/Milestone30D/Full-Power-Shot.jpg)

## Verification limits

The focused checks use the saved production scene in Windows Editor, with deterministic lineup and isolated Input System events. Physical touch hardware, Android GPU/thermal performance, broader tactical balance and the full-duration match remain outside this run. Backlog and later work remain in `plan.md`. [Live handoff and exact next action](MILESTONE_30D_LIVE_HANDOFF.md)

## Manual check and recovery

Start with **Tools > Water Polo > Test Lab > Swimming and Floating > Underwater Freestyle**, then use **Underwater Sprint Freestyle** and **Underwater Ball Leading**. Move forward for several cycles before slowing to eggbeater. Use **Shooting and Passing > Long Pass to Receiver Facing Away** and press **B**, then use **Ball Control and Player Switching > Directional Aim and Held Rotation** with **Left Alt + direction**. **Full Power Shooting Elevation** takes a hold and release of **Space**. **Quick Shooting Elevation** takes a tap. **Referees and Match Rules > Ordinary Foul Repositioning** waits for **B/PASS**. [Complete manual guide](MILESTONE_30D_MANUAL_TEST_GUIDE.md)

The Test Lab uses the existing arena and restores Boot on stop. For recovery from an interrupted run use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. The working-tree hashes at the start of 30D are recorded in `Logs/Milestone30D/Starting-Workspace.json`. For source recovery compare the affected files with the pre-30D checkpoint rather than importing historical scene data. The full 30D file inventory is relative to the starting working tree. [Exact inventory](MILESTONE_30D_FILES.txt)
