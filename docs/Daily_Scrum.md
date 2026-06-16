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

### 2026-06-01 - Level 03 Theme Planning

| Question | Notes |
| --- | --- |
| What was completed? | Planned the main direction for `Level03_RopeTower`. The level theme was shaped around moving cube shadows and rope-shadow swinging, with the goal of making the route feel more vertical and more dynamic than the earlier levels. The first layout idea focused on repeated movement cycles, rope transitions, and a less predictable endpoint position. |
| What will be worked on next? | Start blocking out the scene with simple cube platforms and moving shadow casters before adding rope assets. The first technical target is to make moving cube shadows usable without building a separate animation for every platform. |
| Blockers, risks, or problems | The level could become too mechanically busy if moving platforms and ropes are introduced at the same time. The first prototype should keep the objects simple and test one new interaction at a time. |

### 2026-06-02 - Level 03 Moving Shadow Prototype

| Question | Notes |
| --- | --- |
| What was completed? | Added reusable moving cube behaviour for the Level 03 blockout and tested the first moving shadow platforms. The moving objects can now be configured through Inspector values, which makes it easier to reuse the mechanic across the level without creating a new animation clip for every cube. |
| What will be worked on next? | Continue checking whether the player can stay stable on faster moving shadows, then begin preparing the first rope placement near the early Level 03 route. |
| Blockers, risks, or problems | Faster moving shadows can expose small timing issues in player carry behaviour. The moving platform settings need to be tuned carefully so the challenge feels fair instead of unstable. |

### 2026-06-03 - Rope Asset Import and Grab Prototype

| Question | Notes |
| --- | --- |
| What was completed? | Imported the required Optimized Ropes and Cables Tool rope scripts/materials and created the first rope anchor setup for `Level03_RopeTower`. A first projected rope-shadow grab zone was created so the player can interact with the rope shadow rather than the physical rope object itself. |
| What will be worked on next? | Improve the rope interaction so the player grabs automatically, attaches by the hand point, and moves with the rope shadow in a more readable way. The rope and its projected shadow also need to swing together instead of behaving like separate objects. |
| Blockers, risks, or problems | The rope visual, rope shadow, player hand point, and projected trigger zone all need to stay aligned. Small offset errors can make the interaction look disconnected even when the gameplay trigger works. |

### 2026-06-04 - Level 03 Rope Swing Prototype Commit

| Question | Notes |
| --- | --- |
| What was completed? | Refined the first rope-swing prototype with automatic grabbing, hand attachment, player-facing changes, swing limits, Shift-assisted swing boost, and release momentum. Shared player-launch and projected-shadow edge behaviour were updated to support the new rope and moving-shadow mechanics, and the first Level 03 prototype was committed through a feature branch. |
| What will be worked on next? | Continue building out the full Level 03 route with more moving shadow platforms and additional rope-swing sections. The next design work should focus on pacing, difficulty progression, repeated swing chains, and connecting the prototype mechanic into a complete level with a clear endpoint. |
| Blockers, risks, or problems | Rope swinging has several tuning-sensitive values, including grab distance, hand offset, swing acceleration, release velocity, maximum angle, and Shift boost strength. The new shared script changes should keep being retested in earlier levels so Level 03 progress does not accidentally weaken existing shadow-platform or player-movement behaviour. |

### 2026-06-05 - Level 03 Rope Path Polish

| Question | Notes |
| --- | --- |
| What was completed? | Polished the later `Level03_RopeTower` rope path after testing repeated rope use, slow rope release, moving rope setups, and the `MovingCaster_07` shadow platform section. Rope grabbing now stays automatic while Space is used only for releasing, release/re-grab behaviour was stabilised, rope release momentum was tuned to avoid sudden extra acceleration from idle or slow swings, moving-parent rope visuals were improved so the rope does not bend unnaturally when attached to a moving block, and the Level 03 completion overview camera was given a separate end-of-level framing. The gameplay changes were committed on the `feature/level03-rope-path-polish` branch. |
| What will be worked on next? | Update the documentation branch with the Daily Scrum and Testing Log evidence for the Level 03 polish work, then push the gameplay branch and prepare the pull request. After that, continue testing the final Level 03 path for pacing, difficulty, and camera readability before treating the level as final. |
| Blockers, risks, or problems | The changed systems include shared player movement, camera return behaviour, and generated shadow platform handling, so Level 01 and Level 02 still need quick Play Mode regression checks before merging. Unity scene files can also create large diffs from layout and serialisation changes, so commits should continue to stage only the intended files. |

### 2026-06-06 - Level 04 Dual-Light Route Planning

| Question | Notes |
| --- | --- |
| What was completed? | Planned the main direction for `Level04_DualLight`. The level was shaped around switching between two active lights, with some shadow objects belonging to Phase A, some to Phase B, and some remaining usable in both phases. The design goal was to make light switching part of route reading instead of only a visual effect. |
| What will be worked on next? | Build the first Level 04 blockout, create the dual-light control logic, and decide how phase-specific ladders, ropes, and shadow platforms should be marked in the scene. |
| Blockers, risks, or problems | A two-light level can easily become confusing if the active phase is not clear. Switching lights also creates a technical risk because shared shadow systems, rope attachment, and ladder climb zones all need to respond cleanly to the active light. |

### 2026-06-07 - Level 04 Dual-Light Implementation and Layout

| Question | Notes |
| --- | --- |
| What was completed? | Implemented the Level 04 dual-light setup with a dedicated controller, Phase A and Phase B light references, separate light-angle limits, phase-binding components, and an editor setup utility for wiring the scene. The main Level 04 route was built with phase-specific shadow platforms, ladder sections, rope-shadow sections, and a shared light-platform material. |
| What will be worked on next? | Finish the final Level 04 layout pass, tune rope lengths and ladder trigger placement, calculate the start and completion camera framing, and prepare the project files for a focused gameplay commit. |
| Blockers, risks, or problems | Level 04 depends on shared player, shadow-platform, and rope scripts. Any fix for the new level must avoid weakening Level 01, Level 02, or Level 03, so the commit scope needs to be checked carefully before pushing. |

### 2026-06-08 - Level 04 Final Project Commit

| Question | Notes |
| --- | --- |
| What was completed? | Completed the final Level 04 layout and camera framing, then prepared the project-side update on the `feature-level04-dual-light` branch. The staged project files were checked against the active Unity project, unused copied assets were excluded, and the gameplay update was pushed for pull request review. |
| What will be worked on next? | Update the documentation branch so the README, Daily Scrum, and Testing Log match the Level 04 work and the final project commit. After that, the main development focus should move from individual level construction to connecting the four designed scenes into a complete game flow, including a main menu, level selection, and clearer navigation between levels. |
| Blockers, risks, or problems | The main remaining risk is regression across earlier levels because Level 04 uses shared movement, generated shadow platform, and rope release logic. Manual Unity testing across Level 01 to Level 04 is still recommended before treating the merged version as final. |

### 2026-06-09 - Main Menu and Level Flow Planning

| Question | Notes |
| --- | --- |
| What was completed? | Planned how the separate `ShadowPath` levels should be connected into a complete playable flow. The work focused on the new main menu direction, scene order, level-to-level transitions, pause menu consistency, and how instruction popups should behave across later levels without interrupting the player too much. |
| What will be worked on next? | Implement the main menu scene, connect the build scene list, add reusable pause and instruction UI support to the later levels, and check that Level 01 through Level 04 can be reached in the intended order. |
| Blockers, risks, or problems | The main risk is that menu and transition work touches several scenes at once, which can create large Unity scene diffs. The update needs careful commit staging so unrelated Unity-generated files, example assets, or font cache changes are not accidentally included. |

### 2026-06-10 - Menu Flow Project Commit

| Question | Notes |
| --- | --- |
| What was completed? | Completed the project-side menu and level-flow update. A new `MainMenu` scene was added, the current playable levels were added to Build Settings, level completion can continue through the chapter transition flow, and each level now has instruction and pause support. The project update was staged carefully and committed on the menu/level-flow feature branch. |
| What will be worked on next? | Continue refining the menu logic, especially checking navigation behaviour, button feedback, return-to-menu flow, and whether the level sequence feels clear when played from the start. Documentation should also be updated for the new menu flow, testing notes, and UI font usage. |
| Blockers, risks, or problems | The feature touches all playable scenes, so regression testing across Level 01 to Level 04 is still important. The remaining local Unity-generated changes should stay out of the documentation commit unless they are reviewed and proven necessary. |

### 2026-06-12 - Menu Instructions and UI Audio Polish

| Question | Notes |
| --- | --- |
| What was completed? | Polished the main menu instructions flow by adding a two-page `HOW TO PLAY` popup with Goal/Core Mechanic content on the first page and Controls on the second page. Refined button behaviour so menu, instruction, and level-complete buttons reset after hover/click instead of staying visually selected. Added UI hover and click-confirm sound effects, restyled the level-complete buttons back toward the project's black/white menu language, and fixed hidden button hover sounds during chapter subtitle transitions. The project update was committed and pushed through the project polish branch. |
| What will be worked on next? | Update the documentation branch with the new UI audio credits, Daily Scrum notes, and Testing Log evidence. After that, continue final regression testing across the main menu, level transitions, and level-complete screens before final submission. |
| Blockers, risks, or problems | UI polish touches shared menu scripts and transition flow, so it needs careful regression testing to ensure audio feedback does not trigger from inactive or hidden UI. Generated and processed UI audio also needs accurate asset-credit documentation. |

### 2026-06-17 - Final Polish and Repository Sync

| Question | Notes |
| --- | --- |
| What was completed? | Synced the repository Unity project with the latest local playable project, removed an unused legacy shadow script, moved `LightAngleController` into the core script folder, and updated final documentation. |
| What will be worked on next? | Open the final pull request, check the GitHub files, and prepare the short presentation around the latest menu, level select, Level 03, and Level 04 work. |
| Blockers, risks, or problems | No major blocker. The final check is to make sure the pushed branch and PR show the intended project and documentation changes. |
