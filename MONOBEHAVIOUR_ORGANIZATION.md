# MonoBehaviour Organization & The Seam

## The Seam Philosophy

We divide classes into two categories to enable testing and separation of concerns.

### 1. Pure C# (The Domain)
**Goal:** Testable logic. No `UnityEngine`.
*   `Character`
*   `CombatState`
*   `ResourceChangeResolver`
*   `EffectPlan` / `Effect` / `Buff`

### 2. Unity Monobehaviours (The Adapter)
**Goal:** Rendering, Input, Scene Management.
*   `CharacterBehavior`: The "Body" of a character in the scene.
*   `ActorCharacter`: The "Puppet" (Animations/VFX).
*   `CombatReferee`: The "Game Master" (Time/Turns).
*   `SpawnPointProvider`: The "Factory" (Instantiates Prefabs).

## How They Connect

1.  **Instantiation:** `SpawnPointProvider` instantiates the Prefab (which has `CharacterBehavior`).
2.  **Initialization:** `CharacterBehavior` creates the pure `Character` model using `ICharacterConfig`.
3.  **Linking:** `Character.ViewRef` is set to point to `CharacterBehavior` (weak link).
4.  **Usage:** Systems like `StageChoreographer` receive the pure `Character` in events, check `ViewRef`, and find the `ActorCharacter` to play animations.

## Interfaces for Decoupling
To keep the pure side pure, we abstract Unity types:
*   `ScriptableObject` -> `ICharacterConfig`, `IElementType`, `IResourceType`.
*   `Random` -> `IRandomService`.
