# To-Do List

## Phase 1 - Python CV & Architecture Planning
- [ ] Initialize Python environment and install dependencies (OpenCV, MediaPipe).
- [ ] Implement basic hand tracking using MediaPipe.
- [ ] Develop logic to recognize "Arrow Volley" gesture.
- [ ] Develop logic to recognize "Lightning Strike" gesture.
- [ ] Develop logic to recognize "Fortify/Heal Wall" gesture.
- [ ] Log recognized gestures to the console.

## Phase 2 - The Bridge
- [ ] Create UDP Sender in Python to serialize and send JSON payloads.
- [ ] Create asynchronous UDP Receiver in Unity C# (`UDPReceiver.cs`).
- [ ] Implement JSON parsing in Unity without blocking the main thread.
- [ ] Test end-to-end communication between Python and Unity.

## Phase 3 - Core Game Systems
- [ ] Implement `ObjectPooler.cs` for efficient object management.
- [ ] Implement `EnemyManager.cs` and `EnemyAI.cs` with NavMesh integration.
- [ ] Implement `SkillManager.cs` and define skill structures.
- [ ] Implement `CastleManager.cs` for gate/wall health management.
- [ ] Set up Unity Inspector variables and prepare NavMesh instructions for the user.

## Phase 4 - Integration & Polish
- [ ] Connect `GestureParser.cs` to `SkillManager.cs` to trigger actual skills from UDP data.
- [ ] Hook up KayKit animations (Idle, Walk, Attack, Die).
- [ ] Add visual and particle effects for skills (arrows falling, lightning strike, healing aura).
- [ ] Balance test and polish gameplay mechanics.
