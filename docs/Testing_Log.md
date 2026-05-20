# Testing Log

Created: 2026-05-20

## Purpose

This document records testing for the `ShadowPath` coursework project.

Testing notes are used to show how gameplay features are checked, how bugs are found, and how improvements are made during development.

## Testing Format

Each test entry should include:

- Date
- Feature tested
- Expected result
- Actual result
- Status

## Status Key

| Status | Meaning |
| --- | --- |
| Pass | The feature worked as expected. |
| Partial | The feature worked partly, but needs improvement. |
| Fail | The feature did not work as expected. |
| Fixed | A previous issue has been corrected and retested. |

## Test Entries

### 2026-05-20 - Player Movement Test

| Field | Notes |
| --- | --- |
| Feature tested | Basic player movement |
| Expected result | The player should move left and right using the assigned movement keys. |
| Actual result | The player moves left and right correctly in the current prototype scene. |
| Status | Pass |

### 2026-05-20 - Player Jump Test

| Field | Notes |
| --- | --- |
| Feature tested | Player jumping and ground detection |
| Expected result | The player should jump while grounded and should not jump repeatedly in mid-air. |
| Actual result | The player jumps correctly from the ground in the current prototype scene. |
| Status | Pass |

### 2026-05-20 - Shadow Light Control Test

| Field | Notes |
| --- | --- |
| Feature tested | Directional light control and shadow movement |
| Expected result | The arrow keys should adjust the light direction and visibly change the projected shadow position or shape. |
| Actual result | Directional light control works in the current shadow test setup, and the projected shadow changes visibly. |
| Status | Pass |