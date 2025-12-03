using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// Renamed from Character to CharacterBehavior to clearly indicate it's a wrapper/view
public class CharacterBehavior : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField]
    public CharacterConfig Config;

    // --- The Pure Model ---
    public Character Model { get; private set; }

    // --- Wrapper Properties ---
    public int currentHealth
    {
        get => Model != null ? Model.currentHealth : 1;
        set { if (Model != null) Model.currentHealth = value; }
    }
    public int currentStagger
    {
        get => Model != null ? Model.currentStagger : 0;
        set { if (Model != null) Model.currentStagger = value; }
    }
    public bool isDead => Model != null && Model.isDead;
    public bool IsCurrentCombatant
    {
        get => Model != null && Model.IsCurrentCombatant;
        set { if (Model != null) Model.IsCurrentCombatant = value; }
    }
    public List<Buff> Buffs => Model != null ? Model.Buffs : new List<Buff>();
    public int GenericWaveCounter
    {
        get => Model != null ? Model.GenericWaveCounter : 0;
        set { if (Model != null) Model.GenericWaveCounter = value; }
    }

    public ICombatPosition PositionInfo => Model?.PositionInfo;

    // Visuals Only
    public bool IsHighlighted = false;
    public Sprite AlternativePortrait;

    void Awake()
    {
        if (Config != null)
        {
            InitializeModel(Config);
        }
    }

    public void InitializeModel(CharacterConfig config)
    {
        Config = config;
        Model = new Character(config, new UnityRandomService());
        Model.ViewRef = this;

        // Subscribe to events to update view
        // Model.OnHealthChanged += UpdateHealthBar; // Example

        // For backwards compatibility, some systems might set PositionInfo directly on the MonoBehaviour
        // We need to ensure the Model gets it.
    }

    public void SetPositionInfo(ICombatPosition pos) {
        if (Model != null) Model.SetPositionInfo(pos);
    }

    // --- Forwarded Methods ---

    public Resource GetResource(ResourceType type) => Model?.GetResource(type);
    public void SetResource(ResourceType type, int value) => Model?.SetResource(type, value);
    public Buff GetBuff<T>() where T : Buff => Model?.GetBuff<T>();
    public bool HasBuff<T>() where T : Buff => Model != null && Model.HasBuff<T>();
    public Buff RemoveBuff<T>() where T : Buff => Model?.RemoveBuff<T>();
    public List<AbilityCategory> GetAvailableAbilities(int LightPoints, int ShadowPoints) => Model?.GetAvailableAbilities(LightPoints, ShadowPoints);
    public void AddBuff(Buff newBuff) => Model?.AddBuff(newBuff);
    public List<Buff> AgeBuffsForPhase(CombatPhase phase) => Model?.AgeBuffsForPhase(phase);
    public void RemoveRandomDebuff() => Model?.RemoveRandomDebuff();
    public void RemoveAllBuffs() => Model?.RemoveAllBuffs();
    public void RestoreStagger() => Model?.RestoreStagger();

    public void FirstTimeInitialization()
    {
        if (Model == null && Config != null) InitializeModel(Config);
        Model?.FirstTimeInitialization();
    }

    public void TurnStart() => Model?.TurnStart();
    public void TurnEnd() => Model?.TurnEnd();
    public int GetBasicAttackRoll() => Model != null ? Model.GetBasicAttackRoll() : 0;
    public int GetSpecialAttackRoll(bool isAHealRoll) => Model != null ? Model.GetSpecialAttackRoll(isAHealRoll) : 0;
    public void TakeDamage(int Damage) => Model?.TakeDamage(Damage);
    public void TakeStagger(int Damage) => Model?.TakeStagger(Damage);

    // Dev Tools
    [ContextMenu("tell me your buffs")]
    void tellmeyourbuffs() {
        if (Model == null) return;
        foreach(var buff in Model.Buffs) {
            Debug.Log(buff.Name + " " + buff.Charges);
        }
    }
    [ContextMenu("check state")]
    void CheckState() {
        // Debug.Log(Config.AttackTreeLevel + " ATTACK");
        // Debug.Log(Config.SupportTreeLevel + " SUPPORT");
    }
}

// Wrapper for Unity Random to inject into Character
public class UnityRandomService : IRandomService
{
    public int Range(int min, int max) => UnityEngine.Random.Range(min, max);
    public float Range(float min, float max) => UnityEngine.Random.Range(min, max);
    public bool TryChance(int percentChance) => UnityEngine.Random.Range(0, 100) < percentChance;
}
