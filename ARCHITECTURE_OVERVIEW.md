# Architecture Overview

## The Great Divide: Pure Logic vs. Unity Engine

This project follows a strict architectural "Seam" to separate Game Logic from the Unity Engine. This allows for robust, fast C# unit testing of complex mechanics without the overhead of the Unity Editor.

### 1. The Pure C# Domain (Logic)
*   **Location:** `2 - State`, `4 - Subsystems`, `Systems/Resources`
*   **Characteristics:** NO `UnityEngine` dependencies (mostly). POCOs (Plain Old C# Objects).
*   **Key Classes:**
    *   `Character`: The core model. Holds stats, resources, buffs.
    *   `CombatState`: The orchestration state machine for battle data.
    *   `ResourceChangeResolver`: The math engine for damage and healing.
    *   `EffectPlan`: The command object describing *what* happened.
*   **Interfaces:**
    *   `ICharacterConfig`: Abstracts `CharacterConfig` (ScriptableObject).
    *   `IElementType`: Abstracts `ElementType` (ScriptableObject).
    *   `IResourceType`: Abstracts `ResourceType` (ScriptableObject).
    *   `IRandomService`: Abstracts RNG.

### 2. The Unity Adapter Layer (View/Controller)
*   **Location:** `1 - Orchestration`, `3 - Systems`
*   **Characteristics:** Inherits `MonoBehaviour`, `ScriptableObject`. Handles rendering, input, and physics.
*   **Key Classes:**
    *   `CharacterBehavior`: Wraps a `Character` model. Bridges Unity events to Logic.
    *   `CombatReferee`: Runs the game loop (Turn order, Phases).
    *   `StageChoreographer`: Listens to logic events and plays animations.
    *   `SpawnPointProvider`: Creates the Unity GameObjects.

### 3. The Bridge
*   **ViewRef:** The pure `Character` model holds a weak reference (`object ViewRef`) to its `CharacterBehavior`. This allows View systems to find the Unity object given a Logic object, without the Logic object depending on Unity types.

## Testing Strategy
*   **Logic Tests:** Write NUnit tests against the Pure Domain (e.g., `CombatDamageTests.cs`). Mock the interfaces (`ICharacterConfig`, etc.).
*   **Integration Tests:** (Future) Run in Unity to verify the Adapter Layer.
