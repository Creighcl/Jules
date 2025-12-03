# Implementation Plan: Genericize RPG Systems (Run 001)

This plan details the steps taken to genericize the RPG system, separating the core engine from game-specific mechanics.

## Phase 1: Positioning Abstraction
**Goal:** Decouple the core system from the grid-based `BattlefieldPosition`.

1.  **Define Positioning Interfaces**
    *   Create `Systems/Positioning/ICombatPosition.cs`: A marker interface for an entity's position.
    *   Create `Systems/Positioning/IPositioningSystem.cs`: Interface for queries (`GetDistance`, `GetNeighbors`, `GetNearbyAllies`).
    *   Create `Systems/Positioning/IBattlefieldPositionProvider.cs`: Formalize the missing provider interface.
2.  **Refactor Character Positioning**
    *   Modify `Character.cs`: Replace `BattlefieldPosition` property with `ICombatPosition`.
    *   Remove `SetPositionInfo` and rely on the provider/system.
3.  **Refactor Combat Logic**
    *   Update `CombatState.cs`: Inject `IPositioningSystem`.
    *   Update `CombatantListFilter.cs` and `Effect.cs`: Move `GetNearbyAlliesOfCharacter` logic into `IPositioningSystem` (via helper methods in `Effect`).
    *   Create a `GridPositioningSystem` (implementation) to preserve existing behavior.

## Phase 2: Dynamic Element Types
**Goal:** Replace hardcoded `PowerType` enum with ScriptableObjects.

4.  **Create Element Architecture**
    *   Create `Scriptables/ElementType.cs`: Empty ScriptableObject to serve as a key.
    *   Create `Systems/Elements/ElementInteractionRule.cs`: Logic for resistance/weakness (e.g., `IsResistant(ElementType attacker, ElementType defender)`).
5.  **Refactor Character Config**
    *   Update `CharacterConfig.cs`: Replace `PowerType` enum with `ElementType` field.
    *   Remove `PowerType.cs` enum file.
6.  **Update Resolution Logic**
    *   Update `DamageResolver` (later `ResourceChangeResolver`): Use `ElementInteractionRule` to determine resistance instead of hardcoded enum checks.

## Phase 3: Generic Resource System (The Deep Dive)
**Goal:** Unify Health, Stagger, and Light/Shadow into a generic Resource system.

7.  **Create Resource Architecture**
    *   Create `Scriptables/ResourceType.cs`: Definition (Name, Min, Max, Regenerates?).
    *   Create `Systems/Resources/Resource.cs`: Runtime container (CurrentValue, MaxValue).
    *   Create `Systems/Resources/ResourceChangeOrder.cs`: Replaces `DamageOrder`, `ScaleOrder`, `HealOrder`.
    *   Create `Systems/Resources/ResourceChangeResult.cs`: Generic result object.
8.  **Implement Logic & Strategies**
    *   Create `Systems/Resources/IResourceLogic.cs`: Interface for applying changes.
    *   Implement `StandardResourceLogic`: Basic clamping.
    *   Implement `HealthResourceLogic`: Triggers `Die()` when depleted (conceptually).
    *   **Buff Modification Hooks:** Added `ModifyOutgoingResourceAmount` and `ModifyIncomingResourceAmount` to `Buff.cs` to allow interception of resource changes.
9.  **Refactor Character State**
    *   Update `Character.cs`: Replace `currentHealth`, `currentStagger` logic with `Dictionary<ResourceType, Resource>`.
    *   Expose helper methods `GetResource(ResourceType)` for convenience.
10. **Refactor Resolution Pipeline**
    *   Rename/Refactor `DamageResolver.cs` to `ResourceChangeResolver.cs`.
    *   Update `EffectPlan.cs`: Add `List<ResourceChangeOrder>`.
    *   Update `CombatState.cs`: Process `ResourceChangeOrders` using the resolver.
    *   Update `ResourceChangeResolver`: Implement `Resolve(ResourceChangeOrder)` using the modification pipeline (Outgoing Buffs -> Incoming Buffs -> Logic).

## Phase 4: Cleanup & Verification
11. **Cleanup**
    *   Remove hardcoded Light/Shadow logic from `CombatState.cs`.
    *   Clean up whitespace in `CombatState.cs`.
    *   Ensure `Effect.cs` is correctly updated with `Configure` method.
