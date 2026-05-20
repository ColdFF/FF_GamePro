# Development Workflow

Created: 2026-05-20

## Purpose

This document explains how the `ShadowPath` coursework project will be managed during development.

The workflow is based on iterative development, regular Git commits, GitHub Issues, a Kanban board, testing notes, and short development reflections. This supports the coursework requirement for professional repository management and evidence of steady progress.

## Development Approach

`ShadowPath` will be developed as a small playable vertical slice first.

The main priority is to make the core shadow-platform mechanic clear, stable, and playable before adding extra features. Optional improvements will only be added after the minimum playable version works reliably.

Development will follow this general cycle:

1. Select a small task from the Kanban board.
2. Work on the task in Unity or documentation.
3. Test the change locally.
4. Commit the change with a clear message.
5. Push the change to GitHub.
6. Update the related Issue or Kanban item.
7. Record important testing notes or design decisions if needed.

## Git Workflow

The `main` branch should represent the stable state of the repository.

Small documentation setup commits may be made directly to `main`. For gameplay features and larger changes, feature branches should be used.

Recommended workflow for feature work:

1. Create or select a GitHub Issue.
2. Create a feature branch.
3. Make focused changes.
4. Test locally.
5. Commit and push the branch.
6. Open a Pull Request.
7. Check CI results.
8. Merge into `main`.

## Branch Naming

Branch names should be short and descriptive.

Examples:

| Branch Name | Purpose |
| --- | --- |
| `feature/player-controller` | Player movement and jumping. |
| `feature/light-control` | Light direction control. |
| `feature/shadow-platform` | Shadow platform mechanic. |


## Commit Messages

Commit messages should clearly describe the change.

Good examples:

- `Add ShadowPath game concept document`
- `Implement basic player movement`
- `Add light angle controller`


Vague messages such as `Update`, `Fix stuff`, or `Final version` should be avoided.

## Kanban Board

A GitHub Project board will be used to manage tasks.

Recommended columns:

| Column | Meaning |
| --- | --- |
| `Backlog` | Tasks and ideas not started yet. |
| `Ready` | Tasks selected for the next work session. |
| `In Progress` | Tasks currently being worked on. |
| `Testing` | Completed changes that need checking. |
| `Done` | Finished, tested, and committed work. |

Detailed tasks and priorities will be managed through GitHub Issues and the Kanban board rather than being fixed in this document.

## Issue Usage

Issues should be used for meaningful tasks, bugs, documentation work, and testing tasks.

Useful issue labels include:

- `feature`
- `bug`
- `documentation`
- `testing`
- `polish`
- `build`

Each Issue should describe the task clearly and include acceptance criteria when possible.

## Testing Notes

Testing will be recorded in `docs/testing-log.md`.

Testing notes should include the date, tested feature, expected result, actual result, and any fixes or follow-up tasks.

Testing should cover player movement, jumping, shadow readability, light control, level completion, restart behaviour, UI feedback, and final builds.

## Daily Scrum Notes

Short development reflections will be recorded in `docs/daily-scrum.md`.

Each entry should briefly answer:

1. What was completed?
2. What will be worked on next?
3. Are there any blockers or problems?

This provides evidence of regular progress and agile reflection.

## Definition of Done

A task can be moved to `Done` when:

- The planned change is complete.
- The change has been tested if it affects gameplay.
- Important bugs or follow-up tasks have been recorded.
- The work has been committed to Git.
- The related Issue or Kanban item has been updated.
- Documentation has been updated if needed.

## CI Usage

GitHub Actions will be used to check the repository where practical.

At minimum, CI should help confirm that repository files and documentation are present. Later, if suitable for the Unity project, CI may be extended to include Unity-related checks or build steps.

CI should support the development workflow, but it should not replace local testing in Unity.