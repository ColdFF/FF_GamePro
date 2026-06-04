# FF_GamePro

This repository contains my Game Programming coursework, including class exercises, Unity practice projects, planning evidence, testing records, and the main coursework game project.

## Main Coursework Game: ShadowPath

`ShadowPath` is a 2.5D shadow-platform puzzle game developed in Unity 2022.3 LTS.

The concept draws inspiration from Chinese shadow puppetry, where light, screens, and silhouettes are used to create movement and storytelling. In `ShadowPath`, this visual idea becomes a gameplay mechanic: shadows are not only part of the scene atmosphere, but also form playable routes through the level.

The player controls a stickman character in a side-view platform environment. Unlike a traditional platformer, progression depends on observing shadows cast by 3D objects and adjusting a hidden light direction with the arrow keys. As the light changes, the projected shadows shift shape and position, creating paths, bridging gaps, and supporting spatial puzzle solving.

The intended experience is thoughtful and experimental: observe the scene, adjust the light, test the shadow path, and move carefully toward the level goal.

The active Unity project is stored in `ShadowPath/`. Supporting documentation, including design notes, testing evidence, asset credits, and development reflections, is stored in `docs/`.

## How To Run ShadowPath

1. Clone or download this repository.
2. Open Unity Hub.
3. Add the `ShadowPath/` folder as a Unity project.
4. Open the project with Unity `2022.3 LTS`.
5. Open the scene `Assets/Scenes/Level01_Tutorial.unity`.
6. Press Play in the Unity Editor.

## Controls

| Action | Input |
| --- | --- |
| Move left / swing rope left | `A` |
| Move right / swing rope right | `D` |
| Run | `Shift` + `A` / `D` |
| Jump | `W` or `Space` |
| Climb up when on a ladder | `W` |
| Climb down / detach near ladder bottom | `S` |
| Auto-grab rope shadow | Jump into a rope-shadow grab zone |
| Release rope while swinging | `Space` |
| Boost rope swing | Hold `Shift` while swinging |
| Adjust light direction | Arrow keys |

## Repository Structure

| Folder / File | Purpose |
| --- | --- |
| `2D_Demo_Improvement/` | Improved version of a 2D shooter class demo. |
| `SolarSystem/` | Solar system Unity class demo. |
| `Lecture_Q&A/` | In-class questions, answers, and discussion work. |
| `Welldone!!_post/` | In-class group activity post. |
| `ShadowPath/` | Main coursework Unity project. |
| `docs/` | Planning, testing notes, asset credits, agile records, and development reflections. |
| `Prototype.png` | Early concept sketch kept as process evidence, although the current game design has changed. |

## Development Approach

This project follows an iterative development process:

1. Build a focused playable vertical slice.
2. Prioritise one clear core mechanic: using shadows as platforms.
3. Keep the scope realistic for the coursework timeframe.
4. Commit progress regularly through feature branches and pull requests.
5. Track development work using a Kanban board.
6. Test features after each meaningful change.
7. Record design decisions, technical issues, and improvements.

## Assessment Focus

This repository is maintained as evidence of:

- A clear and realistic game concept.
- Unity gameplay programming and scene development.
- Iterative development and steady progress.
- Testing, debugging, and refinement.
- Responsible asset use and credit documentation.
- Reflection on design and technical decisions.

## Documentation

| Document | Purpose |
| --- | --- |
| `docs/Game_Concept.md` | Game concept, design goals, scope, and risks. |
| `docs/Development_Workflow.md` | Development workflow and repository process. |
| `docs/Asset_Credits.md` | External asset sources, licences, and credit records. |
| `docs/Testing_Log.md` | Feature tests, bug checks, and retesting evidence. |
| `docs/Daily_Scrum.md` | Short agile-style progress reflections. |

## Asset Credits

External assets are recorded in `docs/Asset_Credits.md`, including source page, creator, licence, access date, intended use, and any edits made.

## Build Information

The game is developed using:

- Unity `2022.3 LTS`
- 3D Built-in Render Pipeline
- Target platform: Windows first, with WebGL as a possible later target
