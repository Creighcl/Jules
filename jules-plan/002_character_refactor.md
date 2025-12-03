# Character Refactor: Splitting Model and Behavior

## Intentions

The goal of this refactor is to decouple the Core Game Logic from the Unity Engine ("The Seam"). This allows us to write fast, reliable, and complex unit tests for game mechanics (Damage, Buffs, Effects) without the overhead or restrictions of the Unity Editor.

## The Seam

We are drawing a line between **State/Logic** and **Presentation/Engine**.

### Pure C# Layer (The Logic)
*   **`Character` (New Class):** Contains all stats (HP, SP), resources, buffs, and logic for taking damage/healing. It has **zero** dependency on `UnityEngine`.
*   **`CombatState`:** Orchestrates the combat logic using `Character` models.
*   **`DamageResolver` / `ResourceChangeResolver`:** Calculates math based on `Character` models.
*   **`ICharacterConfig`:** Interface for character configuration, implemented by the ScriptableObject.
*   **`IRandomService`:** Interface for RNG, allowing deterministic testing.

### Unity Layer (The View/Wrapper)
*   **`CharacterBehavior` (Renamed from old `Character`):** Inherits from `MonoBehaviour`. It wraps a `Character` instance.
    *   It forwards events from the Unity world (Animations, Clicks) to the `Character` model.
    *   It listens to changes in the `Character` model to update the UI/Visuals.
    *   It links itself to the Model via `Model.ViewRef` (as an object) so View systems can find it.
*   **`CharacterConfig` (ScriptableObject):** Configures the `Character`. Implements `ICharacterConfig` so the pure model can read it without knowing it's a ScriptableObject.

## Benefits

1.  **Testability:** We can write a battery of tests for combat flows (e.g., "A deals 10 damage to B") that run instantly in a standard C# environment.
2.  **Extensibility:** We can create new implementations of `Character` or `ICharacterConfig` for different contexts (AI simulation, server-side validation) easily.
3.  **Clarity:** Separates "How it works" (Math/Rules) from "How it looks" (Sprites/Transforms).
