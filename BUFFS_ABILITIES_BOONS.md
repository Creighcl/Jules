# Buffs, Abilities, Boons, and BuffEffects

## 🎯 Design Philosophy

This system implements **data-driven effect composition** where complex combat mechanics emerge from simple, composable building blocks. Each mechanic type has a clear role and predictable patterns.

---

## 🔮 Abilities

### **Design Philosophy**

Abilities are **actions that characters can perform**. They inherit from `Effect` and define:
1. **What happens** (via `GetUncommitted()`)
2. **Who can be targeted** (via `TargetScope`)
3. **Metadata** (name, description, portrait)

**Core Principle**: Abilities are **data builders**, not executors. They create `EffectPlan` objects that describe intent, then the `CombatState` processes them.

---

### **Anatomy of an Ability**

```csharp
public class AbilityFireball : Effect
{
    // ✅ Constructor: Set metadata
    public AbilityFireball() {
        Name = "Fireball";
        Description = "Deal fire damage to target enemy";
        TargetScope = EligibleTargetScopeType.ENEMY;
        IsUltimate = false;
        IsAbility = true;
    }

    // ✅ Core method: Build the effect plan
    public override EffectPlan GetUncommitted(
        Character source,      // Who is casting
        Character target,      // Primary target (can be null)
        List<Character> AllCombatants  // Full battlefield
    ) {
        var plan = new EffectPlan(source, target, this);
        
        // Roll for damage
        int damage = source.GetSpecialAttackRoll(false);
        
        // Add damage order
        plan.Add(new DamageOrder(source, target, damage, this));
        
        return plan;
    }
}
```

---

### **Ability Patterns**

#### **Pattern 1: Single Target Damage**

```csharp
public class AbilityBasicAttack : Effect
{
    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        plan.Add(new DamageOrder(
            source,
            target,
            source.GetBasicAttackRoll(),
            this
        ));
        
        return plan;
    }
}
```

**Use Case**: Standard attacks, spells

---

#### **Pattern 2: AOE (Area of Effect)**

```csharp
public class AbilityMeteorStorm : Effect
{
    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        // Get all enemies
        List<Character> enemies = CombatantListFilter.ByScope(
            AllCombatants,
            source,
            EligibleTargetScopeType.ENEMY
        );
        
        // Add damage for each
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

**Use Case**: Multi-target abilities, explosions

---

#### **Pattern 3: Damage + Debuff**

```csharp
public class AbilityPoison : Effect
{
    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        // Immediate damage
        plan.Add(new DamageOrder(
            source,
            target,
            source.GetSpecialAttackRoll(false),
            this
        ));
        
        // Ongoing poison
        plan.Add(new BuffPoisoned(source, target, 3, damagePerTurn: 5));
        
        return plan;
    }
}
```

**Use Case**: Status effect abilities, DOTs

---

#### **Pattern 4: Conditional Effects**

```csharp
public class AbilityShieldBash : Effect
{
    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        int damage = source.GetSpecialAttackRoll(false);
        plan.Add(new DamageOrder(source, target, damage, this));
        
        // 75% chance to stun
        bool stunLanded = TryChance(75);
        if (stunLanded) {
            plan.Add(new BuffStunned(source, target, 1));
        }
        
        return plan;
    }
}
```

**Use Case**: Chance-based effects, critical hits

---

#### **Pattern 5: Healing (Negative Damage)**

```csharp
public class AbilityHeal : Effect
{
    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        // Negative damage = healing
        plan.Add(new DamageOrder(
            source,
            target,
            -source.GetSpecialAttackRoll(true),  // true = is heal roll
            this
        ));
        
        return plan;
    }
}
```

**Use Case**: Healing spells, regeneration

---

#### **Pattern 6: Buff-Only (No Targeting)**

```csharp
public class AbilitySelfBuff : Effect
{
    public AbilitySelfBuff() {
        TargetScope = EligibleTargetScopeType.NONE; // No UI targeting
    }
    
    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, source, this);
        
        // Buff self
        plan.Add(new BuffStrengthen(source, source, 3));
        
        return plan;
    }
}
```

**Use Case**: Self-buffs, rage abilities

---

#### **Pattern 7: Upgrade-Aware Abilities**

```csharp
public class AbilityShieldBash : Effect
{
    int _attackLevel = 0;
    int _supportLevel = 0;
    
    public AbilityShieldBash(int AttackLevel = 0, int SupportLevel = 0) {
        _attackLevel = AttackLevel;
        _supportLevel = SupportLevel;
    }

    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        int damage = source.GetSpecialAttackRoll(false);
        
        // Upgrade: Increase damage
        if (_attackLevel > 1) {
            damage = (int)(damage * 1.25f);
        }
        
        plan.Add(new DamageOrder(source, target, damage, this));
        
        // Upgrade: Add AOE splash
        if (_attackLevel > 0) {
            List<Character> nearby = GetNearbyAlliesOfCharacter(target, AllCombatants);
            foreach (Character adjacent in nearby) {
                plan.Add(new DamageOrder(
                    source,
                    adjacent,
                    (int)(damage * 0.25f),
                    this
                ));
            }
        }
        
        return plan;
    }
}
```

**Use Case**: Abilities that scale with progression

---

### **Relationship to Stat Sheets**

Abilities **read from character stats** but **don't modify them directly**:

```csharp
// ✅ Read stats
int damage = source.GetSpecialAttackRoll(false);
int currentHP = target.currentHealth;
PowerType elementType = source.Config.PowerType;

// ❌ Don't modify directly
// source.currentHealth += 10; // BAD!

// ✅ Use orders instead
plan.Add(new DamageOrder(source, source, -10, this)); // Healing via damage order
```

**Why?** 
- Damage goes through `DamageResolver` (mitigation, stagger, buffs)
- All modifications are logged and broadcasted
- Allows interception and modification

---

## 🛡️ Buffs

### **Design Philosophy**

Buffs are **status effects** that persist over time. They:
1. **Modify character capabilities** (prevent actions, change stats)
2. **Trigger effects** at specific phases (poison ticks, regeneration)
3. **Age automatically** based on `AgingPhase`

**Core Principle**: Buffs are **passive modifiers** that the system queries. They don't actively execute logic; the system asks them "do you affect this?"

---

### **Anatomy of a Buff**

```csharp
public class BuffStunned : Buff
{
    public BuffStunned(Character src, Character tgt, int duration) 
        : base(src, tgt, duration)
    {
        Name = "Stunned";
        Description = "Cannot take any actions on their turn";
        AgingPhase = CombatPhase.CHARACTERTURN_CLEANUP; // Age at turn end
        isDebuff = true;
        PortraitArt = "bufficons/stun";
    }
    
    // No other methods needed - system queries HasBuff<BuffStunned>()
}
```

---

### **Buff Categories**

#### **Category 1: Capability Blockers**

Prevent certain actions by existing:

```csharp
// In Character.cs
public List<AbilityCategory> GetAvailableAbilities(int LightPoints, int ShadowPoints) {
    // Stunned = no actions
    if (HasBuff<BuffStunned>() || HasBuff<BuffSearingStun>()) {
        return new List<AbilityCategory>();
    }
    
    // Silenced/Charmed = basic attack only
    if (HasBuff<BuffSilenced>() || HasBuff<BuffCharmed>()) {
        return new List<AbilityCategory>(){ 
            AbilityCategory.BASICATTACK,
        };
    }
    
    // Normal abilities
    // ...
}
```

**Examples**:
- `BuffStunned` - No actions
- `BuffSilenced` - No special abilities
- `BuffCharmed` - Attack allies instead of enemies
- `BuffBlinded` - Reduced hit chance

---

#### **Category 2: Stat Modifiers**

Checked during calculations:

```csharp
// In DamageResolver.cs
int attackerRawDamage = rawDamage;

if (attacker.HasBuff<BuffWeakness>()) {
    attackerRawDamage = (int)(attackerRawDamage * 0.5f); // 50% damage
}

if (attacker.HasBuff<BuffStrengthen>()) {
    attackerRawDamage *= 2; // 200% damage
}

if (victim.HasBuff<BuffElementalVulnerability>()) {
    unmitigatedDamage = (int)(unmitigatedDamage * 1.25f); // Take 25% more
}
```

**Examples**:
- `BuffWeakness` - Deal 50% damage
- `BuffStrengthen` - Deal 200% damage
- `BuffElementalVulnerability` - Take 25% more damage
- `BuffShield` - Block incoming damage (see below)

---

#### **Category 3: Damage Shields**

Special handling with **charges**:

```csharp
public class BuffShield : Buff
{
    public BuffShield(Character src, Character tgt, int duration, int charges) 
        : base(src, tgt, duration, charges)
    {
        Name = "Shield";
        Description = "Prevents the next " + charges + " damage taken";
    }
}

// In Character.cs - TakeDamage()
if (HasBuff<BuffShield>()) {
    int shieldCharges = Buffs.First(buff => buff is BuffShield).Charges;
    
    if (shieldCharges > Damage) {
        // Shield absorbs all damage
        Buffs.First(buff => buff is BuffShield).Charges -= Damage;
        DamageToHealth = 0;
    } else {
        // Shield breaks, excess damage goes through
        DamageToHealth -= shieldCharges;
        Buffs.RemoveAll(buff => buff is BuffShield);
    }
}
```

---

#### **Category 4: Effect Triggers**

Have `ResolvePreflightEffects()` that returns an `EffectPlan`:

```csharp
public class BuffPoisoned : Buff
{
    public EffectFlatDotDamage DotAbility;
    
    public BuffPoisoned(Character src, Character tgt, int duration, int damage) 
        : base(src, tgt, duration)
    {
        Name = "Poisoned";
        Description = "Takes damage at the start of their turn";
        AgingPhase = CombatPhase.CHARACTERTURN_CLEANUP;
        DotAbility = new EffectFlatDotDamage(damage, "Poison Damage", "Enemy is Poisoned");
        isDebuff = true;
    }

    public override EffectPlan ResolvePreflightEffects() {
        // Return an effect plan to execute
        return DotAbility.GetUncommitted(Source, Target, null);
    }
}

// Called by CombatState at turn start:
public void ResolvePreflightBuffsForCurrentCombatant() {
    List<EffectPlan> effectPlans = CurrentCombatant
        .Buffs
        .Select(buff => buff.ResolvePreflightEffects())
        .Where(plan => plan != null)
        .ToList();
    
    ExecuteEffectList(effectPlans);
}
```

**Examples**:
- `BuffPoisoned` - Deal damage at turn start
- `BuffRegeneration` - Heal at turn start
- `BuffBurning` - Fire DOT

---

#### **Category 5: Reactive Triggers**

Checked by trigger systems in `CombatState`:

```csharp
public class BuffCounterattack : Buff
{
    public BuffCounterattack(Character src, Character tgt, int duration) 
        : base(src, tgt, duration)
    {
        Name = "Counterattack";
        Description = "When taking damage, this unit will attack the source back";
    }
}

// In CombatState.cs - ResolveDamageTriggers()
void ResolveDamageTriggers(EffectPlan _e) {
    foreach(CalculatedDamage dmg in _e.DamageResults) {
        if (dmg.Target.HasBuff<BuffCounterattack>()) {
            EffectPlan counterPlan = new AbilityCounterattack()
                .GetUncommitted(dmg.Target, dmg.Attacker, FullCombatantList);
            _e.Add(counterPlan);
        }
    }
}
```

**Examples**:
- `BuffCounterattack` - Strike back when hit
- `BuffVolcanicBowelSyndrome` - Explode on death
- `BuffPyroPeakboo` - Resurrect on death

---

### **Buff Lifecycle**

```
1. Buff created by ability
   ↓
2. Added to EffectPlan.BuffOrders
   ↓
3. CombatState.ResolveBuffAdditions() adds to character
   ↓
4. Character.AddBuff() - replaces existing if same type
   ↓
5. Buff exists on character
   ↓
6. System queries HasBuff<T>() during calculations
   ↓
7. ResolvePreflightEffects() called at turn start (if implemented)
   ↓
8. Character.AgeBuffsForPhase() called at AgingPhase
   ↓
9. Buff.Tick() reduces TurnsRemaining
   ↓
10. If TurnsRemaining < 1, buff removed, OnBuffExpired event
```

---

### **Relationship to Stat Sheets**

Buffs **query and modify stats indirectly**:

```csharp
// ✅ Query stat
if (victim.HasBuff<BuffShield>()) {
    int charges = victim.GetBuff<BuffShield>().Charges;
}

// ✅ Modify via effect plan
public override EffectPlan ResolvePreflightEffects() {
    return poisonEffect.GetUncommitted(Source, Target, null);
}

// ❌ Don't modify directly
// Target.currentHealth -= 10; // BAD!
```

---

## 🎁 Boons

### **Design Philosophy**

Boons are **persistent upgrades between waves**. They modify `CharacterConfig` directly and are **not part of combat execution**.

**Core Principle**: Boons are **meta-progression**. They affect character configuration before combat starts.

---

### **Anatomy of a Boon**

```csharp
public class BoonAttackRankUp : BaseBoonResolver
{
    public BoonAttackRankUp() {
        Name = "Attack Rank Up";
        Description = "Increase attack tree rank by 1";
        UpgradeType = "Attack";
        Character = PCAdventureClassType.WARRIOR;
    }

    public override void ApplyToEligible(List<CharacterConfig> characters) {
        foreach(CharacterConfig config in characters) {
            if (IsEligible(config)) {
                config.AttackTreeLevel++;
            }
        }
    }

    public override void RemoveFromOwning(List<CharacterConfig> characters) {
        foreach(CharacterConfig config in characters) {
            if (config.PlayerClass == Character) {
                config.AttackTreeLevel--;
            }
        }
    }

    public override bool IsEligible(CharacterConfig character) {
        return character.PlayerClass == Character 
            && character.AttackTreeLevel < 3;
    }
}
```

---

### **Boon Patterns**

#### **Stat Increases**
```csharp
public override void ApplyToEligible(List<CharacterConfig> characters) {
    foreach(CharacterConfig config in characters) {
        if (IsEligible(config)) {
            config.BaseHP += 10;
            config.BaseAttackMax += 5;
        }
    }
}
```

#### **Ability Unlocks**
```csharp
public override void ApplyToEligible(List<CharacterConfig> characters) {
    foreach(CharacterConfig config in characters) {
        if (IsEligible(config)) {
            config.SpecialAttack = UserAbilitySelection.FIREBALL;
        }
    }
}
```

#### **Tree Level Progression**
```csharp
// Attack tree: 0 → 1 → 2 → 3
// Support tree: 0 → 1 → 2 → 3
config.AttackTreeLevel++;
config.SupportTreeLevel++;
```

Abilities check these levels:
```csharp
public class AbilityShieldBash : Effect {
    int _attackLevel = 0;
    
    public AbilityShieldBash(int AttackLevel = 0) {
        _attackLevel = AttackLevel;
    }
    
    public override EffectPlan GetUncommitted(...) {
        // Different behavior at different levels
        if (_attackLevel > 1) {
            damage *= 1.25f;
        }
        
        if (_attackLevel > 2) {
            // Add AOE
        }
    }
}
```

---

### **Boon Flow**

```
Wave Complete
    ↓
CombatReferee broadcasts OnWaveComplete
    ↓
BoonLibrary.GetAvailableBoons(party)
    ↓
UI shows boon selection
    ↓
Player selects boon
    ↓
UI invokes OnInput_BoonSelected
    ↓
CombatReferee.HandleUserChoseBoon()
    ↓
Boon.ApplyToEligible(party)
    ↓
CharacterConfig modified
    ↓
Next wave starts with upgraded characters
```

---

### **Relationship to Stat Sheets**

Boons **directly modify `CharacterConfig`** (which is a ScriptableObject):

```csharp
// ✅ Direct modification (because it's meta-progression)
config.AttackTreeLevel++;
config.BaseHP += 10;
config.SpecialAttack = UserAbilitySelection.FIREBALL;

// Characters initialized from config at wave start
character.currentHealth = character.Config.BaseHP;
```

---

## ⚡ BuffEffects

### **Design Philosophy**

BuffEffects are **non-ability effects** - they inherit from `Effect` but are NOT player-selectable. Used by buffs for their triggered behaviors.

**Core Principle**: BuffEffects are **effect implementations for buffs**. They allow buffs to reuse the effect system without being abilities.

---

### **Anatomy of a BuffEffect**

```csharp
public class EffectFlatDotDamage : Effect
{
    int TickDamage = 0;
    
    public EffectFlatDotDamage(int tickDamage, string name = "Poison Effect", string description = "Poisoned Enemy does damage") {
        Name = name;
        Description = description;
        TickDamage = tickDamage;
        TargetScope = EligibleTargetScopeType.ANYALIVE;
        IsAbility = false; // ⭐ Not an ability!
    }

    public override EffectPlan GetUncommitted(Character source, Character target, List<Character> AllCombatants)
    {
        var plan = new EffectPlan(source, target, this);
        
        plan.Add(new DamageOrder(
            source,
            target,
            TickDamage,
            this
        ));
        
        return plan;
    }
}
```

---

### **Why Separate from Abilities?**

**`IsAbility = false`** has implications:

```csharp
// In CombatState - resource management
void AdjustScaleByAbilityCast(EffectPlan plan) {
    if (!plan.Source.IsAbility) return; // BuffEffects don't cost resources
    
    // Ability logic...
}

// In trigger systems
if (damage.Source is AbilityCounterattack) return; // Don't counter a counter
```

**Benefits**:
- DOT damage doesn't generate resources
- Triggered effects don't consume actions
- Clear distinction between player actions and system reactions

---

### **BuffEffect Patterns**

#### **DOT (Damage Over Time)**
```csharp
public class EffectFlatDotDamage : Effect {
    // Fixed damage each turn
}
```

#### **AOE Secondary Effects**
```csharp
public class EffectHeavyweightHeatwave : Effect {
    // Explosion damage from Heatwave buff
}
```

---

### **Relationship to Stat Sheets**

BuffEffects follow the same pattern as abilities:

```csharp
// ✅ Read stats
int damage = TickDamage;

// ✅ Create orders
plan.Add(new DamageOrder(source, target, damage, this));

// ❌ Don't modify directly
// target.currentHealth -= damage; // BAD!
```

---

## 📊 Summary Table

| Type | Purpose | Execution | Stat Relationship | Examples |
|------|---------|-----------|-------------------|----------|
| **Ability** | Player/AI actions | Via EffectPlan | Read stats, create orders | Fireball, Heal, Shield Bash |
| **Buff** | Status effects | Passive queries + optional triggers | Query stats, trigger effects | Stun, Poison, Shield, Counterattack |
| **Boon** | Meta-progression | Direct config modification | Directly modify CharacterConfig | Attack Rank Up, HP Increase |
| **BuffEffect** | Buff-triggered effects | Via EffectPlan (like abilities) | Read stats, create orders | DOT damage, Explosion effects |

---

## 🎯 Key Assumptions

### **For Abilities**
1. Always return an `EffectPlan` from `GetUncommitted()`
2. Never modify state directly - use orders
3. Can query any character stat
4. Can use `CombatantListFilter` for targeting
5. Can use `TryChance()` for randomness

### **For Buffs**
1. System will query `HasBuff<T>()` at appropriate times
2. `ResolvePreflightEffects()` called at turn start if implemented
3. `Tick()` called automatically at `AgingPhase`
4. Can have `Charges` that deplete per-trigger instead of per-turn
5. Replacing: Adding same buff type replaces existing

### **For Boons**
1. Applied between waves, not during combat
2. Modify `CharacterConfig` directly
3. Should implement `IsEligible()` for validation
4. Should implement `RemoveFromOwning()` for resets

### **For BuffEffects**
1. Set `IsAbility = false`
2. Follow same patterns as abilities
3. Used by buffs via `ResolvePreflightEffects()`
4. Don't consume resources or actions

---

This design creates a **flexible, data-driven combat system** where complex behaviors emerge from simple, composable pieces!
