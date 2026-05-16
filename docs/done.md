# Completed Tasks

- [x] Project initiation and architecture planning.
- [x] Created essential project documentation (`architecture.md`, `todo.md`, `done.md`, `academic_notes.md`).
- [x] Fixed pooled spawn transform order and normalized archer tower root rotation to remove the 45-degree start-angle bug.
- [x] Cached prefab baseline rotation in `EnemyAI` and `ArcherAI` so spawn animation or pooled reuse cannot leave a persistent tilt.
- [x] Cleared the `SampleScene` rotation overrides on castle/tower scene objects so their roots now match the prefab baseline.
