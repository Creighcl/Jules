# Combat System Documentation

## 📚 Documentation Index

This folder contains comprehensive documentation for the combat system architecture. Read these in order for a complete understanding, or jump to specific topics as needed.

---

## 🎯 Quick Start

**New to this system?** Start here:
1. [Architecture Overview](./ARCHITECTURE_OVERVIEW.md) - Folder structure and design philosophy
2. [Effect Stack Deep Dive](./EFFECT_STACK_DEEP_DIVE.md) - The heart of the combat system
3. [Combat Flow and Core Logic](./COMBAT_FLOW_AND_CORE_LOGIC.md) - Where the game logic lives

**Building new content?** Reference these:
- [Buffs, Abilities, Boons](./BUFFS_ABILITIES_BOONS.md) - How to create new mechanics
- [Enums and Helpers](./ENUMS_AND_HELPERS.md) - Utility reference

**Understanding the architecture?** Deep dives:
- [Event System Architecture](./EVENT_SYSTEM_ARCHITECTURE.md) - How everything communicates
- [MonoBehaviour Organization](./MONOBEHAVIOUR_ORGANIZATION.md) - The Great Divide explained

---

## 📖 Document Summaries

### [ARCHITECTURE_OVERVIEW.md](./ARCHITECTURE_OVERVIEW.md)
**Topics**:
- Numbered folder hierarchy (1-5)
- The Great Divide (game logic vs presentation)
- Key design patterns and their benefits
- Dependency flow diagram
- What makes this system reusable

**Read this if you want to understand**: Why the code is organized this way and the overall system philosophy.

---

### [EFFECT_STACK_DEEP_DIVE.md](./EFFECT_STACK_DEEP_DIVE.md)
**Topics**:
- The recursive effect processing system
- Complete flowchart of effect execution
- How abilities spawn child effects
- Design choices and edge case handling
- Developer mechanisms for creating effects

**Read this if you want to understand**: How a single ability can trigger a cascade of reactions (counterattacks, death explosions, etc.).

---

### [EVENT_SYSTEM_ARCHITECTURE.md](./EVENT_SYSTEM_ARCHITECTURE.md)
**Topics**:
- Two-layer event system design
- EventProvider pattern
- All event types and their uses
- Event-driven vs direct calls comparison
- Debugging and performance considerations

**Read this if you want to understand**: How game logic broadcasts events and presentation layers react without tight coupling.

---

### [ENUMS_AND_HELPERS.md](./ENUMS_AND_HELPERS.md)
**Topics**:
- All enums with detailed explanations
- Helper class reference (CombatantListFilter, DamageResolver, etc.)
- Usage examples and patterns
- Order objects (DamageOrder, ReviveOrder, etc.)

**Read this if you want to understand**: What utilities are available and how to use them in your code.

---

### [MONOBEHAVIOUR_ORGANIZATION.md](./MONOBEHAVIOUR_ORGANIZATION.md)
**Topics**:
- The Great Divide principle
- MonoBehaviour inventory and responsibilities
- Interface injection pattern
- Testing strategy (pure logic vs integration)
- Why certain classes are MonoBehaviours

**Read this if you want to understand**: How Unity integration works while keeping core logic testable and portable.

---

### [BUFFS_ABILITIES_BOONS.md](./BUFFS_ABILITIES_BOONS.md)
**Topics**:
- Design philosophy for each mechanic type
- Complete pattern library with examples
- How each type relates to stat sheets
- Assumptions and contracts
- Code templates for new content

**Read this if you want to**: Create new abilities, buffs, or upgrade mechanics.

---

### [COMBAT_FLOW_AND_CORE_LOGIC.md](./COMBAT_FLOW_AND_CORE_LOGIC.md)
**Topics**:
- Where the deep game logic lives (two layers)
- Turn-based state machine breakdown
- Stat sheet processing pipeline
- How to adapt for different combat types (real-time, cards, auto-battler)
- What to keep vs replace for new games

**Read this if you want to**: Understand the combat loop or adapt this system for a different game type.

---

## 🎨 System Highlights

### **Recursive Effect Stack**
The crown jewel - effects can spawn child effects that process recursively, enabling complex chains:
```
Player casts Fireball
  → Enemy takes damage
    → Enemy has Counterattack buff
      → Enemy attacks back
        → Player has Thorns buff
          → Enemy takes thorns damage
```

### **The Great Divide**
Pure C# game logic separated from Unity MonoBehaviours:
- **Layer 2 (State)**: Zero Unity dependencies, fully testable
- **Layer 3 (Systems)**: Unity presentation, subscribes to events
- Communication via EventProvider (observer pattern)

### **Event-Driven Architecture**
Game state broadcasts what happened, multiple systems react:
```
CombatState → OnDamageResolved →  ┬→ StageChoreographer (animation)
                                  ├→ UIManager (HP bar)
                                  ├→ AudioManager (sound)
                                  └→ CombatReferee (check win)
```

### **Data-Driven Design**
ScriptableObjects configure characters, abilities map to enums, buffs are composable modifiers.

---

## 🚀 Common Tasks

### **Creating a New Ability**
1. Read: [BUFFS_ABILITIES_BOONS.md](./BUFFS_ABILITIES_BOONS.md) - "Abilities" section
2. Create class inheriting from `Effect`
3. Implement `GetUncommitted()` returning an `EffectPlan`
4. Use patterns: single target, AOE, conditional, etc.

### **Creating a New Buff**
1. Read: [BUFFS_ABILITIES_BOONS.md](./BUFFS_ABILITIES_BOONS.md) - "Buffs" section
2. Create class inheriting from `Buff`
3. Set `AgingPhase` in constructor
4. Optionally implement `ResolvePreflightEffects()` for turn-start triggers
5. System will query `HasBuff<YourBuff>()` automatically

### **Understanding Damage Calculation**
1. Read: [COMBAT_FLOW_AND_CORE_LOGIC.md](./COMBAT_FLOW_AND_CORE_LOGIC.md) - "Stat Sheet Processing"
2. Look at `DamageResolver.ResolveOrder()` in code
3. See [ENUMS_AND_HELPERS.md](./ENUMS_AND_HELPERS.md) - "DamageResolver" section

### **Adding a New Event**
1. Read: [EVENT_SYSTEM_ARCHITECTURE.md](./EVENT_SYSTEM_ARCHITECTURE.md) - "Event Categories"
2. Add delegate type to `EventProvider.cs`
3. Invoke in game logic layer
4. Subscribe in presentation layer

### **Adapting for Real-Time Combat**
1. Read: [COMBAT_FLOW_AND_CORE_LOGIC.md](./COMBAT_FLOW_AND_CORE_LOGIC.md) - "For Your Next Game"
2. Keep: `CombatState`, `EventProvider`, effect processing
3. Replace: `CombatReferee.ExecuteGameLogicForPhase()` with your flow
4. Call `combatState.ExecuteEffectPlan()` when abilities fire

---

## 🔍 Design Patterns Used

- **Observer Pattern**: EventProvider for decoupled communication
- **Command Pattern**: Order objects (DamageOrder, ReviveOrder, etc.)
- **Strategy Pattern**: Effect base class with polymorphic abilities
- **Builder Pattern**: EffectPlan with fluent interface
- **Chain of Responsibility**: Recursive effect processing
- **Facade Pattern**: CombatReferee simplifying subsystem access
- **Mediator Pattern**: EventProvider coordinating between systems
- **Dependency Injection**: Interfaces for Unity dependencies

---

## 🎯 Core Principles

1. **Separation of Concerns**: Each layer has a clear, single responsibility
2. **Dependency Flow**: Higher-numbered folders depend on lower, never reverse
3. **Event-Driven**: State broadcasts, systems react
4. **Testability**: Core logic is pure C#, no Unity required
5. **Extensibility**: New content = new files, not modifications
6. **Data-Driven**: Configuration via ScriptableObjects and enums
7. **Composability**: Complex behavior from simple building blocks

---

## 💡 Tips for New Developers

1. **Start with the flowcharts** in EFFECT_STACK_DEEP_DIVE.md to visualize execution
2. **Reference the pattern libraries** in BUFFS_ABILITIES_BOONS.md when creating content
3. **Use context menus** on Characters in Unity Inspector for testing
4. **Subscribe to events** for debugging - see what's firing when
5. **The number prefixes matter** - they show dependency direction
6. **Layer 2 is pure C#** - if you need Unity types there, use interfaces
7. **Read the "Why" sections** - they explain the architectural choices

---

## 🛠️ Extending the System

This system is designed to be **highly extensible**:

- **New Abilities**: Just inherit from `Effect`
- **New Buffs**: Just inherit from `Buff`
- **New Order Types**: Create new order class, add `Add()` method to `EffectPlan`
- **New Events**: Add delegate to `EventProvider`
- **New Triggers**: Add method to `CombatState.ExecuteEffectPlan()`
- **New UI**: Subscribe to `EventProvider` events
- **New Game Mode**: Replace `CombatReferee`, keep everything else

---

## 📦 Packaging as Reusable System

To extract as a standalone package:

**Core (Keep)**:
- `CombatState.cs` (rename to `EffectEngine`)
- `EventProvider.cs`
- `Effect.cs`, `Buff.cs`, `EffectPlan.cs`
- Order objects and helpers
- `DamageResolver.cs`, `CombatantListFilter.cs`

**Game-Specific (Leave Behind)**:
- `CombatReferee.cs` (turn-based orchestration)
- Concrete abilities/buffs
- UI/presentation systems
- `CharacterConfig` (ScriptableObject structure)

**Create Interfaces**:
- `IStatSheet` - abstract character data
- `IEffectExecutor` - abstract effect processing
- `ITargetProvider` - abstract targeting

See: [ARCHITECTURE_OVERVIEW.md](./ARCHITECTURE_OVERVIEW.md) - "What Makes This Reusable?"

---

## 🎓 Learning Path

**Day 1**: Architecture fundamentals
1. [ARCHITECTURE_OVERVIEW.md](./ARCHITECTURE_OVERVIEW.md)
2. [MONOBEHAVIOUR_ORGANIZATION.md](./MONOBEHAVIOUR_ORGANIZATION.md)

**Day 2**: Core systems
1. [EFFECT_STACK_DEEP_DIVE.md](./EFFECT_STACK_DEEP_DIVE.md)
2. [EVENT_SYSTEM_ARCHITECTURE.md](./EVENT_SYSTEM_ARCHITECTURE.md)

**Day 3**: Content creation
1. [BUFFS_ABILITIES_BOONS.md](./BUFFS_ABILITIES_BOONS.md)
2. [ENUMS_AND_HELPERS.md](./ENUMS_AND_HELPERS.md)

**Day 4**: Advanced topics
1. [COMBAT_FLOW_AND_CORE_LOGIC.md](./COMBAT_FLOW_AND_CORE_LOGIC.md)
2. Experiment with creating new abilities/buffs

---

## 🙏 Credits

This combat system represents careful architectural design and years of game development experience distilled into a reusable, elegant system. The recursive effect stack, event-driven architecture, and Great Divide separation make it one of the most well-architected combat systems I've seen.

---

## 📝 Document Maintenance

These documents were generated on November 30, 2025 based on the existing codebase. As the system evolves:

- Update docs when adding major new patterns
- Add examples for common use cases
- Keep flowcharts synchronized with code
- Document breaking changes clearly

---

**Happy coding! May your effects cascade beautifully! ⚡✨**
