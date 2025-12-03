# Combat Flow & Core Game Logic Location

## 🎯 Where Does The Deep Game Logic Live?

### **The Answer: It's Split Across Two Layers**

```
┌─────────────────────────────────────────────────────────────┐
│  LAYER 1: Combat Flow (Turn-Based Orchestration)           │
│  Location: CombatReferee.cs                                │
│  Responsibility: WHEN things happen                        │
│  Type: Turn-based state machine (MonoBehaviour)            │
└─────────────────────────────────────────────────────────────┘
                         ↓ calls ↓
┌─────────────────────────────────────────────────────────────┐
│  LAYER 2: Combat Rules (Stat Sheet Processing)             │
│  Location: CombatState.cs, ResourceChangeResolver.cs       │
│  Responsibility: HOW things happen                         │
│  Type: Pure C# Domain Logic                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 Layer 1: Combat Flow (CombatReferee)

**Location**: `1 - Orchestration/CombatReferee.cs`

**Purpose**: Manages **WHEN** things happen. This is your **turn-based game loop**.

### **The State Machine**

```csharp
public enum CombatPhase {
    INIT,
    WAVE_SETUP,
    CHARACTERTURN_PREFLIGHT,
    CHARACTERTURN_CHOOSEABILITY,
    CHARACTERTURN_CHOOSETARGET,
    CHARACTERTURN_EXECUTION,
    CHARACTERTURN_CLEANUP,
    CHARACTERTURN_HANDOFF,
    WAVE_COMPLETE
}
```

(The Core Loop remains similar to original documentation, orchestrating `CombatState`)

---

## ⚙️ Layer 2: Combat Rules (CombatState)

**Location**: `2 - State/CombatState.cs`

**Purpose**: Manages **HOW** things happen. This is your **stat sheet processor**.

### **Core Methods**

#### **1. Effect Execution**
```csharp
public void ExecuteSelectedAbility() {
    var completedAbility = AbilitySelected.GetUncommitted(
        CurrentCombatant,
        TargetSelected,
        FullCombatantList
    );

    // ⭐ Process the effect plan
    ExecuteEffectPlan(completedAbility);
}
```

#### **2. Damage Resolution (`ResourceChangeResolver`)**
**Location**: `Systems/Resources/ResourceChangeResolver.cs`

```csharp
void ResolveDamageOrders(EffectPlan _e) {
    // Injected rule dependency allows pure logic testing
    ResourceChangeResolver dr = new ResourceChangeResolver(InteractionRule);
    
    _e.DamageOrders.ForEach(damage => {
        // ⭐ Deep stat calculation
        CalculatedDamage dmgResult = dr.ResolveOrder(damage);
        _e.Add(dmgResult);
    });
}
```

---

## 📊 The Stat Sheet Processing

**Location**: `Systems/Resources/ResourceChangeResolver.cs` + `2 - State/Character.cs` (Pure Model)

### **ResourceChangeResolver - The Core Calculation**

*   Handles Element Interaction Rules (via `IElementInteractionRule`).
*   Calculates raw damage, mitigation, and final health/stagger impact.
*   Is purely functional (inputs -> outputs).

### **Character - The Pure Model**

```csharp
public class Character {
    // ⭐ Stat sheet data
    public ICharacterConfig Config { get; private set; }
    public Dictionary<IResourceType, Resource> Resources;
    
    // ⭐ Pure Logic (No Unity Dependencies)
    public void TakeDamage(int Damage) {
       // Updates internal state
    }
}
```

### **CharacterBehavior - The Unity Wrapper**

**Location**: `2 - State/CharacterBehavior.cs`

```csharp
public class CharacterBehavior : MonoBehaviour {
    public Character Model { get; private set; } // Holds the pure logic object
    
    // Forwards events/calls to the Model
    // Visualizes changes (Animations, UI)
}
```
