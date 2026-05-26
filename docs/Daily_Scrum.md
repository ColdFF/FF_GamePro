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
