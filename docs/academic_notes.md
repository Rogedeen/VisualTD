# Academic Notes

## Why UDP over TCP?
For a real-time gesture-controlled game, latency is a critical factor. TCP (Transmission Control Protocol) is connection-oriented and guarantees delivery, which involves overhead and can cause delays if packets are lost and retransmitted (Head-of-line blocking). UDP (User Datagram Protocol) is connectionless and sends packets without guaranteeing delivery, resulting in much lower latency. In our context, dropping a single frame's gesture data is acceptable and preferred over delaying subsequent frames, making UDP the ideal choice.

## MediaPipe Hand Tracking (21 Landmarks)
MediaPipe Hands utilizes machine learning models to infer 21 3D landmarks of a hand from just a single frame. The model provides precise joint locations, allowing us to calculate distances and angles between specific joints. For instance, determining if a hand is closed (fist) involves checking the distance between the fingertips and the palm. By analyzing the temporal sequence of these landmark configurations, we can define and recognize dynamic gestures like a "swift downward motion" or "hand pushed forward".

## SOLID Principles in Unity
Applying SOLID principles ensures a scalable and maintainable codebase:
- **Single Responsibility**: `UDPReceiver` only handles network traffic, while `SkillManager` only handles skill execution.
- **Open/Closed**: Adding new skills won't require modifying the core `SkillManager` if implemented through a common `ISkill` interface.
- **Dependency Inversion**: High-level modules (like `SkillManager`) do not depend directly on low-level modules (like `UDPReceiver`), but rather on abstractions or event-based communication.

## Object Pooling for Performance
In Unity, repeatedly instantiating and destroying GameObjects (like arrows or enemies) triggers garbage collection and memory allocation, leading to frame rate drops (stutters). Object Pooling solves this by pre-instantiating a set number of objects and disabling them. When an object is needed, it is enabled and repositioned. When it is "destroyed", it is simply disabled and returned to the pool, drastically reducing CPU overhead.

## Pooled Spawn Order Matters
When a pooled object has `OnEnable` logic, its position and rotation should be assigned before calling `SetActive(true)`. Otherwise, the object can briefly initialize with stale transform data from its previous use, which can create incorrect facing, animation, or agent state on the first frame.
