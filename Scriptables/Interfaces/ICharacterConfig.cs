public interface ICharacterConfig
{
    string Name { get; }
    ElementType PowerType { get; }
    TeamType TeamType { get; }
    PCAdventureClassType PlayerClass { get; }
    int ScaleBounty { get; }

    UserAbilitySelection SpecialAttack { get; }
    UserAbilitySelection UltimateAbility { get; }
    NativeBuffOption NativeBuff { get; }

    int BaseMitigation { get; }
    int BaseHP { get; }
    int BaseSP { get; }
    int BaseAttackMin { get; }
    int BaseAttackMax { get; }
    int BaseSpecialMin { get; }
    int BaseSpecialMax { get; }

    // Logic Helpers
    bool IsBoss { get; }
    string WaveIntroTaunt { get; }
    string WaveDefeatTaunt { get; }
}
