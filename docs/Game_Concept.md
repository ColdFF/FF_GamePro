# ShadowPath Game Concept

Created: 2026-05-20

## Overview

`ShadowPath` is a 2.5D shadow-platform puzzle game developed in Unity 2022.3 LTS.

The concept draws inspiration from Chinese shadow puppetry, a traditional performance form where light, screens, and silhouettes are used to create moving stories. `ShadowPath` adapts this idea into an interactive game mechanic: shadows are not only part of the visual atmosphere, but also become playable routes through the level.

The player controls a stickman character in a side-view platform environment. Instead of only jumping on physical platforms, the player observes how 3D objects cast shadows onto a vertical projection surface, then adjusts the light direction to reshape those shadows and create a route forward.

The game combines platform movement with spatial puzzle solving. The player is expected to think before moving, experiment with the light angle, and use the resulting shadow shapes to cross gaps or reach the level goal.

## Design Goal

The goal of `ShadowPath` is to create a small but convincing vertical slice that demonstrates one clear gameplay idea:

> Shadows are not only visual effects; they can become part of the playable level.

The project focuses on clarity, playability, and technical reliability rather than a large amount of content. This matches the coursework requirement to design a realistic concept and develop it into a playable game prototype.

## Target Audience

The target audience is players who enjoy puzzle platformers, experimental mechanics, and short games that reward observation and problem solving.

The game is intended to be understandable without complex instructions. The core idea should become clear through the level design: adjust the light, watch the shadow change, and use the shadow path to progress.

## Platform and Engine

The game is developed using:

- Unity `2022.3 LTS`
- 3D Built-In Render Pipeline
- Fixed side-view camera
- Windows build as the first target platform


## Core Mechanic

The core mechanic is shadow-based platform creation.

Key elements:

- A 3D object blocks light and casts a shadow.
- The shadow is projected onto a vertical surface.
- The player can adjust the light direction.
- Changing the light direction changes the shadow position and shape.
- The shadow can be used as a platform or path.
- The player must combine movement and light adjustment to reach the goal.

This mechanic connects directly to lecture topics such as Unity GameObjects, components, transforms, lighting, cameras, physics, player input, and iterative prototyping.

## Core Gameplay Loop

The intended gameplay loop is:

1. Observe the level layout.
2. Identify where a path is missing.
3. Adjust the light direction to move or reshape the shadow.
4. Test whether the shadow creates a usable route.
5. Move, jump, and cross the shadow path.
6. Reach the goal or revise the solution.

This loop supports experimentation and debugging during development, because each level can be tested by checking whether the shadow solution is readable, reachable, and fair.

## Player Experience

The intended player experience is:

- Thoughtful
- Experimental
- Clear
- Slightly mysterious
- Focused on discovery rather than speed

The game should avoid overwhelming the player with too many mechanics. The main challenge should come from understanding the relationship between light, object, shadow, and movement.

## Minimum Viable Game Scope

The minimum playable version should include:

- One complete playable level
- A controllable stickman player
- Side-view platform movement
- Jumping and basic collision
- A fixed camera
- A vertical shadow projection surface
- At least one shadow-casting object
- A controllable light source
- A goal or level completion condition
- A restart option
- Basic UI feedback
- A stable Windows build

This scope is intentionally small so that the final submission can be polished, tested, and documented properly.

## Possible Stretch Goals

If the minimum version is stable, possible stretch goals include:

- Additional puzzle levels
- More shadow-casting objects
- Moving or rotating objects
- Improved animation transitions
- Sound effects and background music
- Main menu and pause menu polish
- Better visual feedback for valid shadow paths

These features are optional and should only be added after the core vertical slice works reliably.

## Out of Scope

To keep the project realistic, the following features are out of scope for the coursework version:

- Multiplayer
- Online features
- Procedural level generation
- Large narrative systems
- Complex combat
- Large open-world environments
- Advanced character customisation

Avoiding these features helps keep development focused on the main shadow-platform mechanic.

## Main Game Objects and Systems

The planned game will include the following main systems:

| System | Purpose |
| --- | --- |
| Player Controller | Handles player movement, jumping, and interaction with the level. |
| Stickman Visuals | Displays the player character and animation states. |
| Shadow Projection Surface | Receives the shadow used as part of the playable route. |
| Shadow-Casting Objects | Create shadow shapes that can become platforms or paths. |
| Light Controller | Allows the player to adjust the light direction. |
| Camera | Provides a fixed side-view suitable for 2.5D platform gameplay. |
| Game Manager | Tracks level state, restart, success, and basic flow. |
| UI | Provides simple feedback such as restart, goal, or menu information. |

## Design Risks

The main design risks are:

- The shadow path may be difficult for the player to understand.
- The collision between the player and shadow-based platforms may be technically challenging.
- The puzzle may become confusing if too many controls or objects are introduced.
- The game may feel unfair if the player cannot clearly see where the shadow path is valid.

These risks will be managed through small prototypes, frequent testing, and careful scope control.

## Success Criteria

The project will be considered successful if:

- The player can understand the core shadow mechanic.
- The game includes a complete playable vertical slice.
- The main level can be completed from start to finish.
- The player movement feels stable and responsive.
- The shadow adjustment mechanic clearly affects the playable route.
- The repository shows steady development through commits, planning notes, testing notes, and issue tracking.
- External assets are credited responsibly.
