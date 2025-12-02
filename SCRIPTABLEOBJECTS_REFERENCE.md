# ScriptableObjects Reference

## 🎯 Overview

This system uses **Unity ScriptableObjects** as data containers for configuration. They provide a **designer-friendly** way to create and configure game content without code changes.

**Key Insight**: `CharacterConfig` is the **static data template**, while `Character` is the **runtime instance** with current state (health, buffs, etc.).

---

## 📋 ScriptableObject Types

### **CharacterConfig** ⭐ Core Asset

**Location**: `Scriptables/CharacterConfig.cs`

**Purpose**: Complete static definition of a character - their stats, abilities, art, and configuration.

**Pattern**: 
- **Static data** stored in ScriptableObject
- **Runtime state** lives in `Character` MonoBehaviour
- `Character` holds reference to its `CharacterConfig`

```csharp
public class Character : MonoBehaviour {
    public CharacterConfig Config;  // The template
    public int currentHealth;       // Runtime state
    public int currentStagger;      // Runtime state
    public List<Buff> Buffs;        // Runtime state
}
```

---

### **CharacterConfig Breakdown**

#### **🎨 Identity Section**
```csharp
[Header("Character Info")]
private string _Name;                        // Display name
private PowerType _PowerType;                // LIGHT or SHADOW
private TeamType _TeamType;                  // PLAYER, CPU, NEUTRAL
private PCAdventureClassType _PlayerClass;   // Class (for players only)
public int ScaleBounty = 1;                  // Resources granted on death
```

**Portability**:
- ✅ **Keep**: Name, PowerType concept (can rename to ElementType)
- ⚠️ **Game-Specific**: TeamType (3 teams might not fit all games), PlayerClass, ScaleBounty
- 💡 **Recommendation**: Abstract to `ICharacterIdentity` interface

---

#### **🎨 Art Section**
```csharp
[Header("Art")]
private Sprite _Portrait;              // UI portrait
private Sprite _Skin;                  // In-game sprite
private SkeletonDataAsset _SpineSkeleton;  // Spine animation asset
```

**Portability**:
- ✅ **Keep**: Portrait, skin concept
- ⚠️ **Unity-Specific**: Sprite, SkeletonDataAsset (Spine plugin)
- 💡 **Recommendation**: Abstract art references or make optional

---

#### **⚔️ Abilities Section**
```csharp
[Header("Abilities")]
private UserAbilitySelection _SpecialAttack;   // Enum of special abilities
private UserAbilitySelection _UltimateAbility; // Enum of ultimate abilities
private NativeBuffOption _NativeBuff;          // Starting buff (passive ability)
```

**Portability**:
- ✅ **Keep**: Concept of configured abilities
- ⚠️ **Game-Specific**: Three-tier ability system (Basic/Special/Ultimate), native buffs
- 💡 **Recommendation**: Use `List<string>` or `List<AbilityReference>` for flexibility

**Current Limitation**: Abilities are enums, meaning you need to modify code to add abilities. Better approach:
```csharp
public List<Effect> AvailableAbilities; // Direct ScriptableObject references
```

---

#### **📊 Starting Stats Section** ⭐ Most Portable

```csharp
[Header("Starting Stats")]
private int _BaseMitigation = 0;    // Damage reduction %
private int _BaseHP = 0;            // Hit points
private int _BaseSP = 0;            // Stagger points (shield mechanic)
private int _BaseAttackMin = 0;     // Basic attack damage range
private int _BaseAttackMax = 0;
private int _BaseSpecialMin = 0;    // Special attack damage range
private int _BaseSpecialMax = 0;
```

**Portability Analysis**:

| Stat | Universal? | Notes |
|------|-----------|-------|
| `BaseHP` | ✅ **Yes** | Health is nearly universal |
| `BaseSP` | ⚠️ **Maybe** | "Stagger" is your shield/armor mechanic (see below) |
| `BaseMitigation` | ✅ **Yes** | Damage reduction is common |
| `BaseAttackMin/Max` | ✅ **Yes** | Damage ranges are common |
| `BaseSpecialMin/Max` | ⚠️ **Maybe** | Assumes two attack types |

**About Stagger** (you're right!):
- **What it is**: A secondary health pool that must be depleted before full damage applies
- **How it works**: 
  - When stagger > 0: Take 50% damage to HP
  - When stagger = 0: Take 100% damage to HP (character is "cracked")
  - Restores between encounters
- **Alternative names**: Shield, Armor, Guard, Break Meter, Posture
- **Your note**: Should've been labeled "Shield" - agreed! It's functionally a damage shield mechanic

**Recommendation**: Rename conceptually to "Shield" or make it an optional mechanic:
```csharp
public int BaseShieldPoints = 0;  // 0 = no shield mechanic
public bool UsesShieldMechanic => BaseShieldPoints > 0;
```

---

#### **🎮 Progression Section**
```csharp
[Header("Starting Boon Values")]
[HideInInspector]
public int SupportTreeLevel = 0;    // Support skill tree rank
[HideInInspector]
public int AttackTreeLevel = 0;     // Attack skill tree rank
```

**Portability**:
- ⚠️ **Game-Specific**: Two-tree progression system
- 💡 **Better Approach**: Generic progression dictionary or skill point pool
```csharp
public Dictionary<string, int> SkillLevels = new Dictionary<string, int>();
// Or
public int TotalSkillPoints = 0;
public List<string> UnlockedSkills = new List<string>();
```

**Why `[HideInInspector]`**: These are modified at runtime by Boons, not set in editor.

---

#### **👹 Boss Section**
```csharp
[Header("Boss Settings")]
public bool IsBoss = false;
public string WaveIntroTaunt = "";
public string WaveDefeatTaunt = "";
```

**Portability**:
- ⚠️ **Game-Specific**: Dialogue/taunts
- ✅ **Keep**: `IsBoss` flag (useful for special behaviors)
- 💡 **Recommendation**: Extract dialogue to separate system

---

## 🎭 PartyConfig

**Location**: `Scriptables/PartyConfig.cs`

```csharp
[CreateAssetMenu(fileName = "PartyConfig", menuName = "GameOff2023/New Party Configuration")]
public class PartyConfig : ScriptableObject
{
    public string DeveloperDescription = "";
    public List<CharacterConfig> PartyMembers;
}
```

**Purpose**: Groups characters into a party.

**Portability**:
- ✅ **Keep**: Concept of grouping characters
- 💡 **Use Case**: Save files, preset teams, loadouts

**Simple but effective!** Just a list wrapper.

---

## 🌊 StageConfig

**Location**: `Scriptables/StageConfig.cs`

```csharp
[CreateAssetMenu(fileName = "StageConfig", menuName = "GameOff2023/New Stage+Waves Configuration")]
public class StageConfig : ScriptableObject
{
    public string StageName;
    public string BossName;
    public Sprite BossImage;
    public Sprite BackgroundImage;
    public List<CharacterConfig> Wave1;
    public List<CharacterConfig> Wave2;
    public List<CharacterConfig> Wave3;
    public List<CharacterConfig> Wave4;
    public List<CharacterConfig> Wave5;
    public List<CharacterConfig> Wave6;
    public List<CharacterConfig> Wave7;
}
```

**Purpose**: Defines a stage with multiple waves of enemies.

**Portability**:
- ⚠️ **Game-Specific**: Fixed 7 waves, wave-based progression
- ✅ **Keep**: Concept of encounter progression
- 💡 **Better Design**:
```csharp
public class StageConfig : ScriptableObject
{
    public string StageName;
    public Sprite BackgroundImage;
    public List<WaveDefinition> Waves; // Dynamic list!
}

public class WaveDefinition
{
    public List<CharacterConfig> Enemies;
    public string WaveName;
    public bool IsBossWave;
}
```

**Current Limitation**: Hard-coded to 7 waves. Designer can't easily add Wave8 without code change.

---

## 📚 EnemySetList

**Location**: `Scriptables/EnemySetList.cs`

```csharp
[CreateAssetMenu(fileName = "EnemySetList", menuName = "GameOff2023/New Enemy Set List")]
public class EnemySetList : ScriptableObject
{
    public List<StageConfig> GameStages;
}
```

**Purpose**: Top-level container for all stages in the game.

**Portability**:
- ✅ **Keep**: Concept of game progression container
- 💡 **Could be**: Campaign, Chapter, Act, etc.

**Pattern**: 
```
EnemySetList (game)
  ↓ contains
StageConfig (stage/level)
  ↓ contains
Wave1-7 (encounters)
  ↓ contains
CharacterConfig (enemies)
```

---

## 🔍 The Runtime vs Static Pattern

### **Critical Understanding**:

```
CharacterConfig (ScriptableObject)          Character (MonoBehaviour)
├─ Name: "Fire Mage"                   ┌──→ Config: [Reference to Fire Mage]
├─ BaseHP: 100                         │    currentHealth: 100 → 75 → 50 (changes!)
├─ BaseSP: 50                          │    currentStagger: 50 → 25 → 0 (changes!)
├─ BaseAttackMin: 10                   │    Buffs: [Stunned, Burning] (changes!)
├─ BaseAttackMax: 20                   │    isDead: false → true (changes!)
├─ SpecialAttack: FIREBALL             │
└─ PowerType: LIGHT                    └──→ Uses Config for base values
      ↑                                      ↑
   Static template                       Runtime state
   (never changes)                       (changes every frame)
```

**When combat starts**:
```csharp
void InitializeCharacter(Character character) {
    // Copy from config
    character.currentHealth = character.Config.BaseHP;
    character.currentStagger = character.Config.BaseSP;
    character.Buffs.Clear();
    character.isDead = false;
}
```

**During combat**:
```csharp
// Runtime state changes
character.currentHealth -= 25;
character.AddBuff(new BuffStunned(...));

// Config never changes
Debug.Log(character.Config.BaseHP); // Still 100
```

---

## 💡 Is There a Non-Mono Container?

**Question**: "This appears to be a starter file, not the class. There's gotta be a non-mono container for this, right?"

**Answer**: **Not in this codebase!** Here's the pattern:

### **Current Architecture**:
```
CharacterConfig (ScriptableObject - pure data)
    ↓ referenced by
Character (MonoBehaviour - runtime state + methods)
```

### **What You Might Have Expected**:
```
CharacterConfig (ScriptableObject - pure data)
    ↓ creates
CharacterData (pure C# class - runtime state)
    ↓ referenced by
CharacterView (MonoBehaviour - Unity presentation)
```

### **Why This Approach Was Chosen**:

**Pros**:
- ✅ Simpler - one runtime class instead of two
- ✅ Unity Inspector shows everything together
- ✅ Less indirection
- ✅ `GetComponent<Character>()` works directly

**Cons**:
- ❌ Character can't be fully unit tested (MonoBehaviour)
- ❌ Tied to GameObject lifecycle
- ❌ Harder to serialize/network

---

### **Refactoring to Pure C# Container**:

If you wanted to extract the runtime state:

```csharp
// NEW: Pure C# runtime container
public class CharacterState {
    public CharacterConfig Config;
    public int currentHealth;
    public int currentStagger;
    public List<Buff> Buffs = new List<Buff>();
    public bool isDead;
    
    public CharacterState(CharacterConfig config) {
        Config = config;
        currentHealth = config.BaseHP;
        currentStagger = config.BaseSP;
    }
    
    // All stat methods here
    public void TakeDamage(int damage) { /* ... */ }
    public bool HasBuff<T>() where T : Buff { /* ... */ }
}

// MODIFIED: MonoBehaviour becomes thin wrapper
public class Character : MonoBehaviour {
    public CharacterState State; // The data
    
    void Awake() {
        State = new CharacterState(ConfigFromScriptableObject);
    }
}
```

**Benefits**:
- ✅ `CharacterState` is pure C#, fully testable
- ✅ Can serialize/network easily
- ✅ Could run on server without Unity

**Trade-offs**:
- More complex
- Extra indirection layer
- Existing codebase would need updates

---

## 📊 Portability Assessment

### **CharacterConfig Fields**

| Field | Status | Notes |
|-------|--------|-------|
| **Name** | ✅ Universal | Every game needs names |
| **PowerType** | ✅ Portable | Rename to ElementType/DamageType |
| **TeamType** | ⚠️ Game-Specific | 3-team might not fit everywhere |
| **PlayerClass** | ⚠️ Game-Specific | Class system is optional |
| **ScaleBounty** | ❌ Game-Specific | Resource reward on death |
| **Portrait/Skin** | ✅ Universal | Every game needs visuals |
| **SpineSkeleton** | ⚠️ Tool-Specific | Spine animation plugin |
| **SpecialAttack** | ⚠️ Design-Specific | 3-tier ability system |
| **UltimateAbility** | ⚠️ Design-Specific | 3-tier ability system |
| **NativeBuff** | ⚠️ Design-Specific | Starting passive abilities |
| **BaseMitigation** | ✅ Universal | Damage reduction is common |
| **BaseHP** | ✅ Universal | Health is nearly universal |
| **BaseSP** | ✅ Portable | Shield mechanic (rename!) |
| **Attack Ranges** | ✅ Universal | Damage ranges are common |
| **Tree Levels** | ⚠️ Game-Specific | 2-tree progression |
| **Boss Settings** | ⚠️ Game-Specific | Dialogue/taunts |

### **Recommendation: Extract to Interfaces**

```csharp
// Core (keep everywhere)
public interface ICharacterStats {
    int BaseHP { get; }
    int BaseShield { get; } // Renamed from BaseSP
    int BaseMitigation { get; }
    int GetAttackDamage();
}

// Optional module
public interface IElementalCharacter {
    ElementType Element { get; }
}

// Optional module
public interface IAbilityUser {
    List<Effect> AvailableAbilities { get; }
}

// Your concrete implementation
public class CharacterConfig : ScriptableObject, 
    ICharacterStats, 
    IElementalCharacter, 
    IAbilityUser 
{
    // Implement interfaces
}
```

---

## 🎮 Design Patterns in ScriptableObjects

### **1. Data-Driven Configuration**
Designers create content without programming:
```
Assets/Configs/Characters/FireMage.asset (Inspector)
    Name: "Fire Mage"
    HP: 100
    PowerType: LIGHT
    Special: FIREBALL
```

### **2. Shared References**
Multiple Characters can reference the same config:
```csharp
Character enemy1 = Instantiate(enemyPrefab);
enemy1.Config = fireMageConfig;

Character enemy2 = Instantiate(enemyPrefab);
enemy2.Config = fireMageConfig; // Same template!
```

### **3. Composition Over Inheritance**
Instead of subclassing, compose with data:
```
FireMageConfig.asset
    SpecialAttack: FIREBALL
    PowerType: LIGHT

IceMageConfig.asset
    SpecialAttack: ICEBLAST
    PowerType: SHADOW
```

---

## 🔮 Future-Proofing Recommendations

### **1. Make Stagger/Shield Optional**
```csharp
public int BaseShieldPoints = 0; // 0 = disabled
public bool HasShieldMechanic => BaseShieldPoints > 0;
```

### **2. Flexible Ability System**
```csharp
// Instead of enums
public List<Effect> AvailableAbilities = new List<Effect>();

// Or reference by string for data-driven
public List<string> AbilityIds = new List<string>(); // "fireball", "heal", etc.
```

### **3. Modular Stat System**
```csharp
[System.Serializable]
public class StatBlock {
    public string StatName;
    public int BaseValue;
}

public List<StatBlock> Stats = new List<StatBlock>();
```

### **4. Extract to Interface**
```csharp
public interface ICharacterTemplate {
    string Name { get; }
    int GetBaseStat(string statName);
    List<string> GetAbilities();
}
```

Then `CharacterConfig` implements `ICharacterTemplate`, making it swappable with JSON/XML/Database configs.

---

## 📚 Summary

**CharacterConfig is**:
- ✅ **Excellent** for Unity-based games
- ✅ **Designer-friendly** (edit in Inspector)
- ⚠️ **Mixed portability** (some game-specific, some universal)
- ✅ **Solid foundation** for stat-based combat

**Stagger/Shield**:
- Your instinct is right - it's a shield mechanic
- Functionally: secondary health pool
- Should be optional/configurable for other games

**No Pure C# Container**:
- Runtime state lives in `Character` MonoBehaviour
- Could refactor to `CharacterState` (pure C#) + `CharacterView` (MonoBehaviour)
- Current approach is simpler, works well for this game

**For Reusability**:
- Keep: HP, mitigation, damage ranges, shield concept
- Make optional: Team system, class system, progression trees
- Abstract: Ability references (use list instead of enums)
- Extract: Boss-specific features

This is a **pragmatic, designer-friendly configuration system** that works beautifully for your game! 🎯
