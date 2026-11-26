# Milestone 30 — Interactive pool water

Milestone 30B retains this renderer and changes the production finish, wrist-entry priority, sculling, turn wash, impacts and analytical water response. The measurements below record the original Milestone 30 baseline. [Current water and physics handoff](WATER_PHYSICS_AND_DEFENSIVE_GAMEPLAY.md)

## Surface finish

OlympicMatchArena keeps its existing thin transparent URP surface, baked reflection, pool depth blend, dual moving normal samples and quality profiles. The Milestone 30 arena switch retunes the runtime instance of the same material. It uses a deeper blue absorption color, a shallower turquoise transition, gentler specular and reflection, finer normal detail and lower refraction and caustic intensity. The more opaque depth blend makes submerged bodies read through the water while retaining the tiled floor and markings. Low retains the original no-opaque-copy profile. Existing test and prototype profiles still use their original materials. The production arena receives no new reflection camera or full-screen pass.

## Contacts and rendering

`WaterInteractionEffects` retains the existing scene and preference bindings. The interactive mode caches the final Humanoid hands, feet and chest. At execution order 330 it reads the rig after alignment and reads the camera after live or replay framing. It samples 14 athletes and the one authoritative ball. It adds no colliders and writes no ball, swimmer or match state.

- Each hand rearms 4.7 cm above the water. Downward travel through the surface interpolates the contact on the sampled wrist segment. The crossing has its own 0.24 s cooldown. Upward exits produce smaller droplets. Near-surface underwater pulls can leave a weak disturbance within their own cooldown.
- Normal and sprint wakes use fixed world-space positions from actual planar velocity. Normal samples are up to 0.18 s apart and sprint samples up to 0.105 s apart. The split crests break up, expand and fade over at most 1.36 s. Nearby animated feet add a secondary foam and bubble disturbance. The wake stays behind when the swimmer turns.
- Stationary athletes produce spaced, low-opacity tread ripples. A leading ball creates a short bow wake. The visual transition from dribble to gather disturbs the last on-water ball position. Releases add brief droplets from the physical launch point. The original arcade flight trail is suppressed in this mode.
- `BallController.Contact` already supplies swept physical water and skip contacts with incoming velocity. Strength uses downward speed with a smaller planar contribution. This distinguishes a soft fall, a forceful strike and separate legitimate skip impacts without treating a settled ball as another collision. Near-surface keeper saves and changes in vertical chest velocity use the same bounded effect recipes.
- The new shader renders broken crests, surface wash, foam texture approximations and soft droplets through a single shared material. Surface patches and analytically moving sprites are rebuilt into one reusable dynamic mesh. Spray and bubbles use age-based motion and no particle collisions. Only the populated span of the vertex buffer is uploaded. Culling checks the active camera and distance. The shader and mesh add no per-athlete textures, materials or GameObjects.

## Budgets and replay

| Existing graphics setting | Surface quads | Spray and bubble sprites | View range |
| --- | ---: | ---: | ---: |
| Low | 32 | 38 | 44 m |
| Medium | 64 | 84 | 66 m |
| High | 96 | 144 | 66 m |

The values are arena ceilings. The absolute buffers hold up to 112 surface quads and 192 sprites. Low retains hand entries, water impacts, simple tread and reduced wakes. High uses the same gameplay authority with denser spray. Replays use the current graphics tier. `Off` is an Editor preview option for comparisons. A separate cinematic quality preference was not added.

The effect history is an independent fixed ring of 1,536 compact stamps. Live sampling records in match presentation time before view culling. For replay each camera reads the same position, age and particle seed for a given event at the replay cursor. Effects freeze on pause. A phase edge invalidates live contact sampling and presentation. Restart or replay skip clears history and the visible mesh. A scene unload disposes of the local mesh. The replay never simulates the recorded Rigidbody.

## Windows Editor measurement

`Tools/Verification/InteractiveWaterVerification.cs` measured 36 advancing frames per setting at 894 × 476 on Direct3D 12. The fixture held the arena graphics tier and camera pose constant, started the same 14 swimmers on preset routes, and requested sprint for alternating athletes. It switched only the interactive effect tier. Draw calls and triangles include all Editor Game View render passes. Small frame interval differences change the visible scene and its render counters.

| Effect tier | Draws | Rendered triangles | Main thread ms | Water sample + batch CPU ms | Reported GPU ms | Peak quads / droplets |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Off | 1292.1 | 2,545,723 | 6.305 | 0 | 0.499 | 0 / 0 |
| Low | 1296.7 | 2,552,756 | 6.404 | 0.067 | 0.505 | 32 / 38 |
| Medium | 1297.1 | 2,553,030 | 6.388 | 0.088 | 0.512 | 64 / 84 |
| High | 1296.7 | 2,553,377 | 6.463 | 0.110 | 0.509 | 96 / 144 |

The main-thread and GPU fields are Editor ProfilerRecorder and FrameTimingManager samples. They include the test harness or Editor workload and fluctuate between runs. Total managed allocations in that fixture ranged from 11.34–11.44 KB/frame including its async continuations, with no clear tier trend. The new runtime pool owns arrays allocated once. The estimated CPU storage for 1,536 40-byte event stamps and 1,216 32-byte vertices is about 98 KiB, before small sampler objects, mesh metadata, 16-bit indices and native buffer overhead. The contact material adds no textures. The older 845/1,023 draw-call Milestone 29 readings used parked athletes and broadcast cameras. They show the prior baseline workload and cannot be subtracted from this moving broad-angle sample.

The physical water fixture registered 46 left-hand and 41 right-hand entries, 352 wake events and 92 tread events over the sampling and closeup sequence. The two controlled ball drops used the original flight and water response with initial velocities supplied by the fixture. The soft impact scored 0.465 on the effect scale and the fast impact 1.000. Neither settled ball generated another impact. Pause preserved the current effect counts and Off cleared them. A separate advancing goal fixture recorded one physical drop at (−0.65, 0.00, 8.30), scored with the real ball, displayed the recorded water impact in both replay angles, and cleared it on skip. The clock and score stayed fixed during playback. Logs are under `Logs/Milestone30/`. No device FPS, thermals, native GPU timings or full-duration comparison were measured.

## Review and recovery

Use **Tools > Water Polo > Play Olympic Match Arena**, then skip the intro. **Tools > Water Polo > Arena > Water Preview** switches the effect tier in Play Mode without saving local graphics preferences. The match preference controls the production tier. In Scene view select **GameplayRealism** for the sampled waterline and recent contact markers. F6 switches the match camera. F8 cycles the existing arena inspection viewpoints. The [Milestone 30 evidence folder](Evidence/Milestone30/) contains the selected advancing Game views.

In saved Edit Mode use **Tools > Water Polo > Arena > Restore Previous Water Presentation** for the Milestone 29 surface and effect path. **Apply Interactive Water** under the same menu rebinds the new presentation. Both commands preserve the arena GUID and its tuned budget values. Use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** after direct inspection.
