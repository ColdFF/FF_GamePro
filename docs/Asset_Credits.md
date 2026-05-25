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

## Notes

This document should be updated whenever a new external asset is added to the Unity project.

Before final submission, all external assets used in the final game should have accurate source, creator, licence, access date, usage, and edit information recorded here.

AI-assisted project assets should remain clearly documented when they are used, especially when they contribute to the final visual presentation of the game.
