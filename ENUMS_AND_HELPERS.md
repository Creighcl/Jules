# Enums and Helpers Reference

## 📋 Enums Overview

Enums provide **type-safe constants** that make code self-documenting and prevent magic numbers/strings. This system uses enums extensively for state management, targeting, and configuration.

---

## 🎯 Core Enums

### **AbilityCategory**

```csharp
public enum AbilityCategory {
    BASICATTACK,
    SPECIALATTACK,
    ULTIMATE
}
```

**Purpose**: Categorizes player-selectable abilities for UI and resource management.

**Usage**:
```csharp
// In Character.cs - determine available abilities
public List<AbilityCategory> GetAvailableAbilities(int LightPoints, int ShadowPoints) {
    var availableAbilities = new List<AbilityCategory>(){ 
        AbilityCategory.BASICATTACK,
    };
    
    if (Config.SpecialAttack != UserAbilitySelection.NONE) {
        availableAbilities.Add(AbilityCategory.SPECIALATTACK);
    }
    
    if (Config.UltimateAbility != UserAbilitySelection.NONE && 
        LightPoints > 1 && ShadowPoints > 1) {
        availableAbilities.Add(AbilityCategory.ULTIMATE);
    }
    
    return availableAbilities;
}
```

**Game Design**:
- **BASICATTACK**: Always available, generates resources
- **SPECIALATTACK**: Costs 1 resource point
- **ULTIMATE**: Costs 2 Light + 2 Shadow points

---

### **CombatPhase**

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

**Purpose**: Defines the combat state machine. Each phase represents a distinct game state with specific logic.

**State Machine Flow**:
```
INIT
  ↓
WAVE_SETUP (spawn enemies, setup battlefield)
  ↓
CHARACTERTURN_PREFLIGHT (process start-of-turn buffs)
  ↓
CHARACTERTURN_CHOOSEABILITY (player/AI selects ability)
  ↓
CHARACTERTURN_CHOOSETARGET (player/AI selects target)
  ↓
CHARACTERTURN_EXECUTION (process the ability)
  ↓
CHARACTERTURN_CLEANUP (age buffs, check death)
  ↓
CHARACTERTURN_HANDOFF (move to next combatant)
  ↓ (loops back to PREFLIGHT)
  ↓
WAVE_COMPLETE (check win/loss)
  ↓ (back to WAVE_SETUP for next wave OR end combat)
```

**Usage in CombatReferee**:
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
        // ... etc
    }
    return NextPhase;
}
```

**Buff Timing**:
```csharp
public class BuffPoisoned : Buff {
    public BuffPoisoned(...) {
        AgingPhase = CombatPhase.CHARACTERTURN_CLEANUP; // Ages at turn end
    }
}
```

---

### **EligibleTargetScopeType**

```csharp
public enum EligibleTargetScopeType {
    NONE,
    ANYDEAD,
    ANYALIVE,
    ANYATALL,
    ENEMY,
    FRIENDLYORSELF,
    ANYOTHERALLY,
    DEADENEMY,
    DEADFRIENDLY
}
```

**Purpose**: Declaratively defines valid targeting for abilities. Used by `CombatantListFilter` to find eligible targets.

**Scope Breakdown**:

| Scope | Alive? | Dead? | Self? | Allies? | Enemies? | Use Case |
|-------|--------|-------|-------|---------|----------|----------|
| `NONE` | ❌ | ❌ | ❌ | ❌ | ❌ | Self-only buffs, no targeting |
| `ENEMY` | ✅ | ❌ | ❌ | ❌ | ✅ | Damage spells |
| `FRIENDLYORSELF` | ✅ | ❌ | ✅ | ✅ | ❌ | Healing, friendly buffs |
| `ANYOTHERALLY` | ✅ | ❌ | ❌ | ✅ | ❌ | Buffs that exclude self |
| `DEADFRIENDLY` | ❌ | ✅ | ✅ | ✅ | ❌ | Resurrection spells |
| `DEADENEMY` | ❌ | ✅ | ❌ | ❌ | ✅ | Necromancy, corpse effects |
| `ANYALIVE` | ✅ | ❌ | ✅ | ✅ | ✅ | Universal targeting |
| `ANYDEAD` | ❌ | ✅ | ✅ | ✅ | ✅ | Death effects |
| `ANYATALL` | ✅ | ✅ | ✅ | ✅ | ✅ | Debug abilities |

**Usage in Effects**:
```csharp
public class AbilityFireball : Effect {
    public AbilityFireball() {
        Name = "Fireball";
        TargetScope = EligibleTargetScopeType.ENEMY; // Only enemies
    }
}

public class AbilityHeal : Effect {
    public AbilityHeal() {
        Name = "Heal";
        TargetScope = EligibleTargetScopeType.FRIENDLYORSELF; // Allies + self
    }
}
```

**Usage in CombatantListFilter**:
```csharp
List<Character> targets = CombatantListFilter.ByScope(
    AllCombatants,
    caster,
    EligibleTargetScopeType.ENEMY
);
```

---

### **PowerType**

```csharp
public enum PowerType {
    LIGHT,
    SHADOW
}
```

**Purpose**: Elemental affinity system for characters and resource management.

**Game Mechanics**:
1. **Character Affinity**: Each character has a PowerType
2. **Resource Generation**: Basic attacks generate points of character's type
3. **Damage Scaling**: Having more points of your type increases damage
4. **Resistances**: Characters resist their own element

**Usage**:
```csharp
// In CharacterConfig
[SerializeField]
private PowerType _PowerType;

// In CombatState - damage scaling
float GetDamageModifierForPowerType(PowerType powerType) {
    if (powerType == PowerType.LIGHT) {
        if (LightPoints - ShadowPoints == 2) {
            return 1.25f; // 25% damage boost
        }
        if (LightPoints - ShadowPoints == 1) {
            return 1.1f;  // 10% damage boost
        }
    }
    // ... similar for SHADOW
    return 1f;
}

// In DamageResolver - resistance
bool IsVictimResistantToPowerType(Character victim, PowerType attackType) {
    PowerType victimType = GetPowerTypeOfCharacter(victim);
    return victimType == attackType; // Resist your own element
}
```

**Strategic Depth**:
- Building resources of one type makes that element stronger
- But ultimate abilities require BOTH types (2L + 2S)
- Creates tension between specialization and flexibility

---

### **TeamType**

```csharp
public enum TeamType {
    PLAYER,
    CPU,
    NEUTRAL
}
```

**Purpose**: Defines allegiance for targeting and game logic.

**Usage**:
```csharp
// In CharacterConfig
[SerializeField]
private TeamType _TeamType;

// In CombatantListFilter - determine "friendly" vs "enemy"
TeamType referenceTeam = referenceCharacter.Config.TeamType;

bool AllowEnemy = true;
bool AllowFriendly = false;

// ... filter logic
if (candidate.Config.TeamType == referenceTeam || AllowEnemy)
    && (candidate.Config.TeamType != referenceTeam || AllowFriendly)
```

**Charm Effect** (changes allegiance temporarily):
```csharp
if (referenceCharacter.HasBuff<BuffCharmed>()) {
    referenceTeam = referenceTeam == TeamType.PLAYER 
        ? TeamType.CPU 
        : TeamType.PLAYER;
}
```

**Victory Conditions**:
```csharp
List<Character> AliveCPUs = combatState.GetAliveCPUs();
if (AliveCPUs.Count == 0) {
    // Player wins!
}

List<Character> AlivePCs = combatState.GetAlivePCs();
if (AlivePCs.Count == 0) {
    // Player loses!
}
```

---

### **Other Enums**

#### **UserAbilitySelection**
Maps ability names to enum values for configuration in Unity Inspector.

#### **NativeBuffOption**
Special buffs that characters spawn with (e.g., `VOLCANICBOWEL`, `PYROPEAKABOO`).

#### **PCAdventureClassType**
Player character classes (`WARRIOR`, `MAGE`, `ROGUE`, `PRIEST`).

#### **SummonableUnit**
Types of units that can be summoned (`SKELETON`, `WOLF`, etc.).

#### **EnemyType**
Categories of enemies for wave generation.

#### **CombatResult**
Win/Loss outcome of combat.

#### **ActorAnimations**
Animation state enum for `ActorCharacter` animations.

#### **CharacterActorPerformance**
Queued performance actions for stage choreography.

---

## 🛠️ Helper Classes

Helpers are **stateless utility classes** that provide focused functionality. They live in `5 - Helpers/` and are used across layers.

---

### **CombatantListFilter**

**Purpose**: Query and filter character lists based on complex criteria.

**Key Methods**:

```csharp
public static class CombatantListFilter
{
    // Get list of valid targets
    public static List<Character> ByScope(
        List<Character> SearchSpace, 
        Character referenceCharacter, 
        EligibleTargetScopeType type
    )
    
    // Get random valid target
    public static Character RandomByScope(
        List<Character> SearchSpace, 
        Character referenceCharacter, 
        EligibleTargetScopeType type
    )
}
```

**Usage Examples**:

```csharp
// Get all enemies
List<Character> enemies = CombatantListFilter.ByScope(
    AllCombatants,
    caster,
    EligibleTargetScopeType.ENEMY
);

// Get random friendly target for AI
Character randomAlly = CombatantListFilter.RandomByScope(
    AllCombatants,
    aiCharacter,
    EligibleTargetScopeType.FRIENDLYORSELF
);

// Find dead allies for resurrection
List<Character> deadAllies = CombatantListFilter.ByScope(
    AllCombatants,
    healer,
    EligibleTargetScopeType.DEADFRIENDLY
);
```

**Internal Logic**:
Uses boolean flags (`AllowDead`, `AllowAlive`, `AllowSelf`, `AllowEnemy`, `AllowFriendly`) determined by scope type, then filters with LINQ:

```csharp
List<Character> matches = SearchSpace.Where(candidate =>
    (!candidate.isDead || AllowDead)
    && (candidate.isDead || AllowAlive)
    && (candidate != referenceCharacter || AllowSelf)
    && (candidate.Config.TeamType == referenceTeam || AllowEnemy)
    && (candidate.Config.TeamType != referenceTeam || AllowFriendly)
).ToList();
```

---

### **DamageResolver**

**Purpose**: Calculate final damage after all modifiers (mitigation, stagger, buffs, criticals).

**Key Method**:

```csharp
public class DamageResolver {
    public CalculatedDamage ResolveOrder(DamageOrder order)
}
```

**Damage Pipeline**:

```
Raw Damage (from ability)
    ↓
Apply attacker buffs (Weakness -50%, Strengthen x2)
    ↓
Calculate unmitigated damage (victim's mitigation %)
    ↓
Apply vulnerability (+25% if vulnerable)
    ↓
Check stagger state
    ↓ If staggered: full damage
    ↓ If not staggered: half damage
    ↓
Apply to stagger bar
    ↓
Return CalculatedDamage
```

**Stagger System**:
- Characters have **Stagger Points (SP)** and **Health Points (HP)**
- Damage first hits stagger bar
- When stagger reaches 0: character is "cracked"
- While cracked: take **full damage** to HP
- While not cracked: take **half damage** to HP
- Elemental resistance: ignore stagger, no damage

**Usage**:
```csharp
DamageResolver dr = new DamageResolver();
CalculatedDamage result = dr.ResolveOrder(new DamageOrder(
    attacker,
    victim,
    100, // raw damage
    sourceEffect
));

// result.DamageToHealth = actual HP damage (0-100)
// result.DamageToStagger = stagger damage taken
// result.StaggerCrackedByThis = true if this hit cracked them
```

---

### **DamageOrder**

**Purpose**: Data carrier for damage intent.

```csharp
public class DamageOrder {
    public Character Attacker;
    public Character Victim;
    public int RawDamage;
    public Effect Source;
}
```

**Pattern**: Created in abilities, processed by `DamageResolver`, results in `CalculatedDamage`.

---

### **CalculatedDamage**

**Purpose**: Data carrier for damage results.

```csharp
public class CalculatedDamage {
    public Character Attacker;
    public Character Target;
    public int DamageToHealth;
    public int DamageToStagger;
    public int RawDamage;
    public bool StaggerCrackedByThis;
    public Effect Source;
}
```

**Used By**: Trigger systems to react to damage outcomes.

---

### **ReviveOrder / SummonOrder / ScaleOrder**

**Purpose**: Data carriers for other order types.

```csharp
public class ReviveOrder {
    public Character character;
    public int percentHealth = 100;
    public Effect InitiatingAbility;
}

public class SummonOrder {
    public SummonableUnit Unit;
    public TeamType Team;
    public Effect InitiatingAbility;
}

public class ScaleOrder {
    public int ShadowPoints = 0;
    public int LightPoints = 0;
}
```

**Pattern**: Same as damage - intent objects processed by `CombatState`.

---

### **BattlefieldPosition**

**Purpose**: Encapsulates position data for combatants.

```csharp
public class BattlefieldPosition {
    public Vector3 Position;          // World position for rendering
    public int SpotId;                // Unique spot identifier
    public int RelationalReferenceId; // Relative position (for "nearby")
}
```

**Usage**:
```csharp
// Find nearby allies (within 1 position of each other)
List<Character> nearby = AllCombatants
    .Where(c => Mathf.Abs(
        c.PositionInfo.RelationalReferenceId - 
        caster.PositionInfo.RelationalReferenceId
    ) <= 1)
    .ToList();
```

---

### **AttackTypeToAbility**

**Purpose**: Factory pattern - maps `AbilityCategory` + character config to concrete `Effect` instances.

**Usage**:
```csharp
Effect ability = AttackTypeToAbility.GetAbilityForCategory(
    AbilityCategory.SPECIALATTACK,
    character.Config
);
```

**Internally**: Uses switch statement on `character.Config.SpecialAttack` enum to instantiate the correct ability class.

---

### **WaveProvider / WaveInfo**

**Purpose**: Manages wave/stage progression and enemy spawning.

```csharp
public class WaveProvider {
    public WaveInfo GetNextWave();
    public bool HasMoreWaves();
}

public class WaveInfo {
    public int WaveNumber;
    public List<CharacterConfig> EnemyConfigs;
    public StageConfig StageConfig;
}
```

---

### **BoonLibrary**

**Purpose**: Manages between-wave upgrades (boons) for characters.

```csharp
public class BoonLibrary {
    public List<BaseBoonResolver> GetAvailableBoons(List<CharacterConfig> party);
    public void ApplyBoon(BaseBoonResolver boon, List<CharacterConfig> party);
}
```

---

## 🎯 Why These Helpers?

### **Separation of Concerns**
Each helper has a **single, clear responsibility**:
- `DamageResolver` = damage math
- `CombatantListFilter` = target queries
- Order classes = data transfer
- Position classes = spatial logic

### **Reusability**
Helpers are **stateless** and **static** (where appropriate), making them easy to call from anywhere:
```csharp
// No instance needed
List<Character> enemies = CombatantListFilter.ByScope(...);
```

### **Testability**
Pure functions with no side effects:
```csharp
[Test]
public void TestDamageCalculation() {
    DamageResolver resolver = new DamageResolver();
    DamageOrder order = new DamageOrder(attacker, victim, 100, null);
    CalculatedDamage result = resolver.ResolveOrder(order);
    
    Assert.AreEqual(50, result.DamageToHealth); // Half damage if not cracked
}
```

### **Discoverability**
Clear names indicate purpose:
- Need to filter combatants? → `CombatantListFilter`
- Need to calculate damage? → `DamageResolver`
- Need battlefield position? → `BattlefieldPosition`

---

## 📚 Summary

**Enums** provide:
- Type safety
- Self-documenting code
- IDE autocomplete
- Compiler checks

**Helpers** provide:
- Focused utilities
- Stateless operations
- Reusable logic
- Clear responsibilities

Together they keep the codebase **clean, maintainable, and extensible**!
