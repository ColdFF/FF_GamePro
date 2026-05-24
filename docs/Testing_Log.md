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