using System;
using System.Collections.Generic;

public class ResourceChangeResolver {

    // Now allows injection of a rule logic, default can be SO-based or pure
    private IElementInteractionRule InteractionRule;
    private Dictionary<string, IResourceLogic> LogicRegistry = new Dictionary<string, IResourceLogic>();

    public ResourceChangeResolver(IElementInteractionRule rule = null) {
        // Fallback to default SO if not provided, assuming we are in Unity runtime.
        // If testing in pure C#, the test MUST provide the rule.
        if (rule == null) {
            // Legacy behavior: Instantiate the SO. This assumes Unity context.
            // Pure tests will fail if they hit this line.
            InteractionRule = new ElementInteractionRule();
        } else {
            InteractionRule = rule;
        }

        LogicRegistry["Health"] = new HealthResourceLogic();
        LogicRegistry["Stagger"] = new StandardResourceLogic();
    }

    public ResourceChangeResult Resolve(ResourceChangeOrder order) {
        Resource resource = order.Target.GetResource(order.Resource);

        if (resource == null) {
            order.Target.SetResource(order.Resource, order.Resource.DefaultMax);
            resource = order.Target.GetResource(order.Resource);
        }

        int currentAmount = order.Amount;

        if (order.Source != null) {
            foreach(var buff in order.Source.Buffs) {
                currentAmount = buff.ModifyOutgoingResourceAmount(order, currentAmount);
            }
        }

        if (order.Target != null) {
            foreach(var buff in order.Target.Buffs) {
                currentAmount = buff.ModifyIncomingResourceAmount(order, currentAmount);
            }
        }

        IResourceLogic logic = new StandardResourceLogic();
        if (LogicRegistry.ContainsKey(order.Resource.Name)) {
            logic = LogicRegistry[order.Resource.Name];
        }

        ResourceChangeOrder processedOrder = new ResourceChangeOrder(
            order.Source,
            order.Target,
            order.Resource,
            currentAmount,
            order.SourceEffect
        );

        ResourceChangeResult result = logic.Resolve(processedOrder, resource);
        result.OriginalAmount = order.Amount;

        return result;
    }

    public CalculatedDamage ResolveOrder(DamageOrder order) {
        CalculatedDamage damage = CalculateDamageResult(order);
        damage.Target.TakeDamage(damage.DamageToHealth);
        damage.Target.TakeStagger(damage.DamageToStagger);
        return damage;
    }

    CalculatedDamage CalculateDamageResult(
        DamageOrder order
    ) {
        Character attacker = order.Attacker;
        Character victim = order.Victim;
        int rawDamage = order.RawDamage;

        int attackerRawDamage = rawDamage;

        if (attacker.HasBuff<BuffWeakness>()) {
            attackerRawDamage = (int) (attackerRawDamage * 0.5f);
        }

        if (attacker.HasBuff<BuffStrengthen>()) {
            attackerRawDamage *= 2;
        }

        int unmitigatedDamage = GetUnmitigatedDamageFromRaw(
            attackerRawDamage,
            victim,
            GetPowerTypeOfCharacter(attacker)
        );

        bool IsAHeal = attackerRawDamage < 0;

        bool IsVulnerableToAttack = victim.HasBuff<BuffElementalVulnerability>() && !IsVictimResistantToPowerType(victim, GetPowerTypeOfCharacter(attacker));

        if (!IsAHeal && IsVulnerableToAttack) {
            unmitigatedDamage = (int) (unmitigatedDamage * 1.25f);
        }

        if (victim.Config.BaseSP == 0) {
            return new CalculatedDamage(
                attacker,
                victim,
                unmitigatedDamage,
                0,
                attackerRawDamage,
                false,
                order.Source
            );
        }

        bool StaggerNotInvolved = IsAHeal || IsVictimResistantToPowerType(victim, GetPowerTypeOfCharacter(attacker));

        int DamageDealtToStagger = StaggerNotInvolved ? 0 : attackerRawDamage;

        bool CharacterBeganCracked = victim.currentStagger == 0;
        bool CharacterIsCracked = victim.currentStagger <= DamageDealtToStagger;
        bool CharacterCrackedThisTurn = !CharacterBeganCracked && CharacterIsCracked;


        int FinalDamageToHealth = unmitigatedDamage;
        bool FinalDamageShouldBeHalved = !CharacterIsCracked && !IsAHeal;
        if (FinalDamageShouldBeHalved) {
            FinalDamageToHealth = (int) (unmitigatedDamage / 2f);
        }


        CalculatedDamage result = new CalculatedDamage(
            attacker,
            victim,
            FinalDamageToHealth,
            DamageDealtToStagger,
            attackerRawDamage,
            CharacterCrackedThisTurn,
            order.Source
        );

        return result;
    }

    int GetUnmitigatedDamageFromRaw(int rawDamage, Character target, IElementType effectPowerType) {
        if (rawDamage < 0) {
            return rawDamage;
        }

        int mitigationPower = GetFullVictimMitigationPower(target, effectPowerType);

        int mitigatedDamage = (int) (rawDamage * (mitigationPower / 100f));

        int unmitigatedDamage = Math.Clamp(
            rawDamage - mitigatedDamage,
            0,
            rawDamage
        );

        return unmitigatedDamage;
    }

    int GetMitigationPowerForPowerType(Character victim, IElementType element) {
        bool IsResistant =  IsVictimResistantToPowerType(victim, element);
        int MitigationPower = IsResistant ? 10 : 0;
        return MitigationPower;
    }

    int GetFullVictimMitigationPower(Character victim, IElementType element) {
        if (victim == null || victim.Config == null) return 0;
        return victim.Config.BaseMitigation + GetMitigationPowerForPowerType(victim, element);
    }

    bool IsVictimResistantToPowerType(Character victim, IElementType element) {
        if (victim.HasBuff<BuffSkeletalShield>()) {
            return true;
        }

        if (victim.HasBuff<BuffElementalWeakness>()) {
            return false;
        }

        return InteractionRule.IsResistant(element, victim.Config.PowerType);
    }

    IElementType GetPowerTypeOfCharacter(Character character) {
        return character.Config.PowerType;
    }
}
