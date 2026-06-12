# Testing Log

Created: 2026-05-20

## Purpose

This document records testing for the `ShadowPath` coursework project.

Testing notes are used to show how gameplay features are checked, how bugs are found, and how improvements are made during development.

## Testing Format

Each test entry should include:

- Date
- Feature tested
- Expected result
- Actual result
- Status

## Status Key

| Status | Meaning |
| --- | --- |
| Pass | The feature worked as expected. |
| Partial | The feature worked partly, but needs improvement. |
| Fail | The feature did not work as expected. |
| Fixed | A previous issue has been corrected and retested. |

## Test Entries

### 2026-05-20 - Player Movement Test

| Field | Notes |
| --- | --- |
| Feature tested | Basic player movement |
| Expected result | The player should move left and right using the assigned movement keys. |
| Actual result | The player moves left and right correctly in the current prototype scene. |
| Status | Pass |

### 2026-05-20 - Player Jump Test

| Field | Notes |
| --- | --- |
| Feature tested | Player jumping and ground detection |
| Expected result | The player should jump while grounded and should not jump repeatedly in mid-air. |
| Actual result | The player jumps correctly from the ground in the current prototype scene. |
| Status | Pass |

### 2026-05-20 - Shadow Light Control Test

| Field | Notes |
| --- | --- |
| Feature tested | Directional light control and shadow movement |
| Expected result | The arrow keys should adjust the light direction and visibly change the projected shadow position or shape. |
| Actual result | Directional light control works in the current shadow test setup, and the projected shadow changes visibly. |
| Status | Pass |

### 2026-05-22 - Tutorial Scene Project Check

| Field | Notes |
| --- | --- |
| Feature tested | Initial `Level01_Tutorial` scene structure |
| Expected result | The prototype scene should contain a start platform, shadow-casting blocks, a shadow screen, player movement, and an end platform layout for the tutorial route. |
| Actual result | The current scene contains the first tutorial level structure and can be opened from the imported Unity project in the repository. |
| Status | Pass |

### 2026-05-22 - Projected Shadow Silhouette Collision Test

| Field | Notes |
| --- | --- |
| Feature tested | Generated shadow silhouette collision |
| Expected result | The player should be able to land on the projected black shadow silhouette used as the playable route. |
| Actual result | The player can stand on the current projected shadow silhouette colliders in the tutorial prototype. |
| Status | Pass |

### 2026-05-22 - Moving Shadow Passenger Behaviour Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player behaviour while the projected shadow changes with light adjustment |
| Expected result | When the player stands on a shadow path and the light direction changes, the player should follow the changing support edge without being pushed unnaturally toward the shadow edge. |
| Actual result | An earlier version caused visible drift on later shadow silhouettes. The projected shadow carry logic was refined to follow the relevant upper shadow support edge more accurately, and the player now follows the moving shadow path more naturally in the current test. |
| Status | Fixed |

### 2026-05-23 - Tutorial Boundary Test

| Field | Notes |
| --- | --- |
| Feature tested | Left and right movement boundaries in the tutorial level |
| Expected result | The player should not fall out of the level by continuously running left from the start platform or right past the end platform. |
| Actual result | Boundary colliders stop the player from leaving the tutorial route at the outer start and end sides. |
| Status | Pass |

### 2026-05-23 - Stage Wash Lighting Presentation Test

| Field | Notes |
| --- | --- |
| Feature tested | Tutorial level stage wash lighting and spotlight cookie |
| Expected result | The shadow screen should show a controlled theatrical wash effect that supports the light-and-shadow atmosphere in Play Mode. |
| Actual result | The stage wash light and soft cookie texture render on the shadow screen and improve the tutorial level lighting presentation. |
| Status | Pass |

### 2026-05-23 - Light Input Meaning Retest

| Field | Notes |
| --- | --- |
| Feature tested | Arrow-key light adjustment meaning |
| Expected result | Arrow-key input should feel like adjusting the light direction, with the projected shadow path responding to that light change. |
| Actual result | The light-control mapping was updated so the shadow response now better matches the intended player interaction of moving light to shape the path. |
| Status | Pass |

### 2026-05-25 - Projected Shadow Edge Generation Test

| Field | Notes |
| --- | --- |
| Feature tested | Generated projected shadow edge platforms |
| Expected result | The system should generate visible edge strips from the projected shadow outline so the shadow shape can be tested as a traversal route. |
| Actual result | Green debug edge strips are generated from the projected shadow outline and update when the light direction changes. The edge-based approach gives clearer control over which parts of the shadow can support the player. |
| Status | Pass |

### 2026-05-25 - Shadow Edge Alignment Retest

| Field | Notes |
| --- | --- |
| Feature tested | Visual alignment between generated green edge strips and the projected shadow shape |
| Expected result | Generated edge visuals should follow the projected shadow outline closely enough for debugging and level design. |
| Actual result | Earlier generated points and edges did not fully match the visible shadow outline. After tuning the edge generation and screen-space conversion, the green edge strips now follow the moving shadow outline more consistently. |
| Status | Fixed |

### 2026-05-25 - Walkable and Steep Shadow Edge Behaviour Test

| Field | Notes |
| --- | --- |
| Feature tested | Walkable shadow edges and steep sliding shadow edges |
| Expected result | Suitable upward-facing shadow edges should behave as stable platforms, while steep or near-vertical edges should not allow the player to climb or stick. |
| Actual result | Walkable shadow edges support standing, walking, running, and jumping. Steep or near-vertical shadow edges use a no-friction physics material so the player slides down instead of sticking to the edge. |
| Status | Pass |

### 2026-05-25 - Player Jump Landing Retest

| Field | Notes |
| --- | --- |
| Feature tested | Jump landing and grounded detection on normal platforms and shadow edge platforms |
| Expected result | The player should land responsively after jumping and should not appear to hover slowly before returning to the grounded state. |
| Actual result | The player ground check was tuned so landing feels more responsive. The earlier slow landing behaviour was reduced by requiring the player feet to be close enough to the detected ground before setting the grounded state. |
| Status | Fixed |

### 2026-05-25 - Wall and Steep Edge Sticking Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player collision behaviour against vertical walls and steep shadow edges |
| Expected result | The player should slide or fall away from steep surfaces instead of sticking, climbing, or being held in place by horizontal input. |
| Actual result | Wall and steep-edge sticking were reduced through no-friction material use and movement safeguards. The player no longer remains attached to boundary walls during jump and horizontal input tests. |
| Status | Fixed |

### 2026-05-25 - Moving Shadow Edge Carry Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player behaviour while standing on a shadow edge as the light direction changes |
| Expected result | When the player is grounded on a valid shadow edge, the player should follow the moving support edge without large snapping or mismatching to another shadow edge. |
| Actual result | The shadow edge carry logic was refined so the player is carried only when grounded on a matching walkable generated edge. The follow behaviour is more stable during light adjustment, although some level-specific parameters may still require tuning when shadows move quickly. |
| Status | Fixed |

### 2026-05-25 - End Door Sliding Interaction Test

| Field | Notes |
| --- | --- |
| Feature tested | Sliding end-door trigger and movement in `Level01_Tutorial` |
| Expected result | When the player reaches the end door, the door should open smoothly like a sliding door. Only the intended moving door pieces should slide, while the upper rail remains fixed. |
| Actual result | The player can trigger the end door by approaching it. The door panel, wheels, and vertical hanger pieces slide together, while the rail remains fixed, creating the intended sliding-door effect. |
| Status | Pass |

### 2026-05-25 - Dark Doorway Reveal Test

| Field | Notes |
| --- | --- |
| Feature tested | Black doorway reveal behind the sliding door |
| Expected result | The dark doorway should already exist behind the closed door and should be revealed naturally as the door opens, instead of appearing only after the opening movement finishes. |
| Actual result | A black doorway visual is placed behind the door and is revealed during the sliding motion. The setup avoids covering the visible door before the player triggers the opening sequence. |
| Status | Pass |

### 2026-05-25 - Player Entry Sequence Test

| Field | Notes |
| --- | --- |
| Feature tested | Player input lock, automatic entry movement, landing wait, and doorway occlusion |
| Expected result | After the player triggers the door, player input should be locked. The player should wait while the door opens, then walk horizontally into the doorway. If the trigger is hit while jumping, the player should land before entering. As the player enters, the dark doorway should gradually hide the character until the character fully disappears. |
| Actual result | Player input is locked after triggering the door. The player waits in place while the door opens, then automatically walks into the doorway after landing if necessary. The doorway occluder hides the player gradually from the right side during entry, producing the intended disappearance effect. |
| Status | Pass |

### 2026-05-25 - Door Close and Camera Return Test

| Field | Notes |
| --- | --- |
| Feature tested | Post-entry door close and tutorial camera return |
| Expected result | After the player disappears into the doorway, the door should close back to its original position and the camera should return from the end-door focus view to the initial overview framing. |
| Actual result | After the player has fully entered the dark doorway, the sliding door closes and the tutorial camera returns to the wide overview framing. The camera no longer snaps back to the end-door focus after returning. |
| Status | Pass |

### 2026-05-25 - Level-End Blackout and Success Menu Test

| Field | Notes |
| --- | --- |
| Feature tested | End-of-level light fade, full-screen blackout, and completion menu |
| Expected result | After the camera returns to the overview view, the scene lights should fade out, a black screen should fade in, and a level-complete menu should appear with Restart, Next Level, and Main Menu options. |
| Actual result | The end sequence fades down the relevant scene lights, fades in a full-screen black overlay, and displays a `LEVEL COMPLETE` menu. Restart, Next Level, and Main Menu buttons are visible, with Next Level and Main Menu left as future scene hooks. |
| Status | Pass |

### 2026-05-25 - Completion Menu Restart and Hover Retest

| Field | Notes |
| --- | --- |
| Feature tested | Level-complete menu interaction |
| Expected result | The Restart button should reload the current scene. Buttons should provide clear visual feedback when the mouse hovers over them. |
| Actual result | The Restart button reloads the current level in Play Mode. Menu buttons now show a clearer selected/hover state when the cursor is placed over them, making the completion menu easier to read and interact with. |
| Status | Pass |

### 2026-05-25 - Opening Pulse Input Jitter Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player stability when input unlocks after the tutorial opening light pulse |
| Expected result | The player should remain visually stable when the tutorial opening sequence unlocks player input. |
| Actual result | A small player jitter occurred when the opening wash-light pulse finished and input was unlocked. The player controller now refreshes grounded and animation state before input unlock, which removes the visible shake without changing the existing opening flow. |
| Status | Fixed |

### 2026-05-26 - Gameplay Audio Integration Test

| Field | Notes |
| --- | --- |
| Feature tested | Gameplay sound effects for door movement, light changes, player jump, footsteps, falling, and death landing |
| Expected result | Each gameplay event should play the correct sound at the correct time without disrupting the existing movement, door, light, completion, or failure systems. |
| Actual result | Audio was added and tested in `Level01_Tutorial`. The sliding door plays open and close sounds, the opening light pulse plays a light-on cue, the final blackout plays a light-off cue, player jumps play a jump cue, normal platform movement plays a regular footstep cue, falling plays a whoosh cue, and the death visual landing plays a separate soft landing cue. |
| Status | Pass |

### 2026-05-26 - Shadow Footstep Audio Retest

| Field | Notes |
| --- | --- |
| Feature tested | Different footstep audio on normal platforms and generated shadow platforms |
| Expected result | The player should use the normal footstep sound on the start and end platforms, but use a quieter shadow-footstep sound while walking or running on shadow-generated traversal edges. |
| Actual result | The player controller now selects the normal `Walk.wav` cue on regular platform ground and the quieter `ShadowWalk.wav` cue on generated shadow platform ground. The shadow sound feels lighter and better matches the shadow-path fantasy while preserving the clearer normal footstep on physical platforms. |
| Status | Pass |

### 2026-05-26 - Opening Light Pulse Audio and Dim Start Test

| Field | Notes |
| --- | --- |
| Feature tested | Opening tutorial light presentation and light-on sound |
| Expected result | The level should begin with a dimmer focused view, then play the light-on cue as the wash-light pulse appears and returns the scene to normal lighting. This should not reintroduce the earlier player jitter. |
| Actual result | The opening flow now starts with the wash light hidden or dimmed, plays the light-on cue when the pulse begins, and restores the normal tutorial lighting after the pulse. The player remains visually stable when input unlocks. |
| Status | Pass |

### 2026-05-26 - Failure Audio Stop and Landing Retest

| Field | Notes |
| --- | --- |
| Feature tested | Falling sound handoff into death landing sound |
| Expected result | The falling whoosh should play while the player is falling after failure, then stop when the death visual lands so the landing cue is clear and not covered by the continued falling sound. |
| Actual result | The falling audio now stops when the death landing event plays. The softer generated death landing cue is heard clearly when the death visual lands on the start platform. |
| Status | Fixed |

### 2026-05-26 - Failure Camera Return Retest

| Field | Notes |
| --- | --- |
| Feature tested | Camera behaviour after the player falls from the shadow path |
| Expected result | After failure, the camera should return smoothly to the original start-area focus before the death visual drops onto the platform, so the player can see the failure feedback and restart menu. |
| Actual result | A regression caused the camera to briefly jump toward the start focus and then return to the wrong area, hiding the death visual. The failure flow was corrected so the camera locks into the start-area failure focus and moves there more naturally before the death animation appears. |
| Status | Fixed |

### 2026-05-27 - Level 02 Two-Path Layout Planning Test

| Field | Notes |
| --- | --- |
| Feature tested | `Level02_HiddenDoor` route concept and hidden door discovery structure |
| Expected result | The level should support a two-path structure: one path lets the player discover the hidden end door position, while the other path becomes the real route used to reach that door. |
| Actual result | The Level 02 layout direction was checked in the editor. The upper-left route can act as a peek route for discovering the end door, while the right-side area has enough space for a more puzzle-focused route toward the door. |
| Status | Pass |

### 2026-05-27 - Door Direction and Entry Setup Check

| Field | Notes |
| --- | --- |
| Feature tested | Reusing the copied Level 01 end door setup in the Level 02 scene |
| Expected result | The end door should face the correct direction for the new hidden-door layout, and the player entry target should still make sense after the door is repositioned. |
| Actual result | The door group, visual orientation, entry target, and doorway relationship were reviewed and adjusted for the Level 02 layout. This made the copied Level 01 door setup better match the new approach direction. |
| Status | Pass |

### 2026-05-29 - Ladder Shadow Alignment Test

| Field | Notes |
| --- | --- |
| Feature tested | Shadow ladder visual alignment from cube shadow casters |
| Expected result | When the light is adjusted to the intended angle, the scattered cube casters should visually form a readable ladder shadow that starts near the lower platform and leads toward the upper route. |
| Actual result | The ladder caster cubes required many placement and rotation adjustments. After iteration, the shadow forms a more readable ladder shape, with rails and rungs positioned to guide the player upward from the lower platform toward the next group of shadow platforms. |
| Status | Pass |

### 2026-05-29 - Ladder Route Connection Test

| Field | Notes |
| --- | --- |
| Feature tested | Connection between the lower platform, ladder shadow, and upper cube-shadow route |
| Expected result | The player should be able to understand where the ladder begins and where it should lead after the shadow ladder is formed by the light angle. |
| Actual result | The ladder area was tested as part of the Level 02 route. The bottom of the ladder is positioned near the lower platform, and the top area connects toward the upper cube-shadow path. Further tuning is still possible, but the route now has a clearer intended direction. |
| Status | Pass |

### 2026-05-30 - Ladder Auto-Grab and Climb Animation Test

| Field | Notes |
| --- | --- |
| Feature tested | Automatic ladder grab, climb state, and stickman climb animation |
| Expected result | When the player jumps into the ladder trigger, the player should snap to the climb path, enter the climb animation, and climb upward only when W or the up key is held. The animation should pause when climb input is released. |
| Actual result | The player can jump into the climb zone and automatically enter the ladder state. The `Stickman_Climb` animation plays while climbing input is held and pauses on the current frame when no climb input is pressed, giving the ladder movement a more controlled feel. |
| Status | Pass |

### 2026-05-30 - Ladder Exit Platform Carry Test

| Field | Notes |
| --- | --- |
| Feature tested | Exiting from the top of the ladder onto the next shadow platform |
| Expected result | After reaching the top of the ladder, the player should move to the configured ExitPoint, regain normal control, and continue being carried correctly by the moving shadow platform if the light changes. |
| Actual result | The player exits to the configured ExitPoint and can continue moving normally. The player also follows the connected shadow platform after exiting, confirming that the ladder script restores the normal player and platform interaction state. |
| Status | Pass |

### 2026-05-30 - Multiple Ladder Zone Wrong-Exit Retest

| Field | Notes |
| --- | --- |
| Feature tested | Multiple ladder climb zones in the same Level 02 scene |
| Expected result | Climbing one ladder should only use that ladder's BottomPoint, TopPoint, and ExitPoint. The player should not be teleported to another ladder's exit. |
| Actual result | A copied second ladder initially caused the player to exit at the wrong ladder because its trigger zone overlapped the first ladder area. The second climb trigger was separated and its collider placement corrected, so each ladder now uses its own climb path and exit point. |
| Status | Fixed |

### 2026-05-30 - Ladder Descend and Bottom Detach Retest

| Field | Notes |
| --- | --- |
| Feature tested | Climbing down a ladder and detaching near the BottomPoint |
| Expected result | Pressing S or down should play the climb animation while moving downward. Near the BottomPoint, the player should detach from the ladder and fall naturally, landing on any valid shadow or cube platform below if one is present. |
| Actual result | Downward climbing now uses the climb animation instead of a static sprite slide. The bottom detach behaviour was tuned so the player can leave the ladder near the BottomPoint and return to normal gravity, allowing the level geometry or shadow platforms below to catch the player naturally. |
| Status | Fixed |

### 2026-06-01 - Level 03 Route Theme Planning Check

| Field | Notes |
| --- | --- |
| Feature tested | `Level03_RopeTower` core level direction |
| Expected result | The third level should introduce a clearer new theme than Level 01 and Level 02, while still building on shadow-platform traversal. |
| Actual result | The Level 03 direction was planned around moving cube shadows and rope-shadow swinging. The design gives the level a more dynamic vertical route and creates space for repeated moving-platform timing and rope-swing transitions. |
| Status | Pass |

### 2026-06-02 - Level 03 Moving Block Setup Test

| Field | Notes |
| --- | --- |
| Feature tested | Reusable moving cube setup in `Level03_RopeTower` |
| Expected result | A cube with the moving-block script should move between configured points without requiring a separate animation clip for each platform. |
| Actual result | `MovingBlock` was added so Level 03 moving shadow casters can be configured through Inspector values. This supports repeated moving platform setups without hand-building a new animation for every cube. |
| Status | Pass |

### 2026-06-02 - Moving Shadow Platform Passenger Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player stability while standing on moving projected shadow platforms |
| Expected result | When a moving cube casts a usable shadow platform, the player should be carried by the generated shadow collider instead of falling behind or sliding away from the platform movement. |
| Actual result | The projected shadow edge platform system was adjusted so generated edge colliders are grouped under a separate runtime root. The player can stand on moving shadow platforms and follow their movement more reliably, including faster moving casters after parameter tuning. |
| Status | Pass |

### 2026-06-03 - Rope Asset and Shadow Projection Setup Check

| Field | Notes |
| --- | --- |
| Feature tested | First rope setup in `Level03_RopeTower` |
| Expected result | The imported rope should provide a usable visual rope, and its projected shadow should line up well enough to become the actual gameplay interaction target. |
| Actual result | The required rope scripts, rope material, and rope texture files were imported from the Optimized Ropes and Cables Tool. The first rope anchor and rope end setup were placed in the scene, and the projected rope shadow was used as the basis for the first swing zone. |
| Status | Pass |

### 2026-06-03 - Rope Shadow Auto-Grab Test

| Field | Notes |
| --- | --- |
| Feature tested | Automatic rope-shadow grabbing in `Level03_RopeTower` |
| Expected result | When the airborne player jumps into the rope-shadow grab zone, the player should attach automatically without needing to press W or Space at the exact moment of contact. |
| Actual result | `ShadowRopeSwingZone` supports automatic grabbing when the player enters the projected rope-shadow zone. The player hand point attaches to the projected rope end so the grab reads more naturally than attaching from the character centre. |
| Status | Pass |

### 2026-06-04 - Rope Swing Motion and Facing Retest

| Field | Notes |
| --- | --- |
| Feature tested | Rope swing movement, player pose direction, and swing limits |
| Expected result | A/D should act like swing acceleration, the rope and rope shadow should swing with the player, the player should face the swing direction, and the swing should not allow full loops or unnatural over-rotation. |
| Actual result | The rope swing system now drives the rope end during swinging, bends the visual rope/shadow path, applies momentum-based input, limits extreme swing angles, softens boundary behaviour, and stabilises facing so the character does not flicker rapidly near the resting position. |
| Status | Pass |

### 2026-06-04 - Rope Release Momentum Test

| Field | Notes |
| --- | --- |
| Feature tested | Releasing from the rope swing |
| Expected result | Releasing the rope should launch the player using the rope's current swing momentum, even if the player only presses Space and is not holding A or D at the same time. |
| Actual result | `PlayerController` now exposes external launch support so `ShadowRopeSwingZone` can apply release velocity and briefly preserve that momentum before normal air control resumes. The release speed can be tuned manually, and releasing while the rope is already moving produces a clearer throw-off feeling. |
| Status | Pass |

### 2026-06-04 - Level 03 Commit Scope Check

| Field | Notes |
| --- | --- |
| Feature tested | Git commit scope for the Level 03 rope-swing prototype |
| Expected result | The Level 03 gameplay commit should include the new scene, rope asset files needed by the prototype, new Level 03 scripts, and shared script changes required by the rope release and moving shadow systems, without committing unrelated Level 01 or Level 02 scene edits. |
| Actual result | The committed feature branch included `Level03_RopeTower`, `MovingBlock`, `ShadowRopeSwingZone`, the required GogoGaga rope scripts/materials/textures, and the shared `PlayerController` and `ProjectedShadowAllEdgePlatform` updates. Existing Level 01 and Level 02 scene files were kept out of the feature commit. |
| Status | Pass |

### 2026-06-05 - Rope Auto-Grab and Space Release Retest

| Field | Notes |
| --- | --- |
| Feature tested | Rope grab and release input in `Level03_RopeTower` |
| Expected result | The player should automatically grab a valid rope shadow when entering the grab zone, while Space should only release the rope. Pressing Space near a quiet or resting rope should not immediately detach and reattach the player in the same moment. |
| Actual result | `ShadowRopeSwingZone` now keeps rope grabbing automatic and reserves Space for release. A short release/re-grab guard prevents the player from being pulled straight back onto the same rope immediately after letting go. |
| Status | Fixed |

### 2026-06-05 - Slow Rope Release Momentum Retest

| Field | Notes |
| --- | --- |
| Feature tested | Rope release momentum from slow or idle swings |
| Expected result | Releasing from a rope with little swing speed should not create an obvious artificial speed boost or a backward pop. The launch should mainly reflect the rope's actual motion and the configured release tuning. |
| Actual result | Rope release handling was adjusted so idle and slow releases use more controlled momentum instead of always applying a strong launch. This makes letting go near the rope's resting position feel less like the player is being pushed backward. |
| Status | Fixed |

### 2026-06-05 - Repeated Rope Use Stability Retest

| Field | Notes |
| --- | --- |
| Feature tested | Reusing the same rope multiple times |
| Expected result | After repeated grabs, swings, and releases, the rope should return to a natural resting direction and the player's later A/D swing control should still feel consistent. |
| Actual result | The rope interaction was stabilised so repeated use no longer leaves the rope biased toward the player's last release direction. Later swings remain more natural because the player attachment, release state, and rope rest behaviour are reset more cleanly between uses. |
| Status | Fixed |

### 2026-06-05 - Moving Rope Shape Retest

| Field | Notes |
| --- | --- |
| Feature tested | Rope visual behaviour when the rope setup is parented to a moving block |
| Expected result | A rope attached to a moving object should move with the object while keeping its intended hanging shape. The rope should not appear bent only because the parent object moved. |
| Actual result | The rope script now includes shape-preservation support for parent movement, so moving rope groups can be used in Level 03 without the rope visually warping as the parent object travels. |
| Status | Fixed |

### 2026-06-05 - MovingCaster_07 Shadow Platform Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player running and jumping on the `MovingCaster_07` generated shadow platform |
| Expected result | The player should be able to run and jump naturally on the generated shadow platform, without feeling blocked by incorrect edge matching or hidden collision restrictions. |
| Actual result | Generated shadow edge handling was refined for the problematic moving caster setup. The player can use the intended upper shadow support more reliably, reducing the earlier feeling of being held back or forced into weak jumps on that section. |
| Status | Fixed |

### 2026-06-05 - Level 03 Completion Overview Camera Check

| Field | Notes |
| --- | --- |
| Feature tested | End-of-level camera overview for `Level03_RopeTower` |
| Expected result | After completing Level 03, the camera should return to a wide overview that shows the intended full level route without exposing unnecessary outside space. |
| Actual result | `TutorialCameraController` now supports a separate end overview shot, and the Level 03 scene stores a calculated end overview position and orthographic size for the completion sequence. |
| Status | Pass |

### 2026-06-05 - Level 03 Rope Polish Commit Scope Check

| Field | Notes |
| --- | --- |
| Feature tested | Git commit scope for the Level 03 rope path polish update |
| Expected result | The polish commit should include only the intended Level 03 rope, moving shadow platform, shared player/controller, and completion camera changes, without adding unrelated `ProjectSettings`, `Packages`, Level 01 scene, Level 02 scene, terrain, or rope example files. |
| Actual result | The final staged gameplay commit contained only six intended files: `Rope.cs`, `Level03_RopeTower.unity`, `PlayerController.cs`, `ProjectedShadowAllEdgePlatform.cs`, `ShadowRopeSwingZone.cs`, and `TutorialCameraController.cs`. Whitespace checks passed before committing, and the working tree was clean afterwards. |
| Status | Pass |

### 2026-06-06 - Level 04 Dual-Light Design Check

| Field | Notes |
| --- | --- |
| Feature tested | `Level04_DualLight` route concept and two-light phase structure |
| Expected result | The level should support two active light phases where switching the light changes the available shadow route without making the player lose track of the intended path. |
| Actual result | The Level 04 plan was checked around Phase A, Phase B, and shared shadow objects. This gave the level a clear structure for alternating between two lights while still keeping the route readable through repeated shadow-platform, ladder, and rope interactions. |
| Status | Pass |

### 2026-06-07 - Dual-Light Switching Test

| Field | Notes |
| --- | --- |
| Feature tested | Switching between Phase A and Phase B lights in `Level04_DualLight` |
| Expected result | Pressing F should switch the active gameplay light, enable the correct light and screen wash setup, keep the visible lamp rigs understandable, and apply the active light to the relevant shadow systems. |
| Actual result | `Level04DualLightController` switches between the two light phases, keeps independent light-angle values, applies phase-specific angle limits, and refreshes scene shadow users so the active light drives the current route. |
| Status | Pass |

### 2026-06-07 - Phase-Bound Shadow Object Test

| Field | Notes |
| --- | --- |
| Feature tested | Phase-specific shadow platforms, ladders, and rope-shadow zones |
| Expected result | Objects marked for Phase A should only be usable in Phase A, objects marked for Phase B should only be usable in Phase B, and shared objects should remain usable in both phases. Disabled phase objects should not leave active colliders behind. |
| Actual result | `Level04LightPhaseBinding` was added and connected to Level 04 shadow users. The controller enables, disables, or rebuilds shadow platforms, ladder zones, and rope swing zones according to the active phase, preventing inactive phase objects from acting as hidden gameplay supports. |
| Status | Pass |

### 2026-06-07 - Rope Drop on Light Switch Retest

| Field | Notes |
| --- | --- |
| Feature tested | Player state when switching lights while attached to a rope shadow |
| Expected result | If the active light changes while the player is attached to a rope, the player should be released cleanly instead of remaining attached to a rope shadow that has changed phase or position. |
| Actual result | `ShadowRopeSwingZone` now exposes `ForceDropFromRope`, and the Level 04 controller calls it before switching lights. This prevents the player from staying attached to an invalid rope state during a phase change. |
| Status | Fixed |

### 2026-06-08 - Level 04 Layout and Camera Framing Check

| Field | Notes |
| --- | --- |
| Feature tested | Final Level 04 layout, start camera view, and completion overview camera view |
| Expected result | The start view should frame the intended opening area and lower camera props, while the completion view should show the full Level 04 scene including the door, start point, end point, and lower camera props without revealing unnecessary space below their supports. |
| Actual result | The Level 04 camera framing was calculated and stored in the scene. The start camera covers the intended opening view, and the end overview camera frames the full level area more cleanly for the completion sequence. |
| Status | Pass |

### 2026-06-09 - Main Menu and Level Flow Planning Check

| Field | Notes |
| --- | --- |
| Feature tested | Planned main menu and connected level-flow structure |
| Expected result | The planned structure should support starting from a main menu, entering the first level, continuing through the current playable levels, and returning to the menu without requiring manual scene loading in the editor. |
| Actual result | The flow was reviewed before implementation. The scene order, completion-menu next-level behaviour, instruction popup approach, and pause UI reuse were identified as the key systems needed to turn the individual levels into a connected game sequence. |
| Status | Pass |

### 2026-06-10 - Menu, Instruction, Pause, and Level Flow Integration Test

| Field | Notes |
| --- | --- |
| Feature tested | Main menu scene, Build Settings scene order, instruction popups, pause menus, and next-level transitions across the playable levels |
| Expected result | The game should be able to start from `MainMenu`, load the playable levels in order, show the appropriate instruction UI, allow pausing during gameplay, and move to the configured next level from the completion menu. |
| Actual result | The new menu and level-flow update was checked through the staged Unity project files. `MainMenu` and the four playable scenes are now listed in Build Settings, the later levels have pause and instruction UI support, Level 03 includes the rope-swing instruction page, and level completion can use the chapter transition flow to continue to the next scene. |
| Status | Pass |

### 2026-06-12 - Main Menu Instructions Popup Test

| Field | Notes |
| --- | --- |
| Feature tested | Main menu `Instructions` button and two-page `HOW TO PLAY` popup |
| Expected result | Selecting `Instructions` from the main menu should open a readable popup. The first page should explain the game goal and core mechanic, the second page should list the controls, and `Next`, `Back`, and `Close` should navigate without breaking the existing menu buttons. |
| Actual result | The instructions popup opens from the main menu, splits the content into a less crowded two-page layout, keeps controls aligned in a readable column, and allows the player to move between pages or close the popup while returning to the main menu state. |
| Status | Pass |

### 2026-06-12 - Main Menu Button State and UI Audio Retest

| Field | Notes |
| --- | --- |
| Feature tested | Main menu, instruction popup, and menu button hover/click feedback |
| Expected result | Buttons should show hover and click feedback only while the pointer is interacting with them, then return to their normal visual state. Hover should play a softer UI sound, and clicking should play a cleaner confirmation sound without layered or confusing audio. |
| Actual result | Button selected-state persistence was corrected so buttons no longer remain visually pressed or highlighted after the pointer moves away or the click completes. `MenuHoverSoft.wav` is used for soft hover feedback, and `MenuClickConfirm.wav` is used for the click-confirm cue. |
| Status | Fixed |

### 2026-06-12 - Level Complete Menu Button Polish Test

| Field | Notes |
| --- | --- |
| Feature tested | Level-complete menu button styling, hover feedback, click feedback, and audio |
| Expected result | The `LEVEL COMPLETE` menu should keep a simple black/white style consistent with the rest of the project UI. Its buttons should respond to hover and click actions with matching visual and audio feedback. |
| Actual result | The level-complete menu buttons were restyled toward the original black/white UI language, and the runtime button feedback now uses the same soft hover and click-confirm audio approach as the main menu. Button visual states reset after interaction. |
| Status | Pass |

### 2026-06-12 - Chapter Transition Hidden Button Audio Retest

| Field | Notes |
| --- | --- |
| Feature tested | Hidden UI interaction during subtitle-style chapter transitions |
| Expected result | During transitions from the main menu into Level 01 and from one level to the next, moving the mouse over the screen should not trigger hover sounds from buttons that are no longer visible or should no longer be interactive. |
| Actual result | The transition overlay now blocks raycasts, selected UI state is cleared when the transition starts, and menu hover feedback is suppressed while the chapter transition is playing. Moving the cursor during the subtitle transition no longer triggers hidden button hover sounds. |
| Status | Fixed |

