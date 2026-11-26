# Milestone 30B water physics and defensive gameplay

## Movement, water and flight

The motor accelerates and brakes its facing turn at 1,550 degrees per second squared. Large velocity-heading changes reduce speed toward 55% of the requested speed, and sprinting turns retain more momentum. Restart and opening sprint movement keep the earlier response. The existing contact sampler adds short directional wash based on changes in actual velocity. Wakes use recent motion. Stationary wash appears near a hand at the waterline instead of repeatedly drawing a swimmer-centred ring. Independent entry and exit tests use the animated wrists. Ball impacts and hand entry receive higher priority within the existing pooled budgets. Skips throw a stronger forward-shaped disturbance. The 96-patch and 144-droplet High budgets and replay event ring are unchanged.

The production SphereCollider remains 0.14 m in radius and the Rigidbody 0.45 kg. It keeps continuous collision detection. Ordinary contact with the analytical waterline already absorbed vertical flight in Milestone 30. The revised floating response adds a shallow damped dip and bob with at most 0.033 m initial amplitude, with bounded planar drift. There is no new physical water plane or competing buoyancy component. A genuine skip requires an intentional skip shot, planar speed above 6.5 m/s, sufficient incoming vertical speed and a fall-to-travel ratio below 0.63. Steeper entries lose more forward energy. At most two qualifying skips are allowed by default. The second requires at least 1.15 m/s of downward speed and can fail the same momentum and angle gates. The authoritative and prediction paths use the same response. Q + Space and the mobile SKIP setting use the existing shot path.

## Defense and saves

A team can nominate one field defender for a credible opposing shot. The defender must be facing play and within 1.0–3.2 m of the shooter and 0.67 m of its lane. The reaction delay is 0.085 s, followed by 0.28 s of commitment and 0.30 s of mobility recovery. The visual raises one arm while the other remains available for sculling. This does not add a new physical interception volume or a foul type. The current World Aquatics regulations distinguish the goalkeeper's two-hand privilege within their six-metre area, field touch, and free-throw interference. The game uses single-arm field reactions and limits them to ordinary open play. [World Aquatics Competition Regulations, Part Six](https://www.worldaquatics.com/rules/competition-regulations)

In both Press and Zone, a centre back is reserved for the eligible opposing centre forward when possible. Open-play positioning shades the ball side of the centre-forward-to-goal line by a bounded amount and uses the current defended goal. Existing assignment and spacing logic still handles the rest of the field. Commitment reduces AI mobility temporarily and returns to the existing decisions as it fades.

The keeper tracks with a bent-arm ready pose. The anticipated contact point chooses the side and height of the reach and raises the torso by up to 0.22 m. An allowed high central save can present a two-arm block. The accepted swept contact supplies the recovery aim point for catches and deflections. Save reach, outcome probabilities and official phase ownership remain in the existing controllers. The water sampler observes the actual rise and recovery and the existing save event. Recorded replay copies receive the evaluated transforms and existing bounded water events.

## Motion corrections and budgets

Blender checks against the shipped crawl Action showed the toes pointing rearward in the source. The authored foot orientation used a fixed world-up frame. Unity's prone correction now uses the real shin and toe axes and caps excess roll during the fully prone portion. Tread and eggbeater stay outside the correction. Retargeted walking keeps the sampled stride, existing routes and planted-foot solve. The shared deck correction limits excess upward toe pitch and chooses the natural knee plane when the sampled plant has inverted. No source garment or aquatic Action was re-exported.

The Windows D3D12 fixture held the camera and arena tier fixed, sampled 36 advancing frames per effect tier at 894 × 476, and used the same fourteen moving swimmers. Values include the Editor and the fixture. Scene movement and render-pass timing vary between samples.

| Effect tier | Draws | Rendered triangles | Main thread ms | Water sample and batch ms | Reported GPU ms | Peak patches / droplets |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Off | 1291.9 | 2,648,960 | 7.607 | 0.000 | 0.510 | 0 / 0 |
| Low | 1296.4 | 2,656,441 | 7.757 | 0.074 | 0.519 | 32 / 38 |
| Medium | 1296.5 | 2,657,642 | 7.768 | 0.085 | 0.526 | 64 / 84 |
| High | 1296.6 | 2,657,977 | 8.336 | 0.115 | 0.526 | 96 / 144 |

Milestone 30's recorded High was 1296.7 draws, 2.553M triangles, 6.463 ms main thread, 0.110 ms water work and 0.509 ms reported GPU. This is a cross-session Editor comparison. It reflects the measured scene and Editor conditions, including changed geometry onscreen, and does not isolate animation costs or represent phone FPS. The new shared White 256 px albedo is approximately 39 KiB with ASTC 6×6 and mips. Pool memory and draw architecture are unchanged. [Original benchmark](INTERACTIVE_WATER_GRAPHICS_AND_EFFECTS.md)

The fixture captured 45 left entries, 43 right entries, 391 wake events and 19 localized treading events. Soft and fast ball impacts were 0.557 and 1.000 on the effect scale. The open-play keyboard fixture measured one ordinary water contact and a shallow skip changing from 20.59 to 18.42 m/s, with 1.31 m/s upward velocity. The previously recorded skip and water enter and both replay angles were also rerun. [Hand entries and wake](Evidence/Milestone30B/Hand-Entries-and-Wakes.jpg) · [Underwater flutter](Evidence/Milestone30B/Underwater-Flutter.jpg) · [Recorded water in both replay angles](Evidence/Milestone30B/Replay-Both-Angles.jpg)

Staged field-block and directional keeper results are reported in the current [project handoff](../plan.md). Phone GPU, FPS and thermals remain open.
