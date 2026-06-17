# Asset Credits

Created: 2026-05-20

## Purpose

This document records external assets used in the `ShadowPath` coursework project.

All external assets should be listed with their source, creator, licence, access date, usage, and any edits made. This helps ensure that the project uses assets responsibly and follows legal and ethical expectations.

## Asset Use Rules

The project will follow these rules:

- Only use assets with a clear licence.
- Record the source before or immediately after adding an asset to the project.
- Do not use assets if the licence is unclear.
- Credit the creator when required by the licence.
- Record any edits made to the original asset.
- Prefer free, educational-use, public domain, or CC0 assets when possible.
- Record AI-assisted project assets clearly when they are used, so their role in development remains transparent.

## External Assets

| Asset Name | Type | Creator | Source | Licence | Date Accessed | Used For | Edits Made | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Animated Stick Figure Character 2D Free CC0 | Character sprites / animation | RGS_Dev | [itch.io asset page](https://rgsdev.itch.io/animated-stick-figure-character-2d-free-cc0) | [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) | 2026-05-20 | Stickman player character animation, including idle, walk, run, and jump states | Imported into Unity and used for player animation setup | The asset page states that credit is not required, but the creator and source are recorded for coursework transparency. The asset page also states that no generative AI was used. |
| Spotlight and Structure | 3D spotlight prop and structure models | SpaceZeta | [Unity Asset Store asset page](https://assetstore.unity.com/packages/3d/props/interior/spotlight-and-structure-141453) | Standard Unity Asset Store EULA | 2026-05-23 | Stage spotlight visual prop for the tutorial level lighting atmosphere | Imported into Unity and positioned as a static scene prop to support the stage-light presentation | The current tutorial level uses the spotlight visual asset to reinforce the light-and-shadow theme. Imported demo content is not part of the playable level design. |
| Barn Door Asset Pack | 3D barn door models, materials, meshes, textures, and prefabs | BMQ | [Unity Asset Store asset page](https://assetstore.unity.com/packages/3d/props/interior/barn-door-asset-pack-272809) | Standard Unity Asset Store EULA | 2026-05-25 | Sliding end-door visual asset for the `Level01_Tutorial` completion sequence | Imported into Unity, positioned and scaled in the scene, and configured so the door panel, wheels, and vertical hanger parts slide while the rail remains fixed | Used as the visible end-door prop. The trigger logic, black doorway, player entry sequence, occlusion effect, camera return, blackout, and completion menu were created as project gameplay systems. |
| Optimized Ropes And Cables Tool | Rope generation scripts, editor support, materials, and textures | GogoGaga | [Unity Asset Store asset page](https://assetstore.unity.com/packages/tools/physics/optimized-ropes-and-cables-tool-287164) | Standard Unity Asset Store EULA | 2026-06-04 | Rope visual and material basis for the `Level03_RopeTower` rope-swing prototype | Imported the core rope scripts, editor support, rope material, and rope textures needed for the prototype. Example scenes and unrelated example models were not included in the gameplay commit. | Used with the custom `ShadowRopeSwingZone` gameplay system so the player interacts with the projected rope shadow while the visible rope and its shadow swing together. |
| Sliding wooden door.wav | Audio source recording | itinerantmonk108 | [Freesound sound page](https://freesound.org/people/itinerantmonk108/sounds/685106/) | [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) | 2026-05-26 | Sliding end-door open and close cues: `DoorSlide_Open.wav` and `DoorSlide_Close.wav` | The original recording was split into gameplay cues with Codex-assisted editing. The opening cue uses the first section of the file, with the selected first 2.7 seconds shortened to fit the door-open animation. The closing cue uses the later close section from approximately 0:04.25 to 0:06.57 and was shortened to fit the door-close animation. | The original Freesound page describes a wooden door being slid back and forth to open and close. |
| JUMP SFX RANDOMISATION - HUMAN MADE | Audio source recording | CCOUNTERFEIT | [Freesound sound page](https://freesound.org/people/CCOUNTERFEIT/sounds/851416/) | [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) | 2026-05-26 | Player jump cue: `Jump.wav` | A short early jump cue was selected and exported through Codex-assisted editing as a 450 ms Unity-ready WAV for the player jump action. | The source contains multiple human-made jump sound effects. |
| Various footsteps | Audio source recording | AdamK201 | [Freesound sound page](https://freesound.org/people/AdamK201/sounds/851596/) | [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) | 2026-05-26 | Normal platform player footstep cue: `Walk.wav`; source basis for the quieter shadow-footstep variant `ShadowWalk.wav` | A short footstep around 0:05.563 to 0:05.937 was selected, extended slightly after testing, repaired for audibility, and exported through Codex-assisted editing as a Unity-ready WAV. A processed derivative was also used for the shadow-surface footstep sound. | The normal footstep is used on the start and end platforms. The derivative shadow step is documented again under Original Assets because it was created through project-specific audio processing. |
| Light Switch .wav | Audio source recording | 221227 | [Freesound sound page](https://freesound.org/people/221227/sounds/655533/) | [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/) | 2026-05-26 | Opening light-on cue: `LightSwitch_On.wav`; final blackout light-off cue: `LightSwitch_Off.wav`; processed UI click-confirm cue: `MenuClickConfirm.wav` | The light-on cue was cut from approximately 0:05.937 to 0:06.180. The light-off cue was cut from approximately 0:06.745 to 0:07.000. Both were exported through Codex-assisted editing as Unity-ready WAV files. A further short UI click-confirm derivative was created from the existing light-on cue through Codex-assisted processing, including shortening, transient shaping, filtering, and volume balancing for menu confirmation feedback. | Attribution is required by the licence; creator, source, and edits are recorded here. The processed UI click derivative is also documented under Original Assets because it was created specifically for this project UI. |
| FallingWhoosh3 | Audio source recording | Huglex | [Freesound sound page](https://freesound.org/people/Huglex/sounds/842984/) | [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) | 2026-05-26 | Player failure falling / sliding-fall cue: `Falling.wav` | Imported and assigned through Codex-assisted setup as the falling whoosh used while the player falls after leaving the shadow path. The gameplay script stops the sound when the death landing cue begins. | The original Freesound page describes a whoosh made to mimic air displacement from a falling object. |
| Liberation Sans / TextMesh Pro SDF font asset | Font / UI text rendering | Liberation Fonts project / Unity TextMesh Pro package | Unity TextMesh Pro package font files and included OFL licence text | SIL Open Font License 1.1 | 2026-06-10 | UI text rendering for instruction popups, pause/menu buttons, and other TextMesh Pro interface text | Used through Unity TextMesh Pro SDF font assets and materials; no custom font artwork edits were made | Recorded because the menu, pause, and instruction UI rely on TextMesh Pro font rendering. The licence text is included with the Unity TextMesh Pro font files in the project. |

## Unity Assets

| Asset Name | Type | Source | Licence / Notes |
| --- | --- | --- | --- |
| Unity default materials, primitives, lights, cameras, and physics components | Built-in engine assets | Unity 2022.3 LTS | Unity engine built-in functionality used for development. |

## Original Assets

The following assets are created or directed for this coursework project:

| Asset Name | Type | Purpose | Creation / Source Notes |
| --- | --- | --- | --- |
| ShadowPath level layout | Level design | Main puzzle-platform level structure. | Designed in Unity for this coursework project. |
| Shadow projection setup | Scene design | Core shadow-platform mechanic. | Built in Unity for this coursework project. |
| Scripts created for gameplay systems | Code | Player control, light control, game logic, and related systems. | Written and iterated during project development. |
| `StageWashCookie_Soft.png` | AI-assisted lighting texture | Soft spotlight cookie used to support the tutorial level stage wash effect. | Generated through a developer-directed OpenAI image-generation step during development. No external source image was used. The texture was imported into Unity and configured as a spotlight cookie. |
| `DeathLanding.wav` | AI-assisted / generated sound effect | Soft landing impact cue for the failure death visual after the player has fallen. | Generated during development through a Codex-assisted audio creation step at the user's request. No external source recording was used. The file was imported into Unity as `Assets/Audio/Player/DeathLanding.wav`. |
| `ShadowWalk.wav` | AI-assisted / processed derivative sound effect | Quieter shadow-surface footstep cue used when the player walks on generated shadow platforms. | Created during development through Codex-assisted audio processing based on the credited `Various footsteps` source recording. The underlying source is CC0; processing included lowering intensity, softening the transient, filtering, and shaping the sound so it feels lighter than normal platform footsteps. The file was imported into Unity as `Assets/Audio/Player/ShadowWalk.wav`. |
| `MenuHoverSoft.wav` | AI-assisted / generated sound effect | Soft UI hover cue used when the cursor moves over menu and instruction buttons. | Generated during development through a Codex-assisted audio creation step at the user's request. No external source recording was used. The sound was designed to be short, soft, and unobtrusive so it fits the subdued menu style. The file was imported into Unity as `Assets/Resources/Audio/UI/MenuHoverSoft.wav`. |
| `MenuClickConfirm.wav` | AI-assisted / processed derivative sound effect | Stronger but clean UI click-confirm cue used when menu and instruction buttons are pressed. | Created during development through Codex-assisted audio processing based on the credited `Light Switch .wav` source recording by 221227, using the existing `LightSwitch_On.wav` cue as the source basis. The underlying source is licensed under Creative Commons Attribution 4.0, so attribution is recorded in the External Assets table. Processing included shortening, transient shaping, filtering, and volume balancing to create a clearer UI confirmation sound. The file was imported into Unity as `Assets/Resources/Audio/UI/MenuClickConfirm.wav`. |
| `Level01.png` | Original gameplay screenshot / UI thumbnail | Level 1 thumbnail image in the level select menu. | Captured by the developer from the Unity gameplay scene and stored as `Assets/LevelSelect_Image/Level01.png`. |
| `Level02.png` | Original gameplay screenshot / UI thumbnail | Level 2 thumbnail image in the level select menu. | Captured by the developer from the Unity gameplay scene and stored as `Assets/LevelSelect_Image/Level02.png`. |
| `Level03.png` | Original gameplay screenshot / UI thumbnail | Level 3 thumbnail image in the level select menu. | Captured by the developer from the Unity gameplay scene and stored as `Assets/LevelSelect_Image/Level03.png`. |
| `Level04.png` | Original gameplay screenshot / UI thumbnail | Level 4 thumbnail image in the level select menu. | Captured by the developer from the Unity gameplay scene and stored as `Assets/LevelSelect_Image/Level04.png`. |

## Notes

This document should be updated whenever a new external asset is added to the Unity project.

Before final submission, all external assets used in the final game should have accurate source, creator, licence, access date, usage, and edit information recorded here.

AI-assisted project assets should remain clearly documented when they are used, especially when they contribute to the final version of the game.
