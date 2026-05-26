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
