# AI Declaration and Own Contribution Statement

## Document Information

| Item | Details |
| --- | --- |
| Student name | Junfan Zhou |
| Student ID | 2617423 |
| Project title | **ShadowPath** |
| Repository | [`ColdFF/FF_GamePro`](https://github.com/ColdFF/FF_GamePro) |
| Main Unity project | [`ShadowPath/`](https://github.com/ColdFF/FF_GamePro/tree/main/ShadowPath) |
| Unity version | Unity `2022.3.62f3 LTS` |
| Final release | [`ShadowPath v1.0.0`](https://github.com/ColdFF/FF_GamePro/releases/tag/v1.0.0) |

## Purpose of This Statement

This document explains how AI assistance was used during the development and documentation of **ShadowPath**, and clarifies my own contribution to the final submitted project.

AI assistance was part of the production workflow, but it was not treated as a replacement for project judgement. My role was to define the game idea and intended behaviour, test the results in Unity, identify problems, request and check revisions, maintain documentation evidence, and decide what was suitable for the final build.

## My Own Contribution

My main contribution was the design direction, integration judgement, testing process, documentation, and final submission responsibility for **ShadowPath**.

My work included:

- Designing the **ShadowPath** concept and adapting Chinese shadow puppetry inspiration into a playable light-and-shadow puzzle-platform mechanic.
- Defining the intended player experience: observation, light adjustment, shadow reading, careful traversal, and level completion.
- Defining the intended behaviour for the main systems, including light adjustment, projected shadow traversal, shadow ladder interaction, rope-shadow swinging, dual-light switching, UI flow, audio feedback, pause flow, and level completion.
- Organising the Unity project direction, scene flow, level progression, and final Windows build target.
- Designing and refining the four playable levels around a clear progression of shadow mechanics.
- Testing gameplay behaviours in Unity, including movement, generated shadow colliders, ladder climbing, rope release, dual-light switching, UI flow, audio feedback, and final documentation checks.
- Identifying problems during testing, reporting what needed to change, retesting fixes, and accepting or rejecting results based on whether they matched the intended gameplay.
- Maintaining project documentation, asset credits, testing evidence, daily scrum entries, GitHub Issues, Pull Requests, Milestones, and release information.
- Preparing the final report, professionalism portfolio, submission form, README information, and release evidence for submission.

## AI Assistance Used

AI support was used in selected parts of the production workflow. The main areas were:

| Area | How AI was used | My responsibility |
| --- | --- | --- |
| Code-related work | Implementation drafting, revision support, and debugging support for gameplay and project systems. | Defined behaviour requirements, tested results in Unity, reported issues, requested changes, tuned values where needed, and judged whether the behaviour was suitable for the final build. |
| Documentation | Drafting support, wording polish, structure suggestions, and consistency checking across coursework documents. | Selected the evidence, checked the wording against the repository and final build, edited the final text, and took responsibility for the submission. |
| Asset/audio processing | Selected generated or processed asset/audio support for project-specific resources. | Checked whether the resources were appropriate, integrated them into the project where needed, and recorded them transparently in the asset credits. |
| Testing and reflection support | Help with organising testing notes, explaining debugging outcomes, and improving report clarity. | Performed or reviewed the actual Unity behaviour checks, recorded evidence, and decided what changes counted as final. |

## Code-Related AI Support Boundary

For code-related tasks, AI assistance was used for implementation drafts, revisions, and debugging support. These outputs were treated as starting points for testing rather than finished implementation.

My process was:

1. Define the intended behaviour or problem to solve.
2. Use AI assistance to support implementation drafting or debugging.
3. Integrate or edit the output in the Unity project.
4. Test the behaviour in Unity or through build-focused checks.
5. Compare the result with the intended player experience and system behaviour.
6. Report issues and request or make revisions where needed.
7. Record testing evidence in project documentation.
8. Accept the result only when it worked well enough for the final build.

This means AI assistance supported parts of the coding workflow, but the final design direction, testing judgement, integration review, and submission responsibility remained mine.

## AI-Assisted Assets and Audio

The project includes selected AI-assisted or Codex-assisted assets/audio. The recorded items include:

| Asset/audio item | Use in project |
| --- | --- |
| `StageWashCookie_Soft.png` | Lighting cookie used for tutorial/stage wash presentation. |
| `DeathLanding.wav` | Death or landing feedback cue. |
| `ShadowWalk.wav` | Shadow-surface footstep cue based on a credited source and processing workflow. |
| `MenuHoverSoft.wav` | UI hover feedback cue. |
| `MenuClickConfirm.wav` | UI confirmation cue based on a credited source and processing workflow. |

These items are recorded transparently in [`docs/Asset_Credits.md`](https://github.com/ColdFF/FF_GamePro/blob/main/docs/Asset_Credits.md), together with external resources, licences, intended use, and edit notes where relevant.

## Verification and Evidence

AI-assisted outputs were not accepted automatically. They were checked through project evidence such as:

- Unity in-editor testing and build-focused checks.
- Gameplay tests recorded in [`docs/Testing_Log.md`](https://github.com/ColdFF/FF_GamePro/blob/main/docs/Testing_Log.md).
- Documentation consistency checks against the README, asset credits, daily scrum notes, commit history, issues, pull requests, milestones, and the final release build.
- Practical gameplay review of movement, projected shadow platforms, shadow ladder traversal, rope-shadow swinging, dual-light switching, UI flow, audio feedback, and level completion.

Important testing-led changes included refining shadow edge traversal, separating walkable and steep generated edges, improving moving shadow carry behaviour, preserving rope release momentum, forcing rope drop on invalid light switching, and improving UI/instruction/completion flow.

## Final Responsibility

AI assistance was treated as draft support and iteration support, not as a replacement for my project judgement.

The final responsibility for **ShadowPath** remained mine, including:

- the game concept and design direction;
- the intended player experience;
- the level progression and scope decisions;
- the acceptance or rejection of AI-assisted code-related outputs;
- Unity testing and gameplay judgement;
- documentation, asset crediting, and AI declaration;
- final build preparation and submission.

Overall, **ShadowPath** should be understood as a project where AI assistance supported selected production tasks, while my own contribution centred on design direction, requirements, testing, integration review, documentation, and final submission responsibility.