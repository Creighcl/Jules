using System;
using System.Collections.Generic;
using System.Linq;

public class Character
{
    // Configuration
    public ICharacterConfig Config { get; private set; }

    // Dependencies
    private IRandomService _randomService;

    // State
    public Dictionary<IResourceType, Resource> Resources = new Dictionary<IResourceType, Resource>();
    public List<Buff> Buffs = new List<Buff>();

    // Properties (Backwards Compatibility / Helpers)
    public int currentHealth
    {
        get => _legacyHealth;
        set => _legacyHealth = value;
    }

    public int currentStagger
    {
        get => _legacyStagger;
        set => _legacyStagger = value;
    }

    public bool isDead { get; set; } = false;
    public bool IsCurrentCombatant { get; set; } = false;
    public ICombatPosition PositionInfo { get; private set; }
    public int GenericWaveCounter = 0;

    // Reference to the View (Unity Object) held as a pure object to maintain independence
    public object ViewRef;

    // Events (Observers can subscribe to these)
    public event Action<Buff> OnBuffAdded;
    public event Action<Buff> OnBuffRemoved;
    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<int> OnStaggerChanged;

    public Character(ICharacterConfig config, IRandomService randomService = null)
    {
        Config = config;
        _randomService = randomService ?? new SystemRandomService(); // Default to system random if not provided (e.g. tests)

        InitializeResources();
    }

    private void InitializeResources()
    {
        // TODO: ResourceType is currently a ScriptableObject.
        // Future refactor needed to abstract ResourceType (e.g. IResourceType)
        // to populate the Resources dictionary purely.
    }

    public void SetPositionInfo(ICombatPosition pos) {
        PositionInfo = pos;
    }

    // --- Resource Management ---

    public Resource GetResource(IResourceType type) {
        if (Resources.ContainsKey(type)) {
            return Resources[type];
        }
        return null;
    }

    public void SetResource(IResourceType type, int value) {
        if (!Resources.ContainsKey(type)) {
            Resources[type] = new Resource(type, type.DefaultMax);
        }
        Resources[type].CurrentValue = value;
    }

    // Helper for int properties (Health/Stagger)
    private int _legacyHealth = 1;
    private int _legacyStagger = 0;

    // --- Buff Management ---

    public Buff GetBuff<T>() where T : Buff {
        return Buffs.FirstOrDefault(buff => buff is T);
    }

    public bool HasBuff<T>() where T : Buff {
        return Buffs.Any(buff => buff is T);
    }

    public Buff RemoveBuff<T>() where T : Buff {
        Buff buffToRemove = Buffs.FirstOrDefault(buff => buff is T);
        Buffs.RemoveAll(buff => buff is T);
        if (buffToRemove != null) OnBuffRemoved?.Invoke(buffToRemove);
        return buffToRemove;
    }

    public void AddBuff(Buff newBuff) {
        Type newBuffType = newBuff.GetType();
        var existingBuff = Buffs.FirstOrDefault(buff => buff.GetType() == newBuffType);
        if (existingBuff != null)
        {
            Buffs.Remove(existingBuff);
        }
        Buffs.Add(newBuff);
        OnBuffAdded?.Invoke(newBuff);
    }

    public void RemoveAllBuffs() {
        Buffs.Clear();
    }

    public List<Buff> AgeBuffsForPhase(CombatPhase phase) {
        var buffsToAge = Buffs.Where(buff => buff.AgingPhase == phase);
        foreach (var buff in buffsToAge)
        {
            buff.Tick();
        }
        return RemoveAgedBuffs();
    }

    public void RemoveRandomDebuff() {
        if (Buffs.Count == 0) return;

        Buff randomDebuff = Buffs.Where(buff => buff.isDebuff).FirstOrDefault();

        if (randomDebuff != null) {
            Buffs.Remove(randomDebuff);
            OnBuffRemoved?.Invoke(randomDebuff);
        }
    }

    List<Buff> RemoveAgedBuffs() {
        if (Buffs.Count == 0) return new List<Buff>();

        var agedBuffs = Buffs.Where(buff => buff.TurnsRemaining < 1).ToList();
        Buffs.RemoveAll(buff => buff.TurnsRemaining < 1);

        // Todo: Invoke events for aged buffs?

        return agedBuffs;
    }

    // --- Actions / Logic ---

    public void FirstTimeInitialization() {
        isDead = false;

        // Use backing fields for now to avoid ResourceType dependency in pure context immediately
        _legacyHealth = Config.BaseHP;
        _legacyStagger = Config.BaseSP;

        // Initialize Resources if we had the types...
        // For now, the behavior relies on properties `currentHealth`
        // which I will modify to use these fields for simplicity in this step.

        if (Config.NativeBuff == NativeBuffOption.VOLCANICBOWEL) {
            AddBuff(new BuffVolcanicBowelSyndrome(this, this, 999));
        }
        if (Config.NativeBuff == NativeBuffOption.PYROPEAKABOO) {
            AddBuff(new BuffPyroPeakboo(this, this, 999));
        }
    }

    public void TurnStart() {
        IsCurrentCombatant = true;
    }

    public void TurnEnd() {
        IsCurrentCombatant = false;
    }

    // --- Stats & Rolls ---

    int GetCriticalRollChance() {
        int CRIT_CHANCE = 5;
        // TODO: Re-implement AttackTreeLevel logic using local state instead of mutating Config
        return CRIT_CHANCE;
    }

    float GetCriticalHitModifier() {
        return 1.25f;
    }

    int GetHitChance(bool isHeal) {
        int hitChance = 95;
        if (isHeal) hitChance = 100;
        if (HasBuff<BuffBlinded>()) hitChance -= 30;
        return hitChance;
    }

    protected bool TryChance(int percentChance) {
        return _randomService.TryChance(percentChance);
    }

    public int GetBasicAttackRoll() {
        bool HIT_SUCCESSFUL = TryChance(GetHitChance(false));

        if (!HIT_SUCCESSFUL) return 0;
        if (HasBuff<BuffPolymorph>()) return 1;

        bool DidCrit = TryChance(GetCriticalRollChance());
        int damage = _randomService.Range(Config.BaseAttackMin, Config.BaseAttackMax);

        if (DidCrit) {
            damage = (int) (damage * GetCriticalHitModifier());
        }

        return damage;
    }

    public int GetSpecialAttackRoll(bool isAHealRoll) {
        bool HIT_SUCCESSFUL = TryChance(GetHitChance(isAHealRoll));

        if (!HIT_SUCCESSFUL) return 0;
        if (HasBuff<BuffPolymorph>()) return 1;

        bool DidCrit = TryChance(GetCriticalRollChance());
        int damage = _randomService.Range(Config.BaseSpecialMin, Config.BaseSpecialMax);

        if (DidCrit) {
            damage = (int) (damage * GetCriticalHitModifier());
        }

        return damage;
    }

    // --- Damage Taking ---

    public void TakeDamage(int Damage) {
        bool startedDead = _legacyHealth == 0;
        int DamageToHealth = Damage;

        // Shield logic
        if (HasBuff<BuffShield>()) {
            var shieldBuff = (BuffShield)Buffs.First(buff => buff is BuffShield);
            int ShieldCharges = shieldBuff.Charges;

            if (ShieldCharges > Damage) {
                shieldBuff.Charges -= Damage;
                DamageToHealth = 0;
            } else {
                DamageToHealth -= ShieldCharges;
                Buffs.RemoveAll(buff => buff is BuffShield);
            }
        }

        _legacyHealth = Math.Clamp(
            _legacyHealth - DamageToHealth,
            0,
            Config.BaseHP
        );

        OnHealthChanged?.Invoke(_legacyHealth);

        if (_legacyHealth == 0 && !startedDead) {
            _legacyHealth = 0;
            Die();
        }
    }

    public void TakeStagger(int Damage) {
        _legacyStagger -= Damage;
        if (_legacyStagger < 0) {
            _legacyStagger = 0;
        }
        OnStaggerChanged?.Invoke(_legacyStagger);
    }

    public void RestoreStagger() {
        _legacyStagger = Config.BaseSP;
        OnStaggerChanged?.Invoke(_legacyStagger);
    }

    void Die() {
        isDead = true;
        OnDeath?.Invoke();
    }

    // --- Ability Availability ---

    public List<AbilityCategory> GetAvailableAbilities(int LightPoints, int ShadowPoints) {
        var availableAbilities = new List<AbilityCategory>(){
            AbilityCategory.BASICATTACK,
        };

        if (HasBuff<BuffStunned>() || HasBuff<BuffSearingStun>()) {
            return new List<AbilityCategory>();
        }

        if (HasBuff<BuffCharmed>() || HasBuff<BuffSilenced>() || HasBuff<BuffTaunted>()) {
            return new List<AbilityCategory>(){
                AbilityCategory.BASICATTACK,
            };
        }

        if (Config.SpecialAttack != UserAbilitySelection.NONE) {
            availableAbilities.Add(AbilityCategory.SPECIALATTACK);
        };
        if (Config.UltimateAbility != UserAbilitySelection.NONE && LightPoints > 1 && ShadowPoints > 1) {
            availableAbilities.Add(AbilityCategory.ULTIMATE);
        };

        return availableAbilities;
    }
}
