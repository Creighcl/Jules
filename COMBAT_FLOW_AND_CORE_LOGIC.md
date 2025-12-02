# Combat Flow & Core Game Logic Location

## 🎯 Where Does The Deep Game Logic Live?

### **The Answer: It's Split Across Two Layers**

```
┌─────────────────────────────────────────────────────────────┐
│  LAYER 1: Combat Flow (Turn-Based Orchestration)           │
│  Location: CombatReferee.cs                                │
│  Responsibility: WHEN things happen                        │
│  Type: Turn-based state machine                            │
└─────────────────────────────────────────────────────────────┘
                         ↓ calls ↓
┌─────────────────────────────────────────────────────────────┐
│  LAYER 2: Combat Rules (Stat Sheet Processing)             │
│  Location: CombatState.cs, DamageResolver.cs               │
│  Responsibility: HOW things happen                         │
│  Type: Pure calculation & effect processing                │
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

### **The Core Loop**

```csharp
CombatPhase ExecuteGameLogicForPhase(CombatPhase CurrentPhase) {
    CombatPhase NextPhase = CombatPhase.INIT;
    
    switch(CurrentPhase) {
        case CombatPhase.INIT:
            NextPhase = CombatPhase.WAVE_SETUP;
            break;
            
        case CombatPhase.WAVE_SETUP:
            NextPhase = CombatPhase.CHARACTERTURN_PREFLIGHT;
            break;
            
        case CombatPhase.CHARACTERTURN_PREFLIGHT:
            // ⭐ Call stat sheet processing
            combatState.ResolvePreflightBuffsForCurrentCombatant();
            
            NextPhase = CombatPhase.CHARACTERTURN_CHOOSEABILITY;
            break;
            
        case CombatPhase.CHARACTERTURN_CHOOSEABILITY:
            // Wait for player/AI to choose ability
            CombatAwaitingUser = true;
            
            if (combatState.CurrentCombatant.Config.TeamType == TeamType.CPU) {
                DoCpuAbilityChoice();
            }
            
            NextPhase = CombatPhase.CHARACTERTURN_CHOOSETARGET;
            break;
            
        case CombatPhase.CHARACTERTURN_CHOOSETARGET:
            // Wait for player/AI to choose target
            CombatAwaitingUser = true;
            
            if (isCpuTurn) {
                DoCpuTargetChoice();
            }
            
            NextPhase = CombatPhase.CHARACTERTURN_EXECUTION;
            break;
            
        case CombatPhase.CHARACTERTURN_EXECUTION:
            // ⭐ Call stat sheet processing
            combatState.ExecuteSelectedAbility();
            
            NextPhase = CombatPhase.CHARACTERTURN_CLEANUP;
            break;
            
        case CombatPhase.CHARACTERTURN_CLEANUP:
            combatState.CurrentCombatant.TurnEnd();
            
            // Age buffs
            List<Buff> agedOutBuffs = combatState.CurrentCombatant
                .AgeBuffsForPhase(CurrentPhase);
            
            NextPhase = CombatPhase.CHARACTERTURN_HANDOFF;
            break;
            
        case CombatPhase.CHARACTERTURN_HANDOFF:
            // ⭐ Call turn order management
            combatState.MoveToNextCombatant();
            
            NextPhase = CombatPhase.CHARACTERTURN_PREFLIGHT;
            break;
    }
    
    return NextPhase;
}
```

### **The Driver Coroutine**

```csharp
IEnumerator CombatPhaseDriver() {
    CombatPhase nextPhase = CombatPhase.INIT;
    bool CombatIsComplete = false;

    while (!CombatIsComplete) {
        // Wait for animations
        while (__uiManager.IsPerforming || __stageChoreographer.IsPerforming()) {
            yield return new WaitForSeconds(0.1f);
        }
        
        CurrentCombatPhase = nextPhase;
        
        // Broadcast phase start
        eventProvider.OnPhaseAwake?.Invoke(CurrentCombatPhase, combatState.CurrentCombatant);
        
        // Execute phase logic
        nextPhase = ExecuteGameLogicForPhase(CurrentCombatPhase);
        
        // Check win conditions
        if (CheckCombatWinConditions() != CombatResult.IN_PROGRESS) {
            CombatIsComplete = true;
            eventProvider.OnCombatHasEnded?.Invoke();
        }
        
        // Wait for user input if needed
        while (CombatAwaitingUser) {
            yield return new WaitForSeconds(0.1f);
        }
    }
}
```

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

    // Adjust resources
    AdjustScaleByAbilityCast(completedAbility);
    _eventProvider.OnScaleChanged?.Invoke(LightPoints, ShadowPoints);
}
```

#### **2. The Effect Processing Pipeline**
```csharp
void ExecuteEffectPlan(EffectPlan executionPlan) {
    FinalizeEffectPlan(executionPlan);
    
    _eventProvider.OnEffectPlanExecutionStart?.Invoke(executionPlan);
    
    // ⭐ Process in specific order
    ResolveScaleOrders(executionPlan);
    ResolveDamageOrders(executionPlan);        // Calculate damage
    ResolveDamageTriggers(executionPlan);      // React to damage
    ResolveBuffAdditions(executionPlan);       // Apply buffs
    ResolveReviveOrders(executionPlan);        // Resurrect
    ResolveSummonOrders(executionPlan);        // Spawn units
    ResolveDeathTriggers(executionPlan);       // React to deaths
    IdentifyGlobalTriggers(executionPlan);     // Check special conditions
    
    _eventProvider.OnEffectPlanExecutionComplete?.Invoke(executionPlan);
    
    // ⭐ Recursive processing
    ExecuteEffectList(executionPlan.EffectResponseOrders);
}
```

#### **3. Damage Resolution**
```csharp
void ResolveDamageOrders(EffectPlan _e) {
    DamageResolver dr = new DamageResolver();
    
    _e.DamageOrders.ForEach(damage => {
        // ⭐ Deep stat calculation
        CalculatedDamage dmgResult = dr.ResolveOrder(damage);
        
        _e.Add(dmgResult);
        _eventProvider.OnDamageResolved?.Invoke(dmgResult);
    });
}
```

---

## 📊 The Stat Sheet Processing

**Location**: `5 - Helpers/DamageResolver.cs` + `2 - State/Character.cs`

### **DamageResolver - The Core Calculation**

```csharp
public class DamageResolver {
    public CalculatedDamage ResolveOrder(DamageOrder order) {
        Character attacker = order.Attacker;
        Character victim = order.Victim;
        int rawDamage = order.RawDamage;

        // ⭐ STEP 1: Apply attacker buffs
        int attackerRawDamage = rawDamage;
        if (attacker.HasBuff<BuffWeakness>()) {
            attackerRawDamage = (int)(attackerRawDamage * 0.5f);
        }
        if (attacker.HasBuff<BuffStrengthen>()) {
            attackerRawDamage *= 2;
        }

        // ⭐ STEP 2: Calculate unmitigated damage
        int unmitigatedDamage = GetUnmitigatedDamageFromRaw(
            attackerRawDamage,
            victim,
            GetPowerTypeOfCharacter(attacker)
        );

        // ⭐ STEP 3: Apply vulnerability
        bool IsVulnerableToAttack = victim.HasBuff<BuffElementalVulnerability>() 
            && !IsVictimResistantToPowerType(victim, GetPowerTypeOfCharacter(attacker));
        
        if (IsVulnerableToAttack) {
            unmitigatedDamage = (int)(unmitigatedDamage * 1.25f);
        }

        // ⭐ STEP 4: Check stagger state
        bool CharacterIsCracked = victim.currentStagger <= attackerRawDamage;
        
        int FinalDamageToHealth = unmitigatedDamage;
        if (!CharacterIsCracked) {
            FinalDamageToHealth = (int)(unmitigatedDamage / 2f); // Half damage if not cracked
        }

        // ⭐ STEP 5: Apply to character
        victim.TakeDamage(FinalDamageToHealth);
        victim.TakeStagger(attackerRawDamage);

        return new CalculatedDamage(...);
    }
}
```

### **Character - Stat Sheet Queries**

```csharp
public class Character : MonoBehaviour {
    // ⭐ Stat sheet data
    public int currentHealth = 1;
    public int currentStagger = 0;
    public bool isDead = false;
    public List<Buff> Buffs = new List<Buff>();
    
    // ⭐ Stat-based calculations
    public int GetBasicAttackRoll() {
        bool HIT_SUCCESSFUL = TryChance(GetHitChance(false));
        if (!HIT_SUCCESSFUL) return 0;
        
        if (HasBuff<BuffPolymorph>()) return 1;
        
        bool DidCrit = TryChance(GetCriticalRollChance());
        int damage = UnityEngine.Random.Range(Config.BaseAttackMin, Config.BaseAttackMax);
        
        if (DidCrit) {
            damage = (int)(damage * GetCriticalHitModifier());
        }
        
        return damage;
    }
    
    // ⭐ Buff queries
    public bool HasBuff<T>() where T : Buff {
        return Buffs.Any(buff => buff is T);
    }
    
    public Buff GetBuff<T>() where T : Buff {
        return Buffs.FirstOrDefault(buff => buff is T);
    }
    
    // ⭐ Available actions based on state
    public List<AbilityCategory> GetAvailableAbilities(int LightPoints, int ShadowPoints) {
        if (HasBuff<BuffStunned>()) {
            return new List<AbilityCategory>(); // Can't act
        }
        
        if (HasBuff<BuffSilenced>()) {
            return new List<AbilityCategory>(){ 
                AbilityCategory.BASICATTACK, // Basic only
            };
        }
        
        // Normal logic...
    }
}
```

---

## 🎮 For Your Next Game: How to Adapt

### **✂️ What to Keep (Universal Game Logic)**

**KEEP** these - they're combat-agnostic:

1. **Effect Execution System**
   - `EffectPlan` structure
   - `ExecuteEffectPlan()` pipeline
   - Order objects (DamageOrder, etc.)
   - Recursive effect processing
   
2. **Damage Calculation**
   - `DamageResolver`
   - Buff queries (`HasBuff<T>()`)
   - Stat sheet structure
   
3. **Event System**
   - `EventProvider`
   - Event broadcasting
   
4. **The Great Divide**
   - Pure C# state layer
   - MonoBehaviour presentation layer

---

### **🔄 What to Replace (Turn-Based Specific)**

**REPLACE** `CombatReferee.ExecuteGameLogicForPhase()` - this is your **turn-based loop**.

#### **Example: Real-Time Combat**

```csharp
public class RealTimeCombatController : MonoBehaviour
{
    public CombatState combatState; // ✅ Keep this!
    public EventProvider eventProvider; // ✅ Keep this!
    
    void Update() {
        // Real-time ability casting
        foreach(Character character in combatState.FullCombatantList) {
            if (character.AbilityCooldownReady) {
                ExecuteAbility(character);
            }
        }
        
        // Tick DOTs
        if (Time.time - lastTickTime > tickInterval) {
            ProcessDOTs();
        }
    }
    
    void ExecuteAbility(Character character) {
        Effect ability = SelectAbilityAI(character);
        Character target = SelectTargetAI(character, ability);
        
        var plan = ability.GetUncommitted(character, target, combatState.FullCombatantList);
        
        // ✅ Same execution!
        combatState.ExecuteEffectPlan(plan);
    }
}
```

---

#### **Example: Card Game**

```csharp
public class CardGameController : MonoBehaviour
{
    public CombatState combatState; // ✅ Keep this!
    public EventProvider eventProvider; // ✅ Keep this!
    
    public void PlayCard(Card card, Character target) {
        // Convert card to effect
        Effect cardEffect = CardToEffectFactory.Create(card);
        
        var plan = cardEffect.GetUncommitted(
            GetActivePlayer(),
            target,
            combatState.FullCombatantList
        );
        
        // ✅ Same execution!
        combatState.ExecuteEffectPlan(plan);
        
        // Card game specific
        DrawCard();
        AdvanceTurn();
    }
}
```

---

#### **Example: Auto-Battler**

```csharp
public class AutoBattlerController : MonoBehaviour
{
    public CombatState combatState; // ✅ Keep this!
    public EventProvider eventProvider; // ✅ Keep this!
    
    float attackInterval = 1.5f;
    float lastAttackTime;
    
    void Update() {
        if (Time.time - lastAttackTime > attackInterval) {
            ProcessNextAttack();
            lastAttackTime = Time.time;
        }
    }
    
    void ProcessNextAttack() {
        // Get character with highest attack speed
        Character attacker = combatState.FullCombatantList
            .OrderByDescending(c => c.Config.AttackSpeed)
            .First();
        
        // AI select ability + target
        Effect ability = new AbilityBasicAttack();
        Character target = CombatantListFilter.RandomByScope(
            combatState.FullCombatantList,
            attacker,
            EligibleTargetScopeType.ENEMY
        );
        
        var plan = ability.GetUncommitted(attacker, target, combatState.FullCombatantList);
        
        // ✅ Same execution!
        combatState.ExecuteEffectPlan(plan);
    }
}
```

---

### **🗺️ The Abstraction Map**

```
Your Custom Orchestrator
    ↓ (replace CombatReferee)
    ↓ decides WHEN to call
    ↓
CombatState.ExecuteEffectPlan()  ← ✅ KEEP THIS
    ↓
Effect Processing Pipeline       ← ✅ KEEP THIS
    ↓
DamageResolver                   ← ✅ KEEP THIS
    ↓
Character Stat Sheets            ← ✅ KEEP THIS
    ↓
EventProvider broadcasts         ← ✅ KEEP THIS
    ↓
Your Custom Presentation         ← (replace StageChoreographer/UIManager)
```

---

## 🎯 The Core Answer

### **Where is the deep game logic?**

**Two Places**:

1. **Combat Flow**: `CombatReferee.ExecuteGameLogicForPhase()`
   - **What it controls**: Turn order, phase transitions, user input timing
   - **For new games**: Replace this with your flow (real-time, cards, auto-battle)
   
2. **Stat Processing**: `CombatState.ExecuteEffectPlan()` + `DamageResolver`
   - **What it controls**: Damage calculation, buff application, effect chains
   - **For new games**: Keep this! It's combat-type agnostic

---

### **Critical Insight**

The **stat sheet bumping** happens in:
- `DamageResolver.ResolveOrder()` - calculates final damage
- `Character.TakeDamage()` - applies to stat sheet
- `Character.HasBuff<T>()` - queries modifiers
- `CombatState.ExecuteEffectPlan()` - orchestrates all interactions

The **game flow** happens in:
- `CombatReferee.ExecuteGameLogicForPhase()` - turn-based logic
- `CombatReferee.CombatPhaseDriver()` - the main loop

**To make a different combat system**: Replace the flow, keep the stat processing!

---

## 📋 Quick Reference: Where to Look

| What You Want to Change | Where to Look |
|------------------------|---------------|
| Turn order logic | `CombatState.MoveToNextCombatant()` |
| Phase structure | `CombatPhase` enum + `ExecuteGameLogicForPhase()` |
| Combat loop | `CombatReferee.CombatPhaseDriver()` |
| Damage calculation | `DamageResolver.ResolveOrder()` |
| Buff behavior | Individual buff classes + `Character.HasBuff<T>()` queries |
| Effect processing order | `CombatState.ExecuteEffectPlan()` |
| Resource system | `CombatState.AdjustScaleByAbilityCast()` |
| Victory conditions | `CombatReferee.CheckCombatWinConditions()` |
| AI behavior | `CombatReferee.DoCpuAbilityChoice()` |

---

## 🚀 Summary

**The Deep Logic Lives In**:
- **Layer 1 (Flow)**: `CombatReferee` - Turn-based orchestration → **REPLACE for new games**
- **Layer 2 (Rules)**: `CombatState` + `DamageResolver` - Stat processing → **KEEP for new games**

**The stat sheets are bumped by**:
- `DamageResolver` - calculates damage
- `Character.TakeDamage()` - applies to health/stagger
- `EffectPlan` execution - orchestrates all changes
- Buff queries - modify calculations

**To adapt for a new combat flow**:
1. Keep `CombatState` and all Layer 2 code
2. Replace `CombatReferee` with your orchestrator
3. Call `combatState.ExecuteEffectPlan()` when abilities fire
4. Subscribe to `EventProvider` events for presentation
5. Done! ✨
