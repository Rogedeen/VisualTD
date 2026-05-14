# Architecture Blueprint

## System Overview
The system consists of two main components:
1. Python Computer Vision Module (MediaPipe)
2. Unity 3D Tower Defense Game (C#)
Communication between the two is handled via Asynchronous UDP Sockets.

## Python Computer Vision Module
- **Hand Tracking**: MediaPipe Hands to detect 21 hand landmarks.
- **Gesture Recognition**: Analyzes landmarks to classify three dynamic gestures:
  1. Arrow Volley: Fist closed -> Hand pushed forward/opened.
  2. Lightning Strike: Index finger pointing up -> Swift downward motion.
  3. Fortify/Heal Wall: Two open hands facing the camera (Stop gesture).
- **UDP Sender**: Serializes recognized gestures into JSON and sends them to the Unity backend over UDP.

## Unity Game Module
- **UDPReceiver.cs**: Asynchronously listens for incoming UDP packets, deserializes JSON, and queues actions for the main thread.
- **GestureParser.cs**: Reads queued actions and maps them to in-game skills.
- **SkillManager.cs**: Executes the mapped skills (Arrow Volley, Lightning Strike, Fortify Wall).
- **EnemyManager.cs & EnemyAI.cs**: Spawns enemies, handles NavMesh navigation towards the gate, and manages enemy state and health.
- **ObjectPooler.cs**: Manages pooling for arrows, enemies, and particle effects to ensure optimal performance without garbage collection stutters.
- **CastleManager.cs**: Manages the health and state of the main gate/wall.

## Communication Protocol
- Protocol: UDP (User Datagram Protocol) for low latency.
- Format: JSON
- Example Payload: `{"gesture": "arrow_volley", "confidence": 0.95}`
