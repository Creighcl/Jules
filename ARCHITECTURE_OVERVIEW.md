# Combat System Architecture Overview

## 🏗️ Folder Philosophy & Organization

This combat system uses a **numbered hierarchy** to establish clear architectural layers and prevent circular dependencies. The numbers create a mental model where **dependencies flow downward** - higher-numbered folders can depend on lower-numbered ones, but never the reverse.

```
1 - Orchestration/     ← Top-level game flow control
2 - State/             ← Core state containers (no Unity dependencies)
3 - Systems/           ← Presentation layer (Unity MonoBehaviours)
4 - Subsystems/        ← Game logic (Abilities, Buffs, Effects)
5 - Helpers/           ← Utility classes & data carriers
Enum/                  ← Shared type definitions
Scriptables/           ← ScriptableObject data containers
```

### Design Philosophy

#### **Layer 1: Orchestration**
- **Purpose**: Top-level game flow control
- **Key File**: `CombatReferee.cs`
- **Responsibilities**:
  - Manages the combat state machine
  - Coordinates between systems
  - Handles phase transitions
  - Responds to player input via events
  - Owns the "master clock" of combat flow

**Why it exists**: Provides a single source of truth for "what happens next" in combat. All other systems react to this orchestrator.

#### **Layer 2: State**
- **Purpose**: Pure data containers and game logic processors
- **Key Files**: `CombatState.cs`, `Character.cs`, `EventProvider.cs`, `GameState.cs`
- **Responsibilities**:
  - Execute game rules
  - Process effect chains
  - Maintain character state
  - Broadcast events
  - **Zero Unity dependencies** (no MonoBehaviour, no GameObjects)

**Why it exists**: The "Great Divide" - separates game logic from presentation. This layer could theoretically run in a unit test without Unity. All stat calculations, damage resolution, and effect processing happen here.

#### **Layer 3: Systems**
- **Purpose**: Presentation and Unity integration
- **Key Files**: `StageChoreographer.cs`, `UIManager.cs`
- **Responsibilities**:
  - Subscribe to `EventProvider` events
  - Trigger animations and VFX
  - Update UI elements
  - Play audio
  - Handle Unity-specific concerns

**Why it exists**: Keeps presentation code separate from game logic. The `StageChoreographer` listens for game events (like `OnDamageResolved`) and translates them into visual performances without affecting the underlying state.

#### **Layer 4: Subsystems**
- **Purpose**: Concrete implementations of game mechanics
- **Contains**: `Abilities/`, `Buffs/`, `Boons/`, `BuffEffect/`
- **Responsibilities**:
  - Individual ability implementations
  - Buff behavior definitions
  - Effect resolution logic
  - Between-game upgrade logic (Boons)

**Why it exists**: Keeps the core system extensible. New abilities/buffs are just new files that inherit from `Effect` or `Buff` - no modification to core systems needed.

#### **Layer 5: Helpers**
- **Purpose**: Utility classes and data transfer objects
- **Contains**: Order classes, Resolvers, Filters, Providers
- **Responsibilities**:
  - Carry intent/data between layers
  - Provide focused utility functions
  - No state management

**Why it exists**: Prevents bloat in core classes. Helper classes have single, clear purposes (e.g., `DamageResolver` only calculates damage, `CombatantListFilter` only filters character lists).

---

## 🎯 Key Design Choices

### 1. **Numbered Folders Create Dependency Flow**
**Problem**: In complex systems, circular dependencies are easy to create and hard to debug.

**Solution**: Visual hierarchy through folder names. You can immediately see that `CombatReferee` (Layer 1) can use `CombatState` (Layer 2), but `CombatState` should never reference `CombatReferee`.

**Benefit**: 
- Prevents circular dependencies at design time
- Makes architecture immediately visible
- New developers understand the structure instantly

---

### 2. **The Great Divide: State vs. MonoBehaviour**
**Problem**: Unity games often tightly couple game logic to GameObjects, making testing difficult and logic hard to follow.

**Solution**: Layer 2 (`State/`) contains **zero MonoBehaviour classes**. All game logic executes in pure C# classes.

**Architecture**:
```
CombatReferee (MonoBehaviour)
    ↓ owns
CombatState (pure C#)
    ↓ processes
Effects, Buffs, Characters (pure C# except Character.cs wrapper)
    ↓ broadcasts
EventProvider (pure C#)
    ↓ triggers
StageChoreographer, UIManager (MonoBehaviour)
```

**Benefits**:
- Game logic can be unit tested
- State can be serialized/replayed
- Combat could run headless on a server
- Presentation changes don't affect rules

---

### 3. **EventProvider Pattern (Observer)**
**Problem**: Systems need to react to game events without creating tight coupling.

**Solution**: `EventProvider` holds delegates for every significant game event. Systems subscribe to events they care about.

```csharp
// State layer broadcasts
_eventProvider.OnDamageResolved?.Invoke(calculatedDamage);

// Presentation layer reacts
_eventProvider.OnDamageResolved += HandleDamageResolved;
```

**Benefits**:
- UI, audio, VFX subscribe to same events
- Easy to add new listeners
- State layer doesn't know who's listening
- Can record/replay events for debugging

---

### 4. **Command Pattern via "Order" Objects**
**Problem**: Effects need to declare intent without immediately executing, allowing for interception/modification.

**Solution**: Lightweight data carriers for intent:
- `DamageOrder` - "I want to deal X damage"
- `ReviveOrder` - "I want to revive this character"
- `SummonOrder` - "I want to summon a unit"
- `ScaleOrder` - "I want to modify resources"

**Flow**:
```
Ability creates DamageOrder
    ↓ stored in
EffectPlan
    ↓ processed by
CombatState.ResolveDamageOrders()
    ↓ calculates via
DamageResolver
    ↓ produces
CalculatedDamage (result)
```

**Benefits**:
- Separation of intent from execution
- Orders can be inspected/modified before execution
- Results are separate objects
- Easy to log/debug entire effect chains

---

### 5. **Fluent Builder Pattern for EffectPlan**
**Problem**: Abilities need a clean way to declare multiple orders.

**Solution**: `EffectPlan` has `Add()` methods that return `this`, enabling chaining.

```csharp
var plan = new EffectPlan(caster, target, this);
plan.Add(new DamageOrder(...))
    .Add(new Buff(...))
    .Add(new ReviveOrder(...));
return plan;
```

**Benefits**:
- Readable ability definitions
- Easy to see what an ability does at a glance
- Extensible - new order types just need an `Add()` method

---

### 6. **ScriptableObjects for Data Configuration**
**Problem**: Character stats, abilities, and configuration shouldn't be hardcoded.

**Solution**: Unity ScriptableObjects for data:
- `CharacterConfig` - stats, abilities, team
- `PartyConfig` - party composition
- `EnemySetList` - wave definitions
- `StageConfig` - stage data

**Benefits**:
- Designers can modify without code
- Easy to create variants
- Serialized by Unity automatically
- Can be hot-swapped in editor

---

### 7. **Strategy Pattern for Effects**
**Problem**: Each ability has unique behavior but shares common structure.

**Solution**: Abstract `Effect` base class with single method:

```csharp
public abstract class Effect {
    public abstract EffectPlan GetUncommitted(
        Character source, 
        Character target, 
        List<Character> AllCombatants
    );
}
```

**Benefits**:
- Uniform interface for all abilities
- Core system doesn't care about specific abilities
- Easy to add new abilities
- Can store effects in collections polymorphically

---

### 8. **Two-Phase Buff System**
**Problem**: Some buffs trigger at turn start, others at turn end.

**Solution**: `AgingPhase` enum on buffs:
- `CHARACTERTURN_PREFLIGHT` - triggers before actions
- `CHARACTERTURN_CLEANUP` - triggers after turn ends

```csharp
public class BuffPoisoned : Buff {
    public BuffPoisoned(...) {
        AgingPhase = CombatPhase.CHARACTERTURN_CLEANUP;
    }
}
```

**Benefits**:
- Fine control over buff timing
- Prevents edge cases (e.g., poison killing before turn starts)
- Clear contract for buff designers

---

### 9. **Generic Character Counters**
**Problem**: Need per-wave state tracking for special mechanics.

**Solution**: `Character.GenericWaveCounter` - a flexible int that resets each wave.

**Use Case**: Prayer ability (resurrect once per wave)
```csharp
if (Prayer.GenericWaveCounter == 0) {
    // Cast Prayer
    Prayer.GenericWaveCounter = 1; // Mark as used
}
```

**Benefits**:
- Avoids creating specific fields for edge cases
- Resets automatically between waves
- Simple to understand

---

### 10. **Context Menu Debugging**
**Problem**: Need to test specific scenarios in Unity editor.

**Solution**: `[ContextMenu]` attributes for debugging:

```csharp
[ContextMenu("tell me your buffs")]
void tellmeyourbuffs() {
    foreach(var buff in Buffs) {
        Debug.Log(buff.Name + " " + buff.Charges);
    }
}

[ContextMenu("add stun buff")]
void GetStunned() {
    AddBuff(new BuffStunned(this, this, 1));
}
```

**Benefits**:
- Right-click character in Inspector → test scenarios
- No need to set up full combat to test mechanics
- Faster iteration

---

## 🔄 Dependency Graph

```
                    ┌──────────────────┐
                    │ CombatReferee    │
                    │ (Layer 1)        │
                    └────────┬─────────┘
                             │ owns
                    ┌────────▼─────────┐
                    │  CombatState     │
                    │  EventProvider   │
                    │  (Layer 2)       │
                    └────┬────────┬────┘
                         │        │
            ┌────────────┘        └────────────┐
            │                                   │
    ┌───────▼─────────┐              ┌─────────▼────────┐
    │  Effect Stack   │              │ Event Broadcasts │
    │  (Layer 4)      │              │                  │
    └───────┬─────────┘              └─────────┬────────┘
            │                                   │
    ┌───────▼─────────┐              ┌─────────▼────────┐
    │  Order Objects  │              │ StageChoreograph │
    │  Resolvers      │              │ UIManager        │
    │  (Layer 5)      │              │ (Layer 3)        │
    └─────────────────┘              └──────────────────┘
```

---

## 📦 What Makes This Reusable?

The core system is **highly portable** because:

1. **Layer 2 has zero Unity dependencies** - can run anywhere
2. **Clear interfaces** - `Effect`, `Buff`, `Order` objects
3. **Event-driven** - presentation is completely decoupled
4. **Data-driven** - ScriptableObjects could be JSON/XML
5. **Extensible** - new mechanics = new files, not modifications

### To Package This:

**Keep as Core**:
- Effect execution engine (`CombatState` renamed to `EffectEngine`)
- Base classes (`Effect`, `Buff`, `EffectPlan`)
- Order objects and resolvers
- `EventProvider` pattern
- Helper utilities

**Game-Specific**:
- Concrete abilities/buffs
- Character/party configuration
- Orchestration (combat flow specific to turn-based)
- UI/presentation layer

**Create Interfaces**:
- `IStatSheet` - abstract character stats
- `ITargetProvider` - abstract targeting logic
- `IResourceManager` - abstract power/mana/scale

This system could power turn-based RPGs, card games, auto-battlers, or even real-time combat with appropriate orchestration changes!
