# Daily Scrum Log

Created: 2026-05-20

## Purpose

This document records short development reflections for the `ShadowPath` coursework project.

Although the project is developed individually, these notes are used to show an agile-style working process: reviewing completed work, planning the next task, and identifying any blockers or risks.

## Entry Format

Each entry should briefly answer:

- What was completed?
- What will be worked on next?
- Are there any blockers, risks, or problems?

## Entries

### 2026-05-20 - Repository Documentation Setup

| Question | Notes |
| --- | --- |
| What was completed? | Added initial repository documentation, including the README, game concept, development workflow, asset credits, and testing log. |
| What will be worked on next? | Set up the GitHub Project Kanban board and create initial Issues for the main coursework tasks. |
| Blockers, risks, or problems | The Unity game project has not been added to the repository yet. The repository structure and workflow should be prepared before regular game project uploads begin. |

### 2026-05-22 - Initial ShadowPath Prototype Import

| Question | Notes |
| --- | --- |
| What was completed? | Built and refined the first `ShadowPath` tutorial prototype structure, including the start platform, shadow-casting blocks, projected shadow silhouettes, player movement on the shadow route, and initial light-driven shadow behaviour. Imported the active Unity project into the GitHub repository through a branch, pull request, and repository check workflow. |
| What will be worked on next? | Update the Kanban progress for the Unity project integration work, then continue improving the first tutorial level by refining its game flow, goal logic, restart behaviour, and player understanding of the shadow mechanic. |
| Blockers, risks, or problems | The core shadow-platform mechanic is now working in the current prototype, but it still needs further playtesting and polish at different light angles so that the tutorial level feels clear, fair, and stable. |

### 2026-05-23 - Tutorial Lighting and Stage Presentation Iteration

| Question | Notes |
| --- | --- |
| What was completed? | Refined the first tutorial level presentation by adding a stage-inspired wash light effect, a soft spotlight cookie, a static spotlight prop, level boundaries, and improved arrow-key light-control meaning so the player interaction feels more connected to shaping shadows through light. The lighting iteration was committed through a feature branch, pull request, CI check, and merge workflow. |
| What will be worked on next? | Record the new external lighting asset and testing evidence in the project documentation, then continue refining the first tutorial level gameplay flow and decide the next improvement needed for player understanding and level completion. |
| Blockers, risks, or problems | The visual atmosphere is becoming stronger, but the tutorial still needs careful balancing so lighting presentation, shadow readability, and gameplay clarity support each other instead of competing for attention. |

### 2026-05-24 - Shadow Edge Platform Traversal Stabilisation

| Question | Notes |
| --- | --- |
| What was completed? | Reworked the tutorial traversal approach toward projected shadow edge platforms. Generated collider strips from projected shadow outline edges, separated walkable edges from steep sliding edges, tuned player ground detection, reduced wall sticking, improved steep-edge sliding behaviour, and refined shadow-edge carry behaviour while the light direction changes. |
| What will be worked on next? | Continue polishing the first tutorial level route, including puzzle difficulty, light-angle limits, goal flow, failure or restart behaviour, and clearer player guidance for understanding which shadow edges are useful. |
| Blockers, risks, or problems | The edge-based shadow traversal system is now more playable, but it still depends on careful parameter tuning. Fast shadow movement, steep edge classification, and player carry behaviour may need further testing as the level layout changes. |

### 2026-05-25 - Level Completion Flow Implementation

| Question | Notes |
| --- | --- |
| What was completed? | Added the first complete level-ending flow for `Level01_Tutorial`. The player can trigger a sliding barn door, the dark doorway is revealed as the door opens, input is locked, the player automatically enters the doorway, the character is gradually hidden by the doorway, the door closes, the camera returns to the overview view, the lights fade out, and a level-complete menu appears with Restart, Next Level, and Main Menu options. |
| What will be worked on next? | Continue polishing the end-of-level presentation and connect the placeholder Next Level and Main Menu buttons once the next scene and menu scene are available. Further level work should also check whether the completion sequence timing still feels right after puzzle layout changes. |
| Blockers, risks, or problems | The completion flow is working in the current tutorial scene, but some timing values for door movement, camera return, blackout, and menu display may still need final tuning. The newly imported barn door Asset Store package also needs to remain documented in the asset credits for submission transparency. |

### 2026-05-26 - Gameplay Audio and Failure Feedback Polish

| Question | Notes |
| --- | --- |
| What was completed? | Added and connected gameplay audio for the tutorial level, including sliding door open and close sounds, light-on and light-off cues, player jump audio, normal platform footsteps, quieter shadow-platform footsteps, falling audio, and a softer generated death landing cue. The opening light presentation was adjusted so the wash-light pulse works with the light-on sound, the initial light angle was tuned for a clearer puzzle start, and the failure camera sequence was polished so the death visual is visible again at the start-area focus. |
| What will be worked on next? | Update documentation and asset credits for the new audio sources, generated/processed audio files, and related testing. After that, continue balancing audio volume, timing, and scene presentation during playtesting. |
| Blockers, risks, or problems | Audio credits must stay accurate because several clips come from Freesound and one light-switch source requires attribution. Unity scene copies can also create noisy metadata or backup-file changes, so commits should keep gameplay files and documentation updates separated and reviewed carefully. |

### 2026-05-27 - Level 02 Hidden Door Route Planning

| Question | Notes |
| --- | --- |
| What was completed? | Planned the main structure for `Level02_HiddenDoor`. The level direction changed from a single route into a two-path design: one upper-left path lets the player discover and remember the hidden end door position, while the right-side route becomes the real route used to reach the door. The door direction and entry setup were reviewed so the copied Level 01 door could fit the new level layout. |
| What will be worked on next? | Continue blocking out the right-side route and decide how the player should progress from the middle platforms toward the end door. The main design target is to make the right route feel like a puzzle route rather than only a longer platforming path. |
| Blockers, risks, or problems | The overview camera does not show the end door at the start, so the level needs to make the player intentionally discover and remember the door position. The risk is that the two-route structure could feel confusing unless the peek route and real route connect clearly. |

### 2026-05-29 - Level 02 Ladder Puzzle Layout

| Question | Notes |
| --- | --- |
| What was completed? | Built out more of the `Level02_HiddenDoor` layout, including the left-side peek route, the connection back toward the right route, and the first shadow ladder puzzle area. The ladder puzzle was designed so several physical cubes look messy at first, but after adjusting the light angle their shadows visually form a climbable ladder. A lot of iteration went into placing the ladder caster cubes so the shadow ladder lined up at a useful angle and connected naturally from the lower platform toward the upper cube-shadow route. |
| What will be worked on next? | Add player climbing support so the shadow ladder is not only a visual puzzle but also a playable route. The next task is to create or connect a climb animation, define climb start/end points, and make the player transition naturally from normal platforming into ladder climbing. |
| Blockers, risks, or problems | The ladder shadow requires careful object placement and light-angle tuning. If the caster pieces are slightly off, the shadow still looks like a ladder visually but may not line up well with the player's climb path or the upper platform connection. |

### 2026-05-30 - Ladder Interaction Implementation and Level 02 Progress Commit

| Question | Notes |
| --- | --- |
| What was completed? | Implemented the playable ladder interaction for `Level02_HiddenDoor`. Added the `Stickman_Climb` animation clip, updated the Player Animator Controller with a climbing state, and added `ShadowLadderClimbZone` to handle automatic ladder grabbing, W/upward climbing, paused animation when no climb input is held, S/downward climbing, bottom detachment, top exit snapping, and multiple ladder zones. |
| What will be worked on next? | Continue testing the right-side route and decide what challenge should come after the ladder section, especially the final approach toward the end platform and hidden door. Documentation should also be updated to record the Level 02 design and ladder testing work. |
| Blockers, risks, or problems | Ladder interactions have several linked parts: trigger placement, BottomPoint/TopPoint/ExitPoint positions, Animator transitions, and platform carry behaviour after exiting the ladder. Small scene placement errors can cause wrong exit points, awkward snapping, or unnatural climb/down-climb behaviour, so the ladder setup needs careful retesting after future layout edits. |
