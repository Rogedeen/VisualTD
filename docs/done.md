# Completed Tasks

- [x] Project initiation and architecture planning.
- [x] Created essential project documentation (`architecture.md`, `todo.md`, `done.md`, `academic_notes.md`).
- [x] Fixed pooled spawn transform order and normalized archer tower root rotation to remove the 45-degree start-angle bug.
- [x] Cached prefab baseline rotation in `EnemyAI` and `ArcherAI` so spawn animation or pooled reuse cannot leave a persistent tilt.
- [x] Cleared the `SampleScene` rotation overrides on castle/tower scene objects so their roots now match the prefab baseline.
- [x] Fixed Main Camera rotation (reverted to intentional 38.92/1.54 design angle).
- [x] Fixed HealthBar.cs to rotate ONLY the UI image, not parent transform (billboard effect without affecting game object rotation).
- [x] Implemented complete enemy AI state machine (Spawning → Moving → Attacking → Dead).
- [x] Fixed NavMesh agent lifecycle: added proper OnEnable/OnDisable cleanup, agent.ResetPath(), validation checks.
- [x] Implemented wave-based enemy spawning with staggering (3 enemies per wave, 0.5s delay between spawns).
- [x] Added target validation loop - enemies now find new castle targets if one is destroyed.
- [x] Fixed attack spam by preventing state re-triggering (TransitionToAttack checks current state).
- [x] Added missing pathPending and hasPath checks for robust NavMesh pathfinding.
- [x] Improved spawn point cycling (round-robin instead of random) to spread enemies across spawn points.
- [x] Projected enemy castle targets onto NavMesh before SetDestination so below-ground castle roots do not break pathfinding.
