# The Recursive Effect Stack System

## 🎯 Overview

The **Recursive Effect Stack** is the heart of this combat system. It's a chain-of-responsibility pattern that allows effects to spawn child effects, creating complex interactions while keeping individual ability implementations simple.

Think of it like **React's reconciliation** or **DOM event bubbling** - one action triggers a cascade of reactions, each processed in a deterministic order.

---

## 🏗️ Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│                        EffectPlan                           │
│  (The container for an entire effect execution)            │
├─────────────────────────────────────────────────────────────┤
│  Character Caster                                           │
│  Character Target                                           │
│  Effect Source                                              │
│                                                             │
│  List<DamageOrder> DamageOrders         ← Intent           │
│  List<Buff> BuffOrders                  ← Intent           │
│  List<ReviveOrder> ReviveOrders         ← Intent           │
│  List<SummonOrder> SummonOrders         ← Intent           │
│  List<ScaleOrder> ScaleOrders           ← Intent           │
│                                                             │
│  List<CalculatedDamage> DamageResults   ← Results          │
│  List<EffectPlan> EffectResponseOrders  ← ⭐ RECURSION!    │
└─────────────────────────────────────────────────────────────┘
```

### The Magic: `EffectResponseOrders`

This is the **key innovation**. An `EffectPlan` can contain **child EffectPlans** that get processed after the parent completes. This enables:

- Counterattacks when damaged
- Explosion on death
- Poison ticking at turn start
- Chain reactions from buffs

---

## 📊 Effect Processing Flowchart

```
┌─────────────────────────────────────────────────────────────────┐
│ ExecuteEffectPlan(EffectPlan plan)                              │
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  FinalizeEffectPlan()        │
        │  - Apply scale modifiers     │
        │  - Prepare orders            │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  Broadcast:                  │
        │  OnEffectPlanExecutionStart  │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  ResolveScaleOrders()        │
        │  - Modify Light/Shadow       │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  ResolveDamageOrders()       │
        │  - Call DamageResolver       │
        │  - Create CalculatedDamage   │
        │  - Store in DamageResults    │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────────────────┐
        │  ResolveDamageTriggers() ⭐               │
        │  - Check for BuffCounterattack           │
        │  - Check for BuffHeatwave charges        │
        │  - ADD CHILD EFFECTPLANS TO RESPONSES!   │
        └──────────┬───────────────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  ResolveBuffAdditions()      │
        │  - Apply buffs to targets    │
        │  - Broadcast OnBuffAdded     │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  ResolveReviveOrders()       │
        │  - Resurrect characters      │
        │  - Re-add to turn order      │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  ResolveSummonOrders()       │
        │  - Spawn new combatants      │
        │  - Add to battlefield        │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────────────────┐
        │  ResolveDeathTriggers() ⭐                │
        │  - Check for BuffVolcanicBowel           │
        │  - Check for BuffPyroPeakboo             │
        │  - ADD CHILD EFFECTPLANS TO RESPONSES!   │
        └──────────┬───────────────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────────────────┐
        │  IdentifyGlobalTriggers() ⭐              │
        │  - Check for Prayer (resurrect ability)  │
        │  - Check for Improved Counterattack      │
        │  - ADD CHILD EFFECTPLANS TO RESPONSES!   │
        └──────────┬───────────────────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │  Broadcast:                  │
        │  OnEffectPlanExecutionComplete│
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌─────────────────────────────────────────────┐
        │  ExecuteEffectList(EffectResponseOrders) ⭐  │
        │  FOR EACH child plan:                       │
        │    ExecuteEffectPlan(child)  ← RECURSION!   │
        └─────────────────────────────────────────────┘
                   │
                   ▼
             (Each child can have
              its own children,
              processed recursively)
```

---

## 🔥 Design Choices

### 1. **Why Separate Orders from Results?**

**Orders** = Intent
**Results** = Outcome

```csharp
DamageOrder order = new DamageOrder(attacker, victim, 100, ability);
// ↓ processed by
CalculatedDamage result = damageResolver.ResolveOrder(order);
// result might be 50 damage (after mitigation, stagger, etc.)
```

**Benefits**:
- Can inspect/modify orders before execution
- Results contain additional calculated data (e.g., `StaggerCrackedByThis`)
- Easy to log entire chain: "Order X produced Result Y"

---

### 2. **Why Process in This Specific Order?**

The sequence matters for game feel and logic:

```
1. Scale     → Modifies resources first (affects later calculations)
2. Damage    → Primary effect
3. DamageTriggers → React to damage (counterattacks)
4. Buffs     → Apply status effects
5. Revives   → Bring back characters
6. Summons   → Add new combatants
7. DeathTriggers → React to deaths (explosions, resurrections)
8. GlobalTriggers → Check for special conditions
9. Responses → Process ALL child effects recursively
```

**Example**: If a character dies from poison → their "explode on death" buff triggers → explosion damages enemies → enemy counterattacks → that counterattack triggers its own effects.

---

### 3. **Why Recursion Instead of a Queue?**

**Considered**: Maintain a global queue and process effects sequentially.

**Chosen**: Recursive processing with child EffectPlans.

**Reason**: 
- **Clarity**: Parent-child relationship is explicit
- **Scoping**: Child effects inherit context from parent
- **Debugging**: Call stack shows the entire chain
- **Event timing**: Can broadcast start/end for each sub-effect

**Safety**: No infinite loops because:
- Effects are created from concrete game actions (damage, buffs, etc.)
- Most effects don't create responses
- Response creators have conditions (e.g., "if has buff X")

---

### 4. **Why Store Both Orders AND Results?**

```csharp
List<DamageOrder> DamageOrders;      // What we wanted to do
List<CalculatedDamage> DamageResults; // What actually happened
```

**Reason**: 
- **Orders** = input to resolvers
- **Results** = used by trigger systems

**Example**:
```csharp
void ResolveDamageTriggers(EffectPlan plan) {
    foreach(CalculatedDamage dmg in plan.DamageResults) {
        if (dmg.Target.HasBuff<BuffCounterattack>()) {
            // Create response using result data
            plan.Add(new AbilityCounterattack().GetUncommitted(...));
        }
    }
}
```

We need the **result** (not the order) because we need to know:
- Did damage actually land?
- How much damage after mitigation?
- Did stagger crack?

---

### 5. **Why Three Trigger Points?**

```csharp
ResolveDamageTriggers()    // React to damage taken
ResolveDeathTriggers()     // React to death
IdentifyGlobalTriggers()   // React to game state
```

**Design**: Separate methods for different **trigger conditions**.

**Benefits**:
- Clear where to add new triggered effects
- Easy to understand when effects fire
- Can disable/modify specific trigger types

**Examples**:
- **DamageTriggers**: Counterattack, Heatwave charge reduction
- **DeathTriggers**: Volcanic Bowel explosion, Pyro Peakaboo resurrection
- **GlobalTriggers**: Prayer (resurrect ally), Improved Counterattack (team-wide)

---

## 🛠️ Developer Mechanisms

### For Game Developers: How to Use This System

#### **Creating a Simple Ability**

```csharp
public class AbilityFireball : Effect
{
    public AbilityFireball() {
        Name = "Fireball";
        Description = "Deal fire damage to target";
        TargetScope = EligibleTargetScopeType.ENEMY;
    }

    public override EffectPlan GetUncommitted(
        Character source, 
        Character target, 
        List<Character> AllCombatants
    ) {
        var plan = new EffectPlan(source, target, this);
        
        // Add a damage order
        plan.Add(new DamageOrder(
            source,
            target,
            source.GetSpecialAttackRoll(false),
            this
        ));
        
        return plan;
    }
}
```

**That's it!** The system handles:
- Damage calculation (mitigation, stagger)
- Triggering counterattacks if target has buff
- Broadcasting events for UI/VFX
- Processing any resulting child effects

---

#### **Creating an AOE Ability**

```csharp
public class AbilityMeteorStorm : Effect
{
    public override EffectPlan GetUncommitted(
        Character source, 
        Character target, 
        List<Character> AllCombatants
    ) {
        var plan = new EffectPlan(source, target, this);
        
        // Get all enemies
        List<Character> enemies = CombatantListFilter.ByScope(
            AllCombatants,
            source,
            EligibleTargetScopeType.ENEMY
        );
        
        // Add damage order for EACH enemy
        foreach(Character enemy in enemies) {
            plan.Add(new DamageOrder(
                source,
                enemy,
                source.GetSpecialAttackRoll(false),
                this
            ));
        }
        
        return plan;
    }
}
```

**All damage resolves in the same execution**, triggering effects as appropriate.

---

#### **Creating a Buff That Triggers Effects**

```csharp
public class BuffThorns : Buff
{
    int ReturnDamage;
    
    public BuffThorns(Character src, Character tgt, int duration, int damage) 
        : base(src, tgt, duration) 
    {
        Name = "Thorns";
        Description = "Return damage when attacked";
        AgingPhase = CombatPhase.CHARACTERTURN_CLEANUP;
        ReturnDamage = damage;
    }
}
```

**Then in CombatState.cs**:
```csharp
void ResolveDamageTriggers(EffectPlan _e) {
    foreach(CalculatedDamage dmg in _e.DamageResults) {
        if (dmg.Target.HasBuff<BuffThorns>()) {
            BuffThorns thorns = dmg.Target.GetBuff<BuffThorns>();
            
            // Create a damage effect back at attacker
            var thornsDamage = new EffectFlatDotDamage(
                thorns.ReturnDamage,
                "Thorns Damage",
                "Reflected by thorns"
            );
            
            // Add as child effect
            _e.Add(thornsDamage.GetUncommitted(
                dmg.Target,
                dmg.Attacker,
                AllCombatants
            ));
        }
    }
}
```

**The child effect processes automatically** after the parent completes.

---

#### **Creating a Buff That Activates at Turn Start**

```csharp
public class BuffRegeneration : Buff
{
    int HealPerTurn;
    
    public BuffRegeneration(Character src, Character tgt, int duration, int heal) 
        : base(src, tgt, duration) 
    {
        Name = "Regeneration";
        Description = "Heal at start of turn";
        AgingPhase = CombatPhase.CHARACTERTURN_CLEANUP;
        HealPerTurn = heal;
    }

    // This method is called during PREFLIGHT phase
    public override EffectPlan ResolvePreflightEffects() {
        var healEffect = new EffectFlatDotDamage(
            -HealPerTurn,  // Negative damage = healing
            "Regeneration",
            "Healing from regeneration buff"
        );
        
        return healEffect.GetUncommitted(Source, Target, null);
    }
}
```

**Called automatically** by `CombatState.ResolvePreflightBuffsForCurrentCombatant()`.

---

#### **Creating Complex Interactions**

Example: **"When you kill an enemy, summon a skeleton"**

```csharp
void ResolveDeathTriggers(EffectPlan _e) {
    foreach(CalculatedDamage dmg in _e.DamageResults) {
        if (!dmg.Target.isDead) continue;
        
        if (dmg.Attacker.HasBuff<BuffNecromancer>()) {
            // Summon a skeleton
            _e.Add(new SummonOrder(
                SummonableUnit.SKELETON,
                dmg.Attacker.Config.TeamType,
                dmg.Source
            ));
        }
    }
}
```

**The summon happens automatically** as part of the effect chain.

---

## 🎮 Available Order Types

Game developers can use these orders in `EffectPlan`:

### **DamageOrder**
```csharp
plan.Add(new DamageOrder(attacker, victim, rawDamage, sourceEffect));
```
- Automatically calculates mitigation, stagger, crits
- Produces `CalculatedDamage` result
- Triggers damage-based responses

### **Buff**
```csharp
plan.Add(new BuffStunned(source, target, duration));
```
- Applied to target's buff list
- Auto-ages based on `AgingPhase`
- Can have `ResolvePreflightEffects()` for turn-start behavior

### **ReviveOrder**
```csharp
plan.Add(new ReviveOrder(character, percentHealth, sourceEffect));
```
- Resurrects dead character
- Restores health percentage
- Adds back to turn order

### **SummonOrder**
```csharp
plan.Add(new SummonOrder(unitType, team, sourceEffect));
```
- Spawns new combatant
- Finds open battlefield position
- Adds to turn order

### **ScaleOrder**
```csharp
plan.Add(new ScaleOrder(lightDelta, shadowDelta));
```
- Modifies resource pools
- Clamped to valid ranges
- Broadcasts `OnScaleChanged` event

### **EffectPlan (nested)**
```csharp
plan.Add(anotherAbility.GetUncommitted(source, target, allCombatants));
```
- Child effect processes after parent
- Can have its own children
- Fully recursive

---

## 🔍 Debugging the Effect Stack

### Logging an Entire Chain

```csharp
void ExecuteEffectPlan(EffectPlan plan) {
    Debug.Log($"▶ Executing: {plan.Source.Name}");
    Debug.Log($"  Caster: {plan.Caster.Config.Name}");
    Debug.Log($"  Orders: {plan.DamageOrders.Count} damage, " +
              $"{plan.BuffOrders.Count} buffs");
    
    // ... processing ...
    
    Debug.Log($"  Results: {plan.DamageResults.Count} damage resolved");
    Debug.Log($"  Responses: {plan.EffectResponseOrders.Count} child effects");
}
```

### Visualizing Depth

```csharp
void ExecuteEffectPlan(EffectPlan plan, int depth = 0) {
    string indent = new string(' ', depth * 2);
    Debug.Log($"{indent}▶ {plan.Source.Name}");
    
    // ... processing ...
    
    foreach(var child in plan.EffectResponseOrders) {
        ExecuteEffectPlan(child, depth + 1);
    }
}
```

Output:
```
▶ Fireball
  ▶ Counterattack
    ▶ Thorns Damage
▶ Poison Tick
```

---

## 🧪 Edge Cases Handled

### **Infinite Loops**
**Prevention**: Effects are concrete actions, not conditions. A counterattack doesn't trigger another counterattack.

```csharp
if (damage.Source is AbilityCounterattack) return; // Don't counter a counter
```

### **Death During Processing**
**Handling**: Death triggers checked **after** all damage resolves, preventing state conflicts.

### **Buffs Expiring Mid-Effect**
**Handling**: Buffs checked at specific trigger points, not continuously.

### **Turn Order Changes**
**Handling**: Turn order purges dead combatants automatically before dequeuing.

---

## 🚀 Performance Considerations

- **Typical depth**: 1-3 levels (rare to go deeper)
- **Orders**: Usually 1-5 per effect
- **List allocations**: Minimal (Lists pre-allocated in EffectPlan)
- **No heap allocations** during recursion (just stack frames)

**Optimization opportunities**:
- Object pooling for Order objects
- Pre-allocate EffectPlan lists with capacity
- Cache filter results in `CombatantListFilter`

---

## 📚 Summary

The Recursive Effect Stack is an **elegant solution** to complex combat interactions:

✅ **Simple** - Individual effects are just data builders  
✅ **Powerful** - Complex chains emerge from simple rules  
✅ **Extensible** - New effects/buffs just inherit and implement  
✅ **Debuggable** - Clear parent-child relationships  
✅ **Performant** - Minimal overhead, deterministic processing  

This pattern could be applied to card games (combo chains), puzzle games (cascade effects), or any system where actions trigger reactions!
