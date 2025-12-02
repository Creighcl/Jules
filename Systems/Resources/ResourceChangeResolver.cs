using System;
using System.Collections.Generic;

public class ResourceChangeResolver {

    private ElementInteractionRule InteractionRule = new ElementInteractionRule();
    private Dictionary<string, IResourceLogic> LogicRegistry = new Dictionary<string, IResourceLogic>();

    public ResourceChangeResolver() {
        LogicRegistry["Health"] = new HealthResourceLogic();
        LogicRegistry["Stagger"] = new StandardResourceLogic();
    }

    public ResourceChangeResult Resolve(ResourceChangeOrder order) {
        Resource resource = order.Target.GetResource(order.Resource);

        if (resource == null) {
            order.Target.SetResource(order.Resource, order.Resource.DefaultMax);
            resource = order.Target.GetResource(order.Resource);
        }

        // --- PIPELINE EXECUTION ---

        // 1. Initial Amount
        int currentAmount = order.Amount;

        // 2. Outgoing Modifications (Attacker's Buffs/Traits)
        if (order.Source != null) {
            foreach(var buff in order.Source.Buffs) {
                currentAmount = buff.ModifyOutgoingResourceAmount(order, currentAmount);
            }
        }

        // 3. Incoming Modifications (Defender's Buffs/Traits)
        if (order.Target != null) {
            foreach(var buff in order.Target.Buffs) {
                currentAmount = buff.ModifyIncomingResourceAmount(order, currentAmount);
            }
        }

        // 4. Resource-Specific Logic (Application)
        IResourceLogic logic = new StandardResourceLogic();
        if (LogicRegistry.ContainsKey(order.Resource.Name)) {
            logic = LogicRegistry[order.Resource.Name];
        }

        // Create a modified order for the final step (preserving original context)
        // We might want to pass 'currentAmount' separately to Logic, but for now we clone the order or just trust the logic to use the amount.
        // To be clean, let's create a transient order or assume logic uses 'currentAmount' if we passed it.
        // But IResourceLogic interface takes 'ResourceChangeOrder'. Let's act as if we modified the order's intent effectively.
        // A cleaner way is to update the order's Amount, OR pass it.
        // Let's make a copy to avoid mutating the original intent record if we care about "Original vs Final" in the order object itself.
        // Actually, ResourceChangeResult tracks "OriginalAmount" and "FinalAmount".

        // We'll mutate a copy/proxy or just modify the amount passed to logic.
        // Simpler: Just create a result here passing the modified amount to logic?
        // Logic.Resolve typically DOES the math.
        // Let's assume Logic.Resolve takes the order.Amount. So we need to update it.

        ResourceChangeOrder processedOrder = new ResourceChangeOrder(
            order.Source,
            order.Target,
            order.Resource,
            currentAmount, // The modified amount
            order.SourceEffect
        );

        ResourceChangeResult result = logic.Resolve(processedOrder, resource);
        result.OriginalAmount = order.Amount; // Restore original intent for logging/events

        return result;
    }

    // Legacy Support
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

    int GetUnmitigatedDamageFromRaw(int rawDamage, Character target, ElementType effectPowerType) {
        // mitigation is zero if rawDamage is negative, this is a heal
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

    int GetMitigationPowerForPowerType(Character victim, ElementType element) {
        bool IsResistant =  IsVictimResistantToPowerType(victim, element);
        int MitigationPower = IsResistant ? 10 : 0;
        return MitigationPower;
    }

    int GetFullVictimMitigationPower(Character victim, ElementType element) {
        if (victim == null || victim.Config == null) return 0; //WHY WOULD THIS HAPPEN???
        return victim.Config.BaseMitigation + GetMitigationPowerForPowerType(victim, element);
    }

    bool IsVictimResistantToPowerType(Character victim, ElementType element) {
        if (victim.HasBuff<BuffSkeletalShield>()) {
            return true;
        }

        if (victim.HasBuff<BuffElementalWeakness>()) {
            return false;
        }

        return InteractionRule.IsResistant(element, victim.Config.PowerType);
    }

    ElementType GetPowerTypeOfCharacter(Character character) {
        return character.Config.PowerType;
    }
}
