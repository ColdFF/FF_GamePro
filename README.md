# FF_GamePro

This repository documents my Game Programming coursework process, including class exercises, Unity practice projects, planning evidence, and the main coursework game project.

## Main Coursework Game: ShadowPath

`ShadowPath` is a 2.5D shadow-platform puzzle game developed in Unity 2022.3 LTS.

The player controls a stickman character in a side-view platform environment. Unlike a traditional platform game, the player must use shadows cast by 3D objects as playable routes. The player adjusts a hidden light direction with the arrow keys, changing the position and shape of shadows to create paths, bridge gaps, and solve spatial puzzles.

The intended player experience is thoughtful and experimental: observe the scene, adjust the light, test the shadow path, and move carefully toward the level goal.

The active coursework Unity project is stored in `ShadowPath/`. Detailed progress, testing notes, asset credits, and development records are documented in the `docs/` folder.

## How to Run ShadowPath

1. Clone or download this repository.
2. Open Unity Hub.
3. Add the `ShadowPath/` folder as a Unity project.
4. Open the project with Unity `2022.3 LTS`.
5. Open the scene `Assets/Scenes/Level01_Tutorial.unity`.
6. Press Play in the Unity Editor.

## Controls

| Action | Input |
| --- | --- |
| Move left | `A` |
| Move right | `D` |
| Run | `Shift` + `A` / `D` |
| Jump | `W` or `Space` |
| Adjust light direction | Arrow keys |

## Repository Structure

| Folder / File | Purpose |
| --- | --- |
| `2D_Demo_Improvement/` | Improved version of the 2D shooter class demo. |
| `SolarSystem/` | Solar system Unity class demo. |
| `Lecture_Q&A/` | In-class questions, answers, and discussion work. |
| `Welldone!!_post/` | In-class group activity post. |
| `ShadowPath/` | Main coursework Unity game project. |
| `docs/` | Planning, testing notes, asset credits, agile records, and development reflections. |
| `Prototype.png` | Early concept sketch kept as process evidence, although the current game design has changed. |

## Development Approach

This project follows an iterative development process:

1. Build a small playable vertical slice.
2. Focus on one clear core mechanic: using shadows as platforms.
3. Keep the scope realistic.
4. Commit progress regularly.
5. Track work using a Kanban board.
6. Test features after each meaningful change.
7. Record design decisions, technical issues, and improvements.

## Assessment Focus

This repository is maintained to show:

- A clear and realistic game concept.
- Evidence of Unity game programming.
- Iterative development and steady progress.
- Testing, debugging, and improvement.
- Responsible asset use and credit records.
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

External assets are recorded in `docs/Asset_Credits.md`, including source page, creator, licence, access date, and any edits made.

## Build Information

The game is developed using:

- Unity `2022.3 LTS`
- 3D Built-In Render Pipeline
- Target platform: Windows build first, with possible WebGL build later