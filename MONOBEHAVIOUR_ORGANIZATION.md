# MonoBehaviour Organization & The Great Divide

## 🏗️ Overview

This combat system implements **The Great Divide**: a strict architectural separation between **game logic** (pure C#) and **presentation** (Unity MonoBehaviours). This separation enables testing, portability, and maintainability.

---

## 🎯 The Great Divide Principle

### **Core Concept**

```
┌─────────────────────────────────────────────────────────┐
│           PRESENTATION LAYER (Unity)                    │
│                                                         │
│  MonoBehaviours: CombatReferee, UIManager,            │
│                  StageChoreographer, Character         │
│                                                         │
│  Responsibilities: User input, rendering, audio,       │
│                    animation, Unity lifecycle          │
└─────────────────┬───────────────────────────────────────┘
                  │
                  │ owns / coordinates
                  │
┌─────────────────▼───────────────────────────────────────┐
│           GAME LOGIC LAYER (Pure C#)                    │
│                                                         │
│  Classes: CombatState, EventProvider, GameState,       │
│           Effect, Buff, Order objects                  │
│                                                         │
│  Responsibilities: Combat rules, stat calculations,    │
│                    effect processing, state management │
│                                                         │
│  🔒 ZERO Unity dependencies (no MonoBehaviour,         │
│     no GameObject, no Transform)                       │
└─────────────────────────────────────────────────────────┘
```

---

## 💎 Benefits of The Great Divide

### **1. Testability**
Game logic can be **unit tested** without Unity:

```csharp
[Test]
public void TestDamageCalculation() {
    // No Unity needed!
    Character attacker = new Character();
    Character victim = new Character();
    DamageOrder order = new DamageOrder(attacker, victim, 100, null);
    
    DamageResolver resolver = new DamageResolver();
    CalculatedDamage result = resolver.ResolveOrder(order);
    
    Assert.AreEqual(50, result.DamageToHealth);
}
```

### **2. Portability**
Core combat logic could run:
- On a server (multiplayer)
- In a non-Unity engine
- In a simulation/analysis tool
- In a replay system

### **3. Hot-Reload Friendly**
Pure C# classes reload faster than MonoBehaviours during development.

### **4. Clearer Architecture**
Forces you to think about **what's game logic vs. what's presentation**.

### **5. Easier Debugging**
Game state can be inspected without Unity Editor running.

---

## 🎭 MonoBehaviour Organization

### **Layer 1: Orchestration** (MonoBehaviour)

#### **CombatReferee** (`1 - Orchestration/CombatReferee.cs`)

**Role**: The **master conductor** of combat. It's the ONLY MonoBehaviour that directly owns game state.

```csharp
public class CombatReferee : MonoBehaviour
{
    // ✅ Owns pure C# game logic
    public GameState gameState;
    public CombatState combatState;
    public EventProvider eventProvider;
    WaveProvider waveProvider;
    BoonLibrary boonLibrary;

    // ✅ References to presentation systems
    UIManager __uiManager;
    StageChoreographer __stageChoreographer;
    SpawnPointProvider __spawnPointProvider;

    // ✅ Orchestrates combat flow
    CombatPhase CurrentCombatPhase = CombatPhase.INIT;
    bool CombatAwaitingUser = false;
}
```

**Responsibilities**:
- Owns `CombatState`, `GameState`, `EventProvider`
- Manages combat state machine (`CombatPhase` transitions)
- Coordinates between UI, Stage, and game logic
- Responds to player input events
- Triggers AI actions

**Key Pattern**: **Owns logic, delegates presentation**

```csharp
void ExecuteGameLogicForPhase(CombatPhase phase) {
    switch(phase) {
        case CombatPhase.CHARACTERTURN_EXECUTION:
            // ✅ Call pure C# logic
            combatState.ExecuteSelectedAbility();
            
            // ✅ Let presentation react via events
            // (StageChoreographer/UIManager subscribed to events)
            
            NextPhase = CombatPhase.CHARACTERTURN_CLEANUP;
            break;
    }
}
```

**Why MonoBehaviour?**
- Needs Unity lifecycle (`Start`, `Update`)
- Needs to reference ScriptableObjects
- Needs `GetComponent<>()` to find coworkers
- Orchestrates Unity-based systems

---

### **Layer 3: Systems** (MonoBehaviours)

#### **StageChoreographer** (`3 - Systems/Stage/StageChoreographer.cs`)

**Role**: Translates game events into **visual performances** (animations, VFX, camera effects).

```csharp
public class StageChoreographer : MonoBehaviour
{
    EventProvider _eventProvider;
    List<ActorCharacter> MyActors = new List<ActorCharacter>();
    
    void SetupHooks() {
        _eventProvider.OnDamageResolved += HandleDamageResolved;
        _eventProvider.OnBuffAdded += HandleBuffAdded;
        _eventProvider.OnEffectPlanExecutionStart += HandleEffectPlanExecutionStart;
        // ... etc
    }
    
    void HandleDamageResolved(CalculatedDamage damage) {
        // ✅ Visual response only, no game logic
        ActorCharacter actor = damage.Target.GetComponent<ActorCharacter>();
        actor.ShowFloatingDamage(damage.DamageToHealth);
        actor.EnqueuePerformance(CharacterActorPerformance.TAKEDAMAGE);
    }
}
```

**Key Characteristics**:
- **Listens** to `EventProvider` events
- **Never modifies** game state directly
- **Pure presentation** logic
- Manages actor performance queues

**Pattern**: **Subscribe-and-react**

---

#### **UIManager** (`3 - Systems/UI/UIManager.cs`)

**Role**: Manages all UI panels and translates user input into events.

```csharp
public class UIManager : MonoBehaviour
{
    EventProvider _eventProvider;
    SelectionState CurrentSelectionState = SelectionState.NONE;
    
    // UI references
    public UI_BattleMenu AbilityUI;
    public UI_TurnOrderManager TurnOrderUI;
    public UI_ScalePanelManager ScalePanelUI;
    // ... etc
    
    // ✅ User input → Event
    public void AbilitySelected(AbilityCategory category) {
        _eventProvider.OnInput_CombatantChoseAbility?.Invoke(category);
    }
    
    public void TargetSelected(Character target) {
        _eventProvider.OnInput_CombatantChoseTarget?.Invoke(target);
    }
    
    // ✅ Game event → UI update
    void HandleScaleChanged(int light, int shadow) {
        ScalePanelUI.UpdateDisplay(light, shadow);
    }
}
```

**Responsibilities**:
- Show/hide UI panels
- Capture player input (clicks, buttons)
- Broadcast input events to `EventProvider`
- Subscribe to game events to update UI

**Pattern**: **Bi-directional bridge** between player and game logic

---

#### **ActorCharacter** (`3 - Systems/Stage/ActorCharacter.cs`)

**Role**: Visual representation of a character. Handles animations and visual effects.

```csharp
public class ActorCharacter : MonoBehaviour
{
    Character _character;  // Reference to pure C# Character
    Queue<CharacterActorPerformance> PerformanceQueue;
    
    public bool IsPerforming { get; private set; }
    
    public void EnqueuePerformance(CharacterActorPerformance perf) {
        PerformanceQueue.Enqueue(perf);
    }
    
    void Update() {
        if (!IsPerforming && PerformanceQueue.Count > 0) {
            StartCoroutine(PerformNext());
        }
    }
    
    IEnumerator PerformNext() {
        IsPerforming = true;
        var performance = PerformanceQueue.Dequeue();
        
        // Play animation, wait for completion
        // ...
        
        IsPerforming = false;
    }
}
```

**Key**: Acts as a **visual proxy** for the logic-based `Character` class.

---

### **Layer 2: State** (Hybrid - Mostly Pure C#)

#### **Character** (`2 - State/Character.cs`)

**Special Case**: This is the ONLY state class that's a MonoBehaviour, but it's designed to minimize Unity dependencies.

```csharp
public class Character : MonoBehaviour
{
    // ✅ Pure data (no Unity types)
    [SerializeField]
    public CharacterConfig Config;
    
    public int currentHealth = 1;
    public int currentStagger = 0;
    public bool isDead = false;
    public List<Buff> Buffs = new List<Buff>();
    
    // ❌ Only Unity dependency: position for rendering
    public BattlefieldPosition PositionInfo { get; internal set; }
    
    // ✅ All methods are pure logic
    public void TakeDamage(int Damage) { /* ... */ }
    public void AddBuff(Buff newBuff) { /* ... */ }
    public List<AbilityCategory> GetAvailableAbilities(...) { /* ... */ }
}
```

**Why MonoBehaviour?**
- Needs to exist on GameObjects for Unity's scene system
- Needs position (`Transform`) for rendering
- Convenient for Unity Inspector debugging

**Design Principle**: Treat it like a **data container with methods**, not a traditional MonoBehaviour.

**Alternatives Considered**:
1. Pure C# `Character` + separate `CharacterView` MonoBehaviour
   - ❌ More complex, requires linking
2. Current approach
   - ✅ Simpler, minimal Unity coupling

---

## 🔥 The Firewall

### **How The Great Divide is Enforced**

```
┌─────────────────────────────────────────────────────────┐
│  CombatReferee (MonoBehaviour)                          │
└──────────────────┬──────────────────────────────────────┘
                   │
                   │ owns
                   ▼
        ┌──────────────────────┐
        │    CombatState       │ ← 🔒 PURE C#
        │    EventProvider     │ ← 🔒 PURE C#
        │    GameState         │ ← 🔒 PURE C#
        └──────────┬───────────┘
                   │ uses
                   ▼
        ┌──────────────────────┐
        │    Effect            │ ← 🔒 PURE C#
        │    Buff              │ ← 🔒 PURE C#
        │    Order objects     │ ← 🔒 PURE C#
        │    Helpers           │ ← 🔒 PURE C#
        └──────────────────────┘
```

**Rules**:
1. **CombatState** never references:
   - `MonoBehaviour`
   - `GameObject`
   - `Transform`
   - Unity-specific types
   
2. **CombatState** communicates via:
   - `EventProvider` broadcasts
   - Interfaces (e.g., `IBattlefieldPositionProvider`)
   
3. **MonoBehaviours** communicate with state via:
   - Method calls (e.g., `combatState.ExecuteSelectedAbility()`)
   - Event subscriptions

---

### **Interface Injection for Unity Dependencies**

When state layer needs Unity functionality, use **interfaces**:

```csharp
// Interface (pure C#)
public interface IBattlefieldPositionProvider {
    BattlefieldPosition GetNextOpenBattlefieldPositionForTeam(
        List<int> takenSpots, 
        TeamType team
    );
    Character InstantiateNewCharacterForConfig(CharacterConfig config);
}

// CombatState uses interface (pure C#)
public class CombatState {
    public IBattlefieldPositionProvider _bfpProvider;
    
    public Character SummonUnitForTeam(CharacterConfig config, TeamType team) {
        BattlefieldPosition bfInfo = _bfpProvider.GetNextOpenBattlefieldPositionForTeam(...);
        Character character = _bfpProvider.InstantiateNewCharacterForConfig(config);
        // ...
    }
}

// MonoBehaviour implements interface
public class SpawnPointProvider : MonoBehaviour, IBattlefieldPositionProvider {
    public BattlefieldPosition GetNextOpenBattlefieldPositionForTeam(...) {
        // Unity-specific logic (find spawn points, etc.)
    }
    
    public Character InstantiateNewCharacterForConfig(CharacterConfig config) {
        // Instantiate prefab
        GameObject go = Instantiate(characterPrefab);
        return go.GetComponent<Character>();
    }
}

// Injection in CombatReferee
void Awake() {
    combatState = new CombatState(eventProvider, GetComponent<SpawnPointProvider>());
}
```

**Benefit**: `CombatState` stays pure C#, but can still request Unity functionality through abstraction.

---

## 🎮 MonoBehaviour Lifecycle Usage

### **CombatReferee**

```csharp
void Awake() {
    // ✅ Create pure C# instances
    eventProvider = new EventProvider();
    gameState = new GameState();
    waveProvider = new WaveProvider(...);
    boonLibrary = new BoonLibrary(...);
    combatState = new CombatState(eventProvider, ...);
    
    // ✅ Get references to presentation
    __uiManager = GetComponent<UIManager>();
    __stageChoreographer = GetComponent<StageChoreographer>();
}

void Start() {
    // ✅ Subscribe to input events from UI
    eventProvider.OnInput_CombatantChoseAbility += HandleAbilityChoice;
    eventProvider.OnInput_CombatantChoseTarget += TargetSelected;
    
    // ✅ Start game flow
    SetupParty();
    StartCoroutine(SetupWave());
}

void Update() {
    // ✅ Drive state machine
    if (!CombatAwaitingUser && !IsWaitingOnPerformances()) {
        CurrentPhase = ExecuteGameLogicForPhase(CurrentPhase);
    }
}
```

---

### **StageChoreographer**

```csharp
void Awake() {
    // ✅ Get reference to EventProvider
    _eventProvider = GetComponent<CombatReferee>().eventProvider;
    SetupHooks();
}

void SetupHooks() {
    // ✅ Subscribe to game events
    _eventProvider.OnDamageResolved += HandleDamageResolved;
    _eventProvider.OnBuffAdded += HandleBuffAdded;
    // ...
}

void OnDestroy() {
    // ✅ Unsubscribe to prevent memory leaks
    _eventProvider.OnDamageResolved -= HandleDamageResolved;
    // ...
}
```

---

### **UIManager**

```csharp
void Start() {
    _eventProvider = GetComponent<CombatReferee>().eventProvider;
    
    // ✅ Subscribe to game events
    _eventProvider.OnScaleChanged += HandleScaleChanged;
    _eventProvider.OnTurnOrderChanged += HandleTurnOrderChanged;
    // ...
}

// ✅ User input callbacks
public void OnAbilityButtonClicked(AbilityCategory category) {
    _eventProvider.OnInput_CombatantChoseAbility?.Invoke(category);
}
```

---

## 📋 MonoBehaviour Inventory

### **Orchestration Layer**
| Class | Purpose | Unity Dependencies | State Ownership |
|-------|---------|-------------------|-----------------|
| `CombatReferee` | Master orchestrator | GetComponent, Coroutines, ScriptableObjects | Owns all state |

### **Presentation Layer**
| Class | Purpose | Unity Dependencies | State Access |
|-------|---------|-------------------|--------------|
| `StageChoreographer` | Visual performances | GetComponent, Coroutines, Animation | Read-only via events |
| `UIManager` | UI management | UI components, Input | Read-only via events |
| `ActorCharacter` | Character visuals | Transform, Animation, VFX | References Character |
| `SpawnPointProvider` | Battlefield management | Transform, Instantiate | None |

### **Hybrid**
| Class | Purpose | Unity Dependencies | Notes |
|-------|---------|-------------------|-------|
| `Character` | Character state | Transform (minimal) | Treated as data container |

---

## 🧪 Testing Strategy

### **Pure Logic Tests** (No Unity)

```csharp
[TestFixture]
public class CombatStateTests {
    [Test]
    public void TestDamageCalculation() {
        // Setup
        EventProvider eventProvider = new EventProvider();
        MockBattlefieldProvider bfProvider = new MockBattlefieldProvider();
        CombatState state = new CombatState(eventProvider, bfProvider);
        
        // Execute
        DamageOrder order = new DamageOrder(attacker, victim, 100, null);
        // ...
        
        // Assert
        Assert.AreEqual(expected, actual);
    }
}
```

---

### **Integration Tests** (With Unity)

```csharp
[UnityTest]
public IEnumerator TestCombatFlow() {
    // Setup scene with CombatReferee
    GameObject refereeGO = new GameObject();
    CombatReferee referee = refereeGO.AddComponent<CombatReferee>();
    
    // ... setup
    
    yield return new WaitForSeconds(1f);
    
    // Assert state
    Assert.AreEqual(CombatPhase.CHARACTERTURN_CHOOSEABILITY, 
                    referee.CurrentCombatPhase);
}
```

---

## 🎯 Design Patterns Applied

### **1. Facade Pattern**
`CombatReferee` provides a simplified interface to complex subsystems.

### **2. Mediator Pattern**
`EventProvider` mediates communication between systems without them knowing about each other.

### **3. Dependency Injection**
Interfaces injected into pure C# classes to avoid Unity coupling.

### **4. Observer Pattern**
Event subscriptions for decoupled reactions.

---

## 📚 Summary

**The Great Divide** separates:

**Game Logic** (Pure C#):
- `CombatState`, `EventProvider`, `GameState`
- `Effect`, `Buff`, Order objects
- Helpers, Resolvers
- ✅ Testable, portable, fast to iterate

**Presentation** (MonoBehaviours):
- `CombatReferee` (orchestrator)
- `StageChoreographer`, `UIManager`, `ActorCharacter`
- ✅ Unity-aware, handles rendering/input/animation

**Communication**: Via `EventProvider` (events) and interfaces.

**Benefit**: Clean architecture, testable code, flexible presentation!
