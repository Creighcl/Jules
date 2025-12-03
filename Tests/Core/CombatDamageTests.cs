using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class CombatDamageTests
{
    private class TestElementType : IElementType {
        public string Name;
    }

    private class TestElementInteractionRule : IElementInteractionRule
    {
        public bool IsResistant(IElementType attacker, IElementType defender)
        {
            // For this test, we assume no resistance to keep math simple (10 dmg -> 10 taken)
            // unless we specifically want to test resistance.
            return false;
        }
    }

    private class TestCharacterConfig : ICharacterConfig
    {
        public string Name { get; set; } = "Test Char";
        public IElementType PowerType { get; set; }
        public TeamType TeamType { get; set; } = TeamType.PLAYER;
        public PCAdventureClassType PlayerClass { get; set; } = PCAdventureClassType.WARRIOR;
        public int ScaleBounty { get; set; } = 1;

        public UserAbilitySelection SpecialAttack { get; set; } = UserAbilitySelection.NONE;
        public UserAbilitySelection UltimateAbility { get; set; } = UserAbilitySelection.NONE;
        public NativeBuffOption NativeBuff { get; set; } = NativeBuffOption.NONE;

        public int BaseMitigation { get; set; } = 0;
        public int BaseHP { get; set; } = 100;
        public int BaseSP { get; set; } = 10;
        public int BaseAttackMin { get; set; } = 10;
        public int BaseAttackMax { get; set; } = 10;
        public int BaseSpecialMin { get; set; } = 10;
        public int BaseSpecialMax { get; set; } = 10;

        public bool IsBoss { get; set; } = false;
        public string WaveIntroTaunt { get; set; } = "";
        public string WaveDefeatTaunt { get; set; } = "";
    }

    private class MockPositioningSystem : IPositioningSystem
    {
        public int GetDistance(ICombatPosition a, ICombatPosition b) => 1;
        public List<Character> GetNeighbors(Character center, List<Character> candidates) => new List<Character>();
        public List<Character> GetNearbyAllies(Character center, List<Character> candidates) => new List<Character>();
        public ICombatPosition GetPositionForCharacter(Character character) => null;
    }

    private class MockBattlefieldProvider : IBattlefieldPositionProvider
    {
        public ICombatPosition GetNextOpenBattlefieldPositionForTeam(List<int> takenSpotIds, TeamType team) => null;
        public Character InstantiateNewCharacterForConfig(ICharacterConfig config) => null;
        public ICharacterConfig GetConfigForUnitType(SummonableUnit unitType) => null;
    }

    [Test]
    public void BasicAttack_DealsExpectedDamage_Integration()
    {
        // Setup Types (POCOs)
        var physicalType = new TestElementType { Name = "Physical" };
        var rule = new TestElementInteractionRule();

        // Arrange
        var statsA = new TestCharacterConfig { Name = "Attacker", BaseAttackMin = 10, BaseAttackMax = 10, PowerType = physicalType };
        var statsB = new TestCharacterConfig { Name = "Victim", BaseHP = 100, BaseMitigation = 0, PowerType = physicalType };

        var charA = new Character(statsA);
        var charB = new Character(statsB);
        charA.FirstTimeInitialization();
        charB.FirstTimeInitialization();

        // Inject the rule into CombatState
        var combatState = new CombatState(
            new EventProvider(),
            new MockBattlefieldProvider(),
            new MockPositioningSystem(),
            rule
        );

        combatState.FullCombatantList.Add(charA);
        combatState.FullCombatantList.Add(charB);
        combatState.CurrentCombatant = charA;

        var plan = new EffectPlan(null);
        var damageOrder = new DamageOrder(charA, charB, 10, null);
        plan.Add(damageOrder);

        // Act
        combatState.ExecuteEffectPlan(plan);

        // Assert
        Assert.AreEqual(90, charB.currentHealth);
    }
}
