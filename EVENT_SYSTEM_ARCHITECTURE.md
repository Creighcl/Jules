# Event System Architecture

## 🎯 Overview

This combat system uses a **two-layer event architecture** that cleanly separates game logic from presentation. It's essentially an **Observer Pattern** implementation that allows complete decoupling between state changes and reactions.

---

## 🏗️ The Two Layers

### **Layer 1: EventProvider (The Hub)**

A pure C# class that acts as a centralized event bus. It contains **delegates** for every significant game event.

**Location**: `2 - State/EventProvider.cs`

```csharp
public class EventProvider
{
    // Combat events
    public CalculatedDamageDelegate OnDamageResolved;
    public BuffDelegate OnBuffAdded;
    public BuffDelegate OnBuffExpired;
    public CharacterDelegate OnCharacterRevived;
    public EffectPlanDelegate OnEffectPlanExecutionStart;
    public EffectPlanDelegate OnEffectPlanExecutionComplete;
    
    // Resource events
    public ScaleDelegate OnScaleChanged;
    
    // Turn management events
    public TurnOrderDelegate OnTurnOrderChanged;
    public CombatPhaseDelegate OnPhaseAwake;
    public CombatPhaseDelegate OnPhasePrompt;
    public CombatPhaseDelegate OnPhaseExiting;
    
    // Wave/Stage events
    public WaveInfoDelegate OnWaveSetupStart;
    public WaveInfoDelegate OnWaveReady;
    public IntDelegate OnWaveVictory;
    public StandardDelegate OnWaveComplete;
    public WaveInfoDelegate OnStageComplete;
    public IntDelegate OnGameOver;
    public StandardDelegate OnGameVictory;
    
    // Character events
    public CharacterDelegate OnCharacterSummoned;
    public CharactersDelegate OnEligibleTargetsChanged;
    
    // Input events (from UI)
    public AbilityCategoryDelegate OnInput_CombatantChoseAbility;
    public CharacterDelegate OnInput_CombatantChoseTarget;
    public StandardDelegate OnInput_BackOutOfTargetSelection;
    public BoolDelegate OnInput_ReviveResponse;
    public StandardDelegate OnInput_RetryResponse;
    public BoonDelegate OnInput_BoonSelected;
    
    // Boon events
    public BoonsDelegate OnBoonOffer;
}
```

**Key Features**:
- **Zero Unity dependencies** - Pure C#
- **Nullable invocation** - `?.Invoke()` prevents null reference errors
- **Typed delegates** - Compile-time safety for event parameters
- **Centralized** - Single source for all events

---

### **Layer 2: System Subscribers (The Listeners)**

MonoBehaviour classes that subscribe to events and handle presentation:

1. **StageChoreographer** - Visual performances (animations, VFX)
2. **UIManager** - UI updates
3. **AudioManager** (implied) - Sound effects
4. **CombatReferee** - Orchestration logic

---

## 📊 Event Flow Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                      CombatState                             │
│                   (Game Logic Layer)                         │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     │ Broadcasts via EventProvider
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│                    EventProvider                             │
│                   (Event Hub - Layer 1)                      │
│                                                              │
│  OnDamageResolved?.Invoke(calculatedDamage)                 │
└───┬──────────────┬──────────────┬──────────────┬────────────┘
    │              │              │              │
    │              │              │              │
    ▼              ▼              ▼              ▼
┌─────────┐  ┌──────────┐  ┌─────────┐  ┌──────────────┐
│  Stage  │  │    UI    │  │  Audio  │  │   Combat     │
│Choreog. │  │ Manager  │  │ Manager │  │   Referee    │
└─────────┘  └──────────┘  └─────────┘  └──────────────┘
    │              │              │              │
    │              │              │              │
    ▼              ▼              ▼              ▼
┌─────────┐  ┌──────────┐  ┌─────────┐  ┌──────────────┐
│Play Anim│  │Update HP │  │Play SFX │  │ Check Win    │
│Show VFX │  │Show Num  │  │         │  │ Condition    │
└─────────┘  └──────────┘  └─────────┘  └──────────────┘
```

---

## 🎨 Design Choices

### **1. Why Two Layers?**

**Problem**: Game logic shouldn't know about presentation.

**Solution**: 
- **Layer 1** (EventProvider) = Contract between logic and presentation
- **Layer 2** (Subscribers) = Implementation of reactions

**Benefits**:
- Game logic can run headless (unit tests, server)
- Can swap presentation without touching logic
- Multiple systems react to same event
- Easy to add new listeners

---

### **2. Why Typed Delegates?**

Each event type has a **custom delegate signature**:

```csharp
public delegate void CalculatedDamageDelegate(CalculatedDamage cd);
public delegate void BuffDelegate(Buff buff);
public delegate void CharacterDelegate(Character character);
public delegate void ScaleDelegate(int light, int dark);
public delegate void StandardDelegate();
```

**Alternative**: Use `Action<T>` or `UnityEvent<T>`

**Why custom delegates?**
- **Self-documenting** - Name describes the event
- **Discoverability** - Easy to find all damage-related delegates
- **Flexible signatures** - Can add parameters without breaking `Action<T>` patterns

---

### **3. Why Null-Conditional Invocation?**

```csharp
_eventProvider.OnDamageResolved?.Invoke(calculatedDamage);
```

The `?.` operator prevents errors if no listeners are subscribed.

**Alternative**: Check manually
```csharp
if (_eventProvider.OnDamageResolved != null) {
    _eventProvider.OnDamageResolved(calculatedDamage);
}
```

**Benefit**: Cleaner, idiomatic C#

---

### **4. Why Separate Input Events?**

```csharp
// Input from UI
public AbilityCategoryDelegate OnInput_CombatantChoseAbility;
public CharacterDelegate OnInput_CombatantChoseTarget;
public StandardDelegate OnInput_BackOutOfTargetSelection;
```

**Naming**: `OnInput_` prefix distinguishes user input from game events.

**Flow**:
```
User clicks button
    ↓
UIManager detects click
    ↓
UIManager invokes OnInput_CombatantChoseAbility
    ↓
CombatReferee handles event
    ↓
CombatState processes ability
    ↓
Broadcasts OnEffectPlanExecutionStart
    ↓
UIManager/StageChoreographer react
```

**Benefit**: Bi-directional communication without coupling

---

## 🔄 Event Categories

### **Combat Events** (State → Presentation)

| Event | When Fired | Data | Typical Listeners |
|-------|-----------|------|------------------|
| `OnDamageResolved` | After damage calculated | `CalculatedDamage` | StageChoreographer (damage numbers), UIManager (HP bars) |
| `OnBuffAdded` | Buff applied to character | `Buff` | StageChoreographer (buff icon), UIManager (status display) |
| `OnBuffExpired` | Buff removed from character | `Buff` | StageChoreographer (remove icon), UIManager (update status) |
| `OnCharacterRevived` | Character brought back | `Character` | StageChoreographer (revive animation), UIManager (update portrait) |
| `OnCharacterSummoned` | New combatant spawned | `Character` | StageChoreographer (spawn VFX), UIManager (add to display) |

### **Effect Execution Events** (State → Presentation)

| Event | When Fired | Data | Purpose |
|-------|-----------|------|---------|
| `OnEffectPlanExecutionStart` | Before processing effect | `EffectPlan` | Start animations, lock UI |
| `OnEffectPlanExecutionComplete` | After effect fully processed | `EffectPlan` | End animations, unlock UI |

**Pattern**: Start/Complete pairs for animation sequencing

```csharp
// In StageChoreographer
void HandleEffectPlanExecutionStart(EffectPlan plan) {
    if (plan.Source is AbilityFireball) {
        plan.Caster.GetComponent<ActorCharacter>()
            .EnqueuePerformance(CharacterActorPerformance.CAST_SPELL);
    }
}
```

---

### **Turn Management Events** (State → Referee/Presentation)

| Event | When Fired | Data | Purpose |
|-------|-----------|------|---------|
| `OnTurnOrderChanged` | Turn queue modified | `Character current, List<Character> queue` | Update UI turn display |
| `OnPhaseAwake` | Entering new phase | `CombatPhase, Character` | Initialize phase-specific logic |
| `OnPhasePrompt` | Phase ready for input/execution | `CombatPhase, Character` | Show UI, trigger AI |
| `OnPhaseExiting` | Leaving current phase | `CombatPhase, Character` | Cleanup |

**Usage in CombatReferee**:
```csharp
void TransitionToPhase(CombatPhase newPhase) {
    _eventProvider.OnPhaseExiting?.Invoke(CurrentPhase, combatState.CurrentCombatant);
    CurrentPhase = newPhase;
    _eventProvider.OnPhaseAwake?.Invoke(newPhase, combatState.CurrentCombatant);
    _eventProvider.OnPhasePrompt?.Invoke(newPhase, combatState.CurrentCombatant);
}
```

---

### **Resource Events** (State → UI)

| Event | When Fired | Data | Purpose |
|-------|-----------|------|---------|
| `OnScaleChanged` | Light/Shadow points modified | `int light, int shadow` | Update resource display |

**Example**:
```csharp
// In CombatState
void AdjustScaleByAbilityCast(EffectPlan plan) {
    LightPoints += 1;
    _eventProvider.OnScaleChanged?.Invoke(LightPoints, ShadowPoints);
}

// In UIManager
void HandleScaleChanged(int light, int shadow) {
    ScalePanelUI.UpdateDisplay(light, shadow);
}
```

---

### **Wave/Stage Events** (Referee → Presentation)

| Event | When Fired | Data | Purpose |
|-------|-----------|------|---------|
| `OnWaveSetupStart` | Before wave loads | `WaveInfo` | Show wave intro |
| `OnWaveReady` | Wave fully loaded | `WaveInfo` | Start combat |
| `OnWaveVictory` | Wave defeated | `int waveNumber` | Victory animation |
| `OnWaveComplete` | Wave finished (win/loss) | None | Cleanup |
| `OnStageComplete` | All waves in stage done | `WaveInfo` | Show rewards/boons |
| `OnGameOver` | No lives remaining | `int finalWave` | Game over screen |
| `OnGameVictory` | All stages complete | None | Victory screen |

---

### **Input Events** (UI → Referee)

| Event | When Fired | Data | Purpose |
|-------|-----------|------|---------|
| `OnInput_CombatantChoseAbility` | Player selects ability | `AbilityCategory` | Store ability choice |
| `OnInput_CombatantChoseTarget` | Player selects target | `Character` | Store target, execute ability |
| `OnInput_BackOutOfTargetSelection` | Player cancels targeting | None | Return to ability selection |
| `OnInput_BoonSelected` | Player picks upgrade | `BaseBoonResolver` | Apply boon, continue |
| `OnInput_ReviveResponse` | Player chooses to revive | `bool` | Use/decline revive |
| `OnInput_RetryResponse` | Player retries wave | None | Reset wave |

---

## 🛠️ Implementation Patterns

### **Pattern 1: Broadcasting from State**

```csharp
// In CombatState.cs
public void ExecuteSelectedAbility() {
    var completedAbility = AbilitySelected.GetUncommitted(
        CurrentCombatant,
        TargetSelected,
        FullCombatantList
    );

    ExecuteEffectPlan(completedAbility);
    
    AdjustScaleByAbilityCast(completedAbility);
    _eventProvider.OnScaleChanged?.Invoke(LightPoints, ShadowPoints); // ⭐
}
```

---

### **Pattern 2: Subscribing in Awake/Start**

```csharp
// In StageChoreographer.cs
void Awake() {
    _eventProvider = GetComponent<CombatReferee>().eventProvider;
    SetupHooks();
}

void SetupHooks() {
    _eventProvider.OnPhasePrompt += HandlePhasePrompts;
    _eventProvider.OnEffectPlanExecutionComplete += HandleAbilityExecuted;
    _eventProvider.OnCharacterRevived += HandleCharacterRevived;
    _eventProvider.OnDamageResolved += HandleDamageResolved;
    _eventProvider.OnBuffAdded += HandleBuffAdded;
    _eventProvider.OnBuffExpired += HandleBuffRemoved;
    _eventProvider.OnCharacterSummoned += HandleCharacterSummoned;
}
```

**Important**: 
- Subscribe in `Awake()` or `Start()`
- Always unsubscribe in `OnDestroy()` to prevent memory leaks

```csharp
void OnDestroy() {
    _eventProvider.OnDamageResolved -= HandleDamageResolved;
    // ... unsubscribe all
}
```

---

### **Pattern 3: Handling Events**

```csharp
// In StageChoreographer.cs
void HandleDamageResolved(CalculatedDamage damage) {
    // Show damage numbers
    ActorCharacter actor = damage.Target.GetComponent<ActorCharacter>();
    actor.ShowFloatingDamage(damage.DamageToHealth);
    
    // Play hit animation
    actor.EnqueuePerformance(CharacterActorPerformance.TAKEDAMAGE);
    
    // Screen shake for high damage
    if (damage.DamageToHealth > 50) {
        CameraShake.Instance.Shake(0.3f, 0.2f);
    }
}
```

---

### **Pattern 4: Multi-System Reactions**

Multiple systems can react to the same event:

```csharp
// CombatReferee listens for game logic
_eventProvider.OnDamageResolved += CheckForDeaths;

// StageChoreographer listens for visuals
_eventProvider.OnDamageResolved += PlayDamageAnimation;

// UIManager listens for display updates
_eventProvider.OnDamageResolved += UpdateHealthBars;

// AudioManager listens for sound
_eventProvider.OnDamageResolved += PlayHitSound;
```

**All four methods execute** when `OnDamageResolved` is invoked!

---

### **Pattern 5: Async Operations with Events**

```csharp
// In StageChoreographer
void HandleEffectPlanExecutionStart(EffectPlan plan) {
    StartCoroutine(PerformAbilityAnimation(plan));
}

IEnumerator PerformAbilityAnimation(EffectPlan plan) {
    ActorCharacter caster = plan.Caster.GetComponent<ActorCharacter>();
    
    // Play casting animation
    caster.EnqueuePerformance(CharacterActorPerformance.CAST_SPELL);
    
    // Wait for animation
    yield return new WaitUntil(() => !caster.IsPerforming);
    
    // Animation complete, UI can proceed
}
```

**CombatReferee waits** for `IsPerforming` to clear before continuing.

---

## 🎬 Example: Complete Event Sequence

**Scenario**: Player casts Fireball on enemy

```
1. User clicks "Fireball" button
   ↓
2. UIManager.AbilitySelected(AbilityCategory.SPECIALATTACK)
   ↓
3. UIManager invokes: OnInput_CombatantChoseAbility
   ↓
4. CombatReferee.HandleIncomingCombatantAbilityChoice()
   ↓ Creates ability, shows targets
5. CombatReferee invokes: OnEligibleTargetsChanged
   ↓
6. UIManager highlights valid targets
   ↓
7. User clicks enemy
   ↓
8. UIManager invokes: OnInput_CombatantChoseTarget
   ↓
9. CombatReferee.TargetSelected()
   ↓ Stores target
10. CombatState.ExecuteSelectedAbility()
    ↓
11. CombatState invokes: OnEffectPlanExecutionStart
    ↓
12. StageChoreographer plays casting animation
    ↓
13. CombatState.ExecuteEffectPlan() processes damage
    ↓
14. CombatState invokes: OnDamageResolved (for each target hit)
    ↓
15. StageChoreographer plays damage animation
16. UIManager updates HP bar
17. AudioManager plays hit sound
    ↓
18. CombatState invokes: OnEffectPlanExecutionComplete
    ↓
19. StageChoreographer finishes animations
20. UIManager unlocks UI
    ↓
21. CombatReferee advances to next phase
```

---

## 🧩 Event-Driven vs. Direct Calls

### ❌ **Tightly Coupled (Bad)**

```csharp
// In CombatState
public void ApplyDamage(Character target, int damage) {
    target.currentHealth -= damage;
    
    // BAD: Direct dependencies on presentation
    UIManager.Instance.UpdateHealthBar(target);
    StageChoreographer.Instance.PlayHitAnimation(target);
    AudioManager.Instance.PlaySound("hit");
}
```

**Problems**:
- CombatState knows about UI
- Can't run without Unity
- Hard to test
- Presentation changes break logic

---

### ✅ **Event-Driven (Good)**

```csharp
// In CombatState
public void ApplyDamage(Character target, int damage) {
    target.currentHealth -= damage;
    
    // GOOD: Just broadcast what happened
    _eventProvider.OnDamageResolved?.Invoke(
        new CalculatedDamage(attacker, target, damage, ...)
    );
}

// Presentation systems subscribe separately
// StageChoreographer.cs
_eventProvider.OnDamageResolved += PlayHitAnimation;

// UIManager.cs
_eventProvider.OnDamageResolved += UpdateHealthBar;

// AudioManager.cs
_eventProvider.OnDamageResolved += PlayHitSound;
```

**Benefits**:
- CombatState has zero presentation dependencies
- Easy to add new reactions
- Can run headless
- Systems are independent

---

## 🔍 Debugging Events

### **See All Subscribers**

```csharp
void LogEventSubscribers() {
    var delegates = _eventProvider.OnDamageResolved?.GetInvocationList();
    if (delegates != null) {
        foreach(var d in delegates) {
            Debug.Log($"OnDamageResolved subscriber: {d.Method.Name} " +
                     $"on {d.Target}");
        }
    }
}
```

---

### **Event Tracing**

```csharp
// Wrap invocations with logging
void InvokeOnDamageResolved(CalculatedDamage dmg) {
    Debug.Log($"[EVENT] OnDamageResolved: {dmg.DamageToHealth} damage " +
             $"to {dmg.Target.Config.Name}");
    _eventProvider.OnDamageResolved?.Invoke(dmg);
}
```

---

### **Performance Monitoring**

```csharp
void HandleDamageResolved(CalculatedDamage damage) {
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Handle event
    UpdateHealthBar(damage.Target);
    
    stopwatch.Stop();
    if (stopwatch.ElapsedMilliseconds > 16) {
        Debug.LogWarning($"Slow event handler: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

---

## 🚀 Performance Considerations

### **Memory**
- Delegates are reference types (heap allocated)
- Each subscriber = one delegate instance
- **Mitigation**: Use `UnityAction<T>` for Unity-specific events (potentially lighter)

### **Call Overhead**
- Invoking delegates has minimal overhead (~nanoseconds per call)
- **Concern**: If 100 systems subscribe to `OnDamageResolved` and damage happens 60 times/second
- **Mitigation**: Only subscribe to events you need, unsubscribe when done

### **Null Checks**
- `?.Invoke()` checks for null every time
- **Negligible cost** in practice

---

## 📚 Summary

The two-layer event system provides:

✅ **Decoupling** - State never knows about presentation  
✅ **Flexibility** - Easy to add new listeners  
✅ **Testability** - State can run without Unity  
✅ **Clarity** - Event names document what happened  
✅ **Extensibility** - New systems just subscribe  

**Layer 1** (EventProvider) = The contract  
**Layer 2** (Subscribers) = The implementations  

This pattern makes the codebase **maintainable, testable, and elegant**!
